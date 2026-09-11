using StepGo.Domain.Identity;
using StepGo.Domain.SharedKernel;

namespace StepGo.Domain.Tests.Identity;

public class TeacherProfileTests
{
    private static BankAccount MakeAccount(string holderName) => new("008", "1234567890", holderName);

    [Fact]
    public void Submit_WithMismatchedAccountHolderName_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            TeacherProfile.Submit(Guid.NewGuid(), "王小明", "A123456789", "front.jpg", "back.jpg", MakeAccount("陳小華")));

        Assert.Equal("payout_account_name_mismatch", ex.Code);
    }

    [Fact]
    public void Submit_WithMatchingAccountHolderName_StartsPendingReview()
    {
        var profile = TeacherProfile.Submit(Guid.NewGuid(), "王小明", "A123456789", "front.jpg", "back.jpg", MakeAccount("王小明"));

        Assert.Equal(TeacherVerificationStatus.PendingReview, profile.VerificationStatus);
        Assert.False(profile.CanCreateOrPublishCourse);
    }

    [Fact]
    public void GuardCanCreateOrPublishCourse_WhenNotVerified_Throws()
    {
        var profile = TeacherProfile.Submit(Guid.NewGuid(), "王小明", "A123456789", "front.jpg", "back.jpg", MakeAccount("王小明"));

        var ex = Assert.Throws<DomainException>(profile.GuardCanCreateOrPublishCourse);
        Assert.Equal("teacher_not_verified", ex.Code);
    }

    [Fact]
    public void Approve_AllowsCourseCreation()
    {
        var profile = TeacherProfile.Submit(Guid.NewGuid(), "王小明", "A123456789", "front.jpg", "back.jpg", MakeAccount("王小明"));
        profile.Approve();

        Assert.True(profile.CanCreateOrPublishCourse);
    }

    [Fact]
    public void Reject_WithoutReason_Throws()
    {
        var profile = TeacherProfile.Submit(Guid.NewGuid(), "王小明", "A123456789", "front.jpg", "back.jpg", MakeAccount("王小明"));

        Assert.Throws<DomainException>(() => profile.Reject(""));
    }
}
