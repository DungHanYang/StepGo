using StepGo.Shared.Application;
using StepGo.Notifications.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Notifications.Application;

public sealed record UpdateNotificationTemplateCommand(string Key, string BodyTemplate);

/// <summary>Task 9.3: admins edit copy without a deploy; the next trigger picks up the new text immediately.</summary>
public sealed class ManageNotificationTemplateHandler(INotificationTemplateRepository repository, ICurrentUserAccessor currentUser, IClock clock)
{
    public async Task<NotificationTemplate> HandleAsync(UpdateNotificationTemplateCommand command, CancellationToken ct)
    {
        if (currentUser.Role != Role.Admin)
        {
            throw new AuthorizationException("僅管理者可調整通知範本。");
        }

        var template = await repository.FindAsync(command.Key, ct);
        if (template is null)
        {
            template = NotificationTemplate.Create(command.Key, command.BodyTemplate, currentUser.UserId, clock.UtcNow);
        }
        else
        {
            template.UpdateBody(command.BodyTemplate, currentUser.UserId, clock.UtcNow);
        }

        await repository.SaveAsync(template, ct);
        return template;
    }
}
