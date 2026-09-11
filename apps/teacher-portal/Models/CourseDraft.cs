using StepGo.PricingRules;

namespace StepGo.TeacherPortal.Models;

/// <summary>Holds state across the 4-step course-creation wizard (frontend-teacher-portal spec:
/// "課程建立四步驟精靈...與步驟間資料保留").</summary>
public sealed class CourseDraft
{
    // Step 1: 基本資料
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ScheduleSummary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    // Step 2: 定價與付款
    public decimal Price { get; set; } = 3000m;
    public bool AllowCreditCard { get; set; }
    public bool AllowAtm { get; set; }

    // Step 3: 退費規則 (teacher may raise above the platform floor, never below)
    public Dictionary<RefundTier, decimal> RefundRates { get; set; } = new(RefundPolicy.DefaultPlatformFloor);

    // Step 4: 招生頁
    public string EnrollmentDescription { get; set; } = string.Empty;

    public bool HasAtLeastOnePaymentMethod => AllowCreditCard || AllowAtm;
}
