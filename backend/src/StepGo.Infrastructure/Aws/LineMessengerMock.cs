using StepGo.Application.Notifications;

namespace StepGo.Infrastructure.Aws;

/// <summary>
/// Safe no-op stand-in until the official LINE 官方帳號 and Messaging API channel are provisioned
/// (proposal.md dependency note — LINE is opt-in value-add, never blocking for other capabilities).
/// Swap for a real Messaging API HTTP client once the channel access token is available in Secrets Manager.
/// </summary>
public sealed class LineMessengerMock : ILineMessenger
{
    public Task PushAsync(string lineUserId, string message, CancellationToken ct) => Task.CompletedTask;
}
