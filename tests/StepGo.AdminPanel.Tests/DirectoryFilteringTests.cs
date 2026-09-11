using StepGo.AdminPanel.Models;
using StepGo.AdminPanel.Services;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.AdminPanel.Tests;

public class DirectoryFilteringTests
{
    private static readonly DirectoryEntry[] Entries =
    [
        new("陳老師", VerificationStatus.Verified, "兒童繪畫班", "招生中"),
        new("林老師", VerificationStatus.UnderReview, "親子瑜伽", "審核中"),
        new("張老師", VerificationStatus.UnderReview, "程式設計入門", "招生中"),
    ];

    [Fact]
    public void Filtering_By_VerificationStatus_Returns_Only_Matching_Entries()
    {
        var result = DirectoryFiltering.Filter(Entries, VerificationStatus.UnderReview, courseStatus: null, keyword: null);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(VerificationStatus.UnderReview, e.VerificationStatus));
    }

    [Fact]
    public void Keyword_Matches_Teacher_Or_Course_Name()
    {
        var result = DirectoryFiltering.Filter(Entries, verificationStatus: null, courseStatus: null, keyword: "瑜伽");

        Assert.Single(result);
        Assert.Equal("林老師", result[0].TeacherName);
    }
}
