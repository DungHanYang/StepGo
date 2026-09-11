using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.S3;
using Amazon.SimpleEmail;
using Amazon.SQS;
using Amazon.StepFunctions;
using StepGo.Api.Shared.Infrastructure;
using StepGo.Courses.Application;
using StepGo.Courses.Infrastructure;
using StepGo.Governance.Application;
using StepGo.Governance.Infrastructure;
using StepGo.Identity.Application;
using StepGo.Identity.Infrastructure;
using StepGo.Notifications.Application;
using StepGo.Notifications.Infrastructure;
using StepGo.Orders.Application;
using StepGo.Orders.Infrastructure;
using StepGo.Payouts.Application;
using StepGo.Payouts.Infrastructure;
using StepGo.RefundTickets.Application;
using StepGo.RefundTickets.Domain;
using StepGo.RefundTickets.Infrastructure;
using StepGo.Shared.Application;
using StepGo.Shared.Application.FeeLedger;
using StepGo.Shared.Infrastructure;
using StepGo.Shared.Infrastructure.Dynamo;

namespace StepGo.Api.Shared.Composition;

/// <summary>
/// Manual composition root — no reflection-based DI container. Each StepGo.Api.* Program.cs new()s one
/// instance per Lambda cold start and reuses it across warm invocations. Every property is a plain
/// concrete-typed field; nothing here relies on runtime type discovery, keeping the whole graph
/// Native-AOT safe.
/// </summary>
public sealed class CompositionRoot
{
    public StepGoTableOptions TableOptions { get; } = StepGoTableOptions.FromEnvironment();
    public IAmazonDynamoDB DynamoDb { get; } = new AmazonDynamoDBClient();
    public IAmazonS3 S3 { get; } = new AmazonS3Client();
    public IAmazonSQS Sqs { get; } = new AmazonSQSClient();
    public IAmazonEventBridge EventBridge { get; } = new AmazonEventBridgeClient();
    public IAmazonSimpleEmailService Ses { get; } = new AmazonSimpleEmailServiceClient();
    public IAmazonStepFunctions StepFunctions { get; } = new AmazonStepFunctionsClient();

    public IClock Clock { get; } = new SystemClock();
    public IBusinessDayCalendar BusinessDayCalendar { get; } = new TaiwanBusinessDayCalendar();

    private DynamoTransactionWriter? _transactionWriter;
    public DynamoTransactionWriter TransactionWriter => _transactionWriter ??= new DynamoTransactionWriter(DynamoDb);

    public IUserRepository UserRepository => new UserRepository(DynamoDb, TableOptions);
    public ITeacherProfileRepository TeacherProfileRepository => new TeacherProfileRepository(DynamoDb, TableOptions);
    public ICourseRepository CourseRepository => new CourseRepository(DynamoDb, TableOptions);
    public IOrderRepository OrderRepository => new OrderRepository(DynamoDb, TableOptions, TransactionWriter);
    public IPayoutBatchRepository PayoutBatchRepository => new PayoutBatchRepository(DynamoDb, TableOptions);
    public IRefundTicketRepository RefundTicketRepository => new RefundTicketRepository(DynamoDb, TableOptions);
    public IPlatformFeeSettingRepository PlatformFeeSettingRepository => new PlatformFeeSettingRepository(DynamoDb, TableOptions);
    public ITermsVersionRepository TermsVersionRepository => new TermsVersionRepository(DynamoDb, TableOptions);
    public ITeacherConsentRepository TeacherConsentRepository => new TeacherConsentRepository(DynamoDb, TableOptions);
    public IChangeLogRepository ChangeLogRepository => new ChangeLogRepository(DynamoDb, TableOptions);
    public INotificationTemplateRepository NotificationTemplateRepository => new NotificationTemplateRepository(DynamoDb, TableOptions);
    public INotificationRecordRepository NotificationRecordRepository => new NotificationRecordRepository(DynamoDb, TableOptions);

    public IObjectStorage ObjectStorage => new S3ObjectStorage(S3, Environment.GetEnvironmentVariable("STEPGO_ID_PHOTOS_BUCKET") ?? "stepgo-teacher-id-photos");
    public IEventPublisher EventPublisher => new EventBridgePublisher(EventBridge, Environment.GetEnvironmentVariable("STEPGO_EVENT_BUS_NAME") ?? "stepgo-events");
    public IOrderNotificationQueue OrderNotificationQueue => new SqsOrderNotificationQueue(Sqs, Environment.GetEnvironmentVariable("STEPGO_PAYMENT_NOTIFICATION_QUEUE_URL") ?? "");
    public IPaymentNotificationIdempotencyStore IdempotencyStore => new DynamoDbPaymentNotificationIdempotencyStore(DynamoDb, TableOptions);
    public IPaymentGatewayClient PaymentGatewayClient => new SandboxPaymentGatewayClient(
        Environment.GetEnvironmentVariable("STEPGO_ECPAY_MERCHANT_ID") ?? "2000132",
        Environment.GetEnvironmentVariable("STEPGO_ECPAY_HASH_KEY") ?? "",
        Environment.GetEnvironmentVariable("STEPGO_ECPAY_HASH_IV") ?? "");
    public IPlatformBankAccountProvider PlatformBankAccountProvider => new EnvironmentPlatformBankAccountProvider();
    public IFeeRateScheduleProvider FeeRateScheduleProvider => new FeeRateScheduleProvider(PlatformFeeSettingRepository);
    public IEmailSender EmailSender => new SesEmailSender(Ses, Environment.GetEnvironmentVariable("STEPGO_NOTIFICATION_FROM_EMAIL") ?? "no-reply@stepgo.tw");
    public ILineMessenger LineMessenger => new LineMessengerMock();
    public IRecipientContactLookup RecipientContactLookup => new StaticRecipientContactLookup(DynamoDb, TableOptions);
    public IRefundSlaScheduler RefundSlaScheduler => new StepFunctionsRefundSlaScheduler(
        StepFunctions, Environment.GetEnvironmentVariable("STEPGO_REFUND_SLA_STATE_MACHINE_ARN") ?? "");
}
