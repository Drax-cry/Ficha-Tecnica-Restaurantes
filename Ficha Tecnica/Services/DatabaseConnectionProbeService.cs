using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Ficha_Tecnica.Services;

public class DatabaseConnectionProbeService : IHostedService
{
    private readonly ILogger<DatabaseConnectionProbeService> _logger;
    private readonly DatabaseConnectionProbeOptions _options;
    private Task? _probeTask;

    public DatabaseConnectionProbeService(
        IOptions<DatabaseConnectionProbeOptions> options,
        ILogger<DatabaseConnectionProbeService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogInformation(
                "Database connection probe skipped because no connection string was available. Configure {Section}:{Key} to enab"
                + "le the startup probe.",
                DatabaseConnectionProbeOptions.SectionName,
                nameof(DatabaseConnectionProbeOptions.ConnectionString));

            return Task.CompletedTask;
        }

        _probeTask = Task.Run(() => ProbeAsync(cancellationToken), CancellationToken.None);

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
            _logger.LogDebug("Database connection probe stop request cancelled before completion.");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Database connection probe task faulted during shutdown.");
        }
    }

    private async Task ProbeAsync(CancellationToken cancellationToken)
    {
        var timeout = _options.ResolveTimeout();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await using var connection = new MySqlConnection(_options.ConnectionString);
            await connection.OpenAsync(linkedCts.Token);

            stopwatch.Stop();

            _logger.LogInformation(
                "Database connection probe succeeded in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException ex) when (linkedCts.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Database connection probe timed out after {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Database connection probe failed after {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
