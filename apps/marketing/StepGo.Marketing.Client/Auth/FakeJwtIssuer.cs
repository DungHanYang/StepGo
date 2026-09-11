using System.Text;
using System.Text.Json;

namespace StepGo.Marketing.Client.Auth;

/// <summary>
/// Issues an unsigned, JWT-shaped token carrying a role claim — Mock-first stand-in for the
/// real Cognito/backend-issued JWT (design.md decision 7/8), used only so the login page's
/// role-based redirect logic (frontend-auth spec) has something real to decode.
/// </summary>
public static class FakeJwtIssuer
{
    public static string Issue(string role, string subject)
    {
        var header = Base64UrlEncode("""{"alg":"none","typ":"JWT"}"""u8.ToArray());
        var payloadJson = JsonSerializer.Serialize(new { role, sub = subject });
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
