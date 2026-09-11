using StepGo.Shared.Domain;

namespace StepGo.Payouts.Domain;

/// <summary>A settlement-time snapshot of one order's net receivable, taken when the batch was drafted.</summary>
public sealed record PayoutBatchOrderLine(Guid OrderId, Money NetReceivableSnapshot);
