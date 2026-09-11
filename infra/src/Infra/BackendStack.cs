using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.Scheduler;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Constructs;

namespace Infra
{
    /// <summary>
    /// Backend infrastructure for StepGo (design.md decisions 3-8): DynamoDB single table, dual Cognito
    /// user pools, API Gateway HTTP API with a Cognito JWT authorizer, the async plumbing (SQS+DLQ,
    /// EventBridge custom bus + Scheduler, Step Functions), a private S3 bucket, and Secrets Manager
    /// placeholders for the payment-gateway and LINE credentials. Shares one CDK App with the frontend's
    /// (not-yet-applied) stacks — this stack only ever adds StepGo backend resources.
    ///
    /// The HTTP API is wired with L1 (Cfn*) constructs: the L2 aws-apigatewayv2-integrations/authorizers
    /// modules ship as separate "alpha" packages upstream and aren't published for .NET on this CDK
    /// version, so HttpApi/HttpLambdaIntegration/HttpJwtAuthorizer aren't available here.
    /// </summary>
    public class BackendStack : Stack
    {
        private const string BackendPublishRoot = "../backend/src";

        private sealed record RouteSpec(string Method, string Path, bool RequiresAuth);

        // Every route below mirrors the MiniRouter mappings coded in the StepGo.Api */Routes.cs files
        // exactly — this table IS the API Gateway <-> Lambda contract, so a new endpoint must be added in
        // both places. All routes are proxied to the single StepGo.Api Lambda (see CreateApiFunction).
        private static readonly RouteSpec[] Routes =
        [
            new("GET", "/identity/health", false),
            new("POST", "/users", false),
            new("POST", "/teachers/{teacherId}/verification", true),
            new("POST", "/teachers/{teacherId}/verification/review", true),
            new("GET", "/teachers/{teacherId}/verification", true),

            new("GET", "/courses/health", false),
            new("POST", "/courses", true),
            new("POST", "/courses/{courseId}/publish", true),
            new("GET", "/courses/{courseId}", false),
            new("GET", "/teachers/{teacherId}/courses", false),

            new("GET", "/orders/health", false),
            new("POST", "/orders", true),
            new("GET", "/orders/{orderId}", true),
            new("GET", "/students/{studentId}/orders", true),
            new("POST", "/webhooks/payment-notifications", false),

            new("GET", "/payouts/health", false),
            new("GET", "/payout-batches/{payoutBatchId}", true),
            new("GET", "/teachers/{teacherId}/payout-batches", true),
            new("POST", "/payout-batches/{payoutBatchId}/mark-paid", true),
            new("POST", "/payout-batches/{payoutBatchId}/mark-bank-rejected", true),

            new("GET", "/refund-tickets/health", false),
            new("POST", "/refund-tickets", true),
            new("GET", "/refund-tickets/{ticketId}", true),
            new("POST", "/refund-tickets/{ticketId}/teacher-decision", true),
            new("POST", "/refund-tickets/{ticketId}/escalate", true),
            new("POST", "/refund-tickets/{ticketId}/admin-arbitration", true),
            new("POST", "/refund-tickets/{ticketId}/thread", true),
            new("POST", "/refund-tickets/{ticketId}/internal-notes", true),

            new("GET", "/governance/health", false),
            new("POST", "/governance/fee-settings", true),
            new("GET", "/governance/change-log/{category}", true),
            new("POST", "/governance/terms", true),
            new("POST", "/teachers/{teacherId}/terms-consent", true),

            new("GET", "/notifications/health", false),
            new("POST", "/notification-templates/{key}", true),
            new("GET", "/users/{userId}/notifications", true),
            new("POST", "/account/notification-settings", true),
        ];

        public Table Table { get; }
        public UserPool TeacherStudentUserPool { get; }
        public UserPool AdminUserPool { get; }
        public CfnApi HttpApi { get; }
        public Queue PaymentNotificationQueue { get; }
        public EventBus EventBus { get; }
        public StateMachine RefundSlaStateMachine { get; }
        public Bucket TeacherIdPhotosBucket { get; }

