using StepGo.AdminPanel.Models;
using StepGo.AdminPanel.Services;
using Xunit;

namespace StepGo.AdminPanel.Tests;

public class AuditLogFilteringTests
{
    private static readonly AuditLogEntry[] Entries =
    [
        new(DateTimeOffset.UtcNow, "管理員", AuditLogCategory.FeeRate, "調整費率"),
        new(DateTimeOffset.UtcNow, "管理員", AuditLogCategory.Arbitration, "裁決案件 A"),
        new(DateTimeOffset.UtcNow, "管理員", AuditLogCategory.Arbitration, "裁決案件 B"),
    ];

    [Fact]
    public void Filtering_By_Category_Returns_Only_Matching_Entries()
    {
        var result = AuditLogFiltering.ByCategory(Entries, AuditLogCategory.Arbitration);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(AuditLogCategory.Arbitration, e.Category));
    }

    [Fact]
    public void No_Category_Returns_All_Entries()
    {
        var result = AuditLogFiltering.ByCategory(Entries, null);

        Assert.Equal(3, result.Count);
    }
}
