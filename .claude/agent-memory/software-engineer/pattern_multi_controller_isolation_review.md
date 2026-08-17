---
name: pattern-multi-controller-isolation-review
description: NFR-500 architecture review outcome (issue #23) — no static/singleton mutable state found; how TP-500's unit test was shaped
metadata:
  type: project
---

Issue #23 (NFR-500/TP-500) was a design-review/verification pass over
`PlcController`, `TagTable`, `Tag`, `TimerState`, `CounterState`,
`ScanEngine`, `WriteQueue`, `IDriver` + built-in drivers, `DriverFactory`,
and `Program.cs`/`TcpJsonServer` — checking for shared static/singleton
mutable state that would break "two `PlcController` instances share no
mutable state." **Result: no violation found.** Every `static` member in
the codebase (as of issue #23) is a stateless factory/builder method or
an immutable config object (e.g. `ConfigLoader.WireOptions`); all real
runtime state already lived in instance fields, one level owned by
`PlcController` per [[pattern_driver_resolution]] and
[[pattern_timer_elapsed_time]]. This is a consequence of earlier issues
(#15 CORE-209, #18 DATA-OUT-300) already building with NFR-500 in mind,
not something this issue had to fix.

Added `tests/PlcEmulator.Tests/MultiControllerIsolationTests.cs` as the
concrete unit-test artifact TP-500 calls for (its Steps column literally
says "...in the same process (unit test)", not inspection-only despite
the RTVM Verification-Method column saying "Inspection"). Shape: build
two `PlcController`s from configs that **deliberately reuse the same
tag/component names** — the strongest check, since same-named tags in
distinct-named configs wouldn't expose an accidental global/static
registry keyed by name the way same-named ones would. Covers: (1) a
`RunScan` mutation on one controller never leaking into the other's
same-named tag, (2) same NETWORK component/tag names still resolving to
two distinct driver instances bound to two distinct `TagTable`s, (3)
`GetSnapshot()` never sharing backing dictionaries across controllers.

**How to apply:** if a future NFR/architecture-review issue needs a
"prove no shared state" test, reuse the same-name-collision shape rather
than distinct-name configs — it's the harder, more convincing case.

## Windows CI deferred-promotion decision (correction, same issue)

`docs/ci/windows-verification.yml` is **not** deployed to
`.github/workflows/` in this project, and that's intentional — don't
assume "SDD names Windows as a target -> deploy it now" applies here.
`docs/SDD.md`'s "Target-platform verification strategy" section
(revised 2026-08-16, issue #5) explicitly defers NFR-501/DELIV-900
Windows verification to a single late-stage consolidation issue; the
file stays staged, undeployed, in `docs/ci/` until then. I stated the
opposite (claimed it was already deployed) in my first issue #23
hand-off comment and had to post a correction — check `docs/SDD.md`'s
actual current wording on this before asserting its state in a
hand-off comment, don't infer from memory or from the general
instruction-doc default.
