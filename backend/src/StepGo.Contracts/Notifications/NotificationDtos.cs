namespace StepGo.Contracts.Notifications;

[Flags]
public enum NotificationChannelDto
{
    None = 0,
    InApp = 1,
    Email = 2,
    Line = 4,
}

public sealed record NotificationTemplateDto(string Key, string BodyTemplate, Guid UpdatedBy, DateTimeOffset UpdatedAt);

public sealed record UpdateNotificationTemplateRequestDto(string BodyTemplate);

public sealed record NotificationRecordDto(Guid Id, Guid UserId, string TemplateKey, string RenderedText, NotificationChannelDto DispatchedChannels, DateTimeOffset SentAt, bool IsRead);

public sealed record UpdateAccountNotificationSettingsRequestDto(IReadOnlyList<string> DisabledChannels);
