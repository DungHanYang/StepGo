using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using StepGo.Marketing.Components.Pages;
using Xunit;

namespace StepGo.Marketing.Tests;

public class CoursesTests : TestContext
{
    [Fact]
    public void No_Matching_Keyword_Shows_Empty_State_With_Clear_Filters_Action()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("/courses?q=不存在的課程名稱xyz");

        var cut = RenderComponent<Courses>();

        var emptyState = cut.Find("[data-testid='empty-state']");
        Assert.Contains("找不到符合條件的課程", emptyState.TextContent);
        var clearLink = emptyState.QuerySelector("a");
        Assert.Equal("/courses", clearLink!.GetAttribute("href"));
    }

    [Fact]
    public void Matching_Keyword_Shows_Results()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("/courses?q=繪畫");

        var cut = RenderComponent<Courses>();

        var results = cut.Find("[data-testid='results']");
        Assert.Contains("兒童繪畫班", results.TextContent);
    }
}
