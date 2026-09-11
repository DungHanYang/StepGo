using System.Text.Json.Serialization;
using StepGo.Application.Orders;

namespace StepGo.Infrastructure.Aws;

/// <summary>Source-generated serialization for Application-layer payloads that Infrastructure hands to AWS services (SQS body, etc.) — keeps every serialization call Native-AOT safe.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GatewayNotificationPayload))]
public partial class InfrastructureJsonContext : JsonSerializerContext;
