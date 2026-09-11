using System.Text;
using System.Text.Json;

namespace StepGo.ApiClient.Auth;

/// <summary>
/// Decodes the "role" claim out of a JWT payload — no signature verification, since the
/// frontend only needs the claim to decide where to redirect post-login (design.md decision 7:
/// "frontend-auth capability 只規範前端可觀察的行為...不假設特定認證協定的實作細節"). The real
/// issuer/verification lives entirely in the backend change.
/// </summary>
public static class JwtRoleParser
{
    public const string RoleClaimName = "role";

    public static bool TryGetRole(string jwt, out string? role)
    {
        role = null;

        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var document = JsonDocument.Parse(payloadJson);

            if (document.RootElement.TryGetProperty(RoleClaimName, out var roleElement) && roleElement.ValueKind == JsonValueKind.String)
            {
                role = roleElement.GetString();
                return !string.IsNullOrEmpty(role);
            }
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return false;
        }

        return false;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
