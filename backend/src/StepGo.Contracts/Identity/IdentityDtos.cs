namespace StepGo.Contracts.Identity;

public enum RoleDto
{
    Student,
    Teacher,
    Admin,
}

public enum TeacherVerificationStatusDto
{
    PendingReview,
    Verified,
    Rejected,
}

public sealed record RegisterUserRequestDto(string FullName, string PhoneNumber, string? Email, RoleDto Role);

public sealed record UserDto(Guid Id, string FullName, string PhoneNumber, string? Email, RoleDto Role, DateTimeOffset CreatedAt);

public sealed record BankAccountDto(string BankCode, string AccountNumber, string AccountHolderName);

public sealed record SubmitTeacherVerificationRequestDto(
    string RealName, string NationalId, string IdPhotoFrontObjectKey, string IdPhotoBackObjectKey, BankAccountDto PayoutAccount);

public sealed record TeacherVerificationDto(
    Guid TeacherId, string RealName, BankAccountDto PayoutAccount, TeacherVerificationStatusDto Status, string? RejectionReason);

public sealed record ReviewTeacherVerificationRequestDto(bool Approve, string? RejectionReason);
