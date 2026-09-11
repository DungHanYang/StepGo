namespace StepGo.UI;

/// <summary>
/// Two independent responsive breakpoint sets shared by all four apps
/// (frontend-design-system spec: "響應式斷點跨應用一致"). Mirrored in the
/// Tailwind `screens` config (Styles/tailwind config `sys-*` / `mkt-*`
/// variants) so CSS media queries and any C#/JS viewport logic stay in sync.
/// </summary>
public static class Breakpoints
{
    /// <summary>System pages (sidebar layout: teacher/student/admin portals) — wide breakpoint.</summary>
    public const int SystemWide = 1080;

    /// <summary>System pages — narrow breakpoint; below this the sidebar collapses to a horizontal bar.</summary>
    public const int SystemNarrow = 980;

    /// <summary>Marketing pages — wide breakpoint.</summary>
    public const int MarketingWide = 900;

    /// <summary>Marketing pages — narrow breakpoint; below this the layout goes single-column.</summary>
    public const int MarketingNarrow = 640;
}
