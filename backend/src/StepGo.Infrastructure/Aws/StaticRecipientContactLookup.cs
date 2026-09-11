using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.Application.Notifications;
using StepGo.Infrastructure.Dynamo;

namespace StepGo.Infrastructure.Aws;

/// <summary>Reads the recipient's Email off the UserAccount item; LINE binding is not yet modeled (LINE channel mocked per proposal.md) so it always resolves to unbound.</summary>
public sealed class StaticRecipientContactLookup(IAmazonDynamoDB client, StepGoTableOptions options) : IRecipientContactLookup
{
    public async Task<(string? Email, string? LineUserId)> GetContactAsync(Guid userId, CancellationToken ct)
    {
        var response = await client.GetItemAsync(new GetItemRequest
        {
            TableName = options.TableName,
            Key = new() { ["PK"] = new(DynamoDbKeys.UserPk(userId)), ["SK"] = new(DynamoDbKeys.MetadataSk) },
        }, ct);

        return response.Item.Count == 0 ? (null, null) : (response.Item.GetNullableS("Email"), LineUserId: null);
    }
}
