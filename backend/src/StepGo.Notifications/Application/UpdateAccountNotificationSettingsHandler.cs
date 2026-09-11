using StepGo.Shared.Domain;

namespace StepGo.Notifications.Application;

public sealed record UpdateAccountNotificationSettingsCommand(IReadOnlyList<string> DisabledChannels);

/// <summary>
/// Task 9.4: In-app and Email are core, mandatory channels — the account-settings API has no way to
/// turn them off. Only "Line" may ever appear in DisabledChannels; anything else is rejected outright.
/// </summary>
public sealed class UpdateAccountNotificationSettingsHandler
{
    public void Handle(UpdateAccountNotificationSettingsCommand command)
    {
        foreach (var channel in command.DisabledChannels)
        {
            if (!string.Equals(channel, "Line", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("core_channel_cannot_be_disabled", $"「{channel}」為核心必要通知管道，不可關閉。");
            }
        }
    }
}
