using StepGo.Domain.Identity;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Tests.Identity;

public class UserAccountTests
{
    [Fact]
    public void Register_WithoutPhoneNumber_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            UserAccount.Register(Guid.NewGuid(), "王小明", phoneNumber: "", email: null, Role.Student, DateTimeOffset.UtcNow));

        Assert.Equal("phone_number_required", ex.Code);
    }

    [Fact]
    public void Register_WithoutEmail_Succeeds()
    {
        var user = UserAccount.Register(Guid.NewGuid(), "王小明", "0912345678", email: null, Role.Student, DateTimeOffset.UtcNow);

        Assert.Null(user.Email);
        Assert.Equal("0912345678", user.PhoneNumber);
    }
}
