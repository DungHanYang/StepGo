namespace StepGo.ApiClient.Auth;

/// <summary>
/// Role → portal-home mapping (frontend-auth spec: "登入成功後...依帳號角色導向對應後台首頁"
/// — teacher to 老師後台總覽, student to 學生會員中心「我的課程」). Portal base URLs are
/// injected so dev (localhost ports) and prod (teach./learn.stepgo.tw subdomains, per
/// design.md decision 11) can differ without changing this logic.
/// </summary>
public static class PortalRedirect
{
    public const string TeacherRole = "teacher";
    public const string StudentRole = "student";

    public static string? GetRedirectUrl(string? role, PortalBaseUrls baseUrls) => role switch
    {
        TeacherRole => $"{baseUrls.TeacherPortalBaseUrl.TrimEnd('/')}/",
        StudentRole => $"{baseUrls.StudentPortalBaseUrl.TrimEnd('/')}/my-courses",
        _ => null,
    };
}

public sealed record PortalBaseUrls(string TeacherPortalBaseUrl, string StudentPortalBaseUrl)
{
    /// <summary>Local dev defaults matching the ports in each app's launchSettings.json.</summary>
    public static readonly PortalBaseUrls LocalDev = new("http://localhost:5002", "http://localhost:5003");
}
