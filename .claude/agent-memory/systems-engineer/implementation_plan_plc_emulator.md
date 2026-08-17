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

- #5 Generate Code Base (no deps, `agent:software-engineer` directly).
  Closed 2026-08-16. `docs/ci/windows-verification.yml` and
  `docs/ci/build-and-test.yml` were staged in `docs/ci/` but the
  client decided (on this issue's thread) that Windows/VS
  verification is a one-time final consolidation step, not a
  per-feature CI matrix — see REVISED note under
  [[sdd-decisions-plc-emulator]]. Those files stay undeployed
  (never copied to `.github/workflows/`) for the whole dev phase;
  only `ubuntu-latest` CI runs per feature until #24/#27.
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
- #24 NFR-501 (cross-platform consolidated sign-off) — FS on #22.
  CORRECTED 2026-08-16 (superseding the original framing below): this
  is real, first-time work, not a sign-off — every feature issue
  #6-#23 builds/tests on `ubuntu-latest` only during development, per
  the client's explicit decision on issue #5. #24 is where
  `docs/ci/build-and-test.yml`'s ubuntu+windows matrix actually gets
  promoted to `.github/workflows/` and run against the accumulated
  codebase for the first time.
- #25 NFR-502 (dependency policy review) — FS on #15
- #26 NFR-503 (no-persistence verification) — FS on #21
- #27 DELIV-900 (VS solution consolidation) — FS on #23, #24, #25, #26
  (deliberately last, per the late-stage instruction in issue #4 and
  `docs/SDD.md`'s Build & Toolchain Conventions). Closed 2026-08-17
  after a post-release field-defect round-trip (see
  [[issue-closeout-log-plc-emulator]]); shipped as `v1.0.339`.
- #29 DELIV-901 (user quick-start guide, `docs/USER_GUIDE.md`) — added
  2026-08-17 outside the original Implementation Plan sequence (client
  request via issue #28, after #27 was already done and shipped). FS
  on #27 only. Created directly with `status:on-hold` (not
  `agent:software-engineer`) even though #27 was already closed at
  creation time — let `dependency-check.yml`'s sweep confirm and
  release it rather than hand-verifying and skipping the on-hold step;
  keeps the mechanism consistent and avoids a manual judgment call
  that isn't actually necessary.
- #30 OUT-403 (background scan loop in Host) — added 2026-08-17,
  discovered while verifying #29/DELIV-901's guide against a live
  process (see [[requirements_traps_plc_domain]]). No dep, created
  `agent:software-engineer` directly. Round-tripped once: first pass
  free-running `Broadcast` starved `TcpJsonServer`'s `_clientLock`,
  regressing already-`Verified` OUT-400 (second client stopped being
  rejected promptly under load) — Software Engineer fixed by splitting
  the lock (`volatile _clientStream` read lock-free, new `_writeLock`
  scoped only to the write, `_clientLock` untouched by broadcast rate),
  Test Engineer independently reproduced both the break and the fix on
  live processes since the added unit test couldn't repro the race
  in-sandbox. RTVM set `In Test` (commits `a66ea25` impl, `c707b04`
  fix) pending CI/CD merge/Verified. #29 (DELIV-901) still FS on #30.
  **Updated 2026-08-17:** CI/CD merged `issue-30`→`main` as `a32f132`
  (trunk merge, `v1.0.365` tag, no release cut — DELIV-901 still open),
  regression testing flagged as needed. RTVM OUT-403 recorded
  `a32f132` and flipped `In Test`→`Verified`; handed to
  `agent:test-engineer` (`status:ready-for-test`) for the post-merge
  regression pass rather than closing outright. #29 (DELIV-901) is now
  unblocked to proceed once its own dependency check clears.

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

**Gotcha found on #6 (2026-08-16):** issue #5 closed/signed-off but
was never actually merged to `main` by CI/CD — the hand-off chain
ended without a final relabel to `agent:cicd`, so `main` still lacks
the scaffold while `dependency-check.yml` (which only checks
issue-closed state, not trunk content) released #6 anyway. Software
Engineer correctly branched `issue-6` from `origin/issue-5` instead of
`main` and flagged it; flagged again to `agent:cicd` when handing off
#6. **Lesson:** when a downstream issue's branch report says "based on
origin/issue-N, not main" for a dependency that's marked closed, don't
treat that as resolved by silence — explicitly re-flag it in the
hand-off to whichever role can actually do the merge (`agent:cicd`),
every time, until it's confirmed landed.

**Resolved 2026-08-16:** CI/CD merged `issue-6` (which carried
`issue-5`'s full history) into `main` as a fast-forward to `b0ebb72` —
this landed the #5 scaffold gap and the #6 feature work in one merge.
`main` now has `src/`/`tests/`/`PlcEmulator.sln` for the first time.
DATA-IN-100/DATA-IN-101 marked `Verified` in `docs/RTVM.md` with SHA
`b0ebb72`. `VERSION` file created (1.0), tag `v1.0.1` pushed, but no
GitHub Release cut yet — most RTVM items still `Approved`/`In Test`.
Regression testing was requested by CI/CD (trunk merge), so #6 was
handed to `agent:test-engineer` rather than closed outright.
