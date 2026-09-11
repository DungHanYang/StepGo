using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StepGo.ApiClient.Dtos;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.MockApi.Tests;

public class PaymentScenarioEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PaymentScenarioEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("pending", PaymentStatus.Pending)]
    [InlineData("processing", PaymentStatus.Processing)]
    [InlineData("paid", PaymentStatus.Paid)]
    [InlineData("failed", PaymentStatus.Failed)]
    [InlineData("refunded", PaymentStatus.Refunded)]
    public async Task Each_Of_The_Five_Payment_States_Has_A_Switchable_Scenario_Matching_The_OrderDto_Shape(string scenario, PaymentStatus expected)
    {
        var response = await _client.GetAsync($"/api/payment-scenarios/{scenario}");
        response.EnsureSuccessStatusCode();

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.NotNull(order);
        Assert.Equal(expected, order!.PaymentStatus);
        Assert.False(string.IsNullOrEmpty(order.Id));
        Assert.False(string.IsNullOrEmpty(order.CourseName));
    }

    [Fact]
    public async Task Unknown_Scenario_Returns_404()
    {
        var response = await _client.GetAsync("/api/payment-scenarios/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Omitting_Scenario_Returns_All_Five_States()
    {
        var response = await _client.GetAsync("/api/payment-scenarios");
        response.EnsureSuccessStatusCode();

        var all = await response.Content.ReadFromJsonAsync<Dictionary<string, OrderDto>>();

        Assert.NotNull(all);
        Assert.Equal(5, all!.Count);
    }
}
