---
name: pattern-control-logic-parsing
description: How CONTROL_LOGIC JSON parsing is split between PlcEmulator.Config (DTOs, generic) and PlcEmulator.Core (domain build, mnemonic-aware) — reuse this split for DATA-IN-102/NETWORK parsing too
metadata:
  type: project
---

Established on issue #6 (DATA-IN-100/101, branch `issue-6`):
CONTROL_LOGIC JSON parsing is deliberately two-layered because
`PlcEmulator.Config` is a leaf project (can't reference `Core`, see
[[project-plc-emulator-scaffold]]) but the SDD's Coding Standards
require instruction classes — one per mnemonic — to live in
`PlcEmulator.Core.Instructions`.

- **Config layer** (`ConfigLoader.LoadControlLogic`): parses JSON into
  generic DTOs (`TagDef`, `RungDef`, `InstructionDef`, `OperandDef`)
  that know nothing about Core's mnemonic classes. `InstructionDef`
  just holds a mnemonic string + a uniform `operands` array (JSON
  string → tag reference, JSON number → literal) — no per-mnemonic
  field names, no operand-arity checking here. Validates: well-formed
  JSON, recognized tag `type`, `initialValue`/type match, duplicate
  tag names. Throws `ConfigValidationException` (defined in `Config`,
  reused by `Core` — `Core` depends on `Config` so this is fine)
  either way.
- **Core layer** (`ControlLogicBuilder` + `Instructions.InstructionFactory`):
  turns the generic DTOs into real `TagTable`/`Rung`/`IInstruction`
  objects. `InstructionFactory` is the single place that knows the
  full MVP mnemonic list and each mnemonic's exact operand arity
  (single-tag for contacts/coil/timers/counters/RES, two
  tag-or-literal for compare, two tag-or-literal + a destination tag
  for math) — also throws `ConfigValidationException` on a bad
  mnemonic/arity, just later than Config-layer errors (still before
  any scan runs, so the fail-fast *effect* is identical).
- **Instruction classes are structural-only in this issue**: every
  MVP mnemonic (`Xic`, `Xio`, `Ote`, `Ton`, `Tof`, `Ctu`, `Ctd`, `Res`,
  `Equ`/`Neq`/`Grt`/`Les`/`Geq`/`Leq`, `Add`/`Sub`/`Mul`/`Div`) exists
  as a real type with real operand-capturing fields, but
  `Evaluate(TagTable)` throws `NotImplementedException` referencing
  the CORE-2xx item that will fill it in — those are separate,
  later issues per `docs/IMPLEMENTATION_PLAN.md` (items 6-10). Don't
  implement real evaluation semantics when the issue you're on is only
  DATA-IN-100/101-scoped; do create the class shape (needed for the
  rung to even be inspectable/parseable) and grouped by shared shape
  via abstract bases (`SingleTagInstruction`, `CompareInstruction`,
  `MathInstruction`) to avoid per-mnemonic boilerplate.
- **`IInstruction` gained a `Mnemonic` member** (non-breaking addition
  to the interface the SDD had already established with just
  `Evaluate`) so a parsed rung's instruction sequence is inspectable
  via `ToString()`/`Mnemonic` without casting to a concrete type —
  this is what makes TP-101's `[XIC:Start_PB, OTE:Motor_Run]`
  assertion straightforward.

**Reuse for DATA-IN-102 (NETWORK schema, issue #3):** the same
Config-generic/Core-specific split almost certainly applies —
`NetworkDef`/`NetworkComponentConfig` (Config, generic: name,
driver-type string, tag-binding string(s)) vs. driver instantiation
(Core/Drivers, which already knows the `IDriver` implementations and
can validate the driver-type string against what's actually
registered).
