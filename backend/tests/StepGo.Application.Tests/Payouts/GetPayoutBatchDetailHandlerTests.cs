using Moq;
using StepGo.Application.Common;
using StepGo.Application.Payouts;
using StepGo.Domain.Identity;
using StepGo.Domain.Payouts;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Tests.Payouts;

file sealed class FakeCurrentUser(Guid userId, Role role) : ICurrentUserAccessor
{
    public Guid UserId { get; } = userId;
    public Role Role { get; } = role;
}

/// <summary>Task 6.3: the detail response carries every traceable field — covered order lines, net total, transfer fee, and take-home amount.</summary>
public class GetPayoutBatchDetailHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsFullBatchDetail()
    {
        var teacherId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lines = new[] { new PayoutBatchOrderLine(orderId, Money.FromWholeDollars(1000)) };
        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), teacherId, "2026-09", lines, Money.FromWholeDollars(20))!;

        var batchRepo = new Mock<IPayoutBatchRepository>();
        batchRepo.Setup(r => r.FindAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

        var handler = new GetPayoutBatchDetailHandler(batchRepo.Object);
        var result = await handler.HandleAsync(batch.Id, new FakeCurrentUser(teacherId, Role.Teacher), CancellationToken.None);

        Assert.Single(result.OrderLines);
        Assert.Equal(orderId, result.OrderLines[0].OrderId);
        Assert.Equal(1000, result.NetReceivableTotal.Cents);
        Assert.Equal(20, result.TransferFee.Cents);
        Assert.Equal(980, result.NetPayout.Cents);
    }

    [Fact]
    public async Task HandleAsync_AnotherTeacherRequesting_Throws()
    {
        var teacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), teacherId, "2026-09", [new PayoutBatchOrderLine(Guid.NewGuid(), Money.FromWholeDollars(1000))], Money.Zero)!;

        var batchRepo = new Mock<IPayoutBatchRepository>();
        batchRepo.Setup(r => r.FindAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

        var handler = new GetPayoutBatchDetailHandler(batchRepo.Object);

        await Assert.ThrowsAsync<AuthorizationException>(() => handler.HandleAsync(batch.Id, new FakeCurrentUser(otherTeacherId, Role.Teacher), CancellationToken.None));
    }
}
