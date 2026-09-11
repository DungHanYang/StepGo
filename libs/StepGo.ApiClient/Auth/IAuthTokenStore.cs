namespace StepGo.ApiClient.Auth;

/// <summary>Abstracts where the JWT lives so <see cref="StepGoAuthenticationStateProvider"/> is
/// testable without a browser. Real apps supply a localStorage-backed implementation.</summary>
public interface IAuthTokenStore
{
    string? GetToken();

    void SetToken(string? token);
}

public sealed class InMemoryAuthTokenStore : IAuthTokenStore
{
    private string? _token;

    public string? GetToken() => _token;

    public void SetToken(string? token) => _token = token;
}
