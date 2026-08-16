---
name: pattern-scan-engine-rung-power-flow
description: ScanEngine.Evaluate's rung-condition-in/out threading through IInstruction.Evaluate(tags, rungState) — why the extra bool param exists and how future instruction issues (#10-#14) must use it
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

**The contract, for anyone implementing CORE-201/202/203/204/205/206/207/208 (issues #10-#14):**
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

**What's still a stub:** `SingleTagInstruction`, `CompareInstruction`,
`MathInstruction` all still `throw NotImplementedException` — only
their `Evaluate` signature changed to match the new interface, per
[[pattern-control-logic-parsing]]'s established pattern of
structural-only changes outside an issue's real scope.

**Loop mechanics were tested with local test-only `IInstruction` stubs**
(`ScanEngineTests.cs`, not touching the real `Xic`/`Ote` classes) —
issue #9 explicitly permitted this since real `XIC`/`OTE` semantics
land with #10. Full TP-200 (docs/RTVM.md) can't verify end-to-end until
then; only the loop-mechanics subset (program order, per-rung reset,
tag values updated once per scan) is verifiable now.

**Also landed in #9 (small, in-scope side effects):** `WriteQueue`
got a minimal thread-safe `Enqueue`/`DrainAll` (locked list-swap) since
`PlcController.RunScan()` needs to call `DrainAll()` every scan per its
own doc comment — this is just queue mechanics, not OUT-401's network
wiring (`PlcController.QueueWrite()`, the network-facing entry point,
is still a stub — that's OUT-401/#21's job).
