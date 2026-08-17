---
name: pattern-timer-elapsed-time
description: How TON/TOF (CORE-203/204) get real wall-clock elapsed time without a fixed scan period, and why ScanEngine (not IInstruction) owns the clock
metadata:
  type: project
---

Established on issue #11 (CORE-203/204, branch `issue-11`), building on
[[pattern-scan-engine-rung-power-flow]]'s `IInstruction.Evaluate(tags,
rungState)` contract from issue #9.

**The gap:** `TON`'s `.ACC` must accumulate against real elapsed time
(TP-203 samples at literal t=1000ms/t=2100ms), but nothing in
`docs/SDD.md`/`docs/IMPLEMENTATION_PLAN.md`/`docs/PROJECT_DEFINITION.md`
defines a fixed scan period, and `ScanEngine.Evaluate`/`RunScan()` took
no time-related parameter. This was a real architectural gap, not an
assumption to fill silently.

**The resolution (extended, didn't escalate):** rather than invent a
fixed scan-rate config value nobody asked for, `IInstruction.Evaluate`
gained a third parameter: `Evaluate(TagTable tags, bool rungState,
TimeSpan elapsed)`. `elapsed` is the real wall-clock time since the
*previous* call to `ScanEngine.Evaluate` (`TimeSpan.Zero` on a
controller's first scan — no prior scan to measure from). `ScanEngine`
measures this itself with a private `Stopwatch` field, restarted every
call, and passes the same value to every instruction evaluated that
scan. Non-timer instructions (`XIC`, `OTE`, compares, math) simply
ignore the parameter — same "extend uniformly, most implementors
ignore it" pattern as `rungState` in issue #9.

**Why `ScanEngine` holds the clock and not `Ton`/`Tof` themselves:**
`docs/SDD.md` is explicit that instruction classes must stay stateless
("operating only on the tag table ... passed to them") so the same
instruction logic is safe to reuse across controller instances. A
per-instance `lastTick` field on `Ton` would violate that. `ScanEngine`
is already documented as "owned by, not shared across, a
`PlcController`" — so it's the correct place for this one piece of
real per-controller clock state. This is the general rule for any
future v1.0 gap needing similar bookkeeping: look for something
already documented as controller-owned-and-not-shared before adding
instance state to an `IInstruction` implementor.

**TON/TOF logic (both action-type, return `rungState` unchanged):**
- `TON`: enabled → `.EN=true`, `.ACC += elapsed`, `.DN = .ACC >= .PRE`
  (no clamping, matches real Allen-Bradley — `.ACC` keeps climbing past
  `.PRE` while still enabled). Disabled → `.EN=false`, `.ACC=0`, `.DN=false`.
- `TOF`: enabled → `.EN=true`, `.ACC=0` (held at zero, not just reset
  once), `.DN=true` immediately. Disabled → `.EN=false`, `.ACC +=
  elapsed`, `.DN = .ACC < .PRE`. Because `.ACC` is pinned to 0 every
  scan while enabled, the "start accumulating from 0 at the moment of
  disable" requirement falls out naturally — no edge-detection code
  needed for the enabled→disabled transition.

**Misconfigured tag type (TON/TOF pointed at a non-TIMER tag):** no
DATA-IN-103 cross-validation exists yet for "TON/TOF operand must be
TIMER-typed" — this is a real coverage gap in Config Loader validation,
not addressed by issue #11. Handled defensively for now with a
`RequireTimer(tags)` helper on `SingleTagInstruction` (`tags.Get(TagName).Timer
?? throw InvalidOperationException(...)`) so a bad CONTROL_LOGIC file
fails with a descriptive message instead of a `NullReferenceException`.
Whoever picks up CTU/CTD (issue #12, CORE-205/206) should add the
equivalent `RequireCounter` helper the same way, and DATA-IN-103 may
want this validation moved to load time instead — not raised as a
blocker since it doesn't stop CORE-203/204 from being correct, just
noted here in case Systems Engineer wants to prioritize it.

**Test strategy:** unit tests call `Ton.Evaluate`/`Tof.Evaluate`
directly with hand-picked `TimeSpan` values (exact, fast, no sleep) —
see `tests/PlcEmulator.Tests/TimerInstructionTests.cs`. Only one test
(`ScanEngine_MeasuresRealElapsedTime_BetweenCalls`) does a real
`Thread.Sleep` with a loose `>=` bound, just to prove `ScanEngine`'s
`Stopwatch` plumbing itself works — TP-203/TP-204's literal
t=1000ms/t=2100ms real-time procedure is the Test Engineer's to run at
the integration level.
