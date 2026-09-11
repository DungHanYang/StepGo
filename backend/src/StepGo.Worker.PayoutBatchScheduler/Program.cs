using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Application.Payouts;

var root = new CompositionRoot();

// Task 6.1: EventBridge Scheduler fires this on the 5th of every month (design.md decision 5). It walks
// every teacher with at least one pending-payout order and drafts their period's batch; teachers below
// the NT$500 threshold are silently skipped by CalculatePayoutBatchHandler (their orders roll forward).
async Task HandleAsync(ScheduledEvent scheduledEvent, Amazon.Lambda.Core.ILambdaContext context)
{
    var handler = new CalculatePayoutBatchHandler(
        root.OrderRepository, root.PayoutBatchRepository, root.TeacherProfileRepository, root.PlatformFeeSettingRepository, root.PlatformBankAccountProvider);

    var periodYyyyMm = root.Clock.UtcNow.ToString("yyyyMM");
    var teacherIds = await root.OrderRepository.ListTeacherIdsWithPendingPayoutAsync(CancellationToken.None);

    foreach (var teacherId in teacherIds)
    {
        await handler.HandleAsync(teacherId, periodYyyyMm, CancellationToken.None);
    }
}

await LambdaBootstrapBuilder.Create<ScheduledEvent>(HandleAsync, new SourceGeneratorLambdaJsonSerializer<ApiEventJsonContext>())
    .Build()
    .RunAsync();
