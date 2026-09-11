using StepGo.MockApi.Scenarios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Each group covers one state machine from the specs (task 8.1). ?scenario=<name> selects
// one named state; omitting it returns every available scenario so a caller/test can see the
// whole state machine at once.

app.MapGet("/api/payment-scenarios/{scenario?}", (string? scenario) =>
    scenario is null
        ? Results.Ok(PaymentScenarios.All)
        : PaymentScenarios.All.TryGetValue(scenario, out var order) ? Results.Ok(order) : Results.NotFound())
    .WithName("GetPaymentScenario")
    .WithOpenApi();

app.MapGet("/api/verification-scenarios/{scenario?}", (string? scenario) =>
    scenario is null
        ? Results.Ok(VerificationScenarios.All)
        : VerificationScenarios.All.TryGetValue(scenario, out var teacher) ? Results.Ok(teacher) : Results.NotFound())
    .WithName("GetVerificationScenario")
    .WithOpenApi();

app.MapGet("/api/payout-account-scenarios/{scenario?}", (string? scenario) =>
    scenario is null
        ? Results.Ok(PayoutAccountScenarios.All)
        : PayoutAccountScenarios.All.TryGetValue(scenario, out var teacher) ? Results.Ok(teacher) : Results.NotFound())
    .WithName("GetPayoutAccountScenario")
    .WithOpenApi();

app.MapGet("/api/refund-ticket-scenarios/{scenario?}", (string? scenario) =>
    scenario is null
        ? Results.Ok(RefundTicketScenarios.All)
        : RefundTicketScenarios.All.TryGetValue(scenario, out var ticket) ? Results.Ok(ticket) : Results.NotFound())
    .WithName("GetRefundTicketScenario")
    .WithOpenApi();

app.Run();

public partial class Program;
