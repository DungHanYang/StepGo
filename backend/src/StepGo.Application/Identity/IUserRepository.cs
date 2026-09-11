using StepGo.Domain.Identity;

namespace StepGo.Application.Identity;

public interface IUserRepository
{
    Task<UserAccount?> FindAsync(Guid userId, CancellationToken ct);
    Task SaveAsync(UserAccount user, CancellationToken ct);
}
