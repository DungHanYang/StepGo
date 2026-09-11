using StepGo.ApiClient.Auth;

namespace StepGo.Marketing.Client.Auth;

/// <summary>
/// Mock-first account lookup for the shared login page — stands in for the real backend's
/// credential check until that change exists (design.md decision 8). Seed accounts let the
/// login flow be demoed/tested end-to-end today.
/// </summary>
public static class MockAccountDirectory
{
    private static readonly IReadOnlyDictionary<string, string> AccountsByEmail = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["teacher@example.com"] = PortalRedirect.TeacherRole,
        ["student@example.com"] = PortalRedirect.StudentRole,
    };

    public static bool TryGetRole(string email, out string? role) => AccountsByEmail.TryGetValue(email, out role);
}
