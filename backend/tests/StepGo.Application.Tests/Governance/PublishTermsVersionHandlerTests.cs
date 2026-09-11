using Moq;
using StepGo.Application.Common;
using StepGo.Application.Governance;
using StepGo.Domain.Governance;
using StepGo.Domain.Identity;

namespace StepGo.Application.Tests.Governance;

file sealed class FakeAdmin(Guid userId) : ICurrentUserAccessor
{
    public Guid UserId { get; } = userId;
    public Role Role => Role.Admin;
}

file sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow => now;
}

/// <summary>Task 8.3: publishing always adds a new version number; nothing is overwritten, so the prior version stays queryable through GetLatestAsync's untouched history.</summary>
public class PublishTermsVersionHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNoExistingVersion_StartsAtVersion1()
    {
        var admin = new FakeAdmin(Guid.NewGuid());
        var repo = new Mock<ITermsVersionRepository>();
        repo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>())).ReturnsAsync((TermsVersion?)null);

        var handler = new PublishTermsVersionHandler(repo.Object, admin, new FixedClock(DateTimeOffset.UtcNow));
        var version = await handler.HandleAsync(new PublishTermsVersionCommand("條款 v1"), CancellationToken.None);

        Assert.Equal(1, version.VersionNumber);
    }

    [Fact]
    public async Task HandleAsync_WhenVersion2Exists_PublishesVersion3WithoutTouchingPriorContent()
    {
        var admin = new FakeAdmin(Guid.NewGuid());
        var existingV2 = new TermsVersion(Guid.NewGuid(), 2, "條款 v2 內容", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-10));

        var repo = new Mock<ITermsVersionRepository>();
        repo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingV2);

        var handler = new PublishTermsVersionHandler(repo.Object, admin, new FixedClock(DateTimeOffset.UtcNow));
        var newVersion = await handler.HandleAsync(new PublishTermsVersionCommand("條款 v3 內容"), CancellationToken.None);

        Assert.Equal(3, newVersion.VersionNumber);
        Assert.Equal("條款 v2 內容", existingV2.Content); // v2's own object is never mutated by publishing v3
        repo.Verify(r => r.SaveAsync(It.Is<TermsVersion>(v => v.VersionNumber == 3), It.IsAny<CancellationToken>()), Times.Once);
    }
}
