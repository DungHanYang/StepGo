using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using StepGo.AdminPanel.Pages;
using Xunit;

namespace StepGo.AdminPanel.Tests;

public class AdminLoginTests : TestContext
{
    [Fact]
    public void Admin_Login_Route_Is_Completely_Separate_From_FrontendAuth_Login_Route()
    {
        // frontend-admin-panel spec: "管理者登入頁與老師/學生登入頁分離...與 frontend-auth 提供的
        // 老師/學生登入頁面在路由與版面上完全分離". The admin app has no project reference to
        // StepGo.Marketing/.Client at all, so this is structurally guaranteed; this test pins
        // the actual route value so a future edit can't accidentally make them collide.
        var routeAttribute = typeof(AdminLogin)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), inherit: false)
            .Cast<Microsoft.AspNetCore.Components.RouteAttribute>()
            .Single();

        Assert.Equal("/login", routeAttribute.Template);
        Assert.NotEqual("/auth/login", routeAttribute.Template);
    }

    [Fact]
    public void Shows_Ip_Allowlist_Placeholder_And_Two_Factor_Step()
    {
        var cut = RenderComponent<AdminLogin>();

        cut.Find("[data-testid='ip-allowlist-note']");
        cut.Find("[data-testid='credentials-step']");

        cut.FindAll("button").First(b => b.TextContent == "下一步").Click();

        cut.Find("[data-testid='two-factor-step']");
        Assert.Contains("六位數驗證碼", cut.Markup);
    }

    [Fact]
    public void Completing_Two_Factor_Step_Navigates_To_Dashboard()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RenderComponent<AdminLogin>();

        cut.FindAll("button").First(b => b.TextContent == "下一步").Click();
        cut.Find("input").Input("123456");
        cut.FindAll("button").First(b => b.TextContent == "登入").Click();

        Assert.EndsWith("/", nav.Uri);
    }
}
