using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Test;

[NonParallelizable]
public class ALCopsSettingsInheritanceHttpTests
{
    private string _tempRoot = null!;

    [SetUp]
    public void Setup()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"alcops_http_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempRoot, recursive: true);

    [TestCase("ContentLength", 1_048_575, false)]
    [TestCase("ContentLength", 1_048_576, false)]
    [TestCase("ContentLength", 1_048_577, true)]
    [TestCase("Chunked", 1_048_576, false)]
    [TestCase("Chunked", 1_048_577, true)]
    [TestCase("ConnectionClose", 1_048_576, false)]
    [TestCase("ConnectionClose", 1_048_577, true)]
    public async Task HttpResponseSize_EnforcesOneMebibyteLimit(string framing, int bodySize, bool rejected)
    {
        const string prefix = "{\"CognitiveComplexityThreshold\":31,\"$schema\":\"";
        const string suffix = "\"}";
        byte[] body = Encoding.UTF8.GetBytes(prefix + new string('x', bodySize - prefix.Length - suffix.Length) + suffix);

        await VerifyResponseAsync(body, framing, rejected, sizeFailure: rejected).ConfigureAwait(false);
    }

    [Test]
    public async Task HttpResponseSize_CountsUtf8BytesRatherThanCharacters()
    {
        string json = "{\"CognitiveComplexityThreshold\":31,\"$schema\":\"" + new string('\u00e9', 524_288) + "\"}";
        Assert.That(json.Length, Is.LessThan(1_048_576));

        await VerifyResponseAsync(Encoding.UTF8.GetBytes(json), "ContentLength", rejected: true, sizeFailure: true).ConfigureAwait(false);
    }

    [Test]
    public async Task HttpError_ReturnsDefaultsAndReportsCm0001() =>
        await VerifyResponseAsync("{}"u8.ToArray(), "ContentLength", rejected: true, statusCode: 404).ConfigureAwait(false);

    [Test]
    public async Task HttpBodyTimeout_ReturnsDefaultsAndReportsCm0001() =>
        await VerifyResponseAsync("{}"u8.ToArray(), "StalledBody", rejected: true).ConfigureAwait(false);

    private async Task VerifyResponseAsync(byte[] body, string framing, bool rejected, bool sizeFailure = false, int statusCode = 200)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        string source = $"http://127.0.0.1:{endpoint.Port}/alcops.json";
        Task responseTask = ServeResponseAsync(listener, body, framing, rejected, statusCode);
        File.WriteAllText(Path.Combine(_tempRoot, "alcops.json"), JsonSerializer.Serialize(new
        {
            Extends = new { Source = source },
            CyclomaticComplexityThreshold = 41,
            KnownAcronyms = new[] { "Local" },
            StatementBlockSpacing = new { ScopeLeavingMode = "ExitOnly" }
        }));

        var fileSystem = new RelativeFileSystem(_tempRoot);
        var tree = SyntaxTree.ParseObjectText("codeunit 50100 HttpSettingsTest { }");
        var compilation = Compilation.Create("HttpSettingsTest", syntaxTrees: new[] { tree }, fileSystem: fileSystem);
        var driver = ConfigurationCouldNotBeLoadedTests.CreateDriver(compilation);
        var result = ALCopsSettingsProvider.GetLoadResult(driver.Compilation, CancellationToken.None);
        var diagnostics = await driver.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
        await responseTask.ConfigureAwait(false);

        if (!rejected)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.Settings.CognitiveComplexityThreshold, Is.EqualTo(31));
                Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(41));
                Assert.That(result.Failures, Is.Empty);
                Assert.That(diagnostics, Is.Empty);
            });
            return;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Settings.CognitiveComplexityThreshold, Is.EqualTo(15));
            Assert.That(result.Settings.CyclomaticComplexityThreshold, Is.EqualTo(8));
            Assert.That(result.Settings.KnownAcronyms, Is.Null);
            Assert.That(result.Settings.StatementBlockSpacing.ScopeLeavingMode, Is.EqualTo(ScopeLeavingMode.ExitAndError));
            Assert.That(result.Failures, Has.Length.EqualTo(1));
            Assert.That(diagnostics, Has.Length.EqualTo(1));
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.Failures[0].Kind, Is.EqualTo(SettingsLoadFailureKind.Unreadable));
            Assert.That(result.Failures[0].Source, Is.EqualTo(source));
            Assert.That(diagnostics[0].Id, Is.EqualTo(DiagnosticIds.ConfigurationCouldNotBeLoaded));
            if (sizeFailure)
                Assert.That(result.Failures[0].Detail, Does.Contain("1048576"), "The failure must be the buffer limit, not an unrelated network error.");
        });
    }

    private static async Task ServeResponseAsync(TcpListener listener, byte[] body, string framing, bool mayCloseEarly, int statusCode)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using TcpClient client = await listener.AcceptTcpClientAsync(timeout.Token).ConfigureAwait(false);
        await using NetworkStream stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
        while (!string.IsNullOrEmpty(await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false))) { }

        string lengthHeader = framing switch
        {
            "Chunked" => "Transfer-Encoding: chunked\r\n",
            "ConnectionClose" => string.Empty,
            _ => $"Content-Length: {body.Length}\r\n"
        };
        byte[] headers = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {statusCode} Test response\r\nContent-Type: application/json; charset=utf-8\r\n{lengthHeader}Connection: close\r\n\r\n");

        try
        {
            await stream.WriteAsync(headers, timeout.Token).ConfigureAwait(false);
            if (framing == "StalledBody")
            {
                // Wait for the client's five-second timeout to close the connection.
                await stream.ReadAsync(new byte[1], timeout.Token).ConfigureAwait(false);
                return;
            }

            if (framing == "Chunked")
            {
                for (int offset = 0; offset < body.Length; offset += 4096)
                {
                    int count = Math.Min(4096, body.Length - offset);
                    await stream.WriteAsync(Encoding.ASCII.GetBytes($"{count:X}\r\n"), timeout.Token).ConfigureAwait(false);
                    await stream.WriteAsync(body.AsMemory(offset, count), timeout.Token).ConfigureAwait(false);
                    await stream.WriteAsync("\r\n"u8.ToArray(), timeout.Token).ConfigureAwait(false);
                }
                await stream.WriteAsync("0\r\n\r\n"u8.ToArray(), timeout.Token).ConfigureAwait(false);
            }
            else
            {
                await stream.WriteAsync(body, timeout.Token).ConfigureAwait(false);
            }
        }
        catch (IOException) when (mayCloseEarly)
        {
            // An intentionally rejected response can make the client close before the server finishes writing.
        }
    }
}
