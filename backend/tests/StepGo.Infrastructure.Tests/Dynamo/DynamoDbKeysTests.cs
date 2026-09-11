using StepGo.Shared.Infrastructure.Dynamo;

namespace StepGo.Infrastructure.Tests.Dynamo;

public class DynamoDbKeysTests
{
    [Fact]
    public void UserPk_And_TeacherPk_ShareTheSameIdButDifferentPartitions()
    {
        var id = Guid.NewGuid();

        Assert.Equal($"USER#{id}", DynamoDbKeys.UserPk(id));
        Assert.Equal($"TEACHER#{id}", DynamoDbKeys.TeacherPk(id));
        Assert.NotEqual(DynamoDbKeys.UserPk(id), DynamoDbKeys.TeacherPk(id));
    }

    [Fact]
    public void Gsi1SkOrder_SortsChronologicallyAsStrings()
    {
        var orderId = Guid.NewGuid();
        var earlier = DynamoDbKeys.Gsi1SkOrder(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), orderId);
        var later = DynamoDbKeys.Gsi1SkOrder(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), orderId);

        Assert.True(string.CompareOrdinal(earlier, later) < 0);
    }

    [Fact]
    public void RefundTicketSk_IsFixed_GuaranteeingOneTicketPerOrder()
    {
        Assert.Equal("REFUNDTICKET", DynamoDbKeys.RefundTicketSk);
    }

    [Fact]
    public void PayoutBatchOrderSk_EmbedsOrderId()
    {
        var orderId = Guid.NewGuid();

        Assert.Equal($"ORDER#{orderId}", DynamoDbKeys.PayoutBatchOrderSk(orderId));
    }
}
