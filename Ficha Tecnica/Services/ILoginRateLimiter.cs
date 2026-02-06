using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ficha_Tecnica.Services;

public interface ILoginRateLimiter
{
    Task<TimeSpan> GetDelayAsync(string key, CancellationToken cancellationToken);

    Task<LoginAttemptResult> RegisterFailedAttemptAsync(string key, CancellationToken cancellationToken);

    void ResetAttempts(string key);
}

public sealed record LoginAttemptResult(int FailureCount, TimeSpan EnforcedDelay);
