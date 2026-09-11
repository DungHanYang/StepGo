namespace StepGo.Infrastructure;

/// <summary>Table name is injected via the Lambda environment variable STEPGO_TABLE_NAME, set by CDK.</summary>
public sealed class StepGoTableOptions
{
    public required string TableName { get; init; }

    public static StepGoTableOptions FromEnvironment() => new()
    {
        TableName = Environment.GetEnvironmentVariable("STEPGO_TABLE_NAME") ?? "StepGoTable",
    };
}
