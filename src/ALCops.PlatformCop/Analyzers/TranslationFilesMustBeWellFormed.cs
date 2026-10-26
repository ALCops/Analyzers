using System.Collections.Immutable;
using System.Xml;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Translation;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TranslationFilesMustBeWellFormed : DiagnosticAnalyzer
{
    private static readonly XmlReaderSettings WellFormedOnly = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.TranslationFilesMustBeWellFormed);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationAction(AnalyzeCompilation);

    private static void AnalyzeCompilation(CompilationAnalysisContext ctx)
    {
        IFileSystem? fileSystem = ctx.Compilation.FileSystem;
        if (fileSystem is null)
            return;

        NavAppManifest? manifest;
        try
        {
            manifest = ManifestHelper.GetManifest(ctx.Compilation);
        }
        catch (FileNotFoundException)
        {
            manifest = null;
        }

        if (manifest is not null && !manifest.CompilerFeatures.ShouldGenerateTranslationFile())
            return;

        string appName = manifest?.AppName ?? string.Empty;
        List<string> translationFiles;
        try
        {
            translationFiles = LanguageFileUtilities
                .GetXliffLanguageFiles(fileSystem, appName)
                .ToList();
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ReportFileError(
                ctx,
                "Translations",
                $"Unable to enumerate translation files: {exception.Message}");
            return;
        }

        foreach (string translationFile in translationFiles)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            try
            {
                using Stream stream = fileSystem.OpenRead(translationFile);
                using var reader = XmlReader.Create(stream, WellFormedOnly);
                while (reader.Read())
                {
                }
            }
            catch (XmlException exception)
            {
                ReportFileError(
                    ctx,
                    translationFile,
                    $"Malformed XML at line {exception.LineNumber}, column {exception.LinePosition}: {exception.Message}");
            }
            catch (IOException exception)
            {
                ReportFileError(ctx, translationFile, $"Unable to read file: {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                ReportFileError(ctx, translationFile, $"Unable to read file: {exception.Message}");
            }
        }
    }

    private static void ReportFileError(CompilationAnalysisContext ctx, string translationFile, string details) =>
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TranslationFilesMustBeWellFormed,
            Location.None,
            translationFile,
            details));
}
