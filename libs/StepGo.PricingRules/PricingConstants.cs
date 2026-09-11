namespace StepGo.PricingRules;

/// <summary>
/// Fixed fee-rate constants (design/design-handoff/README.md § Business Rules).
/// These mirror what the Admin fee-settings page (frontend-admin-panel) will
/// eventually control server-side; until the backend change exists, the
/// frontend uses these as its single source of truth for all pricing math.
/// </summary>
public static class PricingConstants
{
    /// <summary>信用卡手續費 2.89%（依售價計算）。</summary>
    public const decimal CreditCardFeeRate = 0.0289m;

    /// <summary>ATM 每筆 NT$15，與售價無關。</summary>
    public const decimal AtmFeePerTransaction = 15m;

    /// <summary>平台服務費 10%，以原價（售價）計算，非以淨額計算。</summary>
    public const decimal PlatformFeeRate = 0.10m;

    /// <summary>撥款轉帳費：同行。</summary>
    public const decimal PayoutTransferFeeSameBank = 10m;

    /// <summary>撥款轉帳費：跨行。</summary>
    public const decimal PayoutTransferFeeCrossBank = 20m;

    /// <summary>退款處理費（單筆）。</summary>
    public const decimal RefundProcessingFee = 25m;
}
