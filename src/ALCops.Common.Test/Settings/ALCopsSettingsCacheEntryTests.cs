using System.Collections.Immutable;
using ALCops.Common.Settings;

namespace ALCops.Common.Test;

public class ALCopsSettingsCacheEntryTests
{
    [Test]
    public void RetryCooldown_StartsAfterEachFailedLoad_AndCacheHitsDoNotExtendIt()
    {
        long milliseconds = 0;
        var entry = new ALCopsSettingsCacheEntry(() => milliseconds);
        var failure = RetryableFailure();
        int attempts = 0;
        ALCopsSettingsLoadResult Load()
        {
            attempts++;
            milliseconds += 5_000;
            return failure;
        }

        entry.GetOrLoad(Load, expireHttpFailures: true, CancellationToken.None);
        foreach (long now in new long[] { 5_800, 30_000, 34_999 })
        {
            milliseconds = now;
            Assert.That(entry.GetOrLoad(Load, expireHttpFailures: true, CancellationToken.None), Is.SameAs(failure));
            Assert.That(attempts, Is.EqualTo(1));
        }

        milliseconds = 35_000;
        entry.GetOrLoad(Load, expireHttpFailures: true, CancellationToken.None);
        Assert.That(attempts, Is.EqualTo(2));
        milliseconds = 69_999;
        entry.GetOrLoad(Load, expireHttpFailures: true, CancellationToken.None);
        Assert.That(attempts, Is.EqualTo(2), "Another failure must start a full cooldown after that request ends.");
        milliseconds = 70_000;
        entry.GetOrLoad(Load, expireHttpFailures: true, CancellationToken.None);
        Assert.That(attempts, Is.EqualTo(3));
    }

    [TestCase(null)]
    [TestCase(SettingsLoadFailureKind.Invalid)]
    [TestCase(SettingsLoadFailureKind.Unreadable)]
    [TestCase(SettingsLoadFailureKind.UnknownSetting)]
    public void SuccessAndDeterministicFailures_NeverExpire(SettingsLoadFailureKind? kind)
    {
        long milliseconds = 0;
        var entry = new ALCopsSettingsCacheEntry(() => milliseconds);
        var result = new ALCopsSettingsLoadResult(new ALCopsSettings(), kind.HasValue
            ? ImmutableArray.Create(new SettingsLoadFailure(kind.Value, "alcops.json", "test"))
            : ImmutableArray<SettingsLoadFailure>.Empty);
        entry.GetOrLoad(() => result, expireHttpFailures: true, CancellationToken.None);
        milliseconds = 86_400_000;
        var cached = entry.GetOrLoad(() => throw new AssertionException("A deterministic result must not be reloaded."),
            expireHttpFailures: true, CancellationToken.None);
        Assert.That(cached, Is.SameAs(result));
    }

    [Test]
    public void CancelledRetry_DoesNotRestartCooldown_AndCanRecoverImmediately()
    {
        long milliseconds = 0;
        var entry = new ALCopsSettingsCacheEntry(() => milliseconds);
        entry.GetOrLoad(RetryableFailure, expireHttpFailures: true, CancellationToken.None);
        milliseconds = 30_000;
        using var cancellation = new CancellationTokenSource();

        var exception = Assert.Throws<OperationCanceledException>(() => entry.GetOrLoad(() =>
        {
            cancellation.Cancel();
            return RetryableFailure();
        }, expireHttpFailures: true, cancellation.Token));

        var success = new ALCopsSettingsLoadResult(new ALCopsSettings(), ImmutableArray<SettingsLoadFailure>.Empty);
        var recovered = entry.GetOrLoad(() => success, expireHttpFailures: true, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(exception!.CancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(recovered, Is.SameAs(success), "Cancellation must not extend the expired failure's cooldown.");
        });
    }

    private static ALCopsSettingsLoadResult RetryableFailure() => new(new ALCopsSettings(),
        ImmutableArray.Create(new SettingsLoadFailure(SettingsLoadFailureKind.Unreadable, "https://example.com/alcops.json", "HTTP failure", retryOnNextCompilation: true)));
}
