---
paths:
  - "src/ALCops.PlatformCop/**/ReportLayoutPropertyLength*"
  - "src/ALCops.PlatformCop.Test/Rules/ReportLayoutPropertyLength/**"
---

# PC0032: ReportLayoutPropertyLength

## Purpose

Detects `Caption` and `Summary` properties on report layout blocks (`rendering > layout`) that exceed 250 characters. The AL compiler allows any length, but at runtime Business Central throws "The length of the string is N, but it must be less than or equal to 250 characters" when a user opens the Report Layout Selection page. This is a hard crash with no workaround.

Registers `RegisterSyntaxNodeAction` on `SyntaxKind.ReportLayout`; main type `ReportLayoutPropertyLength`.

**References:**
- [GitHub Issue #176](https://github.com/ALCops/Analyzers/issues/176)

## Design decisions

| Decision | Rationale |
|---|---|
| PlatformCop | Undocumented platform DB field limit causing a runtime crash, same class as PC0028 (TableRelationFieldLength) |
| Severity Error, category Design | Always crashes the Report Layout Selection page; a structural correctness issue |
| `Caption` and `Summary` checked against a constant 250, in both report and reportextension layouts | Both properties are stored in 250-char DB fields; the limit is confirmed empirically from the runtime error and undocumented on MS Learn |
| No CodeFix | Auto-truncating text would produce nonsensical content |
| No version gate | `rendering > layout` blocks exist since BC21; all supported versions have them |

## Deliberate non-reports

- Obsolete symbols (standard ALCops convention).
