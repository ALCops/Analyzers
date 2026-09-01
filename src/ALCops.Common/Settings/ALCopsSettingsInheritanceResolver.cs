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
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
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
    /// Tries to load and merge the external base configuration referenced by the local JSON.
    /// The local JSON is parsed before this method can return <see langword="false"/>, so malformed
    /// local input still follows the provider's existing defaults fallback behavior.
    /// </summary>
    public static bool TryResolve(
        string localJson,
        out string inheritedJson,
        out string effectiveJson)
    {
        inheritedJson = string.Empty;
        effectiveJson = string.Empty;

#if NETSTANDARD2_1
        JObject localConfiguration = JObject.Parse(localJson);
#else
        JsonObject localConfiguration = ParseObject(localJson);
#endif

        if (!TryGetSource(localConfiguration, out string source))
            return false;

        string? externalJson = TryReadExternalConfiguration(source);
        if (externalJson is null)
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
                return false;

            inheritedJson = externalJson;
            MergeObjects(inheritedConfiguration, localConfiguration);
#if NETSTANDARD2_1
            effectiveJson = inheritedConfiguration.ToString(Formatting.None);
#else
            effectiveJson = inheritedConfiguration.ToJsonString();
#endif
            return true;
        }
        catch (JsonException)
        {
            // An invalid external configuration must not prevent the local configuration from loading.
            return false;
        }
    }

    private static string? TryReadExternalConfiguration(string source)
    {
        try
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out Uri? uri) &&
                (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                return _httpClient.GetStringAsync(uri).GetAwaiter().GetResult();
            }

            return Path.IsPathFullyQualified(source)
                ? File.ReadAllText(source)
                : null;
        }
        catch (Exception)
        {
            // External settings are optional. Timeouts, network failures, inaccessible files, and
            // unsupported source formats all fall back to the local configuration.
            return null;
        }
    }

#if NETSTANDARD2_1
    private static bool TryGetSource(JObject configuration, out string source)
    {
        source = string.Empty;
        JProperty? extendsProperty = FindProperty(configuration, "Extends");
        if (extendsProperty?.Value is not JObject extendsObject)
            return false;

        JProperty? sourceProperty = FindProperty(extendsObject, "Source");
        if (sourceProperty?.Value.Type != JTokenType.String)
            return false;

        source = sourceProperty.Value.Value<string>() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(source);
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

    private static bool TryGetSource(JsonObject configuration, out string source)
    {
        source = string.Empty;
        string? extendsPropertyName = FindPropertyName(configuration, "Extends");
        if (extendsPropertyName is null || configuration[extendsPropertyName] is not JsonObject extendsObject)
            return false;

        string? sourcePropertyName = FindPropertyName(extendsObject, "Source");
        if (sourcePropertyName is null ||
            extendsObject[sourcePropertyName] is not JsonValue sourceValue ||
            !sourceValue.TryGetValue(out string? sourceValueText))
        {
            return false;
        }

        source = sourceValueText ?? string.Empty;
        return !string.IsNullOrWhiteSpace(source);
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
}
