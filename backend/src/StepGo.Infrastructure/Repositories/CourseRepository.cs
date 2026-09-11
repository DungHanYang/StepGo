using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.Application.Courses;
using StepGo.Domain.Courses;
using StepGo.Domain.SharedKernel;
using StepGo.Infrastructure.Dynamo;

namespace StepGo.Infrastructure.Repositories;

public sealed class CourseRepository(IAmazonDynamoDB client, StepGoTableOptions options) : ICourseRepository
{
    public async Task<Course?> FindAsync(Guid courseId, CancellationToken ct)
    {
        var response = await client.GetItemAsync(new GetItemRequest
        {
            TableName = options.TableName,
            Key = new() { ["PK"] = new(DynamoDbKeys.CoursePk(courseId)), ["SK"] = new(DynamoDbKeys.MetadataSk) },
        }, ct);

        return response.Item.Count == 0 ? null : Map(response.Item);
    }

    public async Task<IReadOnlyList<Course>> ListByTeacherAsync(Guid teacherId, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :prefix)",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new(DynamoDbKeys.Gsi1Pk(teacherId)),
                [":prefix"] = new("COURSE#"),
            },
        }, ct);

        return response.Items.Select(Map).ToList();
    }

    public Task SaveAsync(Course course, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(DynamoDbKeys.CoursePk(course.Id)),
            ["SK"] = new(DynamoDbKeys.MetadataSk),
            ["Id"] = new(course.Id.ToString()),
            ["GSI1PK"] = new(DynamoDbKeys.Gsi1Pk(course.TeacherId)),
            ["GSI1SK"] = new(DynamoDbKeys.Gsi1SkCourse(course.Id)),
            ["TeacherId"] = new(course.TeacherId.ToString()),
            ["Title"] = new(course.Title),
            ["Price"] = AttributeValueExtensions.N(course.Price.Cents),
            ["AcceptedPaymentMethods"] = AttributeValueExtensions.N((int)course.AcceptedPaymentMethods),
            ["RefundTiers"] = new AttributeValue
            {
                L = [.. course.RefundRules.Tiers.Select(t => new AttributeValue
                {
                    M = new()
                    {
                        ["Days"] = AttributeValueExtensions.N(t.DaysBeforeCourseStart),
                        ["Percentage"] = new(t.RefundPercentage.ToString()),
                    },
                })],
            },
            ["Status"] = new(course.Status.ToString()),
            ["StartsAt"] = new(course.StartsAt.ToString("O")),
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }

    private static Course Map(Dictionary<string, AttributeValue> item)
    {
        var tiers = item["RefundTiers"].L.Select(t => new RefundTier(
            int.Parse(t.M["Days"].N), decimal.Parse(t.M["Percentage"].S)));

        return Course.Rehydrate(
            item.GetGuid("Id"), item.GetGuid("TeacherId"), item.GetS("Title"), item.GetMoney("Price"),
            (PaymentMethod)item.GetN("AcceptedPaymentMethods"), new RefundRuleSet(tiers),
            item.GetEnum<CourseStatus>("Status"), item.GetDateTimeOffset("StartsAt"));
    }
}