        internal BackendStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            Table = CreateTable();
            (TeacherStudentUserPool, AdminUserPool, var teacherStudentAppClient) = CreateCognitoUserPools();
            (PaymentNotificationQueue, var notificationDlq) = CreatePaymentNotificationQueue();
            EventBus = new EventBus(this, "StepGoEventBus", new EventBusProps { EventBusName = "stepgo-events" });
            TeacherIdPhotosBucket = CreateTeacherIdPhotosBucket();
            var secrets = CreateSecrets();

            var sharedEnvironment = new Dictionary<string, string>
            {
                ["STEPGO_TABLE_NAME"] = Table.TableName,
                ["STEPGO_ID_PHOTOS_BUCKET"] = TeacherIdPhotosBucket.BucketName,
                ["STEPGO_EVENT_BUS_NAME"] = EventBus.EventBusName,
                ["STEPGO_PAYMENT_NOTIFICATION_QUEUE_URL"] = PaymentNotificationQueue.QueueUrl,
                ["STEPGO_PLATFORM_BANK_CODE"] = "807",
            };

            var apiFunction = CreateApiFunction(sharedEnvironment);
            HttpApi = CreateHttpApi(apiFunction, teacherStudentAppClient);

            var paymentConsumer = CreateWorkerFunction("PaymentNotificationConsumer", "payment-notification-consumer", sharedEnvironment);
            paymentConsumer.AddEventSource(new Amazon.CDK.AWS.Lambda.EventSources.SqsEventSource(PaymentNotificationQueue));

            var overdueScan = CreateWorkerFunction("OverdueOrderScan", "overdue-order-scan", sharedEnvironment);
            var payoutScheduler = CreateWorkerFunction("PayoutBatchScheduler", "payout-batch-scheduler", sharedEnvironment);
            var notificationDispatcher = CreateWorkerFunction("NotificationDispatcher", "notification-dispatcher", sharedEnvironment);
            var refundSlaCheck = CreateWorkerFunction("RefundSlaCheck", "refund-sla-check", sharedEnvironment);

            Table.GrantReadWriteData(paymentConsumer);
            Table.GrantReadWriteData(overdueScan);
            Table.GrantReadWriteData(payoutScheduler);
            Table.GrantReadWriteData(notificationDispatcher);
            Table.GrantReadWriteData(refundSlaCheck);
            EventBus.GrantPutEventsTo(paymentConsumer);
            EventBus.GrantPutEventsTo(overdueScan);
            secrets.EcpaySecret.GrantRead(apiFunction);

            CreateScheduledRules(overdueScan, payoutScheduler);
            CreateNotificationEventRule(notificationDispatcher);
            RefundSlaStateMachine = CreateRefundSlaStateMachine(refundSlaCheck);
            CreateDlqAlarm(notificationDlq);

            apiFunction.AddEnvironment("STEPGO_REFUND_SLA_STATE_MACHINE_ARN", RefundSlaStateMachine.StateMachineArn);
            RefundSlaStateMachine.GrantStartExecution(apiFunction);

            new CfnOutput(this, "HttpApiEndpoint", new CfnOutputProps { Value = HttpApi.AttrApiEndpoint });
        }

        /// <summary>Single-table design with GSI1-4, per design.md decision 4.</summary>
        private Table CreateTable()
        {
            var table = new Table(this, "StepGoTable", new TableProps
            {
                TableName = "StepGoTable",
                PartitionKey = new Attribute { Name = "PK", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "SK", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                Stream = Amazon.CDK.AWS.DynamoDB.StreamViewType.NEW_AND_OLD_IMAGES,
                RemovalPolicy = RemovalPolicy.RETAIN,
                PointInTimeRecoverySpecification = new PointInTimeRecoverySpecification { PointInTimeRecoveryEnabled = true },
            });

            foreach (var indexName in new[] { "GSI1", "GSI2", "GSI3", "GSI4" })
            {
                table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
                {
                    IndexName = indexName,
                    PartitionKey = new Attribute { Name = $"{indexName}PK", Type = AttributeType.STRING },
                    SortKey = new Attribute { Name = $"{indexName}SK", Type = AttributeType.STRING },
                });
            }

            return table;
        }

