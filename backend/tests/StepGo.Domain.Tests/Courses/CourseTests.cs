using StepGo.Courses.Domain;
using StepGo.Shared.Domain;

namespace StepGo.Domain.Tests.Courses;

public class CourseTests
{
    private static readonly RefundRuleSet PlatformFloor = new([new RefundTier(14, 1.0m)]);
    private static readonly RefundRuleSet ValidTeacherRules = new([new RefundTier(14, 1.0m)]);

    [Fact]
    public void Create_WithoutAnyPaymentMethod_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Course.Create(Guid.NewGuid(), Guid.NewGuid(), "課程", Money.FromWholeDollars(3000),
                PaymentMethod.None, ValidTeacherRules, PlatformFloor, DateTimeOffset.UtcNow.AddDays(30)));

        Assert.Equal("payment_method_required", ex.Code);
    }

    [Fact]
    public void Create_WithAtLeastOnePaymentMethod_Succeeds()
    {
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "課程", Money.FromWholeDollars(3000),
            PaymentMethod.Atm, ValidTeacherRules, PlatformFloor, DateTimeOffset.UtcNow.AddDays(30));

        Assert.Equal(CourseStatus.Draft, course.Status);
    }

    [Fact]
    public void Publish_WhenTeacherNotVerified_Throws()
    {
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "課程", Money.FromWholeDollars(3000),
            PaymentMethod.Atm, ValidTeacherRules, PlatformFloor, DateTimeOffset.UtcNow.AddDays(30));

        var ex = Assert.Throws<DomainException>(() => course.Publish(teacherIsVerified: false));

        Assert.Equal("teacher_not_verified", ex.Code);
        Assert.Equal(CourseStatus.Draft, course.Status);
    }

    [Fact]
    public void Publish_WhenTeacherVerified_Succeeds()
    {
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "課程", Money.FromWholeDollars(3000),
            PaymentMethod.Atm, ValidTeacherRules, PlatformFloor, DateTimeOffset.UtcNow.AddDays(30));

        course.Publish(teacherIsVerified: true);

        Assert.Equal(CourseStatus.Published, course.Status);
    }
}
