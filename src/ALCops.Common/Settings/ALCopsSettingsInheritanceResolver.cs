using System.Net.Http;
#if NETSTANDARD2_1
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
using System.Text.Json.Nodes;
#endif

namespace ALCops.Common.Settings;

/// <summary>
/// Resolves the optional external base configuration declared through <c>Extends.Source</c>.
/// </summary>
internal static class ALCopsSettingsInheritanceResolver
{
    private const int MaxHttpResponseBytes = 1024 * 1024;

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5),
        // GetStringAsync enforces this while buffering, including chunked responses
        // and responses without Content-Length, before JSON deserialization starts.
        MaxResponseContentBufferSize = MaxHttpResponseBytes
    };

#if !NETSTANDARD2_1
    private static readonly JsonNodeOptions _nodeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonDocumentOptions _documentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };
#endif

    /// <summary>
    /// Tries to load and merge the external base configuration referenced by the local JSON,
    /// returning a failure for CM0001 when the declared source cannot be applied.
    /// The local JSON is parsed before this method can return <see langword="false"/>, so malformed
    /// local input still follows the provider's existing defaults fallback behavior.
    /// </summary>
    public static bool TryResolve(
        string localJson,
        string localSource,
        out string inheritedSource,
        out string inheritedJson,
        out string effectiveJson,
        out SettingsLoadFailure? failure)
    {
        inheritedSource = string.Empty;
        inheritedJson = string.Empty;
        effectiveJson = string.Empty;
        failure = null;

#if NETSTANDARD2_1
        JObject localConfiguration = JObject.Parse(localJson);
#else
        JsonObject localConfiguration = ParseObject(localJson);
#endif

        ExtendsSourceState sourceState = GetSource(localConfiguration, out string source);
        if (sourceState == ExtendsSourceState.Absent)
            return false;

        if (sourceState == ExtendsSourceState.Invalid)
        {
            failure = new SettingsLoadFailure(
                SettingsLoadFailureKind.Invalid,
                localSource,
                "Extends.Source must be a non-empty string.");
            return false;
        }

        inheritedSource = source;
        if (!TryReadExternalConfiguration(source, out string externalJson, out failure))
            return false;

        try
        {
#if NETSTANDARD2_1
            JObject inheritedConfiguration = JObject.Parse(externalJson);
#else
            JsonObject inheritedConfiguration = ParseObject(externalJson);
#endif

            // Keep the initial implementation deliberately limited to one inheritance level.
            if (HasProperty(inheritedConfiguration, "Extends"))
            {
                failure = new SettingsLoadFailure(
                    SettingsLoadFailureKind.Invalid,
                    source,
                    "configuration inheritance chains are not supported.");
                return false;
            }

            inheritedJson = externalJson;
            MergeObjects(inheritedConfiguration, localConfiguration);
#if NETSTANDARD2_1
            effectiveJson = inheritedConfiguration.ToString(Formatting.None);
#else
            effectiveJson = inheritedConfiguration.ToJsonString();
#endif
            return true;
        }
        catch (JsonException ex)
        {
            failure = new SettingsLoadFailure(SettingsLoadFailureKind.Invalid, source, ex.Message);
            return false;
        }
    }

    private static bool TryReadExternalConfiguration(
        string source,
        out string externalJson,
        out SettingsLoadFailure? failure)
    {
        externalJson = string.Empty;
        failure = null;

        try
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out Uri? uri) &&
                (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    // CM0001 includes the source verbatim, even for a rejected request.
                    string diagnosticSource = uri.GetComponents(
                        UriComponents.AbsoluteUri & ~UriComponents.UserInfo, UriFormat.UriEscaped);
                    failure = new SettingsLoadFailure(
                        SettingsLoadFailureKind.Invalid,
                        diagnosticSource,
                        "HTTP(S) configuration URLs containing credentials are not allowed.");
                    return false;
                }

                externalJson = _httpClient.GetStringAsync(uri).GetAwaiter().GetResult();
                return true;
            }

            if (!Path.IsPathFullyQualified(source))
            {
                failure = new SettingsLoadFailure(
                    SettingsLoadFailureKind.Invalid,
                    source,
                    "Extends.Source must be an anonymously accessible HTTP(S) URL or an absolute file path.");
                return false;
            }

            externalJson = File.ReadAllText(source);
            return true;
        }
        catch (Exception ex)
        {
            failure = new SettingsLoadFailure(SettingsLoadFailureKind.Unreadable, source, ex.Message);
            return false;
        }
    }

