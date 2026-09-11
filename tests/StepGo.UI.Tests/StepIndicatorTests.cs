using Bunit;
using StepGo.UI.Components;
using Xunit;

namespace StepGo.UI.Tests;

public class StepIndicatorTests : TestContext
{
    [Fact]
    public void Renders_One_Item_Per_Step()
    {
        var cut = RenderComponent<StepIndicator>(parameters => parameters
            .Add(p => p.StepCount, 4)
            .Add(p => p.CurrentStepIndex, 1));

        Assert.Equal(4, cut.FindAll("li").Count);
    }

    [Fact]
    public void Current_Step_Has_Aria_Current()
    {
        var cut = RenderComponent<StepIndicator>(parameters => parameters
            .Add(p => p.StepCount, 3)
            .Add(p => p.CurrentStepIndex, 1));

        var currentStepEls = cut.FindAll("[aria-current='step']");
        Assert.Single(currentStepEls);
    }

    [Fact]
    public void Works_With_Only_StepCount_And_CurrentStepIndex()
    {
        // frontend-design-system spec: teacher's 4-step wizard and student's 3-step
        // checkout must both work by passing only step count + current index.
        var teacherWizard = RenderComponent<StepIndicator>(p => p.Add(x => x.StepCount, 4).Add(x => x.CurrentStepIndex, 0));
        var studentCheckout = RenderComponent<StepIndicator>(p => p.Add(x => x.StepCount, 3).Add(x => x.CurrentStepIndex, 0));

        Assert.Equal(4, teacherWizard.FindAll("li").Count);
        Assert.Equal(3, studentCheckout.FindAll("li").Count);
    }
}
