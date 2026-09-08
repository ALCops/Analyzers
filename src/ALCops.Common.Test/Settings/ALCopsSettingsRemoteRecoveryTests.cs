using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Test;

[NonParallelizable]
public class ALCopsSettingsRemoteRecoveryTests
{
    private string _tempRoot = null!;

    [SetUp]
    public void Setup()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"alcops_recovery_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempRoot, recursive: true);

    [Test]
    public async Task HttpFailure_IsSharedAcrossCompilationsDuringCooldown()
    {
        await using var server = new SettingsServer(failFirst: true);
        var fileSystem = CreateFileSystem(server.Source);

        var first = await AnalyzeAsync(fileSystem).ConfigureAwait(false);
        var later = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => AnalyzeAsync(fileSystem))).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(d => d.Id), Is.EqualTo(new[] { DiagnosticIds.ConfigurationCouldNotBeLoaded }));
            foreach (var diagnostics in later)
                Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { DiagnosticIds.ConfigurationCouldNotBeLoaded }));
            Assert.That(server.RequestCount, Is.EqualTo(1), "Editing during the cooldown must not fetch again for every new compilation.");
            Assert.That(ALCopsSettingsProvider.GetSettings(fileSystem).CognitiveComplexityThreshold, Is.EqualTo(15));
        });
    }

    [Test]
    public async Task HttpFailure_IsRetriedAfterCooldown_AndSuccessIsCached()
    {
        await using var server = new SettingsServer(failFirst: true);
        var fileSystem = CreateFileSystem(server.Source);
        long milliseconds = 0;
        var cache = new ALCopsSettingsProvider.Cache(() => Interlocked.Read(ref milliseconds));

        var first = cache.GetLoadResult(Compilation.Create("First", fileSystem: fileSystem), CancellationToken.None);
        Interlocked.Exchange(ref milliseconds, 29_999);
        var duringCooldown = cache.GetLoadResult(Compilation.Create("DuringCooldown", fileSystem: fileSystem), CancellationToken.None);
        Interlocked.Exchange(ref milliseconds, 30_000);
        using var start = new Barrier(12);
        // These synchronous callers block on HTTP or the workspace lock. Give them dedicated
        // threads so their contention cannot starve the server's thread-pool continuations.
        var recovered = await Task.WhenAll(Enumerable.Range(0, 12).Select(index => Task.Factory.StartNew(() =>
        {
            Assert.That(start.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True, "All competing callers must reach the start barrier.");
            return cache.GetLoadResult(Compilation.Create($"Recovered{index}", fileSystem: fileSystem), CancellationToken.None);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default))).ConfigureAwait(false);
        Interlocked.Exchange(ref milliseconds, 300_000);
        var later = cache.GetLoadResult(Compilation.Create("Later", fileSystem: fileSystem), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Failures, Has.Length.EqualTo(1));
            Assert.That(first.Settings.CognitiveComplexityThreshold, Is.EqualTo(15));
            Assert.That(duringCooldown, Is.SameAs(first));
            foreach (var result in recovered)
            {
                Assert.That(result.Failures.Select(failure => failure.Detail), Is.Empty);
                Assert.That(result.Settings.CognitiveComplexityThreshold, Is.EqualTo(31));
                Assert.That(result, Is.SameAs(later));
            }
            Assert.That(server.RequestCount, Is.EqualTo(2), "Concurrent retries must share one fetch, and success must stay cached.");
        });
    }

    [Test]
    public async Task CancelledCompilation_ClosesHttpRequestPromptly_AndNextCompilationRecovers()
    {
        await using var server = new SettingsServer(stallFirst: true);
        var fileSystem = CreateFileSystem(server.Source);
        using var cancellation = new CancellationTokenSource();
        Task analysis = AnalyzeAsync(fileSystem, cancellation.Token);
        await server.FirstRequest.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

        cancellation.Cancel();
        Task completed = await Task.WhenAny(server.FirstDisconnected, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
        bool closedPromptly = completed == server.FirstDisconnected;
        try { await analysis.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        await server.FirstDisconnected.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

        var next = await AnalyzeAsync(fileSystem).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(closedPromptly, Is.True, "Cancellation must stop the HTTP body read before the five-second timeout.");
            Assert.That(next, Is.Empty, "Cancellation must not cache defaults or a failure for later compilations.");
            Assert.That(server.RequestCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ConcurrentCompilations_FetchSuccessfulSourceOnce()
    {
        await using var server = new SettingsServer();
        var fileSystem = CreateFileSystem(server.Source);
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => AnalyzeAsync(fileSystem))).ConfigureAwait(false);
        Assert.That(results.SelectMany(result => result), Is.Empty);
        Assert.That(server.RequestCount, Is.EqualTo(1));
    }

    [Test]
    public async Task InvalidLocalValue_IsDiagnosedBeforeAnyRemoteRequest()
    {
        await using var server = new SettingsServer();
        var fileSystem = CreateFileSystem(server.Source, invalidLocalValue: true);
        var diagnostics = await AnalyzeAsync(fileSystem).ConfigureAwait(false);
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(DiagnosticIds.ConfigurationCouldNotBeLoaded));
        Assert.That(server.RequestCount, Is.Zero);
    }

    [Test]
    public async Task CompilationSnapshot_KeepsFailureStable_EvenAfterAnotherCompilationRecovers()
    {
        await using var server = new SettingsServer(failFirst: true);
        var fileSystem = CreateFileSystem(server.Source);
        long milliseconds = 0;
        var cache = new ALCopsSettingsProvider.Cache(() => milliseconds);
        var compilation = Compilation.Create("Snapshot", fileSystem: fileSystem);
        var first = cache.GetLoadResult(compilation, CancellationToken.None);
        var again = cache.GetLoadResult(compilation, CancellationToken.None);
        Assert.That(server.RequestCount, Is.EqualTo(1));

        milliseconds = 30_000;
        var afterExpiry = cache.GetLoadResult(compilation, CancellationToken.None);
        Assert.That(server.RequestCount, Is.EqualTo(1), "Expiry alone must not refresh an existing compilation.");
        var next = cache.GetLoadResult(Compilation.Create("Next", fileSystem: fileSystem), CancellationToken.None);
        var original = cache.GetLoadResult(compilation, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(first.Failures, Has.Length.EqualTo(1));
            Assert.That(again, Is.SameAs(first));
            Assert.That(afterExpiry, Is.SameAs(first));
            Assert.That(original, Is.SameAs(first));
            Assert.That(next.Failures, Is.Empty);
            Assert.That(server.RequestCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task CancelledWaiter_DoesNotCancelTheWorkspaceLoad()
    {
        await using var server = new SettingsServer(stallFirst: true);
        var fileSystem = CreateFileSystem(server.Source);
        using var ownerCancellation = new CancellationTokenSource();
        using var waiterCancellation = new CancellationTokenSource();
        Task owner = Task.Run(() => ALCopsSettingsProvider.GetLoadResult(
            Compilation.Create("Owner", fileSystem: fileSystem), ownerCancellation.Token));
        await server.FirstRequest.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        Task waiter = Task.Run(() => ALCopsSettingsProvider.GetLoadResult(
            Compilation.Create("Waiter", fileSystem: fileSystem), waiterCancellation.Token));
        waiterCancellation.Cancel();
        bool waiterCancelled = false;
        try { await waiter.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
        catch (OperationCanceledException ex) { waiterCancelled = ex.CancellationToken == waiterCancellation.Token; }
        bool ownerStillRunning = !owner.IsCompleted && !server.FirstDisconnected.IsCompleted;
        ownerCancellation.Cancel();
        bool ownerCancelled = false;
        try { await owner.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
        catch (OperationCanceledException ex) { ownerCancelled = ex.CancellationToken == ownerCancellation.Token; }
        Assert.Multiple(() =>
        {
            Assert.That(waiterCancelled, Is.True);
            Assert.That(ownerCancelled, Is.True, "The SDK recognizes cancellation only when the exception carries the callback token.");
            Assert.That(ownerStillRunning, Is.True);
            Assert.That(server.RequestCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task HttpLoad_DoesNotCaptureCallerSynchronizationContext()
    {
        await using var server = new SettingsServer();
        var fileSystem = CreateFileSystem(server.Source);
        var context = new RecordingSynchronizationContext();
        var result = await Task.Run(() =>
        {
            SynchronizationContext? previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try { return ALCopsSettingsProvider.GetLoadResult(fileSystem); }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }).ConfigureAwait(false);
        Assert.That(result.Settings.CognitiveComplexityThreshold, Is.EqualTo(31));
        Assert.That(context.PostCount, Is.Zero);
    }

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        private int _postCount;
        public int PostCount => Volatile.Read(ref _postCount);
        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _postCount);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }
    }

    private RelativeFileSystem CreateFileSystem(string source, bool invalidLocalValue = false)
    {
        object settings = invalidLocalValue
            ? new { Extends = new { Source = source }, CognitiveComplexityThreshold = "invalid" }
            : new { Extends = new { Source = source } };
        File.WriteAllText(Path.Combine(_tempRoot, "alcops.json"), JsonSerializer.Serialize(settings));
        return new RelativeFileSystem(_tempRoot);
    }

    private static Task<System.Collections.Immutable.ImmutableArray<Diagnostic>> AnalyzeAsync(IFileSystem fileSystem, CancellationToken cancellationToken = default)
    {
        var tree = SyntaxTree.ParseObjectText("codeunit 50100 RecoveryTest { }", cancellationToken: cancellationToken);
        var compilation = Compilation.Create("RecoveryTest", syntaxTrees: new[] { tree }, fileSystem: fileSystem);
        return ConfigurationCouldNotBeLoadedTests.GetDiagnosticsAsync(compilation, cancellationToken);
    }

    private sealed class SettingsServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _shutdown = new(TimeSpan.FromSeconds(20));
        private readonly TaskCompletionSource _firstRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstDisconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Task _serve;
        private int _requestCount;

        public SettingsServer(bool failFirst = false, bool stallFirst = false)
        {
            _listener.Start();
            Source = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/alcops.json";
            _serve = ServeAsync(failFirst, stallFirst);
        }

        public string Source { get; }
        public int RequestCount => Volatile.Read(ref _requestCount);
        public Task FirstRequest => _firstRequest.Task;
        public Task FirstDisconnected => _firstDisconnected.Task;

        private async Task ServeAsync(bool failFirst, bool stallFirst)
        {
            try
            {
                while (true)
                {
                    using TcpClient client = await _listener.AcceptTcpClientAsync(_shutdown.Token).ConfigureAwait(false);
                    await using NetworkStream stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                    while (!string.IsNullOrEmpty(await reader.ReadLineAsync(_shutdown.Token).ConfigureAwait(false))) { }
                    // Require a queued continuation even when loopback I/O completes inline.
                    // This exposes callers that starve the workers needed to serve a response.
                    await Task.Yield();
                    int request = Interlocked.Increment(ref _requestCount);
                    byte[] body = "{\"CognitiveComplexityThreshold\":31}"u8.ToArray();
                    int status = failFirst && request == 1 ? 503 : 200;
                    byte[] headers = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Test\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(headers, _shutdown.Token).ConfigureAwait(false);
                    _firstRequest.TrySetResult();
                    if (stallFirst && request == 1)
                    {
                        await stream.ReadAsync(new byte[1], _shutdown.Token).ConfigureAwait(false);
                        _firstDisconnected.TrySetResult();
                    }
                    else
                    {
                        await stream.WriteAsync(body, _shutdown.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
        }

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            _listener.Stop();
            await _serve.ConfigureAwait(false);
            _shutdown.Dispose();
        }
    }
}
