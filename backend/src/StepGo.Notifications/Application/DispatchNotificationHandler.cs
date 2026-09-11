using StepGo.Shared.Application;
using StepGo.Notifications.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Notifications.Application;

/// <summary>
/// Task 9.1/9.2: the Notifications Lambda's per-event entry point. In-app is always written; Email is
/// sent whenever the recipient has one on file; LINE is an opt-in extra only when bound. Neither In-app
/// nor Email can be skipped by caller choice — there is no parameter for that (task 9.4).
/// </summary>
public sealed class DispatchNotificationHandler(
    INotificationTemplateRepository templateRepository, INotificationRecordRepository recordRepository,
    IRecipientContactLookup contactLookup, IEmailSender emailSender, ILineMessenger lineMessenger, IClock clock)
{
    public async Task HandleAsync(Guid userId, string templateKey, IReadOnlyDictionary<string, string> variables, CancellationToken ct)
    {
        var template = await templateRepository.FindAsync(templateKey, ct)
            ?? throw new DomainException("notification_template_not_found", $"找不到通知範本：{templateKey}");

        var renderedText = template.Render(variables);
        var (email, lineUserId) = await contactLookup.GetContactAsync(userId, ct);

        var channels = NotificationDispatchPolicy.DetermineChannels(hasEmail: email is not null, hasLineBound: lineUserId is not null);

        if (channels.HasFlag(NotificationChannel.Email) && email is not null)
        {
            await emailSender.SendAsync(email, templateKey, renderedText, ct);
        }

        if (channels.HasFlag(NotificationChannel.Line) && lineUserId is not null)
        {
            await lineMessenger.PushAsync(lineUserId, renderedText, ct);
        }

        // In-app record is always written, regardless of Email/LINE availability.
        var record = new NotificationRecord(Guid.NewGuid(), userId, templateKey, renderedText, channels, clock.UtcNow);
        await recordRepository.SaveAsync(record, ct);
    }
}
