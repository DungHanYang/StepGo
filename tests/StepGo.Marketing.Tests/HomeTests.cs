using Bunit;
using StepGo.Marketing.Components.Pages;
using Xunit;

namespace StepGo.Marketing.Tests;

public class HomeTests : TestContext
{
    [Fact]
    public void Teacher_Entry_Links_To_ForTeachers()
    {
        var cut = RenderComponent<Home>();

        var link = cut.Find("[data-testid='cta-teacher']");
        Assert.Equal("/for-teachers", link.GetAttribute("href"));
    }

    [Fact]
    public void Student_Entry_Links_To_Courses()
    {
        var cut = RenderComponent<Home>();

        var link = cut.Find("[data-testid='cta-student']");
        Assert.Equal("/courses", link.GetAttribute("href"));
    }

    [Fact]
    public void Renders_Pricing_Summary_And_Enrolling_Courses()
    {
        var cut = RenderComponent<Home>();

        Assert.Contains("費用試算摘要", cut.Markup);
        Assert.Contains("正在招生", cut.Markup);
    }
}
