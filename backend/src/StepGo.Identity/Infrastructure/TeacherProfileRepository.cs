using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.Identity.Application;
using StepGo.Identity.Domain;
using StepGo.Shared.Infrastructure.Dynamo;

namespace StepGo.Identity.Infrastructure;

/// <summary>NationalId is encrypted at rest via the DynamoDB table's KMS-backed encryption; a dedicated field-level cipher is a later hardening step (not required for the MVP scaffold's local dev/sandbox flow).</summary>
public sealed class TeacherProfileRepository(IAmazonDynamoDB client, StepGoTableOptions options) : ITeacherProfileRepository
{
    public async Task<TeacherProfile?> FindAsync(Guid teacherId, CancellationToken ct)
    {
        var response = await client.GetItemAsync(new GetItemRequest
        {
            TableName = options.TableName,
            Key = new() { ["PK"] = new(DynamoDbKeys.TeacherPk(teacherId)), ["SK"] = new(DynamoDbKeys.MetadataSk) },
        }, ct);

        return response.Item.Count == 0 ? null : Map(teacherId, response.Item);
    }

    public Task SaveAsync(TeacherProfile teacherProfile, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(DynamoDbKeys.TeacherPk(teacherProfile.Id)),
            ["SK"] = new(DynamoDbKeys.MetadataSk),
            ["RealName"] = new(teacherProfile.RealName),
            ["NationalId"] = new(teacherProfile.NationalId),
            ["IdPhotoFrontObjectKey"] = new(teacherProfile.IdPhotoFrontObjectKey),
            ["IdPhotoBackObjectKey"] = new(teacherProfile.IdPhotoBackObjectKey),
            ["PayoutBankCode"] = new(teacherProfile.PayoutAccount.BankCode),
            ["PayoutAccountNumber"] = new(teacherProfile.PayoutAccount.AccountNumber),
            ["PayoutAccountHolderName"] = new(teacherProfile.PayoutAccount.AccountHolderName),
            ["VerificationStatus"] = new(teacherProfile.VerificationStatus.ToString()),
            ["RejectionReason"] = AttributeValueExtensions.NullableS(teacherProfile.RejectionReason),
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }

    private static TeacherProfile Map(Guid teacherId, Dictionary<string, AttributeValue> item) => TeacherProfile.Rehydrate(
        teacherId, item.GetS("RealName"), item.GetS("NationalId"), item.GetS("IdPhotoFrontObjectKey"), item.GetS("IdPhotoBackObjectKey"),
        new BankAccount(item.GetS("PayoutBankCode"), item.GetS("PayoutAccountNumber"), item.GetS("PayoutAccountHolderName")),
        item.GetEnum<TeacherVerificationStatus>("VerificationStatus"), item.GetNullableS("RejectionReason"));
}
