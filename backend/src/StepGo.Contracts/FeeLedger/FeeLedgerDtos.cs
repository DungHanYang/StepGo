namespace StepGo.Contracts.FeeLedger;

public sealed record FeeCalculationResultDto(long GrossAmount, long GatewayFee, long PlatformServiceFee, long NetReceivable, string FeeScheduleVersionId);
