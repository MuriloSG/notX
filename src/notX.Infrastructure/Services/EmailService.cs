using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using notX.Application.Interfaces;
using notX.Infrastructure.Settings;
using notX.Shared.Results;

namespace notX.Infrastructure.Services;

internal sealed class EmailService(
    IOptions<SmtpSettings> smtpOptions,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpSettings _smtp = smtpOptions.Value;

    public async Task<Result> SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SMTP config — Host: {Host}:{Port}, Username: {Username}, FromEmail: {FromEmail}",
            _smtp.Host, _smtp.Port, _smtp.Username, _smtp.FromEmail);

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port,
                _smtp.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation("Email sent to {Recipient} with subject '{Subject}'", to, subject);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Recipient}", to);
            return Result.Failure(new Error("Email.SendFailed", ex.Message));
        }
    }
}
