using System.Net.Http;
#if NETSTANDARD2_1
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace ALCops.Common.Settings;

/// <summary>Loads the single external base declared by a parsed local configuration.</summary>
internal static class ALCopsSettingsInheritanceResolver
{
    private const int MaxHttpResponseBytes = 1024 * 1024;
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5),
        MaxResponseContentBufferSize = MaxHttpResponseBytes
    };

    public static bool TryResolve(ALCopsSettingsDocument local, string localSource,
        out string inheritedSource, out ALCopsSettingsDocument? inherited,
        out SettingsLoadFailure? failure, CancellationToken cancellationToken)
    {
        inheritedSource = string.Empty;
        inherited = null;
        failure = null;
        if (!local.TryGetSource(out string source))
        {
            failure = new SettingsLoadFailure(SettingsLoadFailureKind.Invalid, localSource, "Extends.Source must be a non-empty string.");
            return false;
        }
        inheritedSource = source;
        if (!TryReadExternalConfiguration(source, out string json, out failure, cancellationToken))
            return false;
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            inherited = ALCopsSettingsDocument.ParseInherited(json);
            if (inherited.HasExtends)
            {
                failure = new SettingsLoadFailure(SettingsLoadFailureKind.Invalid, source, "configuration inheritance chains are not supported.");
                return false;
            }
            return true;
        }
        catch (JsonException ex)
        {
            failure = new SettingsLoadFailure(SettingsLoadFailureKind.Invalid, source, ex.Message);
            return false;
        }
    }

    private static bool TryReadExternalConfiguration(string source, out string json,
        out SettingsLoadFailure? failure, CancellationToken cancellationToken)
    {
        json = string.Empty;
        failure = null;
        cancellationToken.ThrowIfCancellationRequested();
        bool isHttp = false;
        try
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out Uri? uri) &&
                (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    string diagnosticSource = uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.UserInfo, UriFormat.UriEscaped);
                    failure = new SettingsLoadFailure(SettingsLoadFailureKind.Invalid, diagnosticSource,
                        "HTTP(S) configuration URLs containing credentials are not allowed.");
                    return false;
                }
                isHttp = true;
                // NAV analyzer callbacks are synchronous Actions: they must wait for settings
                // before reporting. Every await below avoids context capture; the wait is bounded
                // by the HTTP timeout and interrupted by the compilation's cancellation token.
                json = ReadHttpAsync(uri, cancellationToken).GetAwaiter().GetResult();
                return true;
            }
            if (!Path.IsPathFullyQualified(source))
            {
                failure = new SettingsLoadFailure(SettingsLoadFailureKind.Invalid, source,
                    "Extends.Source must be an anonymously accessible HTTP(S) URL or an absolute file path.");
                return false;
            }
            json = File.ReadAllText(source);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            failure = new SettingsLoadFailure(SettingsLoadFailureKind.Unreadable, source, ex.Message, retryOnNextCompilation: isHttp);
            return false;
        }
    }

    private static async Task<string> ReadHttpAsync(Uri uri, CancellationToken cancellationToken)
    {
        // ResponseContentRead applies the byte cap and cancellation while buffering the body,
        // including chunked responses and responses without Content-Length.
        using HttpResponseMessage response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        cancellationToken.ThrowIfCancellationRequested();
#if NETSTANDARD2_1
        // The body is already buffered; this legacy overload has no cancellation parameter.
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#endif
    }
}
