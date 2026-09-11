using Moq;
using StepGo.Application.Common;
using StepGo.Application.Courses;
using StepGo.Application.Governance;
using StepGo.Application.Identity;
using StepGo.Domain.Courses;
using StepGo.Domain.Identity;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Tests.Courses;

/// <summary>Task 3.3: an unverified teacher's course must stay in Draft; the API rejects the request as unauthorized.</summary>
public class PublishCourseHandlerTests
{
    private static Course MakeDraftCourse(Guid teacherId) => Course.Create(
        Guid.NewGuid(), teacherId, "課程", Money.FromWholeDollars(3000), PaymentMethod.Atm,
        new RefundRuleSet([new RefundTier(14, 1.0m)]), new RefundRuleSet([new RefundTier(14, 1.0m)]), DateTimeOffset.UtcNow.AddDays(30));

    [Fact]
    public async Task HandleAsync_UnverifiedTeacher_ThrowsAuthorizationAndCourseStaysDraft()
    {
        var teacherId = Guid.NewGuid();
        var course = MakeDraftCourse(teacherId);
        var profile = TeacherProfile.Submit(teacherId, "王小明", "A123456789", "front.jpg", "back.jpg", new BankAccount("008", "123", "王小明"));

        var courseRepo = new Mock<ICourseRepository>();
        courseRepo.Setup(r => r.FindAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        var teacherRepo = new Mock<ITeacherProfileRepository>();
        teacherRepo.Setup(r => r.FindAsync(teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var handler = new PublishCourseHandler(
            courseRepo.Object, teacherRepo.Object, new Mock<ITermsVersionRepository>().Object,
            new Mock<ITeacherConsentRepository>().Object, new Mock<IPlatformFeeSettingRepository>().Object, new FixedClock(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<AuthorizationException>(() => handler.HandleAsync(new PublishCourseCommand(course.Id, teacherId), CancellationToken.None));
        Assert.Equal(CourseStatus.Draft, course.Status);
    }

    [Fact]
    public async Task HandleAsync_AnotherTeachersCourse_ThrowsAuthorization()
    {
        var ownerTeacherId = Guid.NewGuid();
        var callerTeacherId = Guid.NewGuid();
        var course = MakeDraftCourse(ownerTeacherId);

        var courseRepo = new Mock<ICourseRepository>();
        courseRepo.Setup(r => r.FindAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

        var handler = new PublishCourseHandler(
            courseRepo.Object, new Mock<ITeacherProfileRepository>().Object, new Mock<ITermsVersionRepository>().Object,
            new Mock<ITeacherConsentRepository>().Object, new Mock<IPlatformFeeSettingRepository>().Object, new FixedClock(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<AuthorizationException>(() => handler.HandleAsync(new PublishCourseCommand(course.Id, callerTeacherId), CancellationToken.None));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
