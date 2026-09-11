namespace StepGo.Notifications.Domain;

/// <summary>
/// In-app + Email are the mandatory core channels and are never user-disable-able; LINE is opt-in
/// value-add only sent when the recipient has bound their LINE account.
/// </summary>
public static class NotificationDispatchPolicy
{
    public static NotificationChannel DetermineChannels(bool hasEmail, bool hasLineBound)
    {
        var channels = NotificationChannel.InApp;

        if (hasEmail)
        {
            channels |= NotificationChannel.Email;
        }

        if (hasLineBound)
        {
            channels |= NotificationChannel.Line;
        }

        return channels;
    }
}
