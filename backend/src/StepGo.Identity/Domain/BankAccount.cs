namespace StepGo.Identity.Domain;

/// <summary>The teacher's payout destination account.</summary>
public sealed record BankAccount(string BankCode, string AccountNumber, string AccountHolderName)
{
    public bool IsSameBankAs(string platformBankCode) => BankCode == platformBankCode;
}
