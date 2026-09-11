namespace StepGo.Payouts.Application;

/// <summary>The platform's own receiving bank account, used to decide same-bank vs cross-bank transfer fees.</summary>
public interface IPlatformBankAccountProvider
{
    Task<string> GetBankCodeAsync(CancellationToken ct);
}
