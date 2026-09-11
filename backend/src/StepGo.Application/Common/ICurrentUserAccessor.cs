using StepGo.Domain.Identity;

namespace StepGo.Application.Common;

/// <summary>The authenticated caller, resolved by StepGo.Api.* from the Cognito JWT's user id/role claims.</summary>
public interface ICurrentUserAccessor
{
    Guid UserId { get; }
    Role Role { get; }
}
