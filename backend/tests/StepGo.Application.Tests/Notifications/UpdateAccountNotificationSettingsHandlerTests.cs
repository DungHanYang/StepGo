using StepGo.Application.Notifications;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Tests.Notifications;

/// <summary>Task 9.4: the account-settings API must not accept a request to disable the two core channels.</summary>
public class UpdateAccountNotificationSettingsHandlerTests
{
    [Theory]
    [InlineData("Email")]
    [InlineData("InApp")]
    public void Handle_AttemptingToDisableCoreChannel_Throws(string coreChannel)
    {
        var handler = new UpdateAccountNotificationSettingsHandler();

        var ex = Assert.Throws<DomainException>(() => handler.Handle(new UpdateAccountNotificationSettingsCommand([coreChannel])));

        Assert.Equal("core_channel_cannot_be_disabled", ex.Code);
    }

    [Fact]
    public void Handle_DisablingLineOnly_Succeeds()
    {
        var handler = new UpdateAccountNotificationSettingsHandler();

        handler.Handle(new UpdateAccountNotificationSettingsCommand(["Line"])); // does not throw
    }
}
