namespace StepGo.Marketing.Models;

/// <summary>
/// Placeholder course content for the marketing site. Mock-first per design.md decision 8 —
/// replaced by real StepGo.ApiClient calls against the Mock API / backend in task 8.1/8.2.
/// </summary>
public sealed record Course(
    string Id,
    string Name,
    string Category,
    decimal Price,
    string ScheduleSummary,
    string Location,
    string RefundPolicySummary,
    bool IsEnrolling);

public static class CourseCatalog
{
    public static readonly IReadOnlyList<Course> All =
    [
        new("c-paint-kids", "兒童繪畫班", "藝術", 3200m, "每週六 10:00-11:30，共 8 堂", "台北市大安區", "開課前 14 日以上可全額退費", true),
        new("c-yoga-parent", "親子瑜伽", "運動", 2400m, "每週三 19:00-20:00，共 6 堂", "新北市板橋區", "開課前 14 日以上可全額退費", true),
        new("c-code-intro", "程式設計入門", "程式", 4500m, "每週日 14:00-16:00，共 10 堂", "線上課程", "開課前 14 日以上可全額退費", true),
        new("c-baking", "親子烘焙", "生活", 1800m, "每月第一個週末 09:00-12:00", "台中市西區", "開課前 14 日以上可全額退費", false),
        new("c-piano", "兒童鋼琴啟蒙", "音樂", 5200m, "每週二 16:00-17:00，共 12 堂", "台北市中山區", "開課前 14 日以上可全額退費", true),
    ];

    public static IReadOnlyList<string> Categories => All.Select(c => c.Category).Distinct().OrderBy(c => c).ToList();

    public static Course? FindById(string id) => All.FirstOrDefault(c => c.Id == id);

    public static IEnumerable<Course> Search(string? category, string? keyword)
    {
        var query = All.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(c => c.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(c => c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }
}
