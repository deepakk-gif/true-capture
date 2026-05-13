using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace TrueCapture.Infrastructure.Http;

public interface IThirdPartyApiLogger
{
    Task LogAsync(
        string serviceName,
        string path,
        string? requestBody,
        string? responseBody,
        int statusCode,
        TimeSpan elapsed,
        CancellationToken ct = default);
}

public sealed class LoggerThirdPartyApiLogger(ILogger<LoggerThirdPartyApiLogger> log) : IThirdPartyApiLogger
{
    public Task LogAsync(string serviceName, string path, string? requestBody, string? responseBody,
                         int statusCode, TimeSpan elapsed, CancellationToken ct = default)
    {
        log.LogInformation("3P {Service} {Path} status={Status} elapsed={Elapsed}ms",
            serviceName, path, statusCode, elapsed.TotalMilliseconds);
        return Task.CompletedTask;
    }
}

public abstract class ThirdPartyApiClientBase(
    IHttpClientFactory   httpFactory,
    IThirdPartyApiLogger logger,
    string               serviceName)
{
    protected async Task<TResponse?> PostAsync<TResponse>(
        string path, object body, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var client = httpFactory.CreateClient(serviceName);
        try
        {
            using var resp = await client.PostAsJsonAsync(path, body, ct);
            var content   = await resp.Content.ReadAsStringAsync(ct);
            await logger.LogAsync(serviceName, path, body.ToString(), content, (int)resp.StatusCode, sw.Elapsed, ct);
            resp.EnsureSuccessStatusCode();
            return string.IsNullOrWhiteSpace(content)
                ? default
                : System.Text.Json.JsonSerializer.Deserialize<TResponse>(content);
        }
        catch (Exception ex)
        {
            await logger.LogAsync(serviceName, path, body.ToString(), ex.Message, 0, sw.Elapsed, ct);
            throw;
        }
    }
}
