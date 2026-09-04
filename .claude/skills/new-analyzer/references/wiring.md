# Wiring a diagnostic: ID, resx, descriptor

Used by `/new-analyzer` step 2. Conventions (prefixes, help URI slugs, resx key names) are in `CLAUDE.md`; this page shows the code shapes.

## `DiagnosticIds.cs`

IDs are `public static readonly string` fields named after the rule (PascalCase). Take the next free number for the cop; never reuse or skip one.

```csharp
namespace ALCops.PlatformCop;

public static class DiagnosticIds
{
    public static readonly string EditableFlowField = "PC0001";
    public static readonly string AutoIncrementInTemporaryTable = "PC0002";
}
```

## `ALCops.{Cop}Analyzers.resx`

Three entries per rule; the build generates a strongly typed class (`PlatformCopAnalyzers`, `LinterCopAnalyzers`, …). A fourth, `{RuleName}CodeAction`, is the title of the code fix when one exists.

```xml
<data name="AutoIncrementInTemporaryTableTitle" xml:space="preserve">
  <value>AutoIncrement fields are not supported in temporary tables</value>
</data>
<data name="AutoIncrementInTemporaryTableMessageFormat" xml:space="preserve">
  <value>AutoIncrement is used in a table with TableType = Temporary, which will cause a runtime error. Remove AutoIncrement or make the table non-temporary.</value>
</data>
<data name="AutoIncrementInTemporaryTableDescription" xml:space="preserve">
  <value>AutoIncrement relies on SQL Server to generate the next value when inserting records. Temporary tables are only in-memory in Business Central and are not created on SQL Server, so this results in runtime failures.</value>
</data>
```

`MessageFormat` uses .NET placeholders (`{0} '{1}' does not explicitly have the Access property set.`); the placeholder count must equal the arguments passed to `Diagnostic.Create`. Proofread the text: it renders verbatim in the editor, and a stray backtick or unbalanced quote ships.

## `DiagnosticDescriptors.cs`

```csharp
public static readonly DiagnosticDescriptor AutoIncrementInTemporaryTable = new(
    id: DiagnosticIds.AutoIncrementInTemporaryTable,
    title: PlatformCopAnalyzers.AutoIncrementInTemporaryTableTitle,
    messageFormat: PlatformCopAnalyzers.AutoIncrementInTemporaryTableMessageFormat,
    category: Category.Design,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: PlatformCopAnalyzers.AutoIncrementInTemporaryTableDescription,
    helpLinkUri: GetHelpUri(DiagnosticIds.AutoIncrementInTemporaryTable));
```

- Field name, `DiagnosticIds` field, analyzer class and test folder share the rule name.
- `Category` is the cop's nested class (`Design`, `Naming`, `Style`, `Usage`, `Performance`, `Security`).
- Severity and `isEnabledByDefault` are confirmed at the skill's gate, never inferred: `Error` for definite runtime failures, `Warning` for the common case, `Info` for suggestions and metrics; `false` for opt-in rules (metrics, opinionated style), whose tests then need a ruleset fixture (`testing.md`).
- `GetHelpUri` already exists per cop and lower-cases the id with `ToLowerInvariant()`.
