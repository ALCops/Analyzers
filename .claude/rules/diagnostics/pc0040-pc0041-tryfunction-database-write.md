---
paths:
  - "src/ALCops.PlatformCop/**/TryFunctionDatabaseWrite*"
  - "src/ALCops.PlatformCop.Test/Rules/TryFunctionDatabaseWrite/**"
---

# PC0040 / PC0041: TryFunctionDatabaseWrite

## Purpose

Reports direct database writes inside a `[TryFunction]`. When the TryFunction fails, its database changes are not rolled back; on-premises, the write can also fail at runtime when `DisableWriteInsideTryFunctions` is enabled.

The analyzer walks the bound operation tree for each TryFunction body. Operation-based invocation analysis also recognizes calls written without parentheses.

**References:**
- [Handling errors using try methods](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/devenv-handling-errors-using-try-methods)
- [Insert, Modify, ModifyAll, Delete, DeleteAll, and Truncate methods](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-insert--method)
- [Record.AddLink](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-addlink-method)
- [Record.DeleteLinks](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-deletelinks-method)
- [Record.Validate](https://learn.microsoft.com/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-validate-method)

## Design decisions

| Decision | Rationale |
|---|---|
| The direct-write diagnostic is a Warning enabled by default; the potential-write diagnostic is Info and disabled by default | A write in a TryFunction is a runtime-semantics hazard, while potential writes include safe helpers and are too noisy to enable by default. |
| The direct-write message describes the lack of rollback rather than saying the method is prohibited | Writes are blocked by default only on-premises; online, they can succeed and remain committed after a TryFunction returns false. |
| Only record built-ins with a resolved, non-temporary record/table receiver count as direct database writes | Method names such as `Insert` and `Delete` are also used by non-record built-ins, and temporary records do not write to the database. |
| Potential writes remain available as an opt-in diagnostic; no CodeFix or setting | The analyzer does not inspect custom method bodies, so a safe rewrite requires understanding caller intent. |
| Detect TryFunctions from the method declaration's `[TryFunction]` attribute using `MethodDeclarationSyntax.IsTryFunction()` | `IMethodSymbol.IsTryFunction` is missing from the netstandard2.1 SDK, while the shared syntax extension works across all supported TFMs. |
| Direct-write built-ins: Insert, Modify, ModifyAll, Delete, DeleteAll, Rename, Truncate, AddLink, CopyLinks, DeleteLink and DeleteLinks | These methods write table records or the Record Link system table. The shared `RecordMethodClassification` sets are reused for Insert, Modify, ModifyAll, Delete, DeleteAll and Rename. |
| Potential built-ins: Commit, Execute, Print, Run, RunModal, RunRequestPage, SaveAs, SaveAsExcel, SaveAsHtml, SaveAsPdf, SaveAsWord, SaveAsXml and Validate | These can commit existing writes, execute code, or invoke validation logic. `Validate` may execute an `OnValidate` trigger. |
| Every non-built-in method call is potentially database-modifying | The analyzer does not inspect method bodies or follow the call graph; this keeps analysis bounded and lets developers suppress known-safe calls with a pragma. |
| Skip obsolete code and TryFunction declarations without bodies | No actionable behavior is available in obsolete code or a bodyless declaration. |

## Deliberate non-reports

- Calls in ordinary methods that are not themselves TryFunctions.
- Safe built-in methods not on either classification list.
- Whether a custom or potential method actually modifies the database; no transitive analysis is performed.
- Operations on temporary records and tables with `TableType = Temporary`.
- Built-ins on non-record receivers, such as `List.Insert`, `TextBuilder.Insert` and `HttpClient.Delete`.
- `RecordRef` writes. Whether a `RecordRef` is temporary is only known at runtime, so the shared receiver resolver cannot safely classify it.

## Known issues

- The potential-write list is a heuristic. Built-in platform methods added in future versions may require review and explicit inclusion.
