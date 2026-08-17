---
name: pattern-fault-flag
description: Tag.Fault (nullable string) as the fault-flag mechanism for defined runtime errors (CORE-208 DIV-by-zero); how it's set/cleared and by whom
metadata:
  type: project
---

Established on issue #14 (CORE-208, branch `issue-14`).

docs/SDD.md's Coding Standards ("Error handling") says the Scan Engine
never throws for expected runtime conditions like divide-by-zero —
"those set a fault flag on the offending tag/instruction result
instead" — but as of #9's scaffold, no fault-flag mechanism existed
anywhere in the codebase (not on `Tag`, not in `TagSnapshot`/DATA-OUT-300
which was still a `// TODO` stub). This was a real gap, not spelled out
by any prior issue, so I designed and added it rather than escalating —
it was narrowly scoped to what CORE-208 needed and didn't require a
judgment call outside an SE's normal implementation latitude.

**The mechanism:** `Tag.Fault` — nullable `string`, `null` when not
faulted. Set by the instruction that owns the "offending... result"
(i.e. the *destination* tag of a math op) with a descriptive message,
instead of throwing. Cleared back to `null` automatically the next
time the same destination is *successfully* written — so a fault
self-heals once the condition that caused it (e.g. a divisor going
non-zero) resolves, matching how a real PLC surfaces this. On fault,
the destination's `Value` is left at its last good value, not
zeroed/overwritten.

**Where the logic lives:** `MathInstruction.Evaluate` (template
method) calls each subclass's `TryCompute(left, right, out result, out
fault)` — returns `false` + a fault message instead of throwing for a
defined error. `Add`/`Sub`/`Mul` always return `true`; `Div` returns
`false` when the right operand is `0`. Any future instruction that can
produce a "defined runtime error" (none currently known) should reuse
`Tag.Fault` rather than invent a second fault-flag field — it's meant
to be the one general-purpose mechanism, not DIV-specific, even though
DIV-by-zero is its only producer today.

**Not yet wired:** `TagSnapshot`/DATA-OUT-300 doesn't expose `Fault`
externally yet — that's DATA-OUT-300/301's scope (issues #18/#19),
not #14's. Flagging here so whoever picks up DATA-OUT-300 knows the
field already exists on `Tag` and just needs surfacing in the
snapshot/protocol, not re-inventing.

See also [[pattern-scan-engine-rung-power-flow]] — math instructions
are action-type: gated by incoming `rungState`, return it unchanged,
never fault or compute when the rung is de-energized.
