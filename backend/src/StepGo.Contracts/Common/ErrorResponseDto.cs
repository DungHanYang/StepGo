namespace StepGo.Contracts.Common;

/// <summary>Uniform error shape for every StepGo.Api.* endpoint (design.md decision 9).</summary>
public sealed record ErrorResponseDto(string Code, string Message);
