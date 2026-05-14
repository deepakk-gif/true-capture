using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrueCapture.Shared.Services;

namespace TrueCapture.Infrastructure.Services;

/// <summary>
/// Firebase Admin SDK wrapper. Lazy-initialises a single process-wide <see cref="FirebaseApp"/>
/// the first time a send is attempted. When the configured service-account JSON is missing or empty
/// (e.g. before the developer has pasted credentials), every call no-ops and logs the payload
/// — same dev-fallback contract as <see cref="SmtpEmailSender"/>.
/// </summary>
public sealed class FirebaseFcmSender : IFcmSender
{
    private readonly FirebaseOptions             _opt;
    private readonly IHostEnvironment            _env;
    private readonly ILogger<FirebaseFcmSender>  _log;
    private readonly Lazy<FirebaseMessaging?>    _messaging;

    public FirebaseFcmSender(
        IOptions<FirebaseOptions>   options,
        IHostEnvironment            env,
        ILogger<FirebaseFcmSender>  log)
    {
        _opt       = options.Value;
        _env       = env;
        _log       = log;
        _messaging = new Lazy<FirebaseMessaging?>(InitMessaging, isThreadSafe: true);
    }

    public Task SendToTokenAsync(string token, NotificationPayload payload, CancellationToken ct = default)
    {
        var messaging = _messaging.Value;
        if (messaging is null)
        {
            _log.LogInformation(
                "FCM (NO SERVICE ACCOUNT) to token {Token}: {Title} — {Body}",
                Mask(token), payload.Title, payload.Body);
            return Task.CompletedTask;
        }

        return messaging.SendAsync(new Message
        {
            Token        = token,
            Notification = ToNotification(payload),
            Data         = ToData(payload.Data),
        }, ct);
    }

    public async Task<FcmMulticastResult> SendToTokensAsync(
        IReadOnlyList<string> tokens, NotificationPayload payload, CancellationToken ct = default)
    {
        if (tokens.Count == 0)
            return new FcmMulticastResult(0, 0, []);

        var messaging = _messaging.Value;
        if (messaging is null)
        {
            _log.LogInformation(
                "FCM (NO SERVICE ACCOUNT) multicast to {Count} tokens: {Title} — {Body}",
                tokens.Count, payload.Title, payload.Body);
            return new FcmMulticastResult(tokens.Count, 0, []);
        }

        var message = new MulticastMessage
        {
            Tokens       = tokens.ToList(),
            Notification = ToNotification(payload),
            Data         = ToData(payload.Data),
        };

        var batch   = await messaging.SendEachForMulticastAsync(message, ct);
        var invalid = new List<string>();
        for (var i = 0; i < batch.Responses.Count; i++)
        {
            var r = batch.Responses[i];
            if (r.IsSuccess) continue;
            var code = r.Exception?.MessagingErrorCode;
            if (code is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                invalid.Add(tokens[i]);
        }

        return new FcmMulticastResult(batch.SuccessCount, batch.FailureCount, invalid);
    }

    public Task SendToTopicAsync(string topic, NotificationPayload payload, CancellationToken ct = default)
    {
        var messaging = _messaging.Value;
        if (messaging is null)
        {
            _log.LogInformation(
                "FCM (NO SERVICE ACCOUNT) to topic {Topic}: {Title} — {Body}",
                topic, payload.Title, payload.Body);
            return Task.CompletedTask;
        }

        return messaging.SendAsync(new Message
        {
            Topic        = topic,
            Notification = ToNotification(payload),
            Data         = ToData(payload.Data),
        }, ct);
    }

    public async Task SubscribeAsync(IReadOnlyList<string> tokens, string topic, CancellationToken ct = default)
    {
        if (tokens.Count == 0) return;
        var messaging = _messaging.Value;
        if (messaging is null)
        {
            _log.LogInformation("FCM (NO SERVICE ACCOUNT) subscribe {Count} tokens to {Topic}", tokens.Count, topic);
            return;
        }
        await messaging.SubscribeToTopicAsync(tokens.ToList(), topic);
    }

    public async Task UnsubscribeAsync(IReadOnlyList<string> tokens, string topic, CancellationToken ct = default)
    {
        if (tokens.Count == 0) return;
        var messaging = _messaging.Value;
        if (messaging is null)
        {
            _log.LogInformation("FCM (NO SERVICE ACCOUNT) unsubscribe {Count} tokens from {Topic}", tokens.Count, topic);
            return;
        }
        await messaging.UnsubscribeFromTopicAsync(tokens.ToList(), topic);
    }

    private FirebaseMessaging? InitMessaging()
    {
        var path = _opt.ServiceAccountJsonPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            _log.LogWarning("Firebase:ServiceAccountJsonPath is empty — FCM disabled (dev fallback).");
            return null;
        }

        var resolved = Path.IsPathRooted(path) ? path : Path.Combine(_env.ContentRootPath, path);

        if (!File.Exists(resolved))
        {
            _log.LogWarning("Firebase service-account file not found at {Path} — FCM disabled (dev fallback).", resolved);
            return null;
        }

        // Empty or "{}" placeholder → treat as not configured. Real service-account JSONs always include
        // a "type":"service_account" field; we use that as the discriminator.
        var content = File.ReadAllText(resolved);
        if (string.IsNullOrWhiteSpace(content) || !content.Contains("service_account"))
        {
            _log.LogWarning("Firebase service-account file at {Path} is a placeholder — FCM disabled (dev fallback).", resolved);
            return null;
        }

        try
        {
            // FirebaseApp is process-global; guard against double-create when tests host the app twice.
            var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(resolved),
                ProjectId  = string.IsNullOrWhiteSpace(_opt.ProjectId) ? null : _opt.ProjectId,
            });
            return FirebaseMessaging.GetMessaging(app);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to initialise FirebaseApp from {Path} — FCM disabled.", resolved);
            return null;
        }
    }

    private static Notification ToNotification(NotificationPayload p) =>
        new() { Title = p.Title, Body = p.Body };

    private static Dictionary<string, string>? ToData(IReadOnlyDictionary<string, string>? data) =>
        data is null || data.Count == 0 ? null : new Dictionary<string, string>(data);

    private static string Mask(string token) =>
        token.Length <= 8 ? "****" : $"{token[..4]}…{token[^4..]}";
}
