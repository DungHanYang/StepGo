using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Api.Shared.Routing;
using StepGo.Identity.Application;
using StepGo.Contracts.Identity;
using StepGo.Contracts.Json;
using StepGo.Identity.Domain;

namespace StepGo.Api.Identity;

public static class IdentityRoutes
{
    public static MiniRouter Map(MiniRouter router, CompositionRoot root, StepGoJsonContext contractsJson) => router
        .MapHealthCheck("/identity/health")
        .MapPost("/users", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.RegisterUserRequestDto)
                ?? throw new StepGo.Shared.Domain.DomainException("invalid_request", "請求內容不正確。");

            var handler = new RegisterUserHandler(root.UserRepository, root.Clock);
            var user = await handler.HandleAsync(new RegisterUserCommand(request.FullName, request.PhoneNumber, request.Email, MapRole(request.Role)), ct);

            return JsonResponses.Created(new UserDto(user.Id, user.FullName, user.PhoneNumber, user.Email, MapRoleDto(user.Role), user.CreatedAt), contractsJson.UserDto);
        }, requiresAuth: false)
        .MapPost("/teachers/{teacherId}/verification", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.SubmitTeacherVerificationRequestDto)
                ?? throw new StepGo.Shared.Domain.DomainException("invalid_request", "請求內容不正確。");
            var teacherId = Guid.Parse(ctx.PathParameters["teacherId"]);

            var handler = new SubmitTeacherVerificationHandler(root.TeacherProfileRepository);
            var profile = await handler.HandleAsync(new SubmitTeacherVerificationCommand(
                teacherId, request.RealName, request.NationalId, request.IdPhotoFrontObjectKey, request.IdPhotoBackObjectKey,
                new BankAccount(request.PayoutAccount.BankCode, request.PayoutAccount.AccountNumber, request.PayoutAccount.AccountHolderName)), ct);

            return JsonResponses.Created(ToDto(profile), contractsJson.TeacherVerificationDto);
        })
        .MapPost("/teachers/{teacherId}/verification/review", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.ReviewTeacherVerificationRequestDto)
                ?? throw new StepGo.Shared.Domain.DomainException("invalid_request", "請求內容不正確。");
            var teacherId = Guid.Parse(ctx.PathParameters["teacherId"]);

            var handler = new ReviewTeacherVerificationHandler(root.TeacherProfileRepository, root.ChangeLogRepository, ctx.CurrentUser, root.Clock);
            var profile = await handler.HandleAsync(new ReviewTeacherVerificationCommand(teacherId, request.Approve, request.RejectionReason), ct);

            return JsonResponses.Ok(ToDto(profile), contractsJson.TeacherVerificationDto);
        })
        .MapGet("/teachers/{teacherId}/verification", async (ctx, ct) =>
        {
            var teacherId = Guid.Parse(ctx.PathParameters["teacherId"]);
            RowLevelAccessGuard.GuardOwnsTeacherResource(ctx.CurrentUser, teacherId);

            var profile = await root.TeacherProfileRepository.FindAsync(teacherId, ct)
                ?? throw new StepGo.Shared.Domain.DomainException("teacher_profile_not_found", "找不到該老師的身分驗證申請。");

            return JsonResponses.Ok(ToDto(profile), contractsJson.TeacherVerificationDto);
        });

    private static Role MapRole(RoleDto role) => role switch
    {
        RoleDto.Student => Role.Student,
        RoleDto.Teacher => Role.Teacher,
        RoleDto.Admin => Role.Admin,
        _ => throw new StepGo.Shared.Domain.DomainException("invalid_role", "不合法的角色。"),
    };

    private static RoleDto MapRoleDto(Role role) => role switch
    {
        Role.Student => RoleDto.Student,
        Role.Teacher => RoleDto.Teacher,
        Role.Admin => RoleDto.Admin,
        _ => throw new StepGo.Shared.Domain.DomainException("invalid_role", "不合法的角色。"),
    };

    private static TeacherVerificationDto ToDto(TeacherProfile profile) => new(
        profile.Id, profile.RealName,
        new BankAccountDto(profile.PayoutAccount.BankCode, profile.PayoutAccount.AccountNumber, profile.PayoutAccount.AccountHolderName),
        (TeacherVerificationStatusDto)profile.VerificationStatus, profile.RejectionReason);
}
