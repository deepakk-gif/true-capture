using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Services;

public sealed class SmtpEmailSender(
    IOptions<EmailOptions>      options,
    ILogger<SmtpEmailSender>    logger)
    : IEmailSender
{
    private readonly EmailOptions _opt = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.Host))
        {
            // Dev fallback: no SMTP configured — log the message body so OTPs are visible in console.
            logger.LogInformation(
                "Email (NO SMTP CONFIGURED) to {To}: {Subject}\n{Body}",
                message.ToEmail, message.Subject, message.BodyText);
            return;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_opt.FromName, _opt.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.ToEmail));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder { TextBody = message.BodyText };
        if (!string.IsNullOrWhiteSpace(message.BodyHtml))
            builder.HtmlBody = message.BodyHtml;
        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_opt.Host, _opt.Port,
            _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrWhiteSpace(_opt.Username))
            await client.AuthenticateAsync(_opt.Username, _opt.Password, ct);

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);
    }
}
