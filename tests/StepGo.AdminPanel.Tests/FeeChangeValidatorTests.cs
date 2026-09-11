using StepGo.AdminPanel.Services;
using Xunit;

namespace StepGo.AdminPanel.Tests;

public class FeeChangeValidatorTests
{
    private static readonly DateOnly SubmittedOn = new(2026, 1, 1);

    [Fact]
    public void EffectiveDate_20_Days_Out_Is_Rejected()
    {
        Assert.False(FeeChangeValidator.IsEffectiveDateAllowed(SubmittedOn.AddDays(20), SubmittedOn));
    }

    [Fact]
    public void EffectiveDate_Exactly_30_Days_Out_Is_Allowed()
    {
        Assert.True(FeeChangeValidator.IsEffectiveDateAllowed(SubmittedOn.AddDays(30), SubmittedOn));
    }

    [Fact]
    public void EffectiveDate_More_Than_30_Days_Out_Is_Allowed()
    {
        Assert.True(FeeChangeValidator.IsEffectiveDateAllowed(SubmittedOn.AddDays(45), SubmittedOn));
    }
}
