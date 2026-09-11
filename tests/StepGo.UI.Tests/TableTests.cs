using Bunit;
using Microsoft.AspNetCore.Components;
using StepGo.UI.Components;
using Xunit;

namespace StepGo.UI.Tests;

public class TableTests : TestContext
{
    [Fact]
    public void Renders_One_Row_Per_Item()
    {
        var items = new[] { "老師甲", "老師乙", "老師丙" };

        var cut = RenderComponent<Table<string>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.RowTemplate, (string item) => (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "td");
                builder.AddContent(1, item);
                builder.CloseElement();
            })));

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(3, rows.Count);
        Assert.Contains("老師甲", cut.Markup);
    }

    [Fact]
    public void Renders_EmptyTemplate_When_No_Items()
    {
        var cut = RenderComponent<Table<string>>(parameters => parameters
            .Add(p => p.Items, Array.Empty<string>())
            .Add(p => p.RowTemplate, (string item) => (RenderFragment)(builder => { }))
            .Add(p => p.EmptyTemplate, (RenderFragment)(builder => builder.AddContent(0, "沒有符合的結果"))));

        var rows = cut.FindAll("tbody tr");
        Assert.Single(rows);
        Assert.Contains("沒有符合的結果", cut.Markup);
    }
}
