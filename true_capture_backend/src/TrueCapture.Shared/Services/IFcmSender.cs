namespace TrueCapture.Shared.Services;

public sealed record NotificationPayload(
    string                              Title,
    string                              Body,
    IReadOnlyDictionary<string, string>? Data = null);

public sealed record FcmMulticastResult(
    int                       SuccessCount,
    int                       FailureCount,
    IReadOnlyList<string>     InvalidTokens);   // tokens FCM reported Unregistered / InvalidArgument — safe to delete

public interface IFcmSender
{
    Task SendToTokenAsync(string token, NotificationPayload payload, CancellationToken ct = default);

    /// <summary>
    /// Multicasts to up to 500 tokens per call (FCM hard limit). Caller batches anything bigger.
    /// Result carries the list of tokens that should be pruned from the device table.
    /// </summary>
    Task<FcmMulticastResult> SendToTokensAsync(
        IReadOnlyList<string> tokens, NotificationPayload payload, CancellationToken ct = default);

    Task SendToTopicAsync(string topic, NotificationPayload payload, CancellationToken ct = default);

    Task SubscribeAsync  (IReadOnlyList<string> tokens, string topic, CancellationToken ct = default);
    Task UnsubscribeAsync(IReadOnlyList<string> tokens, string topic, CancellationToken ct = default);
}
