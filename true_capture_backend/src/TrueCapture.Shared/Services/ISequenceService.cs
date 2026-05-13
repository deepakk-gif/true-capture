namespace TrueCapture.Shared.Services;

public interface ISequenceService
{
    Task<string> NextAsync(string sequenceCode, CancellationToken ct = default);
}
