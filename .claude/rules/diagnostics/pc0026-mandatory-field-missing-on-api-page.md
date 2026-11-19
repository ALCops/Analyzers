---
paths:
  - "src/ALCops.PlatformCop/**/MandatoryFieldMissingOnApiPage*"
  - "src/ALCops.PlatformCop.Test/Rules/MandatoryFieldMissingOnApiPage/**"
---

# PC0026: MandatoryFieldMissingOnApiPage

## Purpose

Requires every API page to expose `SystemId` as control `id` and `SystemModifiedAt` as control `lastModifiedDateTime`. API consumers rely on both for addressing entities and for delta synchronisation.

Registers `RegisterSymbolAction` on `SymbolKind.Page`; one diagnostic per missing pair, reported on the page.

## Design decisions

| Decision | Rationale |
|---|---|
| Existence is decided from `IPageTypeSymbol.FlattenedControls` as `(RelatedFieldSymbol.Name, control Name)` pairs, not from syntax | The source expression can be `Rec.SystemId`, `this.Rec.SystemId`, `Rec."SystemId"` or a bare `SystemId`; only the bound field symbol is spelling-independent. `FlattenedControls` also covers controls outside the repeater |
| A field exposed under another control name still reports | The API contract needs the exact property names `id` and `lastModifiedDateTime`, not just the fields |
| Pages with `SourceTableTemporary = true` are skipped | A temporary source has no persisted `SystemId`/`SystemModifiedAt` to expose |

## Deliberate non-reports

- Obsolete pages and pages whose `PageType` is not `API`.
- API pages without any field control bound to a table field (nothing to check against).

## Known issues

- The analyzer's pair set uses the default (ordinal) tuple comparer, while the CodeFix compares case-insensitively. A control whose name differs from `id` only in casing can therefore still report, and the fix then leaves the page alone instead of inserting an `id` control that would duplicate the control name.

## Test notes

- The `this.Rec.SystemId` CodeFix fixture is gated on runtime 14.0 (`SkipTestIfVersionIsTooLow`).

## CodeFix: MandatoryFieldMissingOnApiPageCodeFix

| Decision | Rationale |
|---|---|
| The fix decides which pairs already exist from the same symbol view as the analyzer (`GetDeclaredSymbol(PageSyntax)` as `IPageTypeSymbol`, field controls in `FlattenedControls`), compared with `SemanticFacts.IsSameName` | Comparing the control's expression text against `Rec.<Field>` missed qualified, quoted and implicit-`Rec` spellings and controls outside the repeater, so applying the fix for one field inserted a duplicate of the other. If the semantic model or page symbol is unavailable the document is returned unchanged |
| Insert the missing controls only when the layout has exactly one repeater, appended to it with the first existing field's leading trivia and end-of-line | With zero or several repeaters the right target is ambiguous; returning the document unchanged is safer than guessing |
| New controls are written as `field(<control>; Rec.<Field>) { }` | The canonical form for API pages |