        /// <summary>Teacher/Student pool (phone required, email optional) + a separate, MFA-required Admin pool (design.md decision 3).</summary>
        private (UserPool teacherStudent, UserPool admin, UserPoolClient teacherStudentAppClient) CreateCognitoUserPools()
        {
            var teacherStudentPool = new UserPool(this, "TeacherStudentUserPool", new UserPoolProps
            {
                UserPoolName = "stepgo-teacher-student",
                SelfSignUpEnabled = true,
                SignInAliases = new SignInAliases { Phone = true, Email = true },
                StandardAttributes = new StandardAttributes
                {
                    PhoneNumber = new StandardAttribute { Required = true, Mutable = true },
                    Email = new StandardAttribute { Required = false, Mutable = true },
                },
                CustomAttributes = new Dictionary<string, ICustomAttribute>
                {
                    ["role"] = new StringAttribute(new StringAttributeProps { Mutable = false }),
                },
                AccountRecovery = AccountRecovery.PHONE_AND_EMAIL,
                RemovalPolicy = RemovalPolicy.RETAIN,
            });

            var appClient = teacherStudentPool.AddClient("TeacherStudentAppClient", new UserPoolClientOptions
            {
                AuthFlows = new AuthFlow { UserPassword = true, UserSrp = true },
            });

            var adminPool = new UserPool(this, "AdminUserPool", new UserPoolProps
            {
                UserPoolName = "stepgo-admin",
                SelfSignUpEnabled = false,
                SignInAliases = new SignInAliases { Email = true },
                Mfa = Mfa.REQUIRED,
                MfaSecondFactor = new MfaSecondFactor { Otp = true, Sms = false },
                CustomAttributes = new Dictionary<string, ICustomAttribute>
                {
                    ["role"] = new StringAttribute(new StringAttributeProps { Mutable = false }),
                },
                RemovalPolicy = RemovalPolicy.RETAIN,
            });
            adminPool.AddClient("AdminAppClient", new UserPoolClientOptions { AuthFlows = new AuthFlow { UserPassword = true, UserSrp = true } });

            return (teacherStudentPool, adminPool, appClient);
        }

        /// <summary>DLQ with maxReceiveCount=5; a CloudWatch alarm on DLQ depth is wired in CreateDlqAlarm (design.md decision 5's DLQ/redrive answer).</summary>
        private (Queue main, Queue dlq) CreatePaymentNotificationQueue()
        {
            var dlq = new Queue(this, "PaymentNotificationDlq", new QueueProps
            {
                QueueName = "stepgo-payment-notifications-dlq",
                RetentionPeriod = Duration.Days(14),
            });

            var mainQueue = new Queue(this, "PaymentNotificationQueue", new QueueProps
            {
                QueueName = "stepgo-payment-notifications",
                VisibilityTimeout = Duration.Seconds(30),
                DeadLetterQueue = new DeadLetterQueue { Queue = dlq, MaxReceiveCount = 5 },
            });

            return (mainQueue, dlq);
        }

        private void CreateDlqAlarm(Queue dlq)
        {
            var topic = new Topic(this, "PaymentNotificationDlqAlarmTopic", new TopicProps { TopicName = "stepgo-payment-notification-dlq-alarm" });
            // Ops email subscription is added post-deploy (the address isn't known at synth time for this scaffold).

            var alarm = new Alarm(this, "PaymentNotificationDlqAlarm", new AlarmProps
            {
                Metric = dlq.MetricApproximateNumberOfMessagesVisible(),
                Threshold = 1,
                EvaluationPeriods = 1,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                AlarmDescription = "金流背景通知經過 5 次重試後進入 DLQ，需要人工介入確認原因。",
            });
            alarm.AddAlarmAction(new SnsAction(topic));
        }

        private Bucket CreateTeacherIdPhotosBucket() => new(this, "TeacherIdPhotosBucket", new BucketProps
        {
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Encryption = BucketEncryption.S3_MANAGED,
            RemovalPolicy = RemovalPolicy.RETAIN,
        });

