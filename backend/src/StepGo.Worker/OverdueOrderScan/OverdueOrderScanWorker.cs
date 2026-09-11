using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;
using Amazon.Lambda.Core;
using StepGo.Api.Shared.Composition;
using StepGo.Application.Orders;

namespace StepGo.Worker.OverdueOrderScan;

/// <summary>
/// Task 4.4 (overdue half): scheduled to run periodically (e.g. hourly via EventBridge Scheduler) and
/// flip any ATM order whose payment window has lapsed to Overdue, publishing OrderPaymentOverdueEvent
/// for the notification pipeline.
/// </summary>
public static class OverdueOrderScanWorker
{
    public static async Task HandleAsync(CompositionRoot root, ScheduledEvent scheduledEvent, ILambdaContext context)
    {
        var handler = new MarkOverdueOrdersHandler(root.OrderRepository, root.Clock, root.EventPublisher);
        var candidates = await root.OrderRepository.ListOverdueCandidateOrderIdsAsync(root.Clock.UtcNow, CancellationToken.None);
        await handler.HandleAsync(candidates, CancellationToken.None);
    }
}
