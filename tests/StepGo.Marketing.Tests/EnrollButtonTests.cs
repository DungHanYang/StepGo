using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using StepGo.Marketing.Client.Components;
using Xunit;

namespace StepGo.Marketing.Tests;

public class EnrollButtonTests : TestContext
{
    [Fact]
    public void Click_Navigates_To_Register_With_Student_Role_And_CourseId()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        var cut = RenderComponent<EnrollButton>(parameters => parameters
            .Add(p => p.CourseId, "c-paint-kids")
            .Add(p => p.CourseName, "兒童繪畫班"));

        cut.Find("button").Click();

        Assert.Equal("/auth/register?role=student&courseId=c-paint-kids", new Uri(nav.Uri).PathAndQuery);
    }
}
