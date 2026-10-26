using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using RoslynTestKit;

namespace ALCops.PlatformCop.Test
{
    public class TranslationFilesMustBeWellFormed : NavCodeAnalysisBase
    {
        private string _testCasePath;

        private static readonly byte[] AppJsonContent = System.Text.Encoding.UTF8.GetBytes(
            """
            {
                "id": "00000000-0000-0000-0000-000000000042",
                "name": "TestApp",
                "publisher": "TestPublisher",
                "version": "1.0.0.0",
                "runtime": "5.1",
                "idRanges": [ { "from": 50000, "to": 99999 } ],
                "features": [ "TranslationFile" ]
            }
            """);

        private static readonly byte[] ValidXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body />
              </file>
            </xliff>
            """);

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                Path.Combine("Rules", nameof(TranslationFilesMustBeWellFormed)));
        }

        [Test]
        public async Task HasDiagnosticForEveryMalformedFile()
        {
            string code = await ReadFixture("HasDiagnostic", "MultipleMalformedFiles").ConfigureAwait(false);
            var diagnostics = await Analyze(code, new Dictionary<string, byte[]>
            {
                ["Translations/TestApp.da-DK.xlf"] = System.Text.Encoding.UTF8.GetBytes("<xliff>\n<file></xliff>"),
                ["Translations/TestApp.de-DE.xlf"] = System.Text.Encoding.UTF8.GetBytes("<xliff>\n<file>")
            }).ConfigureAwait(false);

            Assert.That(diagnostics, Has.Length.EqualTo(2));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Id),
                Is.All.EqualTo(DiagnosticIds.TranslationFilesMustBeWellFormed));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.Location),
                Is.All.EqualTo(Location.None));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.GetMessage()),
                Has.One.Contains("TestApp.da-DK.xlf").And.One.Contains("TestApp.de-DE.xlf"));
            Assert.That(diagnostics.Select(diagnostic => diagnostic.GetMessage()),
                Is.All.Matches<string>(message => message.Contains("line", StringComparison.OrdinalIgnoreCase)
                    && message.Contains("column", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task NoDiagnosticForWellFormedFiles()
        {
            string code = await ReadFixture("NoDiagnostic", "WellFormed").ConfigureAwait(false);
            var diagnostics = await Analyze(code, new Dictionary<string, byte[]>
            {
                ["Translations/TestApp.da-DK.xlf"] = ValidXliffContent
            }).ConfigureAwait(false);

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public async Task NoDiagnosticWhenTranslationFolderIsMissing()
        {
            string code = await ReadFixture("NoDiagnostic", "NoTranslationFolder").ConfigureAwait(false);
            var diagnostics = await Analyze(code, new Dictionary<string, byte[]>()).ConfigureAwait(false);

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public async Task HasDiagnosticWhenTranslationFileCannotBeRead()
        {
            string code = await ReadFixture("HasDiagnostic", "UnreadableFile").ConfigureAwait(false);
            var fileSystem = new FaultingFileSystem(new Dictionary<string, byte[]>
            {
                ["Translations/TestApp.da-DK.xlf"] = ValidXliffContent
            });
            var diagnostics = await Analyze(code, fileSystem).ConfigureAwait(false);

            Assert.That(diagnostics, Has.Length.EqualTo(1));
            Assert.That(diagnostics[0].Id, Is.EqualTo(DiagnosticIds.TranslationFilesMustBeWellFormed));
            Assert.That(diagnostics[0].Location, Is.EqualTo(Location.None));
            Assert.That(diagnostics[0].GetMessage(), Does.Contain("TestApp.da-DK.xlf"));
            Assert.That(diagnostics[0].GetMessage(), Does.Contain("Unable to read file"));
        }

        [Test]
        public async Task HasDiagnosticWhenTranslationFolderCannotBeEnumeratedWithoutManifest()
        {
            string code = await ReadFixture("HasDiagnostic", "UnreadableFile").ConfigureAwait(false);
            var fileSystem = new FaultingFileSystem(
                new Dictionary<string, byte[]>(),
                failDiscovery: true,
                includeManifest: false);
            var diagnostics = await Analyze(code, fileSystem).ConfigureAwait(false);

            Assert.That(diagnostics, Has.Length.EqualTo(1));
            Assert.That(diagnostics[0].Id, Is.EqualTo(DiagnosticIds.TranslationFilesMustBeWellFormed));
            Assert.That(diagnostics[0].GetMessage(), Does.Contain("Translations"));
            Assert.That(diagnostics[0].GetMessage(), Does.Contain("Unable to enumerate translation files"));
        }

        [Test]
        public async Task HasDiagnosticWhenDtdIsPresent()
        {
            string code = await ReadFixture("HasDiagnostic", "UnreadableFile").ConfigureAwait(false);
            var diagnostics = await Analyze(code, new Dictionary<string, byte[]>
            {
                ["Translations/TestApp.da-DK.xlf"] = System.Text.Encoding.UTF8.GetBytes(
                    "<!DOCTYPE xliff [<!ENTITY text \"value\">]><xliff>&text;</xliff>")
            }).ConfigureAwait(false);

            Assert.That(diagnostics, Has.Length.EqualTo(1));
            Assert.That(diagnostics[0].Id, Is.EqualTo(DiagnosticIds.TranslationFilesMustBeWellFormed));
            Assert.That(diagnostics[0].GetMessage(), Does.Contain("TestApp.da-DK.xlf"));
        }

        private async Task<string> ReadFixture(string folder, string name)
        {
            string path = Path.Combine(_testCasePath, folder, $"{name}.al");
            string markup = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return markup.Replace("[|", string.Empty, StringComparison.Ordinal)
                .Replace("|]", string.Empty, StringComparison.Ordinal);
        }

        private static async Task<ImmutableArray<Diagnostic>> Analyze(string source, Dictionary<string, byte[]> files)
        {
            files["app.json"] = AppJsonContent;
            return await Analyze(source, new MemoryFileSystem(files)).ConfigureAwait(false);
        }

        private static async Task<ImmutableArray<Diagnostic>> Analyze(string source, IFileSystem fileSystem)
        {
            var syntaxTree = SyntaxTree.ParseObjectText(source);
            var compilation = Compilation.Create(
                "TranslationFilesMustBeWellFormedTests",
                syntaxTrees: new[] { syntaxTree },
                fileSystem: fileSystem);
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new Analyzers.TranslationFilesMustBeWellFormed());
            var compilationWithAnalyzers = new CompilationWithAnalyzers(
                compilation,
                analyzers,
                options: null!,
                cancellationToken: CancellationToken.None);

            return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
        }

        private sealed class FaultingFileSystem : IFileSystem
        {
            private readonly MemoryFileSystem _inner;
            private readonly bool _failDiscovery;

            public FaultingFileSystem(
                Dictionary<string, byte[]> files,
                bool failDiscovery = false,
                bool includeManifest = true)
            {
                if (includeManifest)
                    files["app.json"] = AppJsonContent;

                _failDiscovery = failDiscovery;
                _inner = new MemoryFileSystem(files);
            }

            public bool Exists(string path) => _inner.Exists(path);

            public Stream OpenRead(string path)
            {
                if (path.EndsWith(".xlf", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Simulated read failure.");

                return _inner.OpenRead(path);
            }

            public string GetDirectoryPath() => _inner.GetDirectoryPath();

            public byte[] ReadBytes(string path) => _inner.ReadBytes(path);

            public byte[] ReadBytes(string path, int count) => _inner.ReadBytes(path, count);

            public void WriteBytes(string path, byte[] content) => _inner.WriteBytes(path, content);

            public bool DirectoryExistsForFile(string path) => _inner.DirectoryExistsForFile(path);

            public IEnumerable<string> GetFiles(string searchPattern) =>
                _failDiscovery
                    ? throw new UnauthorizedAccessException("Simulated directory enumeration failure.")
                    : _inner.GetFiles(searchPattern);

            public IEnumerable<string> GetFiles(string directory, string searchPattern) =>
                _failDiscovery
                    ? throw new UnauthorizedAccessException("Simulated directory enumeration failure.")
                    : _inner.GetFiles(directory, searchPattern);

            public IEnumerable<string> GetFilesRecursively(string directory) =>
                _inner.GetFilesRecursively(directory);

            public void CreateDirectoryForFile(string path) => _inner.CreateDirectoryForFile(path);

            public Stream CreateFile(string path) => _inner.CreateFile(path);

            public Stream OpenWrite(string path) => _inner.OpenWrite(path);

            public Stream OpenFile(
                string filePath,
                FileMode mode,
                FileAccess access,
                FileShare share = FileShare.None,
                int bufferSize = 4096,
                FileOptions options = FileOptions.None) =>
                _inner.OpenFile(filePath, mode, access, share, bufferSize, options);

            public void DeleteFile(string path) => _inner.DeleteFile(path);

            public long GetFileSize(string path) => _inner.GetFileSize(path);

            public string GetAbsolutePath(string relativePath) => _inner.GetAbsolutePath(relativePath);

            public bool DirectoryExists(string directory) => _inner.DirectoryExists(directory);
        }
    }
}
