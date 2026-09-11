using StepGo.Domain.Notifications;

namespace StepGo.Application.Notifications;

public sealed class ListInAppNotificationsHandler(INotificationRecordRepository repository)
{
    public Task<IReadOnlyList<NotificationRecord>> HandleAsync(Guid userId, CancellationToken ct)
        => repository.ListByUserAsync(userId, ct);
}
