using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using StepGo.Application.Notifications;

namespace StepGo.Infrastructure.Aws;

/// <summary>Email is a core, always-on channel (task 9.4) — this is the concrete SES call behind IEmailSender.</summary>
public sealed class SesEmailSender(IAmazonSimpleEmailService client, string fromAddress) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct) => client.SendEmailAsync(new SendEmailRequest
    {
        Source = fromAddress,
        Destination = new Destination { ToAddresses = [toEmail] },
        Message = new Message
        {
            Subject = new Content(subject),
            Body = new Body { Text = new Content(body) },
        },
    }, ct);
}
