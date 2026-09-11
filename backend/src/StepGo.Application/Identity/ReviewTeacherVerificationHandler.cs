using StepGo.Application.Common;
using StepGo.Domain.Governance;
using StepGo.Domain.Identity;
using StepGo.Domain.SharedKernel;

namespace StepGo.Application.Identity;

public sealed record ReviewTeacherVerificationCommand(Guid TeacherId, bool Approve, string? RejectionReason);

/// <summary>Task 2.3: admin approve/reject with a reason on rejection; also logged to the audit trail.</summary>
public sealed class ReviewTeacherVerificationHandler(
    ITeacherProfileRepository repository, StepGo.Application.Governance.IChangeLogRepository changeLog, ICurrentUserAccessor currentUser, IClock clock)
{
    public async Task<TeacherProfile> HandleAsync(ReviewTeacherVerificationCommand command, CancellationToken ct)
    {
        if (currentUser.Role != Role.Admin)
        {
            throw new AuthorizationException("僅管理者可審核老師身分驗證。");
        }

        var profile = await repository.FindAsync(command.TeacherId, ct)
            ?? throw new DomainException("teacher_profile_not_found", "找不到該老師的身分驗證申請。");

        if (command.Approve)
        {
            profile.Approve();
        }
        else
        {
            profile.Reject(command.RejectionReason ?? string.Empty);
        }

        await repository.SaveAsync(profile, ct);
        await changeLog.AppendAsync(
            new ChangeLogEntry(Guid.NewGuid(), ChangeLogCategory.Verification,
                $"老師 {command.TeacherId} 驗證結果：{(command.Approve ? "通過" : "退回：" + command.RejectionReason)}",
                currentUser.UserId, clock.UtcNow),
            ct);

        return profile;
    }
}
