using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using StepGo.Orders.Application;
using StepGo.Courses.Domain;
using StepGo.Orders.Domain;
using StepGo.Shared.Domain;
using StepGo.Shared.Infrastructure.Dynamo;

namespace StepGo.Orders.Infrastructure;

/// <summary>
/// Task 1.4/decision 4's transactional-consistency rule for the payment-confirmed path: order item +
/// teacher monthly summary + platform summary are written together via TransactWriteItems. Every other
/// save (order created, overdue, refunded, payout-batch bookkeeping) is a plain single-item PutItem —
/// those don't touch the summary aggregates.
/// </summary>
public sealed class OrderRepository(IAmazonDynamoDB client, StepGoTableOptions options, DynamoTransactionWriter transactionWriter) : IOrderRepository
{
    public async Task<Order?> FindAsync(Guid orderId, CancellationToken ct)
    {
        var response = await client.GetItemAsync(new GetItemRequest
        {
            TableName = options.TableName,
            Key = new() { ["PK"] = new(DynamoDbKeys.OrderPk(orderId)), ["SK"] = new(DynamoDbKeys.MetadataSk) },
        }, ct);

        return response.Item.Count == 0 ? null : Map(response.Item);
    }

    public async Task<IReadOnlyList<Order>> ListByStudentAsync(Guid studentId, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            IndexName = "GSI4",
            KeyConditionExpression = "GSI4PK = :pk",
            ExpressionAttributeValues = new() { [":pk"] = new(DynamoDbKeys.Gsi4Pk(studentId)) },
        }, ct);

        return response.Items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<Order>> ListByTeacherAsync(Guid teacherId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var orders = await QueryTeacherOrdersAsync(teacherId, ct);
        return orders.Where(o => (from is null || o.CreatedAt >= from) && (to is null || o.CreatedAt <= to)).ToList();
    }

    public async Task<IReadOnlyList<Order>> ListPendingPayoutByTeacherAsync(Guid teacherId, CancellationToken ct)
    {
        var orders = await QueryTeacherOrdersAsync(teacherId, ct);
        return orders.Where(o => o.PayoutStatus == OrderPayoutStatus.PendingPayout).ToList();
    }

    /// <summary>
    /// MVP-scale implementation: scans for distinct teachers with a pending-payout order. Acceptable at
    /// MVP order volume; if this becomes a bottleneck, add a dedicated GSI keyed by PayoutStatus.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> ListTeacherIdsWithPendingPayoutAsync(CancellationToken ct)
    {
        var response = await client.ScanAsync(new ScanRequest
        {
            TableName = options.TableName,
            FilterExpression = "SK = :sk AND PayoutStatus = :status",
            ExpressionAttributeValues = new()
            {
                [":sk"] = new(DynamoDbKeys.MetadataSk),
                [":status"] = new(OrderPayoutStatus.PendingPayout.ToString()),
            },
            ProjectionExpression = "TeacherId",
        }, ct);

        return response.Items.Select(i => i.GetGuid("TeacherId")).Distinct().ToList();
    }

    /// <summary>Same MVP-scale Scan trade-off as ListTeacherIdsWithPendingPayoutAsync above.</summary>
    public async Task<IReadOnlyList<Guid>> ListOverdueCandidateOrderIdsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var response = await client.ScanAsync(new ScanRequest
        {
            TableName = options.TableName,
            FilterExpression = "SK = :sk AND PaymentStatus = :status AND AtmPaymentDueAt < :now",
            ExpressionAttributeValues = new()
            {
                [":sk"] = new(DynamoDbKeys.MetadataSk),
                [":status"] = new(OrderPaymentStatus.PendingPayment.ToString()),
                [":now"] = new(now.ToString("O")),
            },
            ProjectionExpression = "Id",
        }, ct);

        return response.Items.Select(i => i.GetGuid("Id")).ToList();
    }

    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        var justConfirmedPayment = order.DomainEvents.OfType<OrderPaymentConfirmedEvent>().Any();
        var item = BuildItem(order);

        if (!justConfirmedPayment)
        {
            await client.PutItemAsync(new PutItemRequest { TableName = options.TableName, Item = item }, ct);
            return;
        }

        var yyyyMm = order.CreatedAt.ToString("yyyyMM");
        var netReceivable = order.NetReceivable ?? Money.Zero;

        var succeeded = await transactionWriter.TryWriteAsync(
        [
            new TransactWriteItem { Put = new Put { TableName = options.TableName, Item = item } },
            new TransactWriteItem
            {
                Update = new Update
                {
                    TableName = options.TableName,
                    Key = new() { ["PK"] = new(DynamoDbKeys.TeacherPk(order.TeacherId)), ["SK"] = new(DynamoDbKeys.TeacherSummarySk(yyyyMm)) },
                    UpdateExpression = "ADD TotalNetReceivable :net, OrderCount :one",
                    ExpressionAttributeValues = new() { [":net"] = AttributeValueExtensions.N(netReceivable.Cents), [":one"] = AttributeValueExtensions.N(1) },
                },
            },
            new TransactWriteItem
            {
                Update = new Update
                {
                    TableName = options.TableName,
                    Key = new() { ["PK"] = new(DynamoDbKeys.PlatformAggregatePk), ["SK"] = new(DynamoDbKeys.PlatformSummarySk) },
                    UpdateExpression = "ADD TotalNetReceivable :net, OrderCount :one",
                    ExpressionAttributeValues = new() { [":net"] = AttributeValueExtensions.N(netReceivable.Cents), [":one"] = AttributeValueExtensions.N(1) },
                },
            },
        ], ct);

        if (!succeeded)
        {
            throw new DomainException("order_save_transaction_failed", "訂單付款確認的交易寫入失敗，請重試。");
        }
    }

    private async Task<List<Order>> QueryTeacherOrdersAsync(Guid teacherId, CancellationToken ct)
    {
        var response = await client.QueryAsync(new QueryRequest
        {
            TableName = options.TableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :prefix)",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new(DynamoDbKeys.Gsi1Pk(teacherId)),
                [":prefix"] = new("ORDER#"),
            },
        }, ct);

        return response.Items.Select(Map).ToList();
    }

    private static Dictionary<string, AttributeValue> BuildItem(Order order)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new(DynamoDbKeys.OrderPk(order.Id)),
            ["SK"] = new(DynamoDbKeys.MetadataSk),
            ["Id"] = new(order.Id.ToString()),
            ["GSI1PK"] = new(DynamoDbKeys.Gsi1Pk(order.TeacherId)),
            ["GSI1SK"] = new(DynamoDbKeys.Gsi1SkOrder(order.CreatedAt, order.Id)),
            ["GSI4PK"] = new(DynamoDbKeys.Gsi4Pk(order.StudentId)),
            ["GSI4SK"] = new(DynamoDbKeys.Gsi4SkOrder(order.CreatedAt)),
            ["CourseId"] = new(order.CourseId.ToString()),
            ["TeacherId"] = new(order.TeacherId.ToString()),
            ["StudentId"] = new(order.StudentId.ToString()),
            ["CoursePriceAtOrder"] = AttributeValueExtensions.N(order.CoursePriceAtOrder.Cents),
            ["RequestedPaymentMethod"] = AttributeValueExtensions.N((int)order.RequestedPaymentMethod),
            ["GatewayChoosePayment"] = new(order.GatewayChoosePayment.ToString()),
            ["PaymentMethodUsed"] = order.PaymentMethodUsed is null ? new AttributeValue { NULL = true } : AttributeValueExtensions.N((int)order.PaymentMethodUsed.Value),
            ["PaymentStatus"] = new(order.PaymentStatus.ToString()),
            ["PayoutStatus"] = new(order.PayoutStatus.ToString()),
            ["PayoutBatchId"] = AttributeValueExtensions.NullableS(order.PayoutBatchId?.ToString()),
            ["AtmPaymentDueAt"] = AttributeValueExtensions.NullableS(order.AtmPaymentDueAt?.ToString("O")),
            ["FeeScheduleVersionId"] = AttributeValueExtensions.NullableS(order.FeeScheduleVersionId),
            ["GatewayFee"] = order.GatewayFee is null ? new AttributeValue { NULL = true } : AttributeValueExtensions.N(order.GatewayFee.Value.Cents),
            ["PlatformServiceFee"] = order.PlatformServiceFee is null ? new AttributeValue { NULL = true } : AttributeValueExtensions.N(order.PlatformServiceFee.Value.Cents),
            ["NetReceivable"] = order.NetReceivable is null ? new AttributeValue { NULL = true } : AttributeValueExtensions.N(order.NetReceivable.Value.Cents),
            ["CreatedAt"] = new(order.CreatedAt.ToString("O")),
        };
        return item;
    }

    private static Order Map(Dictionary<string, AttributeValue> item) => Order.Rehydrate(
        item.GetGuid("Id"), item.GetGuid("CourseId"), item.GetGuid("TeacherId"), item.GetGuid("StudentId"), item.GetMoney("CoursePriceAtOrder"),
        (PaymentMethod)item.GetN("RequestedPaymentMethod"), item.GetEnum<ChoosePayment>("GatewayChoosePayment"),
        item.GetNullableN("PaymentMethodUsed") is { } m ? (PaymentMethod)m : null,
        item.GetEnum<OrderPaymentStatus>("PaymentStatus"), item.GetEnum<OrderPayoutStatus>("PayoutStatus"),
        item.GetNullableGuid("PayoutBatchId"), item.GetNullableDateTimeOffset("AtmPaymentDueAt"), item.GetNullableS("FeeScheduleVersionId"),
        item.GetNullableMoney("GatewayFee"), item.GetNullableMoney("PlatformServiceFee"), item.GetNullableMoney("NetReceivable"),
        item.GetDateTimeOffset("CreatedAt"));
}
