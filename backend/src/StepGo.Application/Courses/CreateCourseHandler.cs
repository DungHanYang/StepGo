using StepGo.Application.Common;
using StepGo.Application.Governance;
using StepGo.Application.Identity;
using StepGo.Domain.Courses;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Courses;

public sealed record CreateCourseCommand(
    Guid TeacherId, string Title, Money Price, PaymentMethod AcceptedPaymentMethods, IReadOnlyList<RefundTier> RefundRules, DateTimeOffset StartsAt);

/// <summary>Task 3.1/3.2: at least one payment method, refund rules must clear the platform floor.</summary>
public sealed class CreateCourseHandler(
    ICourseRepository courseRepository, ITeacherProfileRepository teacherRepository, IPlatformFeeSettingRepository feeSettingRepository)
{
    /// <summary>
    /// Task 2.3's spec scenario is explicit: an unverified (or no-profile) teacher calling this API gets
    /// HTTP 403, not a generic validation error — so this checks verification status itself and throws
    /// AuthorizationException, rather than letting TeacherProfile.GuardCanCreateOrPublishCourse's
    /// DomainException (mapped to 400 at the API boundary) leak through.
    /// </summary>
    public async Task<Course> HandleAsync(CreateCourseCommand command, CancellationToken ct)
    {
        var teacher = await teacherRepository.FindAsync(command.TeacherId, ct);
        if (teacher is null || !teacher.CanCreateOrPublishCourse)
        {
            throw new AuthorizationException("老師身分驗證狀態非「已認證」，無法建立課程。");
        }

        var currentSetting = await feeSettingRepository.GetCurrentAsync(ct)
            ?? throw new DomainException("platform_fee_setting_missing", "尚未設定平台退費底線。");

        var course = Course.Create(
            Guid.NewGuid(), command.TeacherId, command.Title, command.Price, command.AcceptedPaymentMethods,
            new RefundRuleSet(command.RefundRules), currentSetting.RefundFloor, command.StartsAt);

        await courseRepository.SaveAsync(course, ct);
        return course;
    }
}
