using StepGo.Application.Payouts;

namespace StepGo.Infrastructure.Aws;

public sealed class EnvironmentPlatformBankAccountProvider : IPlatformBankAccountProvider
{
    public Task<string> GetBankCodeAsync(CancellationToken ct)
        => Task.FromResult(Environment.GetEnvironmentVariable("STEPGO_PLATFORM_BANK_CODE") ?? "807");
}
