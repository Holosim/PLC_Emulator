---
name: implementation-plan-plc-emulator
description: Build sequence rationale and issue-number map created from the PLC_Emulator Implementation Plan (issue #4)
metadata:
  type: project
---

Issue #4 ("Implementation Plan") closed after populating
`docs/IMPLEMENTATION_PLAN.md` (single linear priority order — project
didn't warrant the multi-phase complexity/UI/documentation-rigor
breakdown) and creating all downstream work. Issue-number map, in case
a later feature issue references "the timers issue" or similar without
the number:

- #5 Generate Code Base (no deps, `agent:software-engineer` directly —
  also stands up the Windows+Linux CI matrix NFR-501 relies on)
- #6 DATA-IN-100/101 (tag data model + rung/instruction schema)
- #7 DATA-IN-102 (NETWORK schema)
- #8 DATA-IN-103 (cross-file validation)
- #9 CORE-200 (scan engine) — depends only on #6, not on the CLI/UI
  issues, since it's unit-testable directly against the tag/rung model
- #10 CORE-201/202 (XIC/XIO/OTE), #11 CORE-203/204 (TON/TOF),
  #12 CORE-205/206 (CTU/CTD/RES), #13 CORE-207 (compare),
  #14 CORE-208 (math) — all Finish-Start on #9 only, so they can run
  concurrently once #9 lands
- #15 CORE-209 (driver architecture) — FS on #9 and #7
- #16 UI-001/003 (CLI startup + fail-fast) — FS on #8
- #17 UI-002 (diagnostics) — FS on #16
- #18 DATA-OUT-300 (runtime state model) — FS on #9
- #19 DATA-OUT-301 (TCP/JSON serialize) — FS on #18
- #20 OUT-400 (TCP listener/single-client) — FS on #19 and #16
- #21 OUT-401 (tag write), #22 OUT-402 (disconnect) — both FS on #20
- #23 NFR-500 (isolation verification) — FS on #15, #18
- #24 NFR-501 (cross-platform consolidated sign-off) — FS on #22;
  framed explicitly as a sign-off, not new work, since CI already
  gates every feature above it on both runners per `docs/SDD.md`
- #25 NFR-502 (dependency policy review) — FS on #15
- #26 NFR-503 (no-persistence verification) — FS on #21
- #27 DELIV-900 (VS solution consolidation) — FS on #23, #24, #25, #26
  (deliberately last, per the late-stage instruction in issue #4 and
  `docs/SDD.md`'s Build & Toolchain Conventions)

**Why:** grouping RTVM items into ~22 issues (rather than one per
single RTVM ID) kept issue count manageable while still giving each
group its own testable scope and dependency chain; grouping rule used
was "same schema / same instruction family" (e.g. all four compare
mnemonics stay one item, but timers and counters are split from
contacts/coil since they're functionally distinct instruction
families).

**How to apply:** when a `[RTVM-...]` feature issue query references
another feature by RTVM ID rather than issue number, use this map to
find the issue number. If new work is added later, extend this list
rather than starting a separate one — see
[[implementation-plan-plc-emulator]] self-reference avoided;
cross-reference [[sdd-decisions-plc-emulator]] for the architecture
those dependencies rely on.
