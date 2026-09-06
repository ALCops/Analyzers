namespace ALCops.Common.Settings;

/// <summary>
/// Serializes a load without memoizing cancellation exceptions. Waiting callers can cancel
/// independently; compilation entries keep failures, while workspace entries can retry HTTP failures.
/// </summary>
internal sealed class ALCopsSettingsCacheEntry
{
    private readonly object _gate = new();
    private ALCopsSettingsLoadResult? _result;

    public ALCopsSettingsLoadResult GetOrLoad(Func<ALCopsSettingsLoadResult> load, bool cacheHttpFailures, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ALCopsSettingsLoadResult? cached = Volatile.Read(ref _result);
        if (cached is not null)
            return cached;

        // A timed monitor wait permits cancellation without owning a disposable wait handle
        // in an entry whose lifetime is managed by a ConditionalWeakTable.
        while (!Monitor.TryEnter(_gate, millisecondsTimeout: 50))
            cancellationToken.ThrowIfCancellationRequested();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_result is not null)
                return _result;
            ALCopsSettingsLoadResult result = load();
            cancellationToken.ThrowIfCancellationRequested();
            if (cacheHttpFailures || !result.Failures.Any(failure => failure.RetryOnNextCompilation))
                Volatile.Write(ref _result, result);
            return result;
        }
        finally
        {
            Monitor.Exit(_gate);
        }
    }
}
