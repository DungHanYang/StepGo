using StepGo.ApiClient.Dtos;
using StepGo.UI.Status;

namespace StepGo.MockApi.Scenarios;

/// <summary>frontend-teacher-portal spec: "身分驗證頁四狀態（填寫/審核中/未通過/通過）".</summary>
public static class VerificationScenarios
{
    public static readonly IReadOnlyDictionary<string, TeacherDto> All = new Dictionary<string, TeacherDto>(StringComparer.OrdinalIgnoreCase)
    {
        ["filling"] = new("t-mock-1", "陳老師", VerificationStatus.Filling, PayoutAccountStatus.Confirmed, null),
        ["underreview"] = new("t-mock-2", "林老師", VerificationStatus.UnderReview, PayoutAccountStatus.Confirmed, null),
        ["rejected"] = new("t-mock-3", "王老師", VerificationStatus.Rejected, PayoutAccountStatus.Confirmed, null),
        ["verified"] = new("t-mock-4", "張老師", VerificationStatus.Verified, PayoutAccountStatus.Confirmed, null),
    };
}
