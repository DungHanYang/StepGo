using StepGo.UI.Status;

namespace StepGo.ApiClient.Dtos;

/// <summary>frontend-teacher-portal spec: "身分驗證四狀態"、"撥款帳戶四狀態"; frontend-admin-panel
/// spec: "老師與課程合併目錄列表".</summary>
public sealed record TeacherDto(
    string Id,
    string Name,
    VerificationStatus VerificationStatus,
    PayoutAccountStatus PayoutAccountStatus,
    string? PayoutAccountBankRejectionReason);