        private (Secret EcpaySecret, Secret NewebPaySecret, Secret LineSecret) CreateSecrets()
        {
            var ecpay = new Secret(this, "EcpayCredentialsSecret", new SecretProps
            {
                SecretName = "stepgo/ecpay-credentials",
                Description = "綠界金流商 API 金鑰（sandbox 期間可留空，待特店申請通過後填入）。",
            });
            var newebpay = new Secret(this, "NewebPayCredentialsSecret", new SecretProps
            {
                SecretName = "stepgo/newebpay-credentials",
                Description = "藍新金流商 API 金鑰（sandbox 期間可留空，待特店申請通過後填入）。",
            });
            var line = new Secret(this, "LineChannelSecret", new SecretProps
            {
                SecretName = "stepgo/line-channel-secret",
                Description = "LINE Messaging API channel secret（正式帳號申請前留空，通知能力以 mock 驗證）。",
            });

            return (ecpay, newebpay, line);
        }

        /// <summary>
        /// Single Lambda for the whole HTTP API surface (all 8 capabilities' MiniRouter routes are
        /// registered on it in StepGo.Api/Program.cs) — one cold start, one deployable, AOT/provided.al2023.
        /// </summary>
        private Function CreateApiFunction(Dictionary<string, string> sharedEnvironment)
        {
            var function = CreateBootstrapFunction("Api", "stepgo-api", "StepGo.Api", sharedEnvironment);
            Table.GrantReadWriteData(function);
            EventBus.GrantPutEventsTo(function);
            TeacherIdPhotosBucket.GrantReadWrite(function);
            PaymentNotificationQueue.GrantSendMessages(function);
            return function;
        }

        /// <summary>
        /// Every worker shares one StepGo.Worker build artifact; STEPGO_WORKER_NAME (env var, distinct per
        /// deployed function) tells that one executable which handler to bootstrap as. Each still gets its
        /// own Lambda function resource because its AWS trigger (Scheduler, SQS, Step Functions, EventBridge
        /// rule) targets a specific function ARN — but there is exactly one project/one codebase behind them.
        /// </summary>
        private Function CreateWorkerFunction(string workerName, string kebabName, Dictionary<string, string> sharedEnvironment)
        {
            var environment = new Dictionary<string, string>(sharedEnvironment) { ["STEPGO_WORKER_NAME"] = workerName };
            return CreateBootstrapFunction(workerName, $"stepgo-worker-{kebabName}", "StepGo.Worker", environment);
        }

        private Function CreateBootstrapFunction(string logicalId, string functionName, string projectName, Dictionary<string, string> environment) => new(this, logicalId, new FunctionProps
        {
            FunctionName = functionName,
            Runtime = Runtime.PROVIDED_AL2023,
            Handler = "bootstrap",
            Code = Code.FromAsset($"{BackendPublishRoot}/{projectName}/bin/Release/net10.0/linux-x64/publish"),
            MemorySize = 512,
            Timeout = Duration.Seconds(29),
            Architecture = Architecture.X86_64,
            Environment = environment,
        });

        /// <summary>
        /// HTTP API wired from the Routes table above using L1 constructs: one CfnIntegration for the
        /// single StepGo.Api Lambda (every route proxies to it), one CfnAuthorizer (Cognito JWT) shared by
        /// every authenticated route, one CfnRoute per table entry, and a single auto-deployed $default
        /// stage (design.md decision 3).
        /// </summary>
        private CfnApi CreateHttpApi(Function apiFunction, UserPoolClient teacherStudentAppClient)
        {
            var httpApi = new CfnApi(this, "StepGoHttpApi", new CfnApiProps { Name = "stepgo-api", ProtocolType = "HTTP" });

            var authorizer = new CfnAuthorizer(this, "TeacherStudentJwtAuthorizer", new CfnAuthorizerProps
            {
                ApiId = httpApi.Ref,
                Name = "TeacherStudentJwtAuthorizer",
                AuthorizerType = "JWT",
                IdentitySource = ["$request.header.Authorization"],
                JwtConfiguration = new CfnAuthorizer.JWTConfigurationProperty
                {
                    Audience = [teacherStudentAppClient.UserPoolClientId],
                    Issuer = TeacherStudentUserPool.UserPoolProviderUrl,
                },
            });

            var integration = new CfnIntegration(this, "ApiIntegration", new CfnIntegrationProps
            {
                ApiId = httpApi.Ref,
                IntegrationType = "AWS_PROXY",
                IntegrationUri = apiFunction.FunctionArn,
                PayloadFormatVersion = "2.0",
            });

            foreach (var route in Routes)
            {
                var routeId = $"{route.Method}{route.Path}".Replace("/", "_").Replace("{", "").Replace("}", "");

                _ = new CfnRoute(this, $"Route{routeId}", new CfnRouteProps
                {
                    ApiId = httpApi.Ref,
                    RouteKey = $"{route.Method} {route.Path}",
                    Target = $"integrations/{integration.Ref}",
                    AuthorizationType = route.RequiresAuth ? "JWT" : "NONE",
                    AuthorizerId = route.RequiresAuth ? authorizer.Ref : null,
                });
            }

            _ = new CfnStage(this, "DefaultStage", new CfnStageProps { ApiId = httpApi.Ref, StageName = "$default", AutoDeploy = true });

            apiFunction.AddPermission("ApiGatewayInvoke", new Permission
            {
                Principal = new ServicePrincipal("apigateway.amazonaws.com"),
                Action = "lambda:InvokeFunction",
                SourceArn = Fn.Join("", [$"arn:aws:execute-api:{Region}:{Account}:", httpApi.Ref, "/*/*"]),
            });

            return httpApi;
        }

