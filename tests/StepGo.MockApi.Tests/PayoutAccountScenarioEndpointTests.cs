using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StepGo.ApiClient.Dtos;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.MockApi.Tests;

public class PayoutAccountScenarioEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PayoutAccountScenarioEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("confirmed", PayoutAccountStatus.Confirmed)]
    [InlineData("changing", PayoutAccountStatus.Changing)]
    [InlineData("reconciling", PayoutAccountStatus.Reconciling)]
    [InlineData("bankrejected", PayoutAccountStatus.BankRejected)]
    public async Task Each_Of_The_Four_Payout_Account_States_Has_A_Switchable_Scenario(string scenario, PayoutAccountStatus expected)
    {
        var response = await _client.GetAsync($"/api/payout-account-scenarios/{scenario}");
        response.EnsureSuccessStatusCode();

        var teacher = await response.Content.ReadFromJsonAsync<TeacherDto>();

        Assert.NotNull(teacher);
        Assert.Equal(expected, teacher!.PayoutAccountStatus);
    }

    [Fact]
    public async Task BankRejected_Scenario_Includes_A_Rejection_Reason()
    {
        var response = await _client.GetAsync("/api/payout-account-scenarios/bankrejected");
        var teacher = await response.Content.ReadFromJsonAsync<TeacherDto>();

        Assert.False(string.IsNullOrEmpty(teacher!.PayoutAccountBankRejectionReason));
    }
}
