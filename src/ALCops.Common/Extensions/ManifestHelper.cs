using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CommandLine;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;

namespace ALCops.Common.Extensions;

public static class ManifestHelper
{
    public static NavAppManifest? GetManifest(Compilation compilation)
    {
        if (compilation.FileSystem == null)
        {
            return null;
        }

        return GetProjectManifestFromCompilation(compilation)?.AppManifest;
    }

    private static ProjectManifest? GetProjectManifestFromCompilation(Compilation compilation)
    {
        if (compilation.FileSystem != null && compilation.FileSystem.Exists("app.json"))
        {
            using (MemoryStream stream = new MemoryStream(compilation.FileSystem.ReadBytes("app.json")))
            {
                using StreamReader streamReader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                string directoryPath = compilation.FileSystem.GetDirectoryPath();
                string manifestPath = Path.Combine(directoryPath, "app.json");
                ProjectManifestProps? manifestProps = ProjectManifestProps.LoadFromFile(directoryPath, new List<Diagnostic>());
                return ProjectManifest.ReadFromString(manifestPath, streamReader.ReadToEnd(), manifestProps, new List<Diagnostic>());
            }
        }

        return null;
    }
}