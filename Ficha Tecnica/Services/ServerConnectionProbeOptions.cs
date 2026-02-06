using System;

namespace Ficha_Tecnica.Services;

public class ServerConnectionProbeOptions
{
    public const string SectionName = "ServerConnectionProbe";

    public string? Url { get; set; }

    public int TimeoutSeconds { get; set; } = 10;

    internal TimeSpan ResolveTimeout()
    {
        var seconds = Math.Clamp(TimeoutSeconds, 1, 300);
        return TimeSpan.FromSeconds(seconds);
    }
}
