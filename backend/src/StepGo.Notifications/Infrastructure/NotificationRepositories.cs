using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.Notifications.Application;
using StepGo.Notifications.Domain;
using StepGo.Shared.Infrastructure.Dynamo;

namespace StepGo.Notifications.Infrastructure;

public sealed class NotificationTemplateRepository(IAmazonDynamoDB client, StepGoTableOptions options) : INotificationTemplateRepository
{
    private const string Pk = "NOTIFICATIONTEMPLATE";

    public async Task<NotificationTemplate?> FindAsync(string key, CancellationToken ct)
    {
        var response = await client.GetItemAsync(new GetItemRequest
        {
            TableName = options.TableName,
            Key = new() { ["PK"] = new(Pk), ["SK"] = new(key) },
        }, ct);

        return response.Item.Count == 0 ? null : NotificationTemplate.Rehydrate(
            key, response.Item.GetS("BodyTemplate"), response.Item.GetGuid("UpdatedBy"), response.Item.GetDateTimeOffset("UpdatedAt"));
    }

    public Task SaveAsync(NotificationTemplate template, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(Pk),
            ["SK"] = new(template.Id),
            ["BodyTemplate"] = new(template.BodyTemplate),
            ["UpdatedBy"] = new(template.UpdatedBy.ToString()),
            ["UpdatedAt"] = new(template.UpdatedAt.ToString("O")),
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }
}

public sealed class NotificationRecordRepository(IAmazonDynamoDB client, StepGoTableOptions options) : INotificationRecordRepository
{
    public async Task<IReadOnlyList<NotificationRecord>> ListByUserAsync(Guid userId, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
            ExpressionAttributeValues = new() { [":pk"] = new(DynamoDbKeys.UserPk(userId)), [":prefix"] = new("NOTIFICATION#") },
            ScanIndexForward = false,
        }, ct);

        return response.Items.Select(Map).ToList();
    }

    public Task SaveAsync(NotificationRecord record, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(DynamoDbKeys.UserPk(record.UserId)),
            ["SK"] = new($"NOTIFICATION#{record.SentAt:yyyyMMddHHmmssfff}#{record.Id}"),
            ["Id"] = new(record.Id.ToString()),
            ["UserId"] = new(record.UserId.ToString()),
            ["TemplateKey"] = new(record.TemplateKey),
            ["RenderedText"] = new(record.RenderedText),
            ["DispatchedChannels"] = AttributeValueExtensions.N((int)record.DispatchedChannels),
            ["SentAt"] = new(record.SentAt.ToString("O")),
            ["IsRead"] = AttributeValueExtensions.Bool(record.IsRead),
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }

    private static NotificationRecord Map(Dictionary<string, AttributeValue> item)
    {
        var record = new NotificationRecord(
            item.GetGuid("Id"), item.GetGuid("UserId"), item.GetS("TemplateKey"), item.GetS("RenderedText"),
            (NotificationChannel)item.GetN("DispatchedChannels"), item.GetDateTimeOffset("SentAt"));
        if (item.GetBool("IsRead"))
        {
            record.MarkRead();
        }
        return record;
    }
}
