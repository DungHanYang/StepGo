namespace StepGo.Contracts.Common;

/// <summary>DynamoDB's LastEvaluatedKey, opaque-encoded, replaces offset pagination (design.md decision 9).</summary>
public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, string? NextCursor);
