---
paths:
  - "src/ALCops.LinterCop/**/ApiPageCanonicalFieldNameGuide*"
---

# LC0063: ApiPageCanonicalFieldNameGuide

## Purpose

Detects API page field names that do not follow the canonical naming convention (e.g. `no` instead of `number` for a `"No."` source field).

## Design decisions

| Decision | Rationale |
|---|---|
| Only checks fields with `Rec.FieldName` source expressions | `IsIdentifierValueTextRec` requires `MemberAccessExpressionSyntax` with an identifier receiver named "Rec" |

## Known issues

| Issue | Status |
|---|---|
| Bare implicit-with source expression bypasses check (#348) | Known limitation, pinned by `NoDiagnostic/BareImplicitWithSourceExpression.al`. A field like `field(no; "No.")` (without `Rec.`) is not analyzed because the expression is not a `MemberAccessExpressionSyntax`. Non-trivial to fix (requires semantic resolution of the page field source). |
