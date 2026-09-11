using Bunit;
using StepGo.Marketing.Client.Components;
using Xunit;

namespace StepGo.Marketing.Tests;

public class TermsTabsTests : TestContext
{
    [Fact]
    public void Clicking_Student_Tab_Shows_Student_Panel_And_Hides_Teacher_Panel_But_Keeps_It_Switchable_Back()
    {
        var cut = RenderComponent<TermsTabs>(parameters => parameters
            .Add(p => p.TeacherContentHtml, "<p>老師條款內容</p>")
            .Add(p => p.StudentContentHtml, "<p>學生退費規則內容</p>"));

        // Initial state: teacher tab active.
        Assert.Null(cut.Find("[data-testid='teacher-panel']").GetAttribute("hidden"));
        Assert.NotNull(cut.Find("[data-testid='student-panel']").GetAttribute("hidden"));

        var tabs = cut.FindAll("[role='tab']");
        tabs[1].Click(); // "學生退費規則"

        Assert.NotNull(cut.Find("[data-testid='teacher-panel']").GetAttribute("hidden"));
        Assert.Null(cut.Find("[data-testid='student-panel']").GetAttribute("hidden"));
        Assert.Contains("學生退費規則內容", cut.Find("[data-testid='student-panel']").TextContent);

        // Switching back still works — content wasn't removed from the DOM.
        tabs[0].Click();
        Assert.Null(cut.Find("[data-testid='teacher-panel']").GetAttribute("hidden"));
        Assert.Contains("老師條款內容", cut.Find("[data-testid='teacher-panel']").TextContent);
    }
}
