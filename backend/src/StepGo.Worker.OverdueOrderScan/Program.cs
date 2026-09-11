using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Application.Orders;

var root = new CompositionRoot();

// Task 4.4 (overdue half): scheduled to run periodically (e.g. hourly via EventBridge Scheduler) and
// flip any ATM order whose payment window has lapsed to Overdue, publishing OrderPaymentOverdueEvent
// for the notification pipeline.
async Task HandleAsync(ScheduledEvent scheduledEvent, Amazon.Lambda.Core.ILambdaContext context)
{
    var handler = new MarkOverdueOrdersHandler(root.OrderRepository, root.Clock, root.EventPublisher);
    var candidates = await root.OrderRepository.ListOverdueCandidateOrderIdsAsync(root.Clock.UtcNow, CancellationToken.None);
    await handler.HandleAsync(candidates, CancellationToken.None);
}

await LambdaBootstrapBuilder.Create<ScheduledEvent>(HandleAsync, new SourceGeneratorLambdaJsonSerializer<ApiEventJsonContext>())
    .Build()
    .RunAsync();
