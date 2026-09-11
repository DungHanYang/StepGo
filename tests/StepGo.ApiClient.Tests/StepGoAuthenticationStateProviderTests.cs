using System.Security.Claims;
using System.Text;
using StepGo.ApiClient.Auth;
using Xunit;

namespace StepGo.ApiClient.Tests;

public class StepGoAuthenticationStateProviderTests
{
    private static string BuildToken(string role)
    {
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}"u8.ToArray());
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes($"{{\"role\":\"{role}\"}}"));
        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Theory]
    [InlineData("teacher")]
    [InlineData("student")]
    public async Task Different_Role_Claims_Produce_Matching_Redirect_Target(string role)
    {
        var store = new InMemoryAuthTokenStore();
        store.SetToken(BuildToken(role));
        var provider = new StepGoAuthenticationStateProvider(store);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.Equal(role, state.User.FindFirst(ClaimTypes.Role)?.Value);

        var baseUrls = new PortalBaseUrls("https://teach.stepgo.tw", "https://learn.stepgo.tw");
        var redirect = PortalRedirect.GetRedirectUrl(state.User.FindFirst(ClaimTypes.Role)?.Value, baseUrls);

        var expected = role == "teacher" ? "https://teach.stepgo.tw/" : "https://learn.stepgo.tw/my-courses";
        Assert.Equal(expected, redirect);
    }

    [Fact]
    public async Task No_Token_Produces_Unauthenticated_State()
    {
        var provider = new StepGoAuthenticationStateProvider(new InMemoryAuthTokenStore());

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }
}