        /// <summary>EventBridge Scheduler (design.md decision 5's plain-cron jobs: overdue scan + monthly payout batch).</summary>
        private void CreateScheduledRules(Function overdueScan, Function payoutScheduler)
        {
            var schedulerRole = new Role(this, "SchedulerInvokeRole", new RoleProps { AssumedBy = new ServicePrincipal("scheduler.amazonaws.com") });
            overdueScan.GrantInvoke(schedulerRole);
            payoutScheduler.GrantInvoke(schedulerRole);

            _ = new CfnSchedule(this, "OverdueOrderScanSchedule", new CfnScheduleProps
            {
                ScheduleExpression = "rate(1 hour)",
                FlexibleTimeWindow = new CfnSchedule.FlexibleTimeWindowProperty { Mode = "OFF" },
                Target = new CfnSchedule.TargetProperty { Arn = overdueScan.FunctionArn, RoleArn = schedulerRole.RoleArn },
            });

            _ = new CfnSchedule(this, "PayoutBatchSchedule", new CfnScheduleProps
            {
                ScheduleExpression = "cron(0 1 5 * ? *)", // 每月 5 日 01:00 UTC
                FlexibleTimeWindow = new CfnSchedule.FlexibleTimeWindowProperty { Mode = "OFF" },
                Target = new CfnSchedule.TargetProperty { Arn = payoutScheduler.FunctionArn, RoleArn = schedulerRole.RoleArn },
            });
        }

        /// <summary>Notifications Lambda subscribes via an EventBridge rule matching every domain-event detail-type it handles (design.md decision 5).</summary>
        private void CreateNotificationEventRule(Function notificationDispatcher)
        {
            var rule = new Rule(this, "NotificationEventRule", new RuleProps
            {
                EventBus = EventBus,
                EventPattern = new EventPattern
                {
                    DetailType = ["OrderPaymentConfirmed", "OrderPaymentOverdue", "RefundApproved", "RefundTicketEscalatedToArbitration"],
                },
            });
            rule.AddTarget(new Amazon.CDK.AWS.Events.Targets.LambdaFunction(notificationDispatcher));
        }

        /// <summary>Task 7.3's SLA timer: Wait 5 business days -> invoke the check Lambda. Business-day math for the Wait duration is approximated as 7 calendar days at the state-machine level; the Lambda itself re-validates the exact deadline before escalating (RefundTicket.AutoEscalateOnSlaTimeout).</summary>
        private StateMachine CreateRefundSlaStateMachine(Function refundSlaCheck)
        {
            var waitTask = new Wait(this, "WaitForTeacherResponseWindow", new WaitProps { Time = WaitTime.Duration(Duration.Days(7)) });
            var invokeCheck = new LambdaInvoke(this, "InvokeRefundSlaCheck", new LambdaInvokeProps
            {
                LambdaFunction = refundSlaCheck,
                PayloadResponseOnly = true,
            });

            var definition = waitTask.Next(invokeCheck);

            return new StateMachine(this, "RefundSlaStateMachine", new StateMachineProps
            {
                StateMachineName = "stepgo-refund-sla",
                DefinitionBody = DefinitionBody.FromChainable(definition),
                Timeout = Duration.Days(10),
            });
        }
    }
}
