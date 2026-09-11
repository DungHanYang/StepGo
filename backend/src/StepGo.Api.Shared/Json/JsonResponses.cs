using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Amazon.Lambda.APIGatewayEvents;
using StepGo.Contracts.Common;
using StepGo.Contracts.Json;

namespace StepGo.Api.Shared.Json;

/// <summary>Every StepGo.Api.* response body is serialized through StepGoJsonContext (StepGo.Contracts' source-gen context) — never the reflection-based JsonSerializer.Serialize&lt;T&gt; overload.</summary>
public static class JsonResponses
{
    private static readonly StepGoJsonContext Contracts = new();

    public static APIGatewayHttpApiV2ProxyResponse Ok<T>(T body, JsonTypeInfo<T> typeInfo) => new()
    {
        StatusCode = 200,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(body, typeInfo),
    };

    public static APIGatewayHttpApiV2ProxyResponse Created<T>(T body, JsonTypeInfo<T> typeInfo) => new()
    {
        StatusCode = 201,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(body, typeInfo),
    };

    public static APIGatewayHttpApiV2ProxyResponse NoContent() => new() { StatusCode = 204 };

    public static APIGatewayHttpApiV2ProxyResponse Error(int statusCode, string code, string message) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(new ErrorResponseDto(code, message), Contracts.ErrorResponseDto),
    };

    public static T? Deserialize<T>(string? body, JsonTypeInfo<T> typeInfo)
        => string.IsNullOrEmpty(body) ? default : JsonSerializer.Deserialize(body, typeInfo);
}
