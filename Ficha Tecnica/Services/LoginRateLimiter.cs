using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Ficha_Tecnica.Services;

public sealed class LoginRateLimiter : ILoginRateLimiter
{
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromMinutes(15);
    private readonly IMemoryCache _cache;

    public LoginRateLimiter(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public Task<TimeSpan> GetDelayAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();

        if (_cache.TryGetValue<LoginAttemptState>(key, out var state) && state.LockoutEnd > DateTimeOffset.UtcNow)
        {
            return Task.FromResult(state.LockoutEnd - DateTimeOffset.UtcNow);
        }

        return Task.FromResult(TimeSpan.Zero);
    }

    public Task<LoginAttemptResult> RegisterFailedAttemptAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        cancellationToken.ThrowIfCancellationRequested();

        var state = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = EntryLifetime;
            return new LoginAttemptState();
        })!;

        var result = state.RegisterFailure();
        _cache.Set(key, state, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = EntryLifetime
        });

        return Task.FromResult(result);
    }

    public void ResetAttempts(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _cache.Remove(key);
    }

    private sealed class LoginAttemptState
    {
        private const double MaxDelaySeconds = 30;
        private const double BaseDelaySeconds = 1.5;

        private readonly object _syncRoot = new();

        public int FailureCount { get; private set; }
        public DateTimeOffset LockoutEnd { get; private set; } = DateTimeOffset.MinValue;
        public TimeSpan EnforcedDelay { get; private set; } = TimeSpan.Zero;

        public LoginAttemptResult RegisterFailure()
        {
            lock (_syncRoot)
            {
                FailureCount++;
                var delaySeconds = Math.Min(MaxDelaySeconds, Math.Pow(2, FailureCount - 1) * BaseDelaySeconds);
                EnforcedDelay = TimeSpan.FromSeconds(delaySeconds);
                LockoutEnd = DateTimeOffset.UtcNow.Add(EnforcedDelay);

                return new LoginAttemptResult(FailureCount, EnforcedDelay);
            }
        }
    }
}
