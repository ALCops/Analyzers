---
paths:
  - "src/ALCops.LinterCop/**/TryFunctionDatabaseModification*"
  - "src/ALCops.LinterCop.Test/Rules/TryFunctionDatabaseModification/**"
---

# LC0101 / LC0102: TryFunctionDatabaseModification

## Purpose

Reports database-writing or potentially database-writing calls made directly inside a `[TryFunction]`. TryFunction errors do not roll back database changes, so these calls can leave partial updates behind after an error is caught.

Registers `RegisterCodeBlockAction`; checks invocation expressions in TryFunction method bodies and emits one of two descriptors based on the invoked method.

**References:**
- [Handling errors using try methods](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/devenv-handling-errors-using-try-methods)
- [Insert, Modify, ModifyAll, Delete, DeleteAll, and Truncate methods](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-insert--method)
- [Record.AddLink](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-addlink-method)
- [Record.DeleteLinks](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-deletelinks-method)
- [Record.Validate](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-validate-method)

## Design decisions

| Decision | Rationale |
|---|---|
| Two diagnostics from one analyzer: LC0101 Warning for known direct database writes, LC0102 Hidden for potential writes | Direct writes are unambiguously disallowed by the rule; indirect/custom calls require review and can be suppressed with a pragma. |
| Category Usage, enabled by default; no CodeFix and no setting | Agreed with the rule owner. A safe rewrite requires understanding caller intent. |
| Detect TryFunctions from the method declaration's `[TryFunction]` attribute using `MethodDeclarationSyntax.IsTryFunction()` | `IMethodSymbol.IsTryFunction` is missing from the netstandard2.1 SDK, while the shared syntax extension works across all supported TFMs. |
| Direct-write built-ins: Insert, Modify, ModifyAll, Delete, DeleteAll, Rename, Truncate, AddLink, CopyLinks, DeleteLink and DeleteLinks | These methods write table records or the Record Link system table. The shared `RecordMethodClassification` sets are reused for Insert, Modify, ModifyAll, Delete, DeleteAll and Rename. |
| Potential built-ins: Commit, Execute, Print, Run, RunModal, RunRequestPage, SaveAs, SaveAsExcel, SaveAsHtml, SaveAsPdf, SaveAsWord, SaveAsXml and Validate | These can commit existing writes, execute code, or invoke validation logic. `Validate` may execute an `OnValidate` trigger. |
| Every non-built-in method call is potentially database-modifying | The analyzer does not inspect method bodies or follow the call graph; this keeps analysis bounded and lets developers suppress known-safe calls with a pragma. |
| Report every invocation syntactically inside a TryFunction body, regardless of whether callers consume the TryFunction result | The diagnostic concerns code located in the TryFunction; caller-side invocation semantics do not change the implementation in that body. |
| Skip obsolete code and TryFunction declarations without bodies | No actionable behavior is available in obsolete code or a bodyless declaration. |
| No runtime gate | The analyzer uses syntax-based TryFunction detection and string classification rather than the newer `IMethodSymbol.IsTryFunction` SDK member. |

## Deliberate non-reports

- Calls in ordinary methods that are not themselves TryFunctions.
- Safe built-in methods not on either classification list.
- Whether a custom or potential method actually modifies the database; no transitive analysis is performed.

## Known issues

- The potential-warning list is a heuristic. Built-in platform methods added in future versions may require review and explicit inclusion.
