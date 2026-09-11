using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Worker.NotificationDispatcher;
using StepGo.Worker.OverdueOrderScan;
using StepGo.Worker.PaymentNotificationConsumer;
using StepGo.Worker.PayoutBatchScheduler;
using StepGo.Worker.RefundSlaCheck;

var root = new CompositionRoot();

// One project, one build artifact — every worker's trigger (EventBridge Scheduler, SQS, Step Functions,
// EventBridge bus) still needs its own Lambda function/event source in AWS, but they all run this same
// executable. STEPGO_WORKER_NAME (set per Lambda function in the CDK stack) picks which handler — and
// therefore which strongly-typed event/response pair — this instance bootstraps as.
var workerName = Environment.GetEnvironmentVariable("STEPGO_WORKER_NAME")
    ?? throw new InvalidOperationException("STEPGO_WORKER_NAME environment variable is not set.");

switch (workerName)
{
    case "OverdueOrderScan":
        await LambdaBootstrapBuilder.Create<ScheduledEvent>(
            (evt, context) => OverdueOrderScanWorker.HandleAsync(root, evt, context),
            new SourceGeneratorLambdaJsonSerializer<ApiEventJsonContext>())
            .Build().RunAsync();
        break;

    case "PayoutBatchScheduler":
        await LambdaBootstrapBuilder.Create<ScheduledEvent>(
            (evt, context) => PayoutBatchSchedulerWorker.HandleAsync(root, evt, context),
            new SourceGeneratorLambdaJsonSerializer<ApiEventJsonContext>())
            .Build().RunAsync();
        break;

    case "PaymentNotificationConsumer":
        await LambdaBootstrapBuilder.Create<SQSEvent>(
            (evt, context) => PaymentNotificationConsumerWorker.HandleBatchAsync(root, evt, context),
            new SourceGeneratorLambdaJsonSerializer<ApiEventJsonContext>())
            .Build().RunAsync();
        break;

    case "RefundSlaCheck":
        await LambdaBootstrapBuilder.Create<RefundSlaCheckInput, RefundSlaCheckOutput>(
            (evt, context) => RefundSlaCheckWorker.HandleAsync(root, evt, context),
            new SourceGeneratorLambdaJsonSerializer<RefundSlaCheckJsonContext>())
            .Build().RunAsync();
        break;

    case "NotificationDispatcher":
        await LambdaBootstrapBuilder.Create<NotificationEventEnvelope>(
            (evt, context) => NotificationDispatcherWorker.HandleAsync(root, evt, context),
            new SourceGeneratorLambdaJsonSerializer<NotificationEventEnvelopeJsonContext>())
            .Build().RunAsync();
        break;

    default:
        throw new InvalidOperationException($"Unknown STEPGO_WORKER_NAME: {workerName}");
}
