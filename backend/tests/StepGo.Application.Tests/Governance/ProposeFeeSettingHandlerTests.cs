using Moq;
using StepGo.Application.Common;
using StepGo.Application.Governance;
using StepGo.Domain.Courses;
using StepGo.Domain.Governance;
using StepGo.Domain.Identity;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Tests.Governance;

file sealed class FakeCurrentUser(Guid userId, Role role) : ICurrentUserAccessor
{
    public Guid UserId { get; } = userId;
    public Role Role { get; } = role;
}

file sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow => now;
}

/// <summary>Task 8.2: every successful fee-setting change writes a corresponding change-log entry.</summary>
public class ProposeFeeSettingHandlerTests
{
    [Fact]
    public async Task HandleAsync_SuccessfulChange_AppendsChangeLogEntry()
    {
        var now = DateTimeOffset.UtcNow;
        var admin = new FakeCurrentUser(Guid.NewGuid(), Role.Admin);
        var settingRepo = new Mock<IPlatformFeeSettingRepository>();
        var changeLog = new Mock<IChangeLogRepository>();

        var handler = new ProposeFeeSettingHandler(settingRepo.Object, changeLog.Object, admin, new FixedClock(now));
        var command = new ProposeFeeSettingCommand(0.12m, Money.FromWholeDollars(20), [new RefundTier(14, 1.0m)], now.AddDays(31));

        await handler.HandleAsync(command, CancellationToken.None);

        settingRepo.Verify(r => r.SaveAsync(It.IsAny<PlatformFeeSetting>(), It.IsAny<CancellationToken>()), Times.Once);
        changeLog.Verify(r => r.AppendAsync(
            It.Is<ChangeLogEntry>(e => e.Category == ChangeLogCategory.FeeRate && e.OperatorId == admin.UserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NonAdmin_ThrowsAuthorization()
    {
        var now = DateTimeOffset.UtcNow;
        var teacher = new FakeCurrentUser(Guid.NewGuid(), Role.Teacher);
        var handler = new ProposeFeeSettingHandler(new Mock<IPlatformFeeSettingRepository>().Object, new Mock<IChangeLogRepository>().Object, teacher, new FixedClock(now));

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            handler.HandleAsync(new ProposeFeeSettingCommand(0.12m, Money.Zero, [new RefundTier(14, 1.0m)], now.AddDays(31)), CancellationToken.None));
    }
}
