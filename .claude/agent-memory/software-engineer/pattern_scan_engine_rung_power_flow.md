---
name: pattern-scan-engine-rung-power-flow
description: ScanEngine.Evaluate's rung-condition-in/out threading through IInstruction.Evaluate(tags, rungState) contract for #10-#14's instruction semantics; XIC/XIO/OTE (CORE-201/202) landed on this contract in issue #10
metadata:
  type: project
---

Established on issue #9 (CORE-200, branch `issue-9`):
`IInstruction.Evaluate` was extended from `Evaluate(TagTable tags)` to
`Evaluate(TagTable tags, bool rungState)`. This was a real gap in the
issue #5 scaffold, not an assumption filled silently — flagged to the
Systems Engineer in the issue #9 hand-off for `docs/SDD.md`'s Coding
Standards wording to catch up.

**Why the extra parameter is required:** with only `tags` as input, an
output instruction (`OTE`, and later `TON`/`TOF`/`CTU`/`CTD`/math) has
no way to know whether the contacts preceding it *in the same rung*
are currently true — nothing in `TagTable` records that. Standard
ladder-logic engines (including real Rockwell/Allen-Bradley RLL) solve
this with "rung-condition-in / rung-condition-out" power-flow
threading: each instruction receives the accumulated state and returns
the state to hand to the next instruction.

**The contract, for anyone implementing CORE-203/204/205/206/207/208 (issues #11-#14):**
- `ScanEngine.Evaluate` seeds `rungState = true` (energized, left power
  rail) at the *start of every rung* — it does not carry over between
  rungs (see `Evaluate_RungState_DoesNotLeakAcrossRungs` in
  `ScanEngineTests.cs`).
- **Condition-type instructions** (`XIC`, `XIO`, `EQU`/`NEQ`/`GRT`/`LES`/`GEQ`/`LEQ`):
  read their own tag(s), compute their own boolean condition `C`, and
  return `rungState && C`.
- **Action-type instructions** (`OTE`, `TON`, `TOF`, `CTU`, `CTD`, `RES`,
  `ADD`/`SUB`/`MUL`/`DIV`): use the incoming `rungState` to decide
  whether to apply their side effect (e.g. `OTE` writes `rungState` to
  its tag; a math block only executes when `rungState` is true), and
  **return `rungState` unchanged** so power flow continues correctly
  to any further instructions on the same rung.
- `ScanEngine.Evaluate` itself stays completely instruction-type-agnostic
  — it never branches on mnemonic/type, it just threads the bool
  through every instruction in program order: `rungState =
  instruction.Evaluate(tags, rungState)`.

**Issue #10 (CORE-201/202, branch `issue-10`) landed the first real
instructions on this contract — the reference implementation for
everyone doing #11-#14:**
- `Xic.Evaluate` → `rungState && ReadBoolTag(tags)`.
- `Xio.Evaluate` → `rungState && !ReadBoolTag(tags)`.
- `Ote.Evaluate` → writes `rungState` to its tag via `WriteBoolTag`,
  returns `rungState` unchanged.
- `SingleTagInstruction.Evaluate` (the shared base for `XIC`/`XIO`/`OTE`/
  `TON`/`TOF`/`CTU`/`CTD`/`RES`) changed from one shared throwing method
  to **`virtual`**, default body unchanged (still throws
  `NotImplementedException($"{Mnemonic}.Evaluate lands with {_coreItem}.")`).
  Only the three mnemonics whose semantics landed override it — `TON`/
  `TOF`/`CTU`/`CTD`/`RES` files were **not touched**, they still inherit
  the throwing default. Do the same for #11-#13: override in the
  subclass, don't touch the base default or sibling stub files.
- Added two `protected` helpers on `SingleTagInstruction` for reuse by
  future BOOL-tag mnemonics: `ReadBoolTag(tags)` (throws
  `InvalidOperationException` if the tag isn't actually BOOL-typed —
  there's no earlier load-time validation catching an instruction/tag
  type mismatch yet, so this is the first line of defense) and
  `WriteBoolTag(tags, value)`.
- `PlcController.GetSnapshot()` (DATA-OUT-300) is still a stub — can't
  assert on a controller's resulting tag value through the public
  `PlcController` API yet. Tests needing a real tag-value assertion go
  through `ControlLogicBuilder.BuildTagTable` + `ScanEngine.Evaluate`
  directly (see `XicXioOteTests.cs`), same pattern `ScanEngineTests.cs`
  already used.

**Known gap, not blocking, flagged for awareness only:** no CONTROL_LOGIC
load-time validation cross-checks an instruction's tag operand against
that tag's declared type (e.g. nothing stops `XIC` referencing a DINT
tag) — `ReadBoolTag` throws at scan time instead of load time in that
case. Existing precedent (`ControlLogicBuilder`/`ConfigLoader`) doesn't
validate this either, so it wasn't invented for #10; raise it if a
future issue's scope brushes against it.

**Loop mechanics were tested with local test-only `IInstruction` stubs**
(`ScanEngineTests.cs`, not touching the real `Xic`/`Ote` classes) —
issue #9 explicitly permitted this since real `XIC`/`OTE` semantics
landed with #10. TP-200 (docs/RTVM.md) is now verifiable end-to-end
with the real classes via `XicXioOteTests.cs`'s series-AND rung test.

**Also landed in #9 (small, in-scope side effects):** `WriteQueue`
got a minimal thread-safe `Enqueue`/`DrainAll` (locked list-swap) since
`PlcController.RunScan()` needs to call `DrainAll()` every scan per its
own doc comment — this is just queue mechanics, not OUT-401's network
wiring (`PlcController.QueueWrite()`, the network-facing entry point,
is still a stub — that's OUT-401/#21's job).
