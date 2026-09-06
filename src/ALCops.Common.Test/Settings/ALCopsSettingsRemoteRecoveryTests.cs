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
    public async Task HttpFailure_IsRetriedByNextCompilation_AndSuccessIsCached()
    {
        await using var server = new SettingsServer(failFirst: true);
        var fileSystem = CreateFileSystem(server.Source);

        var first = await AnalyzeAsync(fileSystem).ConfigureAwait(false);
        var second = await AnalyzeAsync(fileSystem).ConfigureAwait(false);
        var third = await AnalyzeAsync(fileSystem).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(d => d.Id), Is.EqualTo(new[] { DiagnosticIds.ConfigurationCouldNotBeLoaded }));
            Assert.That(second, Is.Empty, "The recovered source must be retried without restarting the host.");
            Assert.That(third, Is.Empty);
            Assert.That(server.RequestCount, Is.EqualTo(2), "A successful retry must be cached.");
            Assert.That(ALCopsSettingsProvider.GetSettings(fileSystem).CognitiveComplexityThreshold, Is.EqualTo(31));
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
        var compilation = Compilation.Create("Snapshot", fileSystem: fileSystem);
        var first = ALCopsSettingsProvider.GetLoadResult(compilation, CancellationToken.None);
        var again = ALCopsSettingsProvider.GetLoadResult(compilation, CancellationToken.None);
        Assert.That(server.RequestCount, Is.EqualTo(1));

        var next = await AnalyzeAsync(fileSystem).ConfigureAwait(false);
        var original = ALCopsSettingsProvider.GetLoadResult(compilation, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(first.Failures, Has.Length.EqualTo(1));
            Assert.That(again, Is.SameAs(first));
            Assert.That(original, Is.SameAs(first));
            Assert.That(next, Is.Empty);
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
