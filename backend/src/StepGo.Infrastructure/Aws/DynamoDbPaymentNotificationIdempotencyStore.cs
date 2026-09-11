using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.Application.Orders;
using StepGo.Infrastructure.Dynamo;

namespace StepGo.Infrastructure.Aws;

/// <summary>Conditional write on the gateway's unique transaction id — attribute_not_exists guards against processing the same webhook notification twice (design.md decision 4).</summary>
public sealed class DynamoDbPaymentNotificationIdempotencyStore(IAmazonDynamoDB client, StepGoTableOptions options) : IPaymentNotificationIdempotencyStore
{
    private const string Pk = "PAYMENTNOTIFICATION";

    public async Task<bool> TryReserveAsync(string gatewayTransactionId, CancellationToken ct)
    {
        try
        {
            await client.PutItemAsync(new PutItemRequest
            {
                TableName = options.TableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new(Pk),
                    ["SK"] = new(gatewayTransactionId),
                    ["ReservedAt"] = new(DateTimeOffset.UtcNow.ToString("O")),
                },
                ConditionExpression = "attribute_not_exists(PK)",
            }, ct);

            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }
}
