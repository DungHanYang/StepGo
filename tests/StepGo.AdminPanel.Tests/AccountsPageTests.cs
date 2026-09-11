using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StepGo.AdminPanel.Pages;
using StepGo.AdminPanel.Services;
using Xunit;

namespace StepGo.AdminPanel.Tests;

public class AccountsPageTests : TestContext
{
    [Fact]
    public void New_Member_Role_Select_Shows_Only_One_Assignable_Role()
    {
        Services.AddSingleton(new AdminDataStore(TimeProvider.System));

        var cut = RenderComponent<Accounts>();

        var roleSelect = cut.Find("[data-testid='role-select']");
        var options = roleSelect.QuerySelectorAll("option");

        Assert.Single(options);
        Assert.Equal("平台管理員", options[0].TextContent);
    }

    [Fact]
    public void Role_Matrix_Reserves_An_Extra_Column_For_Future_Roles()
    {
        Services.AddSingleton(new AdminDataStore(TimeProvider.System));

        var cut = RenderComponent<Accounts>();

        var matrix = cut.Find("[data-testid='role-matrix']");
        var headers = matrix.QuerySelectorAll("th");

        Assert.True(headers.Length >= 3); // 權限項目 + Admin + at least one reserved column
    }
}
