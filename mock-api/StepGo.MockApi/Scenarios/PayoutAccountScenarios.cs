using StepGo.ApiClient.Dtos;
using StepGo.UI.Status;

namespace StepGo.MockApi.Scenarios;

/// <summary>frontend-teacher-portal spec: "撥款帳戶設定頁四狀態（檢視/變更/核對中/銀行退回）".</summary>
public static class PayoutAccountScenarios
{
    public static readonly IReadOnlyDictionary<string, TeacherDto> All = new Dictionary<string, TeacherDto>(StringComparer.OrdinalIgnoreCase)
    {
        ["confirmed"] = new("t-mock-1", "陳老師", VerificationStatus.Verified, PayoutAccountStatus.Confirmed, null),
        ["changing"] = new("t-mock-2", "林老師", VerificationStatus.Verified, PayoutAccountStatus.Changing, null),
        ["reconciling"] = new("t-mock-3", "王老師", VerificationStatus.Verified, PayoutAccountStatus.Reconciling, null),
        ["bankrejected"] = new("t-mock-4", "張老師", VerificationStatus.Verified, PayoutAccountStatus.BankRejected, "戶名與身分驗證姓名不一致"),
    };
}
