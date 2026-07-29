using System.Text.Json;

using RoslynTestKit;

using StatementBlockSpacingSettings = ALCops.Common.Settings.StatementBlockSpacingSettings;
using ScopeLeavingMode = ALCops.Common.Settings.ScopeLeavingMode;
using ElseChainBeforeMode = ALCops.Common.Settings.ElseChainBeforeMode;
using OneLinerMode = ALCops.Common.Settings.OneLinerMode;

namespace ALCops.FormattingCop.Test
{
    // Schema parity guard for FC0007's StatementBlockSpacing enum-typed settings. If a new value is
    // added to any enum without updating alcops.schema.json (or vice versa) these tests fail loudly.
    // Mirrors NamingPatternSettings.NamingTargetEnumMatchesSchemaPropertyNames.
    public class StatementBlockSpacingSchema : NavCodeAnalysisBase
    {
        [Test]
        public void ScopeLeavingModeEnumMatchesSchema() =>
            AssertEnumMatchesSchemaEnum(nameof(StatementBlockSpacingSettings.ScopeLeavingMode), typeof(ScopeLeavingMode));

        [Test]
        public void ElseChainBeforeModeEnumMatchesSchema() =>
            AssertEnumMatchesSchemaEnum(nameof(StatementBlockSpacingSettings.ElseChainBeforeMode), typeof(ElseChainBeforeMode));

        [Test]
        public void OneLinerModeEnumMatchesSchema() =>
            AssertEnumMatchesSchemaEnum(nameof(StatementBlockSpacingSettings.OneLinerMode), typeof(OneLinerMode));

        private static void AssertEnumMatchesSchemaEnum(string propertyName, Type enumType)
        {
            string repoRoot = FindRepositoryRoot();
            string schemaPath = Path.Combine(repoRoot, "src", "ALCops.Common", "Settings", "alcops.schema.json");

            Assert.That(File.Exists(schemaPath), Is.True, $"Schema file not found: {schemaPath}");

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(schemaPath));

            JsonElement enumValues = document.RootElement
                .GetProperty("properties")
                .GetProperty("StatementBlockSpacing")
                .GetProperty("properties")
                .GetProperty(propertyName)
                .GetProperty("enum");

            var schemaValues = enumValues
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .OfType<string>()
                .ToHashSet(StringComparer.Ordinal);

            var codeValues = Enum.GetNames(enumType)
                .ToHashSet(StringComparer.Ordinal);

            var missingInSchema = codeValues.Except(schemaValues).OrderBy(name => name).ToArray();
            var extraInSchema = schemaValues.Except(codeValues).OrderBy(name => name).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(
                    missingInSchema,
                    Is.Empty,
                    $"{enumType.Name} values missing in schema '{propertyName}': {string.Join(", ", missingInSchema)}");

                Assert.That(
                    extraInSchema,
                    Is.Empty,
                    $"Schema '{propertyName}' contains unknown {enumType.Name} values: {string.Join(", ", extraInSchema)}");
            });
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(Environment.CurrentDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "ALCops.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            Assert.Fail("Could not locate repository root (ALCops.sln).");

            return string.Empty;
        }
    }
}
