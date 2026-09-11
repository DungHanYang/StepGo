using Bunit;
using StepGo.UI.Components;
using Xunit;

namespace StepGo.UI.Tests;

public class WizardTests : TestContext
{
    [Fact]
    public void Renders_StepIndicator_And_ChildContent()
    {
        var steps = new[] { "基本資料", "定價與付款", "退費規則", "招生頁" };

        var cut = RenderComponent<Wizard>(parameters => parameters
            .Add(p => p.Steps, steps)
            .Add(p => p.CurrentStepIndex, 1)
            .AddChildContent("<p>定價表單</p>"));

        Assert.NotNull(cut.FindComponent<StepIndicator>());
        Assert.Contains("定價表單", cut.Markup);
        Assert.Contains("定價與付款", cut.Markup);
    }

    [Fact]
    public void Renders_Footer_When_Provided()
    {
        var cut = RenderComponent<Wizard>(parameters => parameters
            .Add(p => p.Steps, new[] { "確認課程", "填資料選付款", "結果頁" })
            .Add(p => p.CurrentStepIndex, 0)
            .Add(p => p.Footer, (Microsoft.AspNetCore.Components.RenderFragment)(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddContent(1, "下一步");
                builder.CloseElement();
            })));

        Assert.Contains("下一步", cut.Markup);
    }
}
