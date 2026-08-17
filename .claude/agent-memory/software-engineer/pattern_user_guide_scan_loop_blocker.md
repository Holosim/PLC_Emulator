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

**Resolution (2026-08-17, once OUT-403/#30 merged to main):** picked
the issue back up per Systems Engineer's hand-off. Rebased `issue-29`
onto `main` (18 commits of drift, mostly the OUT-403 fix/regression/CI
history — see [[pattern_host_scan_loop]]); only merge conflict was
this MEMORY.md index, resolved by keeping both lines. Rewrote §3's
"Known v1.0 limitation" block into an "Observing a `tag_write`'s
effect on a live process" note describing the *actual* current
behavior, and re-verified live end-to-end against a freshly built
`plcemu` (same technique as the original draft — ran the guide's exact
snippet verbatim, not just read the source).

**A second, more subtle thing the live re-verification caught:** a
naive single-`readline()`-after-write client (what the original
pre-OUT-403 draft's snippet did) *still* looks like the write silently
failed even after OUT-403 landed — not because it wasn't applied
(confirmed applied within a fraction of a millisecond via
[[pattern_host_scan_loop]]'s "~877k msg/2s" cadence finding) but
because the free-running broadcast firehose fills the socket's queue
faster than a slow reader drains it, so the *next* line read is
whatever was already queued from scans before the write landed — in
one live run this took ~2,120 buffered `tag_update` lines before the
write became visible to the reader. Fixed the guide's snippet to loop
until it actually observes the expected value instead of trusting the
next line, and added an explicit paragraph explaining why (reading
side, not writing side). Worth remembering for any future
free-running-broadcast protocol: "confirmed applied" and "confirmed
observable by a naive line-at-a-time reader" are different claims, and
a user guide needs to honestly address both.

TP-901 step 4 (live tag exchange: read + one write) now holds
end-to-end against a live process built from `main` + this guide's own
instructions, verbatim. Full DELIV-901 hand-off restored to
`agent:test-engineer`.
