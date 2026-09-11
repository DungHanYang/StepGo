using StepGo.Shared.Domain;

namespace StepGo.Notifications.Domain;

/// <summary>The persisted in-app notification-center entry — always written regardless of other channels.</summary>
public sealed class NotificationRecord(Guid id, Guid userId, string templateKey, string renderedText, NotificationChannel dispatchedChannels, DateTimeOffset sentAt)
    : Entity<Guid>(id)
{
    public Guid UserId { get; } = userId;
    public string TemplateKey { get; } = templateKey;
    public string RenderedText { get; } = renderedText;
    public NotificationChannel DispatchedChannels { get; } = dispatchedChannels;
    public DateTimeOffset SentAt { get; } = sentAt;
    public bool IsRead { get; private set; }

    public void MarkRead() => IsRead = true;
}
