using StepGo.Domain.Payouts;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Tests.Payouts;

public class PayoutBatchTests
{
    [Fact]
    public void TryDraft_BelowMinimumThreshold_ReturnsNull()
    {
        var lines = new[] { new PayoutBatchOrderLine(Guid.NewGuid(), Money.FromWholeDollars(300)) };

        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), Guid.NewGuid(), "2026-09", lines, Money.Zero);

        Assert.Null(batch);
    }

    [Fact]
    public void TryDraft_AtOrAboveMinimumThreshold_CreatesDraftBatch()
    {
        var lines = new[] { new PayoutBatchOrderLine(Guid.NewGuid(), Money.FromWholeDollars(500)) };

        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), Guid.NewGuid(), "2026-09", lines, Money.Zero);

        Assert.NotNull(batch);
        Assert.Equal(PayoutBatchStatus.Draft, batch!.Status);
        Assert.Equal(500, batch.NetPayout.Cents);
    }

    [Fact]
    public void TryDraft_DeductsTransferFeeFromNetPayout()
    {
        var lines = new[] { new PayoutBatchOrderLine(Guid.NewGuid(), Money.FromWholeDollars(1000)) };

        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), Guid.NewGuid(), "2026-09", lines, Money.FromWholeDollars(20));

        Assert.Equal(980, batch!.NetPayout.Cents);
    }

    [Fact]
    public void GuardPayoutAccountNameMatches_WhenMismatched_Throws()
    {
        var lines = new[] { new PayoutBatchOrderLine(Guid.NewGuid(), Money.FromWholeDollars(500)) };
        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), Guid.NewGuid(), "2026-09", lines, Money.Zero)!;

        var ex = Assert.Throws<DomainException>(() => batch.GuardPayoutAccountNameMatches("陳小華", "王小明"));
        Assert.Equal("payout_account_name_mismatch", ex.Code);
    }

    [Fact]
    public void MarkBankRejected_WithoutReason_Throws()
    {
        var lines = new[] { new PayoutBatchOrderLine(Guid.NewGuid(), Money.FromWholeDollars(500)) };
        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), Guid.NewGuid(), "2026-09", lines, Money.Zero)!;

        Assert.Throws<DomainException>(() => batch.MarkBankRejected(""));
    }

    [Fact]
    public void MarkBankRejected_SetsStatusAndReason()
    {
        var lines = new[] { new PayoutBatchOrderLine(Guid.NewGuid(), Money.FromWholeDollars(500)) };
        var batch = PayoutBatch.TryDraft(Guid.NewGuid(), Guid.NewGuid(), "2026-09", lines, Money.Zero)!;

        batch.MarkBankRejected("戶名不符");

        Assert.Equal(PayoutBatchStatus.BankRejected, batch.Status);
        Assert.Equal("戶名不符", batch.BankRejectionReason);
    }
}
