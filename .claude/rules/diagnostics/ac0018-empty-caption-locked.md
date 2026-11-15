---
paths:
  - "src/ALCops.ApplicationCop/**/EmptyCaptionLocked*"
  - "src/ALCops.ApplicationCop.Test/Rules/EmptyCaptionLocked/**"
---

# AC0018: EmptyCaptionLocked

## Purpose

Reports a `Caption` property whose value is empty or whitespace and that does not set `Locked = true`. An unlocked empty caption is still extracted into the XLIFF translation files, which produces meaningless translation entries.

Registers `RegisterSyntaxNodeAction` on every syntax kind that can declare `Caption` (objects, fields, page controls and actions, query and report columns, enum values, views, layouts, profiles, permission sets, and table, page and profile extensions); main type `EmptyCaptionLocked`.

**References:** [Caption property (Microsoft Learn)](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/properties/devenv-caption-property)

## Design decisions

| Decision | Rationale |
|---|---|
| Syntax-node registration on an explicit kind list based on the Microsoft Learn "Applies to" list of the Caption property | The property is read from the registered node itself, so a kind missing from the list is never checked; new kinds that accept `Caption` must be added explicitly. |
| Table, page and profile extensions are covered | Their SDK property tables accept an object-level `Caption` (the table, page and profile `CAPTION` entries carry extension modifiability), so an extension can introduce an unlocked empty caption just like a base object. |
| Report extensions, permission set extensions and page customizations are not registered | Their SDK property tables have no object-level `Caption`, and the page `CAPTION` entry carries no customization modifiability, so these objects cannot declare the property. |
| Enum extensions are not registered at object level | The compiler rejects any object-level `Caption` on an `enumextension` (AL0171, also for a non-empty value); values added by an enum extension are still reported through `EnumValue`. |
| Whitespace-only captions count as empty | They produce the same meaningless translation entry as `''`. |

## Deliberate non-reports

- Empty captions with `Locked = true`.
- Obsolete objects and members (`IsObsolete`).
- An enum value inside an `enum` object is skipped in the `EnumValue` callback when the containing symbol is the enum, so each enum value caption is reported once, at the value.

## SDK facts

- `ProfileExtensionTypeSymbol.PropertyTypeInfos` is `ObjectParser.ProfileProperties`; the `CAPTION` entry in `ObjectParser.GetProfileProperties` has extension modifiability from 14.0, which is why a `profileextension` may declare `Caption` (verified against `../nav-sdk-source` net10.0).
- The `CAPTION` entries in `ObjectParser.GetTableProperties` and `GetPageProperties` carry extension modifiability.
- `ObjectParser.GetReportExtensionProperties` and `GetPermissionSetExtensionProperties` contain no `CAPTION` entry; the `CAPTION` entry in `ObjectParser.GetPageProperties` has extension but no customization modifiability.
- `EnumExtensionType` is parsed with `ObjectParser.LookupEnumTypeProperty`, whose `CAPTION` entry has neither extension nor customization modifiability.
- `SyntaxKind.ProfileExtensionObject` exists at the 12.0 SDK floor.

## Test notes

- Profile-extension fixtures declare the RoleCenter page and the profile in the same file, so they are gated on 13.0 (same-module target) and on 14.0 (profile extensions need runtime Fall2024).
- The table and page extension fixtures declare their targets in the same file and are gated on 13.0.
- The `HasFix` cases apply the fix at the fixture marker; they do not depend on the analyzer reporting there.

## CodeFix: EmptyCaptionLockedCodeFixProvider

| Decision | Rationale |
|---|---|
| Adds `Locked = true` to the caption label, or replaces an existing `Locked` entry (for example `Locked = false`) with `true` | Locking keeps the intentionally empty caption and removes it from translation, which is the only fix that preserves the author's intent; filling in a caption text cannot be inferred. |
| FixAll through `WellKnownFixAllProviders.BatchFixer` | Each fix rewrites a single label node independently, so batching per document is safe. |
