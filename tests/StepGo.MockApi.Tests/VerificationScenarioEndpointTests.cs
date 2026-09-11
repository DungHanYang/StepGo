using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StepGo.ApiClient.Dtos;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.MockApi.Tests;

public class VerificationScenarioEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public VerificationScenarioEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("filling", VerificationStatus.Filling)]
    [InlineData("underreview", VerificationStatus.UnderReview)]
    [InlineData("rejected", VerificationStatus.Rejected)]
    [InlineData("verified", VerificationStatus.Verified)]
    public async Task Each_Of_The_Four_Verification_States_Has_A_Switchable_Scenario(string scenario, VerificationStatus expected)
    {
        var response = await _client.GetAsync($"/api/verification-scenarios/{scenario}");
        response.EnsureSuccessStatusCode();

        var teacher = await response.Content.ReadFromJsonAsync<TeacherDto>();

        Assert.NotNull(teacher);
        Assert.Equal(expected, teacher!.VerificationStatus);
    }
}
