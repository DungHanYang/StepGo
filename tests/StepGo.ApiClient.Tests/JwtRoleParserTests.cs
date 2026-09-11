using System.Text;
using StepGo.ApiClient.Auth;
using Xunit;

namespace StepGo.ApiClient.Tests;

public class JwtRoleParserTests
{
    private static string BuildToken(string payloadJson)
    {
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}"u8.ToArray());
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Theory]
    [InlineData("teacher")]
    [InlineData("student")]
    public void Decodes_Role_Claim(string role)
    {
        var token = BuildToken($"{{\"role\":\"{role}\"}}");

        var ok = JwtRoleParser.TryGetRole(token, out var decoded);

        Assert.True(ok);
        Assert.Equal(role, decoded);
    }

    [Fact]
    public void Missing_Role_Claim_Returns_False()
    {
        var token = BuildToken("{\"sub\":\"someone\"}");

        var ok = JwtRoleParser.TryGetRole(token, out var role);

        Assert.False(ok);
        Assert.Null(role);
    }

    [Fact]
    public void Malformed_Token_Returns_False()
    {
        var ok = JwtRoleParser.TryGetRole("not-a-jwt", out var role);

        Assert.False(ok);
        Assert.Null(role);
    }
}
