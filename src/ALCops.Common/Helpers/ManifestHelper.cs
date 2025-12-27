using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;

namespace ALCops.Common.Extensions;

public static class ManifestHelper
{
    public static NavAppManifest? GetManifest(Compilation compilation)
    {
        return Microsoft.Dynamics.Nav.Analyzers.Common.ManifestHelper.GetManifest(compilation);
    }
}