---
name: gotcha-concurrent-instruction-issues
description: Issues #10-#14 (instruction implementations) run as separate concurrent SE hand-offs sharing the same Instructions/ folder - stay strictly scoped to your own issue's files
metadata:
  type: project
---

Observed on issue #14 (2026-08-16): issues #10, #11, #12, #13, #14 were
all simultaneously labeled `agent:software-engineer` /
`status:in-progress` — separate parallel SE runs, one per instruction
group (contacts/coil, timers, counters, compares, math), all editing
files under the same `src/PlcEmulator.Core/Instructions/` directory
that #9's scaffold created as stubs.

**How to stay safe:** touch only the files your own issue's RTVM item
names. For #14 (CORE-208) that meant `MathInstruction.cs` +
`Add.cs`/`Sub.cs`/`Mul.cs`/`Div.cs` + `Tag.cs` (for the new fault-flag
field, needed generically, see [[pattern-fault-flag]]) — explicitly
*not* `SingleTagInstruction.cs`, `CompareInstruction.cs`, or any of the
`Xic`/`Xio`/`Ote`/`Ton`/`Tof`/`Ctu`/`Ctd`/`Res`/`Equ`/`Neq`/`Grt`/`Les`/
`Geq`/`Leq` concrete stub files, even though they sit right next to the
ones you're editing. `InstructionFactory.cs` and `IInstruction.cs` are
shared infrastructure — don't edit them for a single-instruction-group
issue unless the contract itself is wrong for your issue (that
happened once, in #9, and got flagged to Systems Engineer rather than
silently changed elsewhere).

Each of #10-#14 branches directly off `main` (not off each other) since
their only real dependency is #9 (already merged) — check
`docs/IMPLEMENTATION_PLAN.md`'s dependency graph before assuming a
same-numbered predecessor issue's branch needs to be a base.
