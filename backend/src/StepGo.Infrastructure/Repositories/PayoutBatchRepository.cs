using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.Application.Payouts;
using StepGo.Domain.Payouts;
using StepGo.Domain.SharedKernel;
using StepGo.Infrastructure.Dynamo;

namespace StepGo.Infrastructure.Repositories;

/// <summary>PK=PAYOUTBATCH#batchId holds the METADATA item plus one ORDER#&lt;orderId&gt; line-item per covered order (design.md decision 4).</summary>
public sealed class PayoutBatchRepository(IAmazonDynamoDB client, StepGoTableOptions options) : IPayoutBatchRepository
{
    public async Task<PayoutBatch?> FindAsync(Guid payoutBatchId, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new() { [":pk"] = new(DynamoDbKeys.PayoutBatchPk(payoutBatchId)) },
        }, ct);

        if (response.Items.Count == 0)
        {
            return null;
        }

        var metadata = response.Items.Single(i => i.GetS("SK") == DynamoDbKeys.MetadataSk);
        var lines = response.Items.Where(i => i.GetS("SK") != DynamoDbKeys.MetadataSk)
            .Select(i => new PayoutBatchOrderLine(i.GetGuid("OrderId"), i.GetMoney("NetReceivableSnapshot")))
            .ToList();

        return Map(metadata, lines);
    }

    public async Task<IReadOnlyList<PayoutBatch>> ListByTeacherAsync(Guid teacherId, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :prefix)",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new(DynamoDbKeys.Gsi1Pk(teacherId)),
                [":prefix"] = new("PAYOUTBATCH#"),
            },
        }, ct);

        var batches = new List<PayoutBatch>();
        foreach (var metadata in response.Items)
        {
            var batch = await FindAsync(metadata.GetGuid("Id"), ct);
            if (batch is not null)
            {
                batches.Add(batch);
            }
        }
        return batches;
    }

    public async Task SaveAsync(PayoutBatch payoutBatch, CancellationToken ct)
    {
        var metadata = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(DynamoDbKeys.PayoutBatchPk(payoutBatch.Id)),
            ["SK"] = new(DynamoDbKeys.MetadataSk),
            ["Id"] = new(payoutBatch.Id.ToString()),
            ["GSI1PK"] = new(DynamoDbKeys.Gsi1Pk(payoutBatch.TeacherId)),
            ["GSI1SK"] = new(DynamoDbKeys.Gsi1SkPayoutBatch(payoutBatch.PaidAt ?? DateTimeOffset.UtcNow)),
            ["TeacherId"] = new(payoutBatch.TeacherId.ToString()),
            ["PeriodYyyyMm"] = new(payoutBatch.PeriodYyyyMm),
            ["NetReceivableTotal"] = AttributeValueExtensions.N(payoutBatch.NetReceivableTotal.Cents),
            ["TransferFee"] = AttributeValueExtensions.N(payoutBatch.TransferFee.Cents),
            ["NetPayout"] = AttributeValueExtensions.N(payoutBatch.NetPayout.Cents),
            ["Status"] = new(payoutBatch.Status.ToString()),
            ["PaidAt"] = AttributeValueExtensions.NullableS(payoutBatch.PaidAt?.ToString("O")),
            ["TransferReference"] = AttributeValueExtensions.NullableS(payoutBatch.TransferReference),
            ["BankRejectionReason"] = AttributeValueExtensions.NullableS(payoutBatch.BankRejectionReason),
        };

        await client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = metadata }, ct);

        foreach (var line in payoutBatch.OrderLines)
        {
            var lineItem = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new(DynamoDbKeys.PayoutBatchPk(payoutBatch.Id)),
                ["SK"] = new(DynamoDbKeys.PayoutBatchOrderSk(line.OrderId)),
                ["OrderId"] = new(line.OrderId.ToString()),
                ["NetReceivableSnapshot"] = AttributeValueExtensions.N(line.NetReceivableSnapshot.Cents),
            };
            await client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = lineItem }, ct);
        }
    }

    private static PayoutBatch Map(Dictionary<string, AttributeValue> metadata, IReadOnlyList<PayoutBatchOrderLine> lines) => PayoutBatch.Rehydrate(
        metadata.GetGuid("Id"), metadata.GetGuid("TeacherId"), metadata.GetS("PeriodYyyyMm"), lines,
        metadata.GetMoney("NetReceivableTotal"), metadata.GetMoney("TransferFee"), metadata.GetEnum<PayoutBatchStatus>("Status"),
        metadata.GetNullableDateTimeOffset("PaidAt"), metadata.GetNullableS("TransferReference"), metadata.GetNullableS("BankRejectionReason"));
}
