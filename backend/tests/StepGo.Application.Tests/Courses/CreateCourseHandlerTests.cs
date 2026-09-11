using Moq;
using StepGo.Application.Common;
using StepGo.Application.Courses;
using StepGo.Application.Governance;
using StepGo.Application.Identity;
using StepGo.Domain.Courses;
using StepGo.Domain.Governance;
using StepGo.Domain.Identity;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Tests.Courses;

/// <summary>Task 2.3: an unverified (or no-profile) teacher calling the course-creation API must get a 403 (AuthorizationException), not a generic 400.</summary>
public class CreateCourseHandlerTests
{
    private static PlatformFeeSetting MakeFeeSetting()
    {
        var now = DateTimeOffset.UtcNow;
        return PlatformFeeSetting.Propose("v1", 0.10m, Money.FromWholeDollars(20), new RefundRuleSet([new RefundTier(14, 1.0m)]), now.AddDays(31), Guid.NewGuid(), now);
    }

    private static CreateCourseCommand MakeCommand(Guid teacherId) => new(
        teacherId, "課程", Money.FromWholeDollars(3000), PaymentMethod.Atm, [new RefundTier(14, 1.0m)], DateTimeOffset.UtcNow.AddDays(30));

    [Fact]
    public async Task HandleAsync_TeacherHasNoProfile_ThrowsAuthorization()
    {
        var teacherId = Guid.NewGuid();
        var teacherRepo = new Mock<ITeacherProfileRepository>();
        teacherRepo.Setup(r => r.FindAsync(teacherId, It.IsAny<CancellationToken>())).ReturnsAsync((TeacherProfile?)null);

        var handler = new CreateCourseHandler(new Mock<ICourseRepository>().Object, teacherRepo.Object, new Mock<IPlatformFeeSettingRepository>().Object);

        await Assert.ThrowsAsync<AuthorizationException>(() => handler.HandleAsync(MakeCommand(teacherId), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_TeacherPendingReview_ThrowsAuthorization()
    {
        var teacherId = Guid.NewGuid();
        var profile = TeacherProfile.Submit(teacherId, "王小明", "A123456789", "front.jpg", "back.jpg", new BankAccount("008", "123", "王小明"));

        var teacherRepo = new Mock<ITeacherProfileRepository>();
        teacherRepo.Setup(r => r.FindAsync(teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var handler = new CreateCourseHandler(new Mock<ICourseRepository>().Object, teacherRepo.Object, new Mock<IPlatformFeeSettingRepository>().Object);

        await Assert.ThrowsAsync<AuthorizationException>(() => handler.HandleAsync(MakeCommand(teacherId), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_VerifiedTeacher_Succeeds()
    {
        var teacherId = Guid.NewGuid();
        var profile = TeacherProfile.Submit(teacherId, "王小明", "A123456789", "front.jpg", "back.jpg", new BankAccount("008", "123", "王小明"));
        profile.Approve();

        var teacherRepo = new Mock<ITeacherProfileRepository>();
        teacherRepo.Setup(r => r.FindAsync(teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var feeSettingRepo = new Mock<IPlatformFeeSettingRepository>();
        feeSettingRepo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(MakeFeeSetting());

        var courseRepo = new Mock<ICourseRepository>();

        var handler = new CreateCourseHandler(courseRepo.Object, teacherRepo.Object, feeSettingRepo.Object);
        var course = await handler.HandleAsync(MakeCommand(teacherId), CancellationToken.None);

        Assert.Equal(CourseStatus.Draft, course.Status);
        courseRepo.Verify(r => r.SaveAsync(course, It.IsAny<CancellationToken>()), Times.Once);
    }
}
