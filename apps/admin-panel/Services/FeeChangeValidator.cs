namespace StepGo.AdminPanel.Services;

/// <summary>frontend-admin-panel spec: "任何調整 SHALL 要求選擇生效日，且生效日 SHALL NOT
/// 早於送出當下起算 30 天".</summary>
public static class FeeChangeValidator
{
    public const int MinNoticeDays = 30;

    public static bool IsEffectiveDateAllowed(DateOnly effectiveDate, DateOnly submittedOn) =>
        effectiveDate >= submittedOn.AddDays(MinNoticeDays);
}
