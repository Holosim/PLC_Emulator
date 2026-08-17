---
name: pattern-counter-edge-detection
description: CTU/CTD rising-edge counting needs inter-scan memory the documented CounterState schema didn't have; resolved by adding Cu/Cd bits (issue #12)
metadata:
  type: project
---

CORE-205/206 (`CTU`/`CTD`/`RES`, issue #12) required detecting a
*rising edge* of each instruction's enable input (`rungState`) to
increment/decrement `.ACC` only once per edge, not once per scan the
input is held true. That requires remembering the previous scan's
enable state somewhere between `IInstruction.Evaluate` calls — but
`IInstruction`/`SingleTagInstruction` are documented stateless (see
[[pattern_scan_engine_rung_power_flow]]), and DATA-IN-100/SDD.md's
documented `CounterState { Pre, Acc, Dn }` (3 fields) had nowhere to
put it — unlike `TimerState`, which already has a 4th field (`En`).

**Why:** `TimerState.En` doesn't actually need history — it just
mirrors the *current* scan's enable state (level-triggered
accumulation). Counters are structurally different: edge-triggered,
so they genuinely need one bit of memory per counting instruction.
Real Rockwell `COUNTER` data types carry exactly this (`.CU`/`.CD` status
bits alongside `.PRE`/`.ACC`/`.DN`), so extending `CounterState` with
`Cu`/`Cd` fields is a faithful domain-model extension, not an
invented mechanism.

**How to apply:** for any future edge-triggered ladder instruction
(anything whose behavior depends on a transition, not just a level),
check whether the documented tag-state schema has a slot for the
previous-scan condition before assuming `rungState` alone is enough —
it usually isn't, and the fix is a schema field on the relevant
`*State` class (next to the tag, not instance state on the stateless
instruction), flagged to the Systems Engineer for RTVM/SDD sign-off the same way the
`rungState` signature change was flagged in issue #9. Don't block on
this — implement the reasonable extension, flag it explicitly in the
hand-off comment, and let the SE/Test Engineer confirm before it lands
in `docs/SDD.md`/`docs/RTVM.md`'s data model text.

Also: `SingleTagInstruction.Evaluate` had to become `virtual` (was a
flat non-virtual `NotImplementedException` stub) so individual
mnemonics (`Ctu`/`Ctd`/`Res`) could override it while the
not-yet-implemented ones (`Xic`/`Xio`/`Ote`/`Ton`/`Tof`) stay stubbed —
expect the same pattern for whichever issue lands those next.
