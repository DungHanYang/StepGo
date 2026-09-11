using StepGo.Shared.Application;
using StepGo.Identity.Domain;

namespace StepGo.Identity.Infrastructure;

/// <summary>
/// API Gateway's Cognito JWT authorizer verifies the token before the Lambda ever runs; the composition
/// root reads the already-validated claims off the request context and constructs this per-request.
/// </summary>
public sealed class JwtClaimsCurrentUserAccessor(Guid userId, Role role) : ICurrentUserAccessor
{
    public Guid UserId { get; } = userId;
    public Role Role { get; } = role;

    public static JwtClaimsCurrentUserAccessor FromClaims(IReadOnlyDictionary<string, string> claims)
    {
        var userId = Guid.Parse(claims["sub"]);
        var role = Enum.Parse<Role>(claims["custom:role"], ignoreCase: true);
        return new JwtClaimsCurrentUserAccessor(userId, role);
    }
}
