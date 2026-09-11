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
    public async Task<Course> HandleAsync(CreateCourseCommand command, CancellationToken ct)
    {
        var teacher = await teacherRepository.FindAsync(command.TeacherId, ct)
            ?? throw new DomainException("teacher_profile_not_found", "老師尚未提交身分驗證。");
        teacher.GuardCanCreateOrPublishCourse();

        var currentSetting = await feeSettingRepository.GetCurrentAsync(ct)
            ?? throw new DomainException("platform_fee_setting_missing", "尚未設定平台退費底線。");

        var course = Course.Create(
            Guid.NewGuid(), command.TeacherId, command.Title, command.Price, command.AcceptedPaymentMethods,
            new RefundRuleSet(command.RefundRules), currentSetting.RefundFloor, command.StartsAt);

        await courseRepository.SaveAsync(course, ct);
        return course;
    }
}
