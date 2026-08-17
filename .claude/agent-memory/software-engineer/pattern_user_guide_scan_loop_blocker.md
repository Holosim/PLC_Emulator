---
name: pattern_user_guide_scan_loop_blocker
description: DELIV-901 (issue #29) — USER_GUIDE.md drafted in full, but blocked on the long-flagged "no free-running scan loop" gap now that a live-demo acceptance bar (TP-901) actually depends on it
metadata:
  type: project
---

**What (issue #29):** Wrote `docs/USER_GUIDE.md` covering all five
DELIV-901 sections (solution outline, CONTROL_LOGIC/NETWORK schemas +
worked examples, launch/CLI/diagnostics, ladder-logic authoring +
instruction reference table, `IDriver` extension steps). Every command
and JSON example in it was actually run against a live `plcemu`
process built from current `main` before being committed (see "always
verify guide commands live" below) — not just read out of the source.

**The blocker:** [[pattern_tag_write_queue]] first flagged (issue #21)
that `Program.cs` never calls `PlcController.RunScan()` on any
cadence — it starts `TcpJsonServer` then blocks forever. At the time
this was "not blocking" because every RTVM test procedure that needed
"next scan cycle" behavior drove it explicitly via a test harness.
DELIV-901/TP-901 is the first requirement that actually needs a *live*
`plcemu` process (not a test harness) to demonstrate a `tag_write`
taking visible effect over a real TCP connection — and empirically it
can't: a live `tag_write` is queued forever and never applied, so a
follow-up `read_request` always returns the stale value. Confirmed by
running the exact guide commands against a real process (see the
transcript in the issue #29 hand-off comment). `docs/SDD.md`'s own ICD
section shows an example exchange implying the opposite (write then a
fresh `tag_update` reflecting it) — so this is a real gap between the
SDD's documented intent and what `Program.cs` actually does, not just
an "undefined cadence" ambiguity.

**Why escalated instead of fixed silently:** adding a background scan
loop is a non-trivial functional change (threading, cadence policy,
possibly a new CLI knob) with no owning RTVM item — implementing it
inside a docs-only issue would be scope creep past what step 3 of
"Working an issue" allows, and picking a cadence myself would be
exactly the kind of silent assumption-filling the role instructions
warn against. Routed to `agent:systems-engineer` with
`status:blocked` instead of handing to `agent:test-engineer`, since
Test Engineer running TP-901 verbatim would hit this same wall with no
way to resolve it themselves.

**Reusable technique — always verify guide commands live, not just by
reading source:** built a `quickstart/` CONTROL_LOGIC/NETWORK pair
exactly as documented, ran the guide's literal `dotnet run --project
src/PlcEmulator.Host -- ...` command, and drove the guide's literal
Python TCP snippet against it — this is what caught the `--port`
default-value slip (guide draft said `5050` as the default; the actual
`Program.DefaultPort` constant is `5000`) before it shipped. Also
independently confirmed the ladder-logic instruction classes reject
dotted `Tag.DN`/`Tag.ACC` addressing entirely (`XIC` requires `BOOL`,
compares require `DINT`/`REAL`, neither accepts a `TIMER`/`COUNTER`
tag) — a real v1.0 limitation worth documenting explicitly so a reader
doesn't waste time trying to chain a timer's `.DN` into a downstream
rung.
