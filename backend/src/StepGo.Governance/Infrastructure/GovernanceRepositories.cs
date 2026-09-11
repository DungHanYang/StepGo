using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.Governance.Application;
using StepGo.Governance.Domain;
using StepGo.Shared.Infrastructure.Dynamo;

namespace StepGo.Governance.Infrastructure;

/// <summary>All fee-setting versions share one partition (low write volume by nature — a 30-day-notice change is rare); "current"/"latest" are resolved in-memory over that small set.</summary>
public sealed class PlatformFeeSettingRepository(IAmazonDynamoDB client, StepGoTableOptions options) : IPlatformFeeSettingRepository
{
    private const string Pk = "GOVERNANCE#FEESETTING";

    public async Task<PlatformFeeSetting?> GetCurrentAsync(CancellationToken ct)
    {
        var all = await QueryAllAsync(ct);
        var now = DateTimeOffset.UtcNow;
        return all.Where(s => s.IsEffectiveAt(now)).OrderByDescending(s => s.EffectiveDate).FirstOrDefault();
    }

    public async Task<PlatformFeeSetting?> GetLatestPublishedAsync(CancellationToken ct)
    {
        var all = await QueryAllAsync(ct);
        return all.OrderByDescending(s => s.ProposedAt).FirstOrDefault();
    }

    public async Task<PlatformFeeSetting?> GetByVersionAsync(string versionId, CancellationToken ct)
    {
        var response = await client.GetItemAsync(new GetItemRequest
        {
            TableName = options.TableName,
            Key = new() { ["PK"] = new(Pk), ["SK"] = new(versionId) },
        }, ct);

        return response.Item.Count == 0 ? null : Map(response.Item);
    }

    public Task SaveAsync(PlatformFeeSetting setting, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(Pk),
            ["SK"] = new(setting.Id),
            ["PlatformServiceFeeRate"] = new(setting.PlatformServiceFeeRate.ToString()),
            ["CrossBankTransferFee"] = AttributeValueExtensions.N(setting.CrossBankTransferFee.Cents),
            ["RefundFloorTiers"] = new AttributeValue
            {
                L = [.. setting.RefundFloor.Tiers.Select(t => new AttributeValue
                {
                    M = new() { ["Days"] = AttributeValueExtensions.N(t.DaysBeforeCourseStart), ["Percentage"] = new(t.RefundPercentage.ToString()) },
                })],
            },
            ["EffectiveDate"] = new(setting.EffectiveDate.ToString("O")),
            ["ProposedBy"] = new(setting.ProposedBy.ToString()),
            ["ProposedAt"] = new(setting.ProposedAt.ToString("O")),
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }

    private async Task<List<PlatformFeeSetting>> QueryAllAsync(CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new() { [":pk"] = new(Pk) },
        }, ct);

        return response.Items.Select(Map).ToList();
    }

    private static PlatformFeeSetting Map(Dictionary<string, AttributeValue> item)
    {
        var tiers = item["RefundFloorTiers"].L.Select(t => new RefundTier(int.Parse(t.M["Days"].N), decimal.Parse(t.M["Percentage"].S)));

        return PlatformFeeSetting.Rehydrate(
            item.GetS("SK"), decimal.Parse(item.GetS("PlatformServiceFeeRate")), item.GetMoney("CrossBankTransferFee"),
            new RefundRuleSet(tiers), item.GetDateTimeOffset("EffectiveDate"), item.GetGuid("ProposedBy"), item.GetDateTimeOffset("ProposedAt"));
    }
}

/// <summary>PK=GOVERNANCE#TERMS SK=&lt;versionNumber padded&gt; — versions are rare, so FindAsync(id) scans the small partition rather than needing a secondary index.</summary>
public sealed class TermsVersionRepository(IAmazonDynamoDB client, StepGoTableOptions options) : ITermsVersionRepository
{
    private const string Pk = "GOVERNANCE#TERMS";

