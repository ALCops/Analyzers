---
paths:
  - "src/ALCops.LinterCop/**/ApiPageCanonicalFieldNameGuide*"
  - "src/ALCops.LinterCop.Test/Rules/ApiPageCanonicalFieldNameGuide/**"
---

# LC0063: ApiPageCanonicalFieldNameGuide

## Purpose

Detects API page field names that do not follow the canonical naming convention (e.g. `no` instead of `number` for a `"No."` source field).

Registers `SymbolAction` on `SymbolKind.Page`; main type `ApiPageCanonicalFieldNameGuide`.

## Design decisions

| Decision | Rationale |
|---|---|
| Syntactic source-expression check: only `Rec.FieldName` (`MemberAccessExpressionSyntax` with an identifier receiver named `Rec`) | Resolving the page-field source semantically is non-trivial; the syntactic form covers the common case and the gap is accepted (see Known issues). |

## Deliberate non-reports

- Obsolete page symbols (standard ALCops convention).
- Fields whose source expression is not written as `Rec.Field` (see Known issues).

## Known issues

- A bare implicit-with source expression such as `field(no; "No.")` (without `Rec.`) bypasses the check because it is not a `MemberAccessExpressionSyntax` ([#348](https://github.com/ALCops/Analyzers/issues/348)). Pinned by `NoDiagnostic/BareImplicitWithSourceExpression.al`; fixing it requires semantic resolution of the page field source.
