using System.Collections.Immutable;
using System.Reflection;
#if NETSTANDARD2_1
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
#else
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
#endif

namespace ALCops.Common.Settings;

/// <summary>
/// Owns JSON parsing, validation, key matching and merging for both local and inherited settings.
/// Each document is parsed once; type validation and unknown-key checks reuse that document.
/// </summary>
internal sealed class ALCopsSettingsDocument
{
    private static readonly HashSet<string> _knownKeys = new(
        typeof(ALCopsSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name),
        StringComparer.OrdinalIgnoreCase) { "$schema", "Extends" };

#if NETSTANDARD2_1
    private static readonly JsonSerializerSettings _settings = new() { Converters = { new StringEnumConverter() } };
    private readonly JObject _root;

    private ALCopsSettingsDocument(JObject root) => _root = root;

    public static ALCopsSettingsDocument? ParseLocal(string json)
    {
        using var reader = new JsonTextReader(new StringReader(json));
        while (reader.Read() && reader.TokenType == JsonToken.Comment) { }
        if (reader.TokenType is JsonToken.None or JsonToken.Comment)
            return null;

        JToken root = JToken.ReadFrom(reader);
        while (reader.Read())
        {
            if (reader.TokenType != JsonToken.Comment)
                throw new JsonException("The ALCops configuration must contain a single JSON value.");
        }

        if (root.Type == JTokenType.Null)
            return null;
        return root is JObject obj ? new ALCopsSettingsDocument(obj) : throw InvalidRoot();
    }

    public bool HasExtends => FindProperty(_root, "Extends") is not null;

    public bool TryGetSource(out string source)
    {
        source = string.Empty;
        if (FindProperty(_root, "Extends")?.Value is not JObject extendsObject ||
            FindProperty(extendsObject, "Source")?.Value is not JValue { Type: JTokenType.String } value)
            return false;
        source = value.Value<string>() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(source);
    }

    private IEnumerable<string> Keys => _root.Properties().Select(p => p.Name);

    private ALCopsSettings DeserializeCore() =>
        _root.ToObject<ALCopsSettings>(JsonSerializer.Create(_settings)) ?? new ALCopsSettings();

    private static JProperty? FindProperty(JObject obj, string name) =>
        obj.Properties().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    private static void Merge(JObject inherited, JObject local)
    {
        foreach (JProperty property in local.Properties())
        {
            JProperty? target = FindProperty(inherited, property.Name);
            if (target?.Value is JObject targetObject && property.Value is JObject localObject)
                Merge(targetObject, localObject);
            else if (target is not null)
                target.Value = property.Value.DeepClone();
            else
                inherited.Add(property.Name, property.Value.DeepClone());
        }
    }
#else
    private static readonly JsonSerializerOptions _settings = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private static readonly JsonNodeOptions _nodeOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly JsonObject _root;

    private ALCopsSettingsDocument(JsonObject root) => _root = root;

    public static ALCopsSettingsDocument? ParseLocal(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });
        if (IsEmptyOrComments(json))
            return null;
        if (!reader.Read())
            return null;

        JsonNode? root = JsonNode.Parse(ref reader, _nodeOptions);
        if (reader.Read())
            throw new JsonException("The ALCops configuration must contain a single JSON value.");
        if (root is null)
            return null;
        return root is JsonObject obj ? new ALCopsSettingsDocument(obj) : throw InvalidRoot();
    }

    public bool HasExtends => _root.ContainsKey("Extends");

    public bool TryGetSource(out string source)
    {
        source = string.Empty;
        if (_root["Extends"] is not JsonObject extendsObject ||
            extendsObject["Source"] is not JsonValue value || !value.TryGetValue(out string? text))
            return false;
        source = text ?? string.Empty;
        return !string.IsNullOrWhiteSpace(source);
    }

    private IEnumerable<string> Keys => _root.Select(p => p.Key);

    private ALCopsSettings DeserializeCore() => _root.Deserialize<ALCopsSettings>(_settings) ?? new ALCopsSettings();

    private static void Merge(JsonObject inherited, JsonObject local)
    {
        foreach (KeyValuePair<string, JsonNode?> property in local)
        {
            if (inherited[property.Key] is JsonObject targetObject && property.Value is JsonObject localObject)
                Merge(targetObject, localObject);
            else
                inherited[property.Key] = property.Value?.DeepClone();
        }
    }

    private static bool IsEmptyOrComments(string json)
    {
        // Utf8JsonReader rejects a final input with no JSON value, even when it contains
        // only valid comments. Recognize only that empty-local-file case before parsing.
        int offset = 0;
        while (offset < json.Length)
        {
            if (json[offset] is ' ' or '\t' or '\r' or '\n')
                offset++;
            else if (offset + 1 < json.Length && json[offset] == '/' && json[offset + 1] == '/')
            {
                offset += 2;
                while (offset < json.Length && json[offset] is not ('\r' or '\n'))
                    offset++;
            }
            else if (offset + 1 < json.Length && json[offset] == '/' && json[offset + 1] == '*')
            {
                int end = json.IndexOf("*/", offset + 2, StringComparison.Ordinal);
                if (end < 0)
                    return false;
                offset = end + 2;
            }
            else
                return false;
        }
        return true;
    }
#endif

    public static ALCopsSettingsDocument ParseInherited(string json) => ParseLocal(json) ?? throw InvalidRoot();

    private static JsonException InvalidRoot() => new("The ALCops configuration root must be a JSON object.");

    public ALCopsSettings DeserializeSettings()
    {
        ALCopsSettings settings = DeserializeCore();
        // Explicit null restores the nested defaults for every consumer.
        settings.StatementBlockSpacing ??= new StatementBlockSpacingSettings();
        return settings;
    }

    public ImmutableArray<SettingsLoadFailure> GetUnknownSettingFailures(string source) =>
        Keys.Where(key => !_knownKeys.Contains(key))
            .Select(key => new SettingsLoadFailure(SettingsLoadFailureKind.UnknownSetting, source, $"unknown setting '{key}'"))
            .ToImmutableArray();

    public void MergeOverrides(ALCopsSettingsDocument local) => Merge(_root, local._root);
}
