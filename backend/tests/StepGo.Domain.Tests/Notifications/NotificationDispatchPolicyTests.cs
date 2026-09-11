using StepGo.Notifications.Domain;

namespace StepGo.Domain.Tests.Notifications;

public class NotificationDispatchPolicyTests
{
    [Fact]
    public void DetermineChannels_NoEmailNoLine_StillIncludesInApp()
    {
        var channels = NotificationDispatchPolicy.DetermineChannels(hasEmail: false, hasLineBound: false);

        Assert.True(channels.HasFlag(NotificationChannel.InApp));
        Assert.False(channels.HasFlag(NotificationChannel.Email));
        Assert.False(channels.HasFlag(NotificationChannel.Line));
    }

    [Fact]
    public void DetermineChannels_WithEmailAndLine_IncludesAllThree()
    {
        var channels = NotificationDispatchPolicy.DetermineChannels(hasEmail: true, hasLineBound: true);

        Assert.True(channels.HasFlag(NotificationChannel.InApp));
        Assert.True(channels.HasFlag(NotificationChannel.Email));
        Assert.True(channels.HasFlag(NotificationChannel.Line));
    }
}
