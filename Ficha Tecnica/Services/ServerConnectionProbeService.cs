using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ficha_Tecnica.Services;

public class ServerConnectionProbeService : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServerConnectionProbeService> _logger;
    private readonly ServerConnectionProbeOptions _options;
    private Task? _probeTask;

    public ServerConnectionProbeService(
        IHttpClientFactory httpClientFactory,
        IOptions<ServerConnectionProbeOptions> options,
        ILogger<ServerConnectionProbeService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Url))
        {
            _logger.LogInformation(
                "Server connection probe skipped because no URL was configured. Set {Section}:{Key} or the SERVER_PROBE_URL environment variable to enable the startup probe.",
                ServerConnectionProbeOptions.SectionName,
                nameof(ServerConnectionProbeOptions.Url));

            return Task.CompletedTask;
        }

        if (!Uri.TryCreate(_options.Url, UriKind.Absolute, out var targetUri))
        {
            _logger.LogWarning(
                "Server connection probe skipped because the configured URL '{ProbeUrl}' is not a valid absolute URI.",
                _options.Url);

            return Task.CompletedTask;
        }

        _probeTask = Task.Run(() => ProbeAsync(targetUri, cancellationToken), CancellationToken.None);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_probeTask is null)
        {
            return;
        }

        try
        {
            await _probeTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Server connection probe stop request cancelled before completion.");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Server connection probe task faulted during shutdown.");
        }
    }

    private async Task ProbeAsync(Uri targetUri, CancellationToken cancellationToken)
    {
        var timeout = _options.ResolveTimeout();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ProbeUrl"] = targetUri.ToString()
        });

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var httpClient = _httpClientFactory.CreateClient(nameof(ServerConnectionProbeService));
            httpClient.Timeout = Timeout.InfiniteTimeSpan;

            using var response = await httpClient.GetAsync(targetUri, linkedCts.Token);

            stopwatch.Stop();

            var durationMs = stopwatch.ElapsedMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Server connection probe succeeded with status code {StatusCode} in {DurationMs}ms.",
                    (int)response.StatusCode,
                    durationMs);

                return;
            }

            var responseSummary = await SummarizeResponseAsync(response).ConfigureAwait(false);

            _logger.LogWarning(
                "Server connection probe completed with status code {StatusCode} in {DurationMs}ms. Reason: {ReasonPhrase}. {ResponseSummary}",
                (int)response.StatusCode,
                durationMs,
                response.ReasonPhrase ?? string.Empty,
                responseSummary);
        }
        catch (OperationCanceledException ex) when (linkedCts.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Server connection probe timed out after {DurationMs}ms.", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Server connection probe failed after {DurationMs}ms.", stopwatch.ElapsedMilliseconds);
        }
    }

    private static async Task<string> SummarizeResponseAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(body))
            {
                return "Response body was empty.";
            }

            const int maxLength = 512;

            if (body.Length > maxLength)
            {
                body = body[..maxLength] + "…";
            }

            return $"Body: {body}";
        }
        catch (Exception ex)
        {
            return $"Response body could not be read: {ex.Message}";
        }
    }
}
