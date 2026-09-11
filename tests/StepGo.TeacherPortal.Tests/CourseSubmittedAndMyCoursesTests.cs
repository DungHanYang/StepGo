using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.TeacherPortal.Pages;
using StepGo.TeacherPortal.Services;
using Xunit;

namespace StepGo.TeacherPortal.Tests;

public class CourseSubmittedAndMyCoursesTests : TestContext
{
    [Fact]
    public void CourseSubmitted_Page_Renders_Waiting_Screen()
    {
        var cut = RenderComponent<CourseSubmitted>();

        cut.Find("[data-testid='submitted-waiting']");
        Assert.Contains("已送出平台審核", cut.Markup);
    }

    [Fact]
    public void MyCourses_Shows_Empty_State_When_No_Courses()
    {
        Services.AddSingleton(new TeacherDataStore(TimeProvider.System, myCourseNamesOverride: []));

        var cut = RenderComponent<MyCourses>();

        cut.Find("[data-testid='my-courses-empty']");
        Assert.Contains("建立第一堂課", cut.Markup);
    }

    [Fact]
    public void MyCourses_Shows_List_When_Courses_Exist()
    {
        Services.AddSingleton(new TeacherDataStore(TimeProvider.System));

        var cut = RenderComponent<MyCourses>();

        cut.Find("[data-testid='my-courses-list']");
    }
}
