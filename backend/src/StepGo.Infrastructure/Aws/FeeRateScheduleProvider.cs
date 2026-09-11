using StepGo.Application.FeeLedger;
using StepGo.Application.Governance;
using StepGo.Domain.FeeLedger;
using StepGo.Domain.SharedKernel;

namespace StepGo.Infrastructure.Aws;

/// <summary>
/// Gateway-side rates (信用卡 2.89%／ATM 每筆 NT$15) are fixed processor pricing, not part of platform
/// governance's fee-setting versioning; only the platform service fee rate is governance-controlled.
/// The composite schedule's VersionId still ties back to the governance PlatformFeeSetting version for
/// full historical traceability (task 5.2's binding requirement).
/// </summary>
public sealed class FeeRateScheduleProvider(IPlatformFeeSettingRepository settingRepository) : IFeeRateScheduleProvider
{
    private const decimal CreditCardGatewayRate = 0.0289m;
    private static readonly Money AtmGatewayFlatFee = Money.FromWholeDollars(15);

    public async Task<FeeRateSchedule> GetCurrentAsync(CancellationToken ct)
    {
        var setting = await settingRepository.GetCurrentAsync(ct)
            ?? throw new DomainException("platform_fee_setting_missing", "尚未設定平台服務費率。");

        return new FeeRateSchedule(setting.Id, CreditCardGatewayRate, AtmGatewayFlatFee, setting.PlatformServiceFeeRate);
    }

    public async Task<FeeRateSchedule> GetByVersionAsync(string versionId, CancellationToken ct)
    {
        var setting = await settingRepository.GetByVersionAsync(versionId, ct)
            ?? throw new DomainException("platform_fee_setting_version_not_found", $"找不到費率版本：{versionId}");

        return new FeeRateSchedule(setting.Id, CreditCardGatewayRate, AtmGatewayFlatFee, setting.PlatformServiceFeeRate);
    }
}
