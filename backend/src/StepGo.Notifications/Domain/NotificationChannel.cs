namespace StepGo.Notifications.Domain;

[Flags]
public enum NotificationChannel
{
    None = 0,
    InApp = 1,
    Email = 2,
    Line = 4,
}
