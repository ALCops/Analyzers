using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;

namespace ALCops.Common.Extensions;

public static class ManifestHelper
{
    public static NavAppManifest? GetManifest(Compilation compilation)
    {
#if NETSTANDARD2_1
        return Microsoft.Dynamics.Nav.Analyzers.Common.AppSourceCopConfiguration.AppSourceCopConfigurationProvider.GetManifest(compilation);
#else
        return Microsoft.Dynamics.Nav.Analyzers.Common.ManifestHelper.GetManifest(compilation);
#endif
    }
}