#if NETSTANDARD2_1
    private static ExtendsSourceState GetSource(JObject configuration, out string source)
    {
        source = string.Empty;
        JProperty? extendsProperty = FindProperty(configuration, "Extends");
        if (extendsProperty is null)
            return ExtendsSourceState.Absent;

        if (extendsProperty.Value is not JObject extendsObject)
            return ExtendsSourceState.Invalid;

        JProperty? sourceProperty = FindProperty(extendsObject, "Source");
        if (sourceProperty?.Value.Type != JTokenType.String)
            return ExtendsSourceState.Invalid;

        source = sourceProperty.Value.Value<string>() ?? string.Empty;
        return string.IsNullOrWhiteSpace(source)
            ? ExtendsSourceState.Invalid
            : ExtendsSourceState.Valid;
    }

    private static bool HasProperty(JObject configuration, string propertyName) =>
        FindProperty(configuration, propertyName) is not null;

    private static JProperty? FindProperty(JObject configuration, string propertyName) =>
        configuration.Properties().FirstOrDefault(property =>
            string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));

    private static void MergeObjects(JObject inheritedConfiguration, JObject localConfiguration)
    {
        foreach (JProperty localProperty in localConfiguration.Properties())
        {
            JProperty? inheritedProperty = FindProperty(inheritedConfiguration, localProperty.Name);
            if (inheritedProperty?.Value is JObject inheritedObject && localProperty.Value is JObject localObject)
            {
                MergeObjects(inheritedObject, localObject);
            }
            else if (inheritedProperty is not null)
            {
                inheritedProperty.Value = localProperty.Value.DeepClone();
            }
            else
            {
                inheritedConfiguration.Add(localProperty.Name, localProperty.Value.DeepClone());
            }
        }
    }
#else
    private static JsonObject ParseObject(string json) =>
        JsonNode.Parse(json, _nodeOptions, _documentOptions) as JsonObject ??
        throw new JsonException("The ALCops configuration root must be a JSON object.");

    private static ExtendsSourceState GetSource(JsonObject configuration, out string source)
    {
        source = string.Empty;
        string? extendsPropertyName = FindPropertyName(configuration, "Extends");
        if (extendsPropertyName is null)
            return ExtendsSourceState.Absent;

        if (configuration[extendsPropertyName] is not JsonObject extendsObject)
            return ExtendsSourceState.Invalid;

        string? sourcePropertyName = FindPropertyName(extendsObject, "Source");
        if (sourcePropertyName is null ||
            extendsObject[sourcePropertyName] is not JsonValue sourceValue ||
            !sourceValue.TryGetValue(out string? sourceValueText))
        {
            return ExtendsSourceState.Invalid;
        }

        source = sourceValueText ?? string.Empty;
        return string.IsNullOrWhiteSpace(source)
            ? ExtendsSourceState.Invalid
            : ExtendsSourceState.Valid;
    }

    private static bool HasProperty(JsonObject configuration, string propertyName) =>
        FindPropertyName(configuration, propertyName) is not null;

    private static string? FindPropertyName(JsonObject configuration, string propertyName)
    {
        foreach (KeyValuePair<string, JsonNode?> property in configuration)
        {
            if (string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                return property.Key;
        }

        return null;
    }

    private static void MergeObjects(JsonObject inheritedConfiguration, JsonObject localConfiguration)
    {
        foreach (KeyValuePair<string, JsonNode?> localProperty in localConfiguration)
        {
            string? inheritedPropertyName = FindPropertyName(inheritedConfiguration, localProperty.Key);
            if (inheritedPropertyName is not null &&
                inheritedConfiguration[inheritedPropertyName] is JsonObject inheritedObject &&
                localProperty.Value is JsonObject localObject)
            {
                MergeObjects(inheritedObject, localObject);
            }
            else if (inheritedPropertyName is not null)
            {
                inheritedConfiguration[inheritedPropertyName] = localProperty.Value?.DeepClone();
            }
            else
            {
                inheritedConfiguration[localProperty.Key] = localProperty.Value?.DeepClone();
            }
        }
    }
#endif

    private enum ExtendsSourceState
    {
        Absent,
        Invalid,
        Valid
    }
}
