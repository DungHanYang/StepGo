using Bunit;
using StepGo.UI.Components;
using Xunit;

namespace StepGo.UI.Tests;

public class CardTests : TestContext
{
    [Fact]
    public void Renders_ChildContent_Inside_StepgoCard_Div()
    {
        var cut = RenderComponent<Card>(parameters => parameters
            .AddChildContent("<p>內容</p>"));

        var div = cut.Find("div.stepgo-card");
        Assert.Contains("內容", div.TextContent);
    }
}
