using System;

namespace Ficha_Tecnica.Services;

public class DatabaseConnectionProbeOptions
{
    public const string SectionName = "DatabaseConnectionProbe";

    public string ConnectionString { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 15;

    public TimeSpan ResolveTimeout()
    {
        return TimeoutSeconds > 0
            ? TimeSpan.FromSeconds(TimeoutSeconds)
            : TimeSpan.FromSeconds(15);
    }
}
