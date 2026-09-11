using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.RefundTickets.Application;
using StepGo.Identity.Domain;
using StepGo.Orders.Domain;
using StepGo.RefundTickets.Domain;
using StepGo.Shared.Infrastructure.Dynamo;

namespace StepGo.RefundTickets.Infrastructure;

/// <summary>
/// PK=ORDER#&lt;orderId&gt; SK=REFUNDTICKET (fixed) — the ticket's aggregate id equals its order id
/// (see SubmitRefundTicketHandler), so FindAsync and FindByOrderAsync are the same GetItem.
/// </summary>
public sealed class RefundTicketRepository(IAmazonDynamoDB client, StepGoTableOptions options) : IRefundTicketRepository
{
    public Task<RefundTicket?> FindAsync(Guid ticketId, CancellationToken ct) => FindByOrderAsync(ticketId, ct);

    public async Task<RefundTicket?> FindByOrderAsync(Guid orderId, CancellationToken ct)
    {
        var response = await client.GetItemAsync(new GetItemRequest
        {
            TableName = options.TableName,
            Key = new() { ["PK"] = new(DynamoDbKeys.OrderPk(orderId)), ["SK"] = new(DynamoDbKeys.RefundTicketSk) },
        }, ct);

        return response.Item.Count == 0 ? null : Map(response.Item);
    }

    public async Task<IReadOnlyList<RefundTicket>> ListPendingByTeacherAsync(Guid teacherId, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            IndexName = "GSI3",
            KeyConditionExpression = "GSI3PK = :pk",
            ExpressionAttributeValues = new() { [":pk"] = new(DynamoDbKeys.Gsi3Pk(teacherId)) },
        }, ct);

        return response.Items.Select(Map).Where(t => t.Status == RefundTicketStatus.TeacherReviewing).ToList();
    }

    public async Task<IReadOnlyList<RefundTicket>> ListByStatusAsync(RefundTicketStatus status, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            IndexName = "GSI2",
            KeyConditionExpression = "GSI2PK = :pk",
            ExpressionAttributeValues = new() { [":pk"] = new(DynamoDbKeys.Gsi2Pk(status.ToString())) },
        }, ct);

        return response.Items.Select(Map).ToList();
    }

    public Task SaveAsync(RefundTicket ticket, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(DynamoDbKeys.OrderPk(ticket.OrderId)),
            ["SK"] = new(DynamoDbKeys.RefundTicketSk),
            ["Id"] = new(ticket.Id.ToString()),
            ["OrderId"] = new(ticket.OrderId.ToString()),
            ["StudentId"] = new(ticket.StudentId.ToString()),
            ["TeacherId"] = new(ticket.TeacherId.ToString()),
            ["GSI2PK"] = new(DynamoDbKeys.Gsi2Pk(ticket.Status.ToString())),
            ["GSI2SK"] = new(DynamoDbKeys.SlaSortKey(ticket.TeacherSlaDeadline, ticket.Id)),
            ["GSI3PK"] = new(DynamoDbKeys.Gsi3Pk(ticket.TeacherId)),
            ["GSI3SK"] = new(DynamoDbKeys.SlaSortKey(ticket.TeacherSlaDeadline, ticket.Id)),
            ["OriginalPaidAmount"] = AttributeValueExtensions.N(ticket.OriginalPaidAmount.Cents),
            ["EligibleRefundAmount"] = AttributeValueExtensions.N(ticket.EligibleRefundAmount.Cents),
            ["Status"] = new(ticket.Status.ToString()),
            ["TeacherSlaDeadline"] = AttributeValueExtensions.NullableS(ticket.TeacherSlaDeadline?.ToString("O")),
            ["RejectionReason"] = AttributeValueExtensions.NullableS(ticket.RejectionReason),
            ["DecidedAmount"] = ticket.DecidedAmount is null ? new AttributeValue { NULL = true } : AttributeValueExtensions.N(ticket.DecidedAmount.Value.Cents),
            ["DecidedDeductionSource"] = AttributeValueExtensions.NullableS(ticket.DecidedDeductionSource?.ToString()),
            ["RefundExecutionRoute"] = AttributeValueExtensions.NullableS(ticket.RefundExecutionRoute?.ToString()),
            ["ManualTransferReference"] = AttributeValueExtensions.NullableS(ticket.ManualTransferReference),
            ["ManualTransferCompletedAt"] = AttributeValueExtensions.NullableS(ticket.ManualTransferCompletedAt?.ToString("O")),
            ["Thread"] = new AttributeValue
            {
                L = [.. ticket.Thread.Select(m => new AttributeValue
                {
                    M = new()
                    {
                        ["AuthorRole"] = new(m.AuthorRole.ToString()),
                        ["AuthorId"] = new(m.AuthorId.ToString()),
                        ["Text"] = new(m.Text),
                        ["CreatedAt"] = new(m.CreatedAt.ToString("O")),
                    },
                })],
            },
            ["InternalNotes"] = new AttributeValue
            {
                L = [.. ticket.InternalNotes.Select(n => new AttributeValue
                {
                    M = new()
                    {
                        ["AdminId"] = new(n.AdminId.ToString()),
                        ["Text"] = new(n.Text),
                        ["CreatedAt"] = new(n.CreatedAt.ToString("O")),
                    },
                })],
            },
        };

        return client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
    }

    private static RefundTicket Map(Dictionary<string, AttributeValue> item)
    {
        var thread = item["Thread"].L.Select(m => new ThreadMessage(
            Enum.Parse<Role>(m.M["AuthorRole"].S), Guid.Parse(m.M["AuthorId"].S), m.M["Text"].S, DateTimeOffset.Parse(m.M["CreatedAt"].S)));

        var notes = item["InternalNotes"].L.Select(n => new InternalNote(
            Guid.Parse(n.M["AdminId"].S), n.M["Text"].S, DateTimeOffset.Parse(n.M["CreatedAt"].S)));

        return RefundTicket.Rehydrate(
            item.GetGuid("Id"), item.GetGuid("OrderId"), item.GetGuid("StudentId"), item.GetGuid("TeacherId"),
            item.GetMoney("OriginalPaidAmount"), item.GetMoney("EligibleRefundAmount"), item.GetEnum<RefundTicketStatus>("Status"),
            item.GetNullableDateTimeOffset("TeacherSlaDeadline"), item.GetNullableS("RejectionReason"), item.GetNullableMoney("DecidedAmount"),
            item.GetNullableEnum<DeductionSource>("DecidedDeductionSource"), item.GetNullableEnum<RefundRoute>("RefundExecutionRoute"),
            item.GetNullableS("ManualTransferReference"), item.GetNullableDateTimeOffset("ManualTransferCompletedAt"), thread, notes);
    }
}
