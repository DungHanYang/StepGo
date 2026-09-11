using StepGo.Notifications.Domain;

namespace StepGo.Notifications.Application;

public sealed class ListInAppNotificationsHandler(INotificationRecordRepository repository)
{
    public Task<IReadOnlyList<NotificationRecord>> HandleAsync(Guid userId, CancellationToken ct)
        => repository.ListByUserAsync(userId, ct);
}
