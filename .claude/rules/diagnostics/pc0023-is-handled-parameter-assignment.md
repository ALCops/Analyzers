---
paths:
  - "src/ALCops.PlatformCop/**/IsHandledParameterAssignment*"
  - "src/ALCops.PlatformCop.Test/Rules/IsHandledParameterAssignment/**"
---

# PC0023: IsHandledParameterAssignment

## Purpose

Detects an event subscriber that assigns its `var IsHandled: Boolean` (or `var Handled: Boolean`) parameter a value that may be false, or passes it on as a `var` argument, without a preceding `if IsHandled then exit;` guard. Other subscribers to the same event may already have set the parameter to `true`; overwriting it with `false` silently re-enables the default behaviour they handled.

Registers `RegisterOperationAction` on `AssignmentStatement` and `InvocationExpression`; main type `IsHandledParameterAssignment`.

**References:** [#449](https://github.com/ALCops/Analyzers/issues/449)

## Design decisions

| Decision | Rationale |
|---|---|
| Parameter names `IsHandled` and `Handled` only | These are the names the Handled pattern uses in the Base and System App; matching any `var Boolean` parameter would flag unrelated flags. |
| Only event subscriber methods, `var` Boolean parameters | The pattern is a contract between publisher and subscribers; in ordinary procedures the parameter has no shared meaning. |
| Guard recognised syntactically as a single `if <param> then exit;` statement before the operation | This is the canonical shape in Microsoft's code; flow analysis of arbitrary conditions was not worth the cost for a design rule. |
| `IsHandled := IsHandled or X` (either operand) is accepted | The self-guarded form can never turn a `true` value back to `false`, so it is as safe as the guard. |
| Guard identifiers compared with the AL name rule on the unquoted text | AL identifiers are case-insensitive and may be quoted, so `if ishandled then exit;` and `if "IsHandled" then exit;` are the same guard; an ordinal comparison reported guarded assignments. |

## Deliberate non-reports

- Assigning the constant `true`: it is the intended use of the parameter.
- Parameters of methods that are not event subscribers, parameters that are not `var`, or not `Boolean`.
- Code inside obsolete objects or members.

## Known issues

- The guard search climbs only to the nearest enclosing `Block` of the assignment or invocation and looks for the guard among that block's descendants before it. Block nesting does not follow control flow: a method-level `if IsHandled then exit;` followed by the assignment inside a nested `if X then begin ... end` is not seen (false positive), and a guard inside a nested block before the assignment is accepted as if it always ran (false negative). Tracked in [#637](https://github.com/ALCops/Analyzers/issues/637).
- Only the single-statement `exit;` shape is recognised: `then begin exit; end` and compound conditions such as `if IsHandled or X then exit;` are not treated as a guard.
