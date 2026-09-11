using StepGo.Application.Common;
using StepGo.Domain.Identity;

namespace StepGo.Application.Identity;

public sealed record SubmitTeacherVerificationCommand(
    Guid TeacherId, string RealName, string NationalId, string IdPhotoFrontObjectKey, string IdPhotoBackObjectKey, BankAccount PayoutAccount);

/// <summary>Task 2.2: payout account holder name must match the teacher's real name, or submission is rejected.</summary>
public sealed class SubmitTeacherVerificationHandler(ITeacherProfileRepository repository)
{
    public async Task<TeacherProfile> HandleAsync(SubmitTeacherVerificationCommand command, CancellationToken ct)
    {
        var profile = TeacherProfile.Submit(
            command.TeacherId, command.RealName, command.NationalId,
            command.IdPhotoFrontObjectKey, command.IdPhotoBackObjectKey, command.PayoutAccount);

        await repository.SaveAsync(profile, ct);
        return profile;
    }
}
