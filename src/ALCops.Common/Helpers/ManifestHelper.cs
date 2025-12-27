using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;

namespace ALCops.Common.Extensions;

public static class ManifestHelper
{
    public static NavAppManifest? GetManifest(Compilation compilation)
    {
#if NETSTANDARD2_1
        // .NET Standard 2.1 relies on the AppSourceCopConfigurationProvider which would neec a dependency to the AppSourceCop analyzer 
        return null;
#else
        return Microsoft.Dynamics.Nav.Analyzers.Common.ManifestHelper.GetManifest(compilation);
#endif
    }
}