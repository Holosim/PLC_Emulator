---
name: pattern-config-loader-validate
description: ConfigLoader.Validate cross-file check (DATA-IN-103, issue #8) — what it checks, what it deliberately doesn't
metadata:
  type: project
---

Implemented in `src/PlcEmulator.Config/ConfigLoader.cs`'s `Validate`
method (issue #8), completing the scaffold stub left by issue #7 (see
[[schema-network-json]]).

- Builds a `HashSet<string>` of `ControlLogicDef.Tags` names
  (`StringComparer.Ordinal` — tag names are case-sensitive, matching
  `BuildControlLogicDef`'s duplicate-name check from issue #6).
- Walks `NetworkDef.Components`, and within each component walks
  `Tags` (already merged singular/plural per DATA-IN-102) — checked
  **per tag, not per component**, since a multi-tag component binds
  several tags at once and each needs its own existence check.
- Throws `ConfigValidationException` on the *first* mismatch found in
  NETWORK document order, naming both the component (`Name`) and the
  undefined tag — this is exactly what TP-005/TP-103 assert on
  (`stderr reports the undefined tag reference by component name and
  tag name`).
- **Deliberately out of scope:** validating that a component's
  `DriverType` string names an actually-registered `IDriver`
  implementation. DATA-IN-103's requirement text only covers tag
  references; driver-type resolution happens later, at
  `PlcController`/`PlcEmulator.Core` construction time, since that's
  the layer that owns the registered driver set (see
  [[pattern-control-logic-parsing]]'s Config/Core split). Don't fold
  driver-type checking into `ConfigLoader.Validate` without a Systems
  Engineer sign-off that DATA-IN-103's scope has grown.

Host's `Program.cs` (UI-001/002/003, CLI wiring + fail-fast at the
Host boundary) is still scaffolding as of issue #8 — that's a
separate, later issue, not touched here.
