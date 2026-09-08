using System.Diagnostics;

namespace ALCops.Common.Settings;

/// <summary>
/// Serializes a load without memoizing cancellation exceptions. Waiting callers can cancel
/// independently; compilation entries keep failures, while workspace entries cool down HTTP retries.
/// </summary>
internal sealed class ALCopsSettingsCacheEntry
{
    private static readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly object _gate = new();
    private readonly Func<long> _getTickCount;
    private CachedResult? _cached;

    public ALCopsSettingsCacheEntry(Func<long>? getTickCount = null) =>
        _getTickCount = getTickCount ?? (() => _clock.ElapsedMilliseconds);

    public ALCopsSettingsLoadResult GetOrLoad(Func<ALCopsSettingsLoadResult> load, bool expireHttpFailures, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CachedResult? cached = Volatile.Read(ref _cached);
        if (cached is not null && IsCurrent(cached))
            return cached.Result;

        // A timed monitor wait permits cancellation without owning a disposable wait handle
        // in an entry whose lifetime is managed by a ConditionalWeakTable.
        while (!Monitor.TryEnter(_gate, millisecondsTimeout: 50))
            cancellationToken.ThrowIfCancellationRequested();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            cached = _cached;
            if (cached is not null && IsCurrent(cached))
                return cached.Result;
            ALCopsSettingsLoadResult result = load();
            cancellationToken.ThrowIfCancellationRequested();
            long? failedAt = expireHttpFailures && result.Failures.Any(failure => failure.RetryOnNextCompilation)
                ? _getTickCount()
                : null;
            // Publish result and expiry together. The cooldown starts after the request fails,
            // uses monotonic time and is not extended by cache hits while the user is editing.
            Volatile.Write(ref _cached, new CachedResult(result, failedAt));
            return result;
        }
        finally
        {
            Monitor.Exit(_gate);
        }
    }

    private bool IsCurrent(CachedResult cached) =>
        cached.FailedAt is not long failedAt || _getTickCount() - failedAt < 30_000;

    private sealed class CachedResult(ALCopsSettingsLoadResult result, long? failedAt)
    {
        public ALCopsSettingsLoadResult Result { get; } = result;
        public long? FailedAt { get; } = failedAt;
    }
}
