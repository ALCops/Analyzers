---
applyTo: 'src/ALCops.Common/Settings/ALCopsSettings.cs'
---

# Settings Schema Synchronization

When modifying `ALCopsSettings.cs` or `NamingPattern.cs`, you **must** update `alcops.schema.json` in the same PR.

## Why

The ALCops VS Code extension provides IntelliSense for `alcops.json` by referencing the JSON Schema hosted at:

```
https://raw.githubusercontent.com/ALCops/Analyzers/main/src/ALCops.Common/Settings/alcops.schema.json
```

If the schema is not updated alongside the C# settings, users will see stale autocompletion, missing properties, or incorrect validation.

## What to update

The schema file is located at `src/ALCops.Common/Settings/alcops.schema.json`.

When you:

| Change | Schema update required |
|---|---|
| Add a new property to `ALCopsSettings` | Add corresponding property to `properties` with type, default, description, and rule ID |
| Remove a property | Remove from schema `properties` |
| Change a default value | Update `default` in the schema |
| Change value constraints (min/max/enum) | Update constraints in the schema |
| Add a new `NamingTarget` enum value | Add to the `propertyNames.enum` array in `NamingPatterns` |
| Add/modify fields in `NamingPattern.cs` | Update the `NamingPattern` definition in `$defs` |

## Description format

Each property description should follow this pattern:

```
"<What the setting controls>. Used by <RULE_ID>. Default: <value>"
```

Example:
```json
"description": "The threshold for the Cognitive Complexity check. Used by LC0090. Default: 15"
```
