---
name: harness-dotnet-scaffolding
description: How to verify the PlcEmulator .NET solution builds/tests clean, matches SDD's project reference graph, and where TP-1xx procedures live
metadata:
  type: project
---

The PLC_Emulator repo (`PlcEmulator.sln`) is a .NET 8 solution (pinned via
`global.json`, SDK 8.0.100, `rollForward: latestFeature`) with one
SDK-style `.csproj` per namespace root under `src/` (`.Config`, `.Core`,
`.Drivers`, `.Network`, `.Host`) plus `tests/PlcEmulator.Tests` (MSTest).

**How to verify a build/test pass from scratch:**
```
find . -name bin -o -name obj | xargs rm -rf
dotnet build PlcEmulator.sln
dotnet test PlcEmulator.sln
```
Expect 0 warnings/0 errors on build; test count grows as features land
(was 1/1 — a scaffolding smoke test — as of issue #5; 11/11 as of issue
#6, DATA-IN-100/101; 27/27 once issue #6 and issue #7's
`ConfigLoaderNetworkTests` — 10 new NETWORK JSON parsing tests — landed
together on `main`, CI/CD merge 2026-08-16). Use the last recorded total
as the regression baseline: if it drops on a later run, that's a signal
even if the new feature's own tests pass.

**SDD dependency direction to check on every scaffolding-adjacent
issue** (grep `ProjectReference` in each `src/*/*.csproj`): `Config` is
a leaf; `Core → Config`; `Drivers → Core, Config`; `Network → Core`;
`Host → all four`. `Core` must never reference `Network` or `Drivers`.
`IDriver` lives in `src/PlcEmulator.Core/Drivers/IDriver.cs` (namespace
`PlcEmulator.Core.Drivers`) — interface next to its consumer
(`PlcController`/`TagTable`), not in `PlcEmulator.Drivers` — this was
an explicit, confirmed architecture decision (see issue #5 thread), not
a mistake to flag.

**NFR-502 (no third-party NuGet in src/):** `grep -rn PackageReference
src/*/*.csproj` should return nothing; only `tests/PlcEmulator.Tests`
should reference MSTest/coverlet packages.

**Why:** Windows/Visual Studio verification (NFR-501, DELIV-900) is a
one-time, late-stage consolidation step per client decision recorded on
issue #5 — not a per-feature CI matrix. `docs/ci/windows-verification.yml`
and `docs/ci/build-and-test.yml` are meant to stay staged, undeployed,
under `docs/ci/` (not copied into `.github/workflows/`) for the
duration of feature development. If a future issue's test procedure
seems to call for Windows-runner verification before the late-stage
consolidation issues (Implementation Plan #24/#27), that's a red flag —
check `docs/RTVM.md` TP-501/TP-900 wording first before assuming it's
required now.

**RTVM test procedures (TP-1xx):** live in `docs/RTVM.md`'s "Test
Procedures" table, keyed by ID and cross-referenced from the requirement
rows above it (e.g. DATA-IN-100 → TP-100). The Software Engineer's
dedicated test methods for a given TP- item are usually named
`TpNNN_...` in `tests/PlcEmulator.Tests/` — cheap way to locate exactly
which test implements which procedure, but always read the test body
against the RTVM row's expected-result text rather than trusting the
method name alone. Confirmed on issue #6 (2026-08-16): `Tp100_...`/
`Tp101_...` test names matched TP-100/TP-101 exactly, line-by-line, no
drift.

**How to apply:** Use this checklist for any issue that touches
scaffolding, adds a new `src/*` project, or changes project references —
not just issue #5 itself. For all issues, start test verification by
grepping `tests/PlcEmulator.Tests/` for the TP-NNN number(s) named in
the requirement's RTVM row, then confirm build/test clean from a fresh
`bin`/`obj` wipe.

**Also doubles as the post-merge regression checklist:** when CI/CD
hands back a trunk-merge regression request (issue #6, 2026-08-16; issue
#7, 2026-08-16), the same steps apply against `main` instead of the
feature branch: fresh `bin`/`obj` wipe + build/test, re-check `NFR-502`
(no `PackageReference` in `src/*/*.csproj`) and the `ProjectReference`
graph, confirm `docs/RTVM.md`'s `Verified` row(s) carry the real merge
commit SHA, and confirm no stray SDD lock markers / dirty `git status`
were left behind by the merge. No separate regression-specific
procedure exists — RTVM TP-1xx + this checklist is the whole regression
suite at this project stage.

**Regression pass hand-off convention (confirmed twice, issue #6 and
#7):** when RTVM already shows the requirement as `Verified` with a
commit SHA *before* you start the regression run (Systems Engineer
already recorded it off CI/CD's merge-confirmation comment), your
regression PASS doesn't need a fresh RTVM edit — say so explicitly and
hand off to `agent:systems-engineer` with `status:ready-for-rtvm-update`
anyway (per the standard two-step pass handoff); Systems Engineer's
follow-up comment just confirms "no further change needed" and closes
the issue out. Don't skip the handoff step just because the RTVM looks
already-current.

**Partial/deferred TP verification is a legitimate pass, if scoped that
way in the issue itself** (issue #9, CORE-200, 2026-08-16): the issue
text explicitly said the scan-loop *mechanics* could be proven with
stub `IInstruction`s, deferring full end-to-end TP-200 (real `XIC`/
`OTE` semantics) to the next issue (#10). Verify the test suite proves
exactly what the issue scoped (program order, once-per-rung, rung-state
threading, no leak across rungs) and that it *explicitly* asserts the
not-yet-built part still throws `NotImplementedException` (proves the
deferral is intentional and tracked, not silently skipped) — that's a
pass, not a partial/blocked result. Don't require the full TP wording
to be satisfiable before the dependent issue lands.

**Software Engineer flagging an SDD-documented signature as
stale/needing sign-off is not a build/test failure** — note it in the
pass comment and hand off normally; it's the Systems Engineer's doc to
fix, not grounds to withhold a pass. Example: issue #9 extended
`IInstruction.Evaluate(TagTable tags)` to `Evaluate(TagTable tags, bool
rungState)` for rung power-flow threading; `docs/SDD.md` line ~168
still shows the old signature as of 2026-08-16 and needs updating by
Systems Engineer.
