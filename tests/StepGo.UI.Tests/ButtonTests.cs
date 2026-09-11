using Bunit;
using StepGo.UI.Components;
using Xunit;

namespace StepGo.UI.Tests;

public class ButtonTests : TestContext
{
    [Fact]
    public void Renders_ChildContent_And_Variant_Class()
    {
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, "primary")
            .AddChildContent("送出"));

        var button = cut.Find("button");
        Assert.Equal("送出", button.TextContent.Trim());
        Assert.Contains("stepgo-btn--primary", button.ClassList);
    }

    [Fact]
    public void Disabled_Sets_Disabled_Attribute()
    {
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Disabled, true)
            .AddChildContent("送出"));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public async Task OnClick_Fires_When_Clicked()
    {
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.OnClick, _ => clicked = true)
            .AddChildContent("送出"));

        await cut.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.True(clicked);
    }
}