    public async Task<TermsVersion?> GetLatestAsync(CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new() { [":pk"] = new(Pk) },
            ScanIndexForward = false,
            Limit = 1,
        }, ct);

        return response.Items.Count == 0 ? null : Map(response.Items[0]);
    }

    public async Task<TermsVersion?> FindAsync(Guid termsVersionId, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new() { [":pk"] = new(Pk) },
        }, ct);

        return response.Items.Select(Map).FirstOrDefault(t => t.Id == termsVersionId);
    }

    public Task SaveAsync(TermsVersion termsVersion, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(Pk),
            ["SK"] = new(termsVersion.VersionNumber.ToString("D10")),
            ["Id"] = new(termsVersion.Id.ToString()),
            ["VersionNumber"] = AttributeValueExtensions.N(termsVersion.VersionNumber),
            ["Content"] = new(termsVersion.Content),
            ["PublishedBy"] = new(termsVersion.PublishedBy.ToString()),
            ["PublishedAt"] = new(termsVersion.PublishedAt.ToString("O")),
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }

    private static TermsVersion Map(Dictionary<string, AttributeValue> item) => new(
        item.GetGuid("Id"), (int)item.GetN("VersionNumber"), item.GetS("Content"), item.GetGuid("PublishedBy"), item.GetDateTimeOffset("PublishedAt"));
}

public sealed class TeacherConsentRepository(IAmazonDynamoDB client, StepGoTableOptions options) : ITeacherConsentRepository
{
    public async Task<TeacherConsent?> FindLatestForTeacherAsync(Guid teacherId, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
            ExpressionAttributeValues = new() { [":pk"] = new(DynamoDbKeys.TeacherPk(teacherId)), [":prefix"] = new("CONSENT#") },
            ScanIndexForward = false,
            Limit = 1,
        }, ct);

        return response.Items.Count == 0 ? null : Map(response.Items[0]);
    }

    public Task SaveAsync(TeacherConsent consent, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(DynamoDbKeys.TeacherPk(consent.TeacherId)),
            ["SK"] = new($"CONSENT#{consent.ConsentedAt:yyyyMMddHHmmssfff}"),
            ["TeacherId"] = new(consent.TeacherId.ToString()),
            ["TermsVersionId"] = new(consent.TermsVersionId.ToString()),
            ["ConsentedAt"] = new(consent.ConsentedAt.ToString("O")),
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }

    private static TeacherConsent Map(Dictionary<string, AttributeValue> item) => new(
        item.GetGuid("TeacherId"), item.GetGuid("TermsVersionId"), item.GetDateTimeOffset("ConsentedAt"));
}

public sealed class ChangeLogRepository(IAmazonDynamoDB client, StepGoTableOptions options) : IChangeLogRepository
{
    public Task AppendAsync(ChangeLogEntry entry, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(DynamoDbKeys.AuditLogPk(entry.Category.ToString())),
            ["SK"] = new(DynamoDbKeys.AuditLogSk(entry.OccurredAt, entry.Id)),
            ["Id"] = new(entry.Id.ToString()),
            ["Category"] = new(entry.Category.ToString()),
            ["Description"] = new(entry.Description),
            ["OperatorId"] = new(entry.OperatorId.ToString()),
            ["OccurredAt"] = new(entry.OccurredAt.ToString("O")),
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }

    public async Task<IReadOnlyList<ChangeLogEntry>> ListByCategoryAsync(ChangeLogCategory category, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new() { [":pk"] = new(DynamoDbKeys.AuditLogPk(category.ToString())) },
            ScanIndexForward = false,
        }, ct);

        return response.Items.Select(i => new ChangeLogEntry(
            i.GetGuid("Id"), i.GetEnum<ChangeLogCategory>("Category"), i.GetS("Description"), i.GetGuid("OperatorId"), i.GetDateTimeOffset("OccurredAt"))).ToList();
    }
}
