using StepGo.Identity.Domain;

namespace StepGo.Identity.Application;

public interface IUserRepository
{
    Task<UserAccount?> FindAsync(Guid userId, CancellationToken ct);
    Task SaveAsync(UserAccount user, CancellationToken ct);
}
