using System.Collections.Immutable;
using System.Xml;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Translation;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TranslationFilesMustBeWellFormed : DiagnosticAnalyzer
{
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
            return;
        }

        if (manifest is null || !manifest.CompilerFeatures.ShouldGenerateTranslationFile())
            return;

        List<string> translationFiles;
        try
        {
            translationFiles = LanguageFileUtilities
                .GetXliffLanguageFiles(fileSystem, manifest.AppName)
                .ToList();
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        foreach (string translationFile in translationFiles)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            try
            {
                using Stream stream = fileSystem.OpenRead(translationFile);
                var document = new XmlDocument();
                document.Load(stream);
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
