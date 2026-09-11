using StepGo.Shared.Domain;

namespace StepGo.Shared.Application;

/// <summary>The authenticated caller, resolved by StepGo.Api.* from the Cognito JWT's user id/role claims.</summary>
public interface ICurrentUserAccessor
{
    Guid UserId { get; }
    Role Role { get; }
}
