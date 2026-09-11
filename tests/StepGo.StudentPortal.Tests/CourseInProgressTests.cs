using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.StudentPortal.Pages;
using StepGo.StudentPortal.Services;
using Xunit;

namespace StepGo.StudentPortal.Tests;

public class CourseInProgressTests : TestContext
{
    [Fact]
    public void Shows_Next_Session_Info()
    {
        Services.AddSingleton(new StudentDataStore(TimeProvider.System));
        Services.AddSingleton(TimeProvider.System);

        // o-2001 is seeded with a NextSessionAt.
        var cut = RenderComponent<CourseInProgress>(parameters => parameters.Add(p => p.OrderId, "o-2001"));

        cut.Find("[data-testid='next-session']");
        Assert.Contains("下一堂：", cut.Markup);
    }
}
