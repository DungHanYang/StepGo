using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;

namespace StepGo.Api.Shared.Json;

/// <summary>Source-generated serialization for the Lambda event/response envelopes themselves — required by SourceGeneratorLambdaJsonSerializer&lt;T&gt; under Native AOT.</summary>
[JsonSourceGenerationOptions]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(SQSEvent))]
[JsonSerializable(typeof(ScheduledEvent))]
public partial class ApiEventJsonContext : JsonSerializerContext;
