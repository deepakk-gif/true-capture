namespace TrueCapture.Shared.Services;

public sealed record EmailMessage(
    string  ToEmail,
    string  Subject,
    string  BodyText,
    string? BodyHtml = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
