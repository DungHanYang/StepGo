using StepGo.Shared.Application;
using StepGo.Identity.Domain;

namespace StepGo.Identity.Application;

public sealed record RegisterUserCommand(string FullName, string PhoneNumber, string? Email, Role Role);

/// <summary>Task 2.1: phone number is mandatory, email optional.</summary>
public sealed class RegisterUserHandler(IUserRepository userRepository, IClock clock)
{
    public async Task<UserAccount> HandleAsync(RegisterUserCommand command, CancellationToken ct)
    {
        var user = UserAccount.Register(Guid.NewGuid(), command.FullName, command.PhoneNumber, command.Email, command.Role, clock.UtcNow);
        await userRepository.SaveAsync(user, ct);
        return user;
    }
}
