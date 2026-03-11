# ALCops Project Reference

## Project Structure

```
src/
├── ALCops.Common/                         # Shared infrastructure
│   ├── Extensions/                        # Extension methods for syntax/symbols
│   ├── Reflection/                        # EnumProvider, VersionProvider, etc.
│   ├── Helpers/                           # ManifestHelper, etc.
│   └── Settings/                          # ALCopsSettings, ALCopsSettingsProvider
├── ALCops.[CopName]/                      # Analyzer project
│   ├── Analyzers/                         # Analyzer implementations
│   ├── CodeFixes/                         # CodeFix implementations
│   ├── DiagnosticIds.cs                   # ID constants
│   ├── DiagnosticDescriptors.cs           # Descriptor definitions
│   └── ALCops.[CopName]Analyzers.resx     # Localized strings
└── ALCops.[CopName].Test/                 # Test project (NUnit + RoslynTestKit)
    └── Rules/
        └── [DiagnosticName]/
            ├── [DiagnosticName].cs        # Test class
            ├── HasDiagnostic/             # .al files that trigger the rule
            ├── NoDiagnostic/              # .al files that don't trigger
            └── HasFix/                    # CodeFix test pairs (current.al / expected.al)
```

## Diagnostic ID Ranges

| Cop | Prefix | Last Used ID | Next Available |
|-----|--------|-------------|----------------|
| LinterCop | LC | LC0090 | LC0091 |
| ApplicationCop | AC | AC0031 | AC0032 |
| DocumentationCop | DC | DC0005 | DC0006 |
| FormattingCop | FC | FC0003 | FC0004 |
| PlatformCop | PC | PC0028 | PC0029 |
| TestAutomationCop | TA | TA0001 | TA0002 |

> **Important:** Always verify the actual last ID in `DiagnosticIds.cs` before assigning — this table may be out of date.

## Diagnostic Categories

| Category | Use For |
|----------|---------|
| `Design` | Application design and architecture rules |
| `Naming` | Naming conventions and identifiers |
| `Style` | Formatting and readability |
| `Usage` | Discouraged language constructs |
| `Performance` | Runtime efficiency concerns |
| `Security` | Exposure, permissions, unsafe practices |

## ALCopsSettings (Configurable Rules)

**Settings file:** `alcops.json` (place in workspace root)

**Settings class:** `src/ALCops.Common/Settings/ALCopsSettings.cs`
```csharp
public sealed class ALCopsSettings
{
    public int CognitiveComplexityThreshold { get; set; } = 15;
    public int CyclomaticComplexityThreshold { get; set; } = 8;
    public int MaintainabilityIndexThreshold { get; set; } = 20;
    // Add new configurable properties here
}
```

**Loading settings in an analyzer:**
```csharp
var settings = ALCopsSettingsProvider.GetSettings(
    compilation.FileSystem?.GetDirectoryPath());
int threshold = settings.YourPropertyThreshold;
```

**Settings are cached** per workspace path. JSON parsing supports case-insensitive property names, comments, and trailing commas.

**Example `alcops.json`:**
```json
{
  "cognitiveComplexityThreshold": 20,
  "cyclomaticComplexityThreshold": 10,
  "maintainabilityIndexThreshold": 25
}
```

## Available Helper Extensions (ALCops.Common)

### Context Extensions
- `ctx.IsObsolete()` — Check if the context is in obsolete code (always check this first)

### Syntax Node Extensions
- `node.GetPropertyValue(propertyKind)` — Extract property values from syntax nodes

### Symbol Extensions
- `symbol.GetContainingObjectTypeSymbol()` — Navigate to containing AL object

### Reflection Providers
- `EnumProvider.SyntaxKind.*` — AL syntax node kinds
- `EnumProvider.OperationKind.*` — AL operation kinds
- `EnumProvider.SymbolKind.*` — AL symbol kinds
- `EnumProvider.PropertyKind.*` — AL property kinds
- `VersionProvider.VersionCompatibility.*` — Version gating helpers (e.g., `Fall2024OrGreater`)

## Help Link URI Pattern

Each cop has a specific help link format in its `DiagnosticDescriptors.cs`:

```
LinterCop:         https://alcops.dev/docs/analyzers/lintercop/{id}/
ApplicationCop:    https://alcops.dev/docs/analyzers/applicationcop/{id}/
DocumentationCop:  https://alcops.dev/docs/analyzers/documentationcop/{id}/
FormattingCop:     https://alcops.dev/docs/analyzers/formattingcop/{id}/
PlatformCop:       https://alcops.dev/docs/analyzers/platformcop/{id}/
TestAutomationCop: https://alcops.dev/docs/analyzers/testautomationcop/{id}/
```
