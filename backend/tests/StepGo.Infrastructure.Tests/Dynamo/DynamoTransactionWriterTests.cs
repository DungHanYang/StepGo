using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Moq;
using StepGo.Shared.Infrastructure.Dynamo;

namespace StepGo.Infrastructure.Tests.Dynamo;

public class DynamoTransactionWriterTests
{
    private static TransactWriteItem DummyItem() => new()
    {
        Put = new Put { TableName = "StepGoTable", Item = new Dictionary<string, AttributeValue> { ["PK"] = new("X") } },
    };

    [Fact]
    public async Task TryWriteAsync_OnSuccess_ReturnsTrue()
    {
        var client = new Mock<IAmazonDynamoDB>();
        client.Setup(c => c.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactWriteItemsResponse());

        var writer = new DynamoTransactionWriter(client.Object);
        var result = await writer.TryWriteAsync([DummyItem()], CancellationToken.None);

        Assert.True(result);
        client.Verify(c => c.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryWriteAsync_OnConditionalCheckFailed_ReturnsFalseWithoutRetry()
    {
        var client = new Mock<IAmazonDynamoDB>();
        client.Setup(c => c.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConditionalCheckFailedException("already exists"));

        var writer = new DynamoTransactionWriter(client.Object);
        var result = await writer.TryWriteAsync([DummyItem()], CancellationToken.None);

        Assert.False(result);
        client.Verify(c => c.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryWriteAsync_OnTransactionConflict_RetriesThenSucceeds()
    {
        var client = new Mock<IAmazonDynamoDB>();
        var callCount = 0;
        client.Setup(c => c.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    throw new TransactionConflictException("hot item contention");
                }
                return new TransactWriteItemsResponse();
            });

        var writer = new DynamoTransactionWriter(client.Object);
        var result = await writer.TryWriteAsync([DummyItem()], CancellationToken.None);

        Assert.True(result);
        Assert.Equal(2, callCount);
    }
}
