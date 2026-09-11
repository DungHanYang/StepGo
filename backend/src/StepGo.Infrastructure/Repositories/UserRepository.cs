using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.Application.Identity;
using StepGo.Domain.Identity;
using StepGo.Infrastructure.Dynamo;

namespace StepGo.Infrastructure.Repositories;

public sealed class UserRepository(IAmazonDynamoDB client, StepGoTableOptions options) : IUserRepository
{
    public async Task<UserAccount?> FindAsync(Guid userId, CancellationToken ct)
    {
        var response = await client.GetItemAsync(new GetItemRequest
        {
            TableName = options.TableName,
            Key = new() { ["PK"] = new(DynamoDbKeys.UserPk(userId)), ["SK"] = new(DynamoDbKeys.MetadataSk) },
        }, ct);

        return response.Item.Count == 0 ? null : Map(response.Item);
    }

    public Task SaveAsync(UserAccount user, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(DynamoDbKeys.UserPk(user.Id)),
            ["SK"] = new(DynamoDbKeys.MetadataSk),
            ["Id"] = new(user.Id.ToString()),
            ["FullName"] = new(user.FullName),
            ["PhoneNumber"] = new(user.PhoneNumber),
            ["Email"] = AttributeValueExtensions.NullableS(user.Email),
            ["Role"] = new(user.Role.ToString()),
            ["CreatedAt"] = new(user.CreatedAt.ToString("O")),
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }

    private static UserAccount Map(Dictionary<string, AttributeValue> item) => UserAccount.Rehydrate(
        item.GetGuid("Id"), item.GetS("FullName"), item.GetS("PhoneNumber"), item.GetNullableS("Email"),
        item.GetEnum<Role>("Role"), item.GetDateTimeOffset("CreatedAt"));
}
