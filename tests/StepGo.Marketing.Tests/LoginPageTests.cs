using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using StepGo.ApiClient.Auth;
using StepGo.Marketing.Client.Pages;
using Xunit;

namespace StepGo.Marketing.Tests;

public class LoginPageTests : TestContext
{
    public LoginPageTests()
    {
        Services.AddSingleton<IAuthTokenStore, InMemoryAuthTokenStore>();
        Services.AddScoped<StepGoAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<StepGoAuthenticationStateProvider>());
    }

    [Fact]
    public void Teacher_Login_Redirects_To_Teacher_Portal()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RenderComponent<LoginPage>();

        cut.FindAll("input")[0].Change("teacher@example.com");
        cut.FindAll("input")[1].Change("anything");
        cut.Find("form").Submit();

        Assert.StartsWith(PortalBaseUrls.LocalDev.TeacherPortalBaseUrl, nav.Uri);
    }

    [Fact]
    public void Student_Login_Redirects_To_Student_Portal_My_Courses()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RenderComponent<LoginPage>();

        cut.FindAll("input")[0].Change("student@example.com");
        cut.FindAll("input")[1].Change("anything");
        cut.Find("form").Submit();

        Assert.StartsWith(PortalBaseUrls.LocalDev.StudentPortalBaseUrl, nav.Uri);
        Assert.Contains("/my-courses", nav.Uri);
    }

    [Fact]
    public void Unknown_Account_Shows_Error_And_Does_Not_Navigate()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var initialUri = nav.Uri;
        var cut = RenderComponent<LoginPage>();

        cut.FindAll("input")[0].Change("nobody@example.com");
        cut.FindAll("input")[1].Change("anything");
        cut.Find("form").Submit();

        Assert.Equal(initialUri, nav.Uri);
        Assert.Contains("帳號或密碼錯誤", cut.Markup);
    }
}
