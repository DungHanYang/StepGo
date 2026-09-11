using StepGo.ApiClient.Auth;
using Xunit;

namespace StepGo.ApiClient.Tests;

public class PortalRedirectTests
{
    private static readonly PortalBaseUrls BaseUrls = new("https://teach.stepgo.tw", "https://learn.stepgo.tw");

    [Fact]
    public void Teacher_Role_Redirects_To_Teacher_Portal()
    {
        var url = PortalRedirect.GetRedirectUrl(PortalRedirect.TeacherRole, BaseUrls);

        Assert.Equal("https://teach.stepgo.tw/", url);
    }

    [Fact]
    public void Student_Role_Redirects_To_Student_My_Courses()
    {
        var url = PortalRedirect.GetRedirectUrl(PortalRedirect.StudentRole, BaseUrls);

        Assert.Equal("https://learn.stepgo.tw/my-courses", url);
    }

    [Fact]
    public void Unknown_Role_Returns_Null()
    {
        var url = PortalRedirect.GetRedirectUrl("admin", BaseUrls);

        Assert.Null(url);
    }
}
