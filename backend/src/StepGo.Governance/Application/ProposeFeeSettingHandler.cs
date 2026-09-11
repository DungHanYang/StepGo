using StepGo.Shared.Application;
using StepGo.Governance.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Governance.Application;

public sealed record ProposeFeeSettingCommand(decimal PlatformServiceFeeRate, Money CrossBankTransferFee, IReadOnlyList<RefundTier> RefundFloor, DateTimeOffset EffectiveDate);

/// <summary>Task 8.1/8.2: any fee/floor change needs >= 30 days notice and is written to the audit trail.</summary>
public sealed class ProposeFeeSettingHandler(IPlatformFeeSettingRepository settingRepository, IChangeLogRepository changeLog, ICurrentUserAccessor currentUser, IClock clock)
{
    public async Task<PlatformFeeSetting> HandleAsync(ProposeFeeSettingCommand command, CancellationToken ct)
    {
        RowLevelGuardIsAdmin();

        var versionId = $"fee-setting-{clock.UtcNow:yyyyMMddHHmmss}";
        var setting = PlatformFeeSetting.Propose(
            versionId, command.PlatformServiceFeeRate, command.CrossBankTransferFee,
            new RefundRuleSet(command.RefundFloor), command.EffectiveDate, currentUser.UserId, clock.UtcNow);

        await settingRepository.SaveAsync(setting, ct);
        await changeLog.AppendAsync(
            new ChangeLogEntry(Guid.NewGuid(), ChangeLogCategory.FeeRate,
                $"新增費率設定版本 {versionId}，生效日 {command.EffectiveDate:yyyy-MM-dd}", currentUser.UserId, clock.UtcNow),
            ct);

        return setting;
    }

    private void RowLevelGuardIsAdmin()
    {
        if (currentUser.Role != StepGo.Identity.Domain.Role.Admin)
        {
            throw new AuthorizationException("僅管理者可變更平台費率設定。");
        }
    }
}
