using StepGo.Shared.Domain.FeeLedger;

namespace StepGo.Shared.Application.FeeLedger;

public interface IFeeRateScheduleProvider
{
    /// <summary>The rate schedule in effect right now — used when an order's payment is confirmed.</summary>
    Task<FeeRateSchedule> GetCurrentAsync(CancellationToken ct);

    /// <summary>The exact schedule an order was bound to at creation — used to reproduce historical fee amounts.</summary>
    Task<FeeRateSchedule> GetByVersionAsync(string versionId, CancellationToken ct);
}
