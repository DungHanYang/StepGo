using StepGo.Application.Governance;
using StepGo.Application.Identity;
using StepGo.Domain.Courses;
using StepGo.Domain.Governance;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Courses;

public sealed record PublishCourseCommand(Guid CourseId, Guid TeacherId);

/// <summary>Task 3.3 + 8.4: publishing requires a verified teacher AND (once the effective date has passed) re-consent to the latest terms.</summary>
public sealed class PublishCourseHandler(
    ICourseRepository courseRepository, ITeacherProfileRepository teacherRepository,
    ITermsVersionRepository termsRepository, ITeacherConsentRepository consentRepository,
    IPlatformFeeSettingRepository feeSettingRepository, StepGo.Application.Common.IClock clock)
{
    public async Task<Course> HandleAsync(PublishCourseCommand command, CancellationToken ct)
    {
        var course = await courseRepository.FindAsync(command.CourseId, ct)
            ?? throw new DomainException("course_not_found", "找不到課程。");

        if (course.TeacherId != command.TeacherId)
        {
            throw new StepGo.Application.Common.AuthorizationException("無權發佈其他老師的課程。");
        }

        var teacher = await teacherRepository.FindAsync(command.TeacherId, ct)
            ?? throw new DomainException("teacher_profile_not_found", "老師尚未提交身分驗證。");

        course.Publish(teacher.CanCreateOrPublishCourse);

        var latestTerms = await termsRepository.GetLatestAsync(ct);
        var latestSetting = await feeSettingRepository.GetLatestPublishedAsync(ct);
        if (latestTerms is not null && latestSetting is not null)
        {
            var consent = await consentRepository.FindLatestForTeacherAsync(command.TeacherId, ct);
            CoursePublishGovernanceGuard.GuardCanPublish(latestTerms, consent, clock.UtcNow, latestSetting.EffectiveDate);
        }

        await courseRepository.SaveAsync(course, ct);
        return course;
    }
}
