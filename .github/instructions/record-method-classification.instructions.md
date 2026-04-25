---
applyTo: 'src/ALCops.Common/RecordMethodClassification.cs'
---

# RecordMethodClassification: Centralized Record Method Categories

## Purpose

Single source of truth for classifying AL built-in record methods by behavioral category. Eliminates duplicated method lists across analyzer projects and ensures consistent coverage when new record methods are added (e.g., `GetBySystemId`).

Builds on `ALCops.Common.Permissions.MethodOperationMap` which handles RIMD permission-level classification. This class extends beyond permissions to cover partial records, triggers, and other analyzer concerns.

## Categories

All properties are `ImmutableHashSet<string>` with `StringComparer.OrdinalIgnoreCase`.

| Property | Methods | Used by |
|---|---|---|
| `ReadMethods` | Find, FindFirst, FindLast, FindSet, Get, GetBySystemId, IsEmpty, Count | General read classification |
| `SingleRecordReadMethods` | Find, FindFirst, FindLast, Get, GetBySystemId | AC0030 (return value check) |
| `InsertMethods` | Insert | General insert classification |
| `ModifyMethods` | Modify, ModifyAll, Rename | General modify classification |
| `DeleteMethods` | Delete, DeleteAll | General delete classification |
| `WriteMethods` | Insert, Modify, ModifyAll, Delete, DeleteAll, Rename, TransferFields, Init, Copy | PC0030/PC0031 (partial records) |
| `LoadFieldsMethods` | SetLoadFields, AddLoadFields, SetBaseLoadFields | PC0030/PC0031 (partial records) |
| `JitLoadWriteMethods` | Insert, Modify, Delete, Rename, TransferFields, Copy | PC0031 (JIT load detection) |
| `TriggerMethods` | Insert, Modify, Delete, DeleteAll, Validate, ModifyAll | PC0028 (temp record triggers) |
| `RunTriggerParameterMethods` | Insert, Modify, ModifyAll, Delete, DeleteAll | LC0047 (explicit RunTrigger) |

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Location | `ALCops.Common` root namespace | Cross-cutting concern, not specific to Permissions |
| Relationship to MethodOperationMap | Complementary, not replacing | MethodOperationMap handles RIMD permission chars; this handles behavioral categories |
| WriteMethods includes non-RIMD methods | Yes (TransferFields, Init, Copy) | These mutate the record buffer even though they don't require RIMD permissions |
| SingleRecordReadMethods excludes FindSet | Yes | FindSet is used with repeat..until Next loops; different return value semantics |
| ReadMethods includes IsEmpty and Count | Yes | These perform SQL reads even though they don't load record buffers |
| Case sensitivity | OrdinalIgnoreCase | AL method names are case-insensitive |

## Adding new methods

When Microsoft adds a new record built-in method:

1. Determine its behavioral category (read, write, trigger, etc.)
2. Add it to the appropriate set(s) in `RecordMethodClassification.cs`
3. If it requires RIMD permissions, also add it to `MethodOperationMap.cs`
4. Run all tests to verify no regressions
5. Check if any analyzer needs specific handling beyond the set membership

## Consumers

| Analyzer | Properties used |
|---|---|
| `PartialRecordOperations` (PC0030/PC0031) | `SingleRecordReadMethods` + FindSet, `WriteMethods`, `LoadFieldsMethods`, `JitLoadWriteMethods` |
| `UseReturnValueForDatabaseReadMethods` (AC0030) | `SingleRecordReadMethods` |
| `ExplicitlySetRunTrigger` (LC0047) | `RunTriggerParameterMethods` |
| `TemporaryRecordTriggerInvocation` (PC0028) | `TriggerMethods` |
| `TableDataAccessRequiresPermissions` (AC0027) | Uses `MethodOperationMap` directly (not this class) |
