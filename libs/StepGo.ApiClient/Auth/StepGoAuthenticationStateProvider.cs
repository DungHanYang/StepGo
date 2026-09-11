using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace StepGo.ApiClient.Auth;

/// <summary>
/// frontend-auth spec: "系統 SHALL...依帳號角色將使用者導向對應後台". Wraps token access and
/// exposes the decoded role as a claim so callers (e.g. route guards) can use the standard
/// <c>AuthenticationStateProvider</c> pattern instead of re-parsing the token themselves.
/// </summary>
public sealed class StepGoAuthenticationStateProvider(IAuthTokenStore tokenStore) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = tokenStore.GetToken();

        if (string.IsNullOrEmpty(token) || !JwtRoleParser.TryGetRole(token, out var role) || role is null)
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role)],
            authenticationType: "StepGoJwt");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    /// <summary>Call after a (mock) login issues a new token, so subscribers re-evaluate auth state.</summary>
    public void NotifyTokenChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
