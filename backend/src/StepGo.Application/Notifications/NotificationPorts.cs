using StepGo.Domain.Notifications;

namespace StepGo.Application.Notifications;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> FindAsync(string key, CancellationToken ct);
    Task SaveAsync(NotificationTemplate template, CancellationToken ct);
}

public interface INotificationRecordRepository
{
    Task<IReadOnlyList<NotificationRecord>> ListByUserAsync(Guid userId, CancellationToken ct);
    Task SaveAsync(NotificationRecord record, CancellationToken ct);
}

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);
}

/// <summary>LINE Messaging API push — implemented as a safe mock until the official LINE channel is provisioned (proposal.md dependency note).</summary>
public interface ILineMessenger
{
    Task PushAsync(string lineUserId, string message, CancellationToken ct);
}

/// <summary>Resolves a user's Email / LINE binding for the dispatch-policy decision.</summary>
public interface IRecipientContactLookup
{
    Task<(string? Email, string? LineUserId)> GetContactAsync(Guid userId, CancellationToken ct);
}
