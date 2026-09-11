using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StepGo.ApiClient.Dtos;
using StepGo.UI.Status;
using Xunit;

namespace StepGo.MockApi.Tests;

public class RefundTicketScenarioEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RefundTicketScenarioEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("pendingteacherreview", RefundTicketStatus.PendingTeacherReview)]
    [InlineData("teacherapproved", RefundTicketStatus.TeacherApproved)]
    [InlineData("teacherrejected", RefundTicketStatus.TeacherRejected)]
    [InlineData("autoapproved", RefundTicketStatus.AutoApproved)]
    [InlineData("escalated", RefundTicketStatus.Escalated)]
    [InlineData("arbitrationresolved", RefundTicketStatus.ArbitrationResolved)]
    [InlineData("completed", RefundTicketStatus.Completed)]
    public async Task Each_Refund_Ticket_Stage_Has_A_Switchable_Scenario(string scenario, RefundTicketStatus expected)
    {
        var response = await _client.GetAsync($"/api/refund-ticket-scenarios/{scenario}");
        response.EnsureSuccessStatusCode();

        var ticket = await response.Content.ReadFromJsonAsync<RefundTicketDto>();

        Assert.NotNull(ticket);
        Assert.Equal(expected, ticket!.Status);
        Assert.True(ticket.SystemCalculatedRefundAmount <= ticket.OriginalPaymentAmount);
    }
}
