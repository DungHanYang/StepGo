using System.Text.RegularExpressions;
using StepGo.Shared.Domain;

namespace StepGo.Notifications.Domain;

/// <summary>
/// Notification copy lives here, not in code, so admins can edit wording without a deploy.
/// Placeholders use "{variableName}" syntax.
/// </summary>
public sealed partial class NotificationTemplate : AggregateRoot<string>
{
    public string BodyTemplate { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private NotificationTemplate(string key, string bodyTemplate, Guid updatedBy, DateTimeOffset updatedAt) : base(key)
    {
        BodyTemplate = bodyTemplate;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    public static NotificationTemplate Create(string key, string bodyTemplate, Guid updatedBy, DateTimeOffset now)
        => new(key, bodyTemplate, updatedBy, now);

    public static NotificationTemplate Rehydrate(string key, string bodyTemplate, Guid updatedBy, DateTimeOffset updatedAt)
        => new(key, bodyTemplate, updatedBy, updatedAt);

    public void UpdateBody(string bodyTemplate, Guid updatedBy, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(bodyTemplate))
        {
            throw new DomainException("template_body_required", "通知範本內容不得為空。");
        }

        BodyTemplate = bodyTemplate;
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }

    public string Render(IReadOnlyDictionary<string, string> variables)
        => PlaceholderPattern().Replace(BodyTemplate, match =>
            variables.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex PlaceholderPattern();
}
