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

**Regression baseline updated (issue #9, 2026-08-16):** 27/27 still —
CORE-200's scan-engine work landed on a branch cut before issue #7's
merge, so its 6 new `ScanEngineTests` (not 7 as first reported
mid-development; final file has 6 `[TestMethod]`s) replaced into the
same 27 total once `main` had both #7 and #9 merged
(1 scaffolding + 10 `ControlLogicSchemaTests` + 10
`ConfigLoaderNetworkTests` + 6 `ScanEngineTests` = 27). Don't be
alarmed if a PR's own branch reports a different total than the
post-merge `main` total — always recount from `grep -c
"\[TestMethod\]" tests/PlcEmulator.Tests/*.cs` against the *current*
`main`, not the number quoted in an earlier branch-only comment.

**Shallow clone can make `main` look like it's missing a merge that's
actually there (issue #9, 2026-08-16):** a plain `git checkout main &&
git pull` on the default shallow clone showed `main` at only 2
commits, with the CORE-200 merge commit (`49d5150`) reported as "not a
valid object" — looked like the merge had vanished. Running `git fetch
--unshallow origin` first fixed it; `49d5150` was a real ancestor of
`main` all along. Always unshallow before concluding a merge is
missing or a commit reference in an RTVM/issue comment is wrong.

**Regression baseline updated (issue #8, DATA-IN-103, 2026-08-16):** 31/31
on branch `issue-8` (27 prior baseline + 4 new `ConfigLoaderValidateTests`
for `ConfigLoader.Validate`, TP-005/TP-103 cross-file tag-reference
check). Same partial/deferred-TP pass pattern as issue #9: TP-005 also
traces to UI-003 in `docs/RTVM.md`, and the CLI/Host `Program.cs` wiring
that would make it a true process-level test (non-zero exit, no TCP
listener) still doesn't exist — verify at the `ConfigLoader.Validate`
unit level instead (exception type + message naming both component and
undefined tag) and call it a legitimate scoped pass, same as CORE-200.
This is becoming a recurring shape at this project stage: several
RTVM test procedures assume Host/CLI wiring that lands in a later,
separate issue — always check whether a TP row cross-references a
UI-00x item before treating an end-to-end gap as a failure.

**Software Engineer flagging an SDD-documented signature as
stale/needing sign-off is not a build/test failure** — note it in the
pass comment and hand off normally; it's the Systems Engineer's doc to
fix, not grounds to withhold a pass. Example: issue #9 extended
`IInstruction.Evaluate(TagTable tags)` to `Evaluate(TagTable tags, bool
rungState)` for rung power-flow threading; `docs/SDD.md` line ~168
still shows the old signature as of 2026-08-16 and needs updating by
Systems Engineer. Same pattern recurred on issue #12 (CORE-205/206,
2026-08-17): Software Engineer added undocumented `Cu`/`Cd` fields to
`CounterState` (edge-detection memory for `CTU`/`CTD`, beyond
DATA-IN-100's documented 3-field `{Pre, Acc, Dn}`) and flagged it
in-code + in the handoff comment for SE sign-off — verified it was a
narrow, well-justified addition (diff-scoped, doesn't touch
CONTROL_LOGIC JSON shape) and passed anyway.

**Regression baseline updated (issue #12, 2026-08-17):** 38/38 on
`issue-12` branch (27 baseline + 11 new `CounterInstructionTests` for
CORE-205/206 CTU/CTD/RES). Confirmed via `git diff <prev>..<head>
--stat` that only counter-related files + the new test file changed;
`Xic`/`Xio`/`Ote`/`Ton`/`Tof` still correctly fall through to the base
`NotImplementedException` stub in `SingleTagInstruction.Evaluate`
(made `virtual` this issue, but default behavior for
not-yet-implemented mnemonics is unchanged) — worth explicitly
re-checking whenever a base class's Evaluate stub is touched, since
that's a plausible place for a silent regression across untouched
instruction types. `InstructionFactory` already has switch cases wired
for `EQU`/`NEQ`/`GRT`/`LES`/`GEQ`/`LEQ`/`ADD`/`SUB`/`MUL`/`DIV` even
though those aren't yet in scope on this branch's own diff — that's
pre-existing scaffolding from an earlier issue, not something this
issue touched; don't mistake a factory `switch` case existing for a
mnemonic as evidence that issue is "in scope" for the current PR
without checking the diff.

**Regression baseline updated (issue #11, CORE-203/204 `TON`/`TOF`,
2026-08-17):** 39/39 on branch `issue-11` (27 baseline + 12 new
`TimerInstructionTests`). Pattern to expect going forward: when a
feature needs real elapsed wall-clock time (timers), the SE threads it
in as an explicit `TimeSpan elapsed` parameter on `IInstruction.Evaluate`
(now 3-arg: `tags, rungState, elapsed`) measured once by `ScanEngine`
via its own `Stopwatch`, rather than instructions tracking their own
state — keeps instruction classes stateless per SDD Coding Standards.
Good unit tests drive `.Evaluate` directly with controlled `TimeSpan`
values (exact math, no real sleeps, non-flaky); only a single
loosely-bounded real-`Thread.Sleep` integration test should exist per
feature, just to prove the engine's `Stopwatch` plumbing itself works
— don't flag more real-sleep-based tests than that as a flakiness
concern, and don't require *fewer* either (need at least one to prove
the plumbing, not just the math). Confirmed the TP-203/TP-204 RTVM row
wording matches the test assertions line-by-line, same verification
style as issue #6's `Tp100_`/`Tp101_` check.

**Regression pass confirmed a third time (issue #11, CI/CD-requested trunk
regression, 2026-08-17):** same checklist against `main`@`e45538d` (post
`issue-11` merge `2e107fa`) — 39/39, no regressions, RTVM already showed
`Verified`/`2e107fa` before the run started, handed off to Systems Engineer
per the established "regression pass still routes through the two-step
handoff even when RTVM looks current" convention. Three-for-three now
(issues #6, #7, #11) — treat this as the settled procedure, not something
to re-derive each time.

**Regression pass confirmed a fourth time (issue #8, DATA-IN-103,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`160bbc5` (post `issue-8` merge `15267cb`, plus two RTVM/memory-only
follow-up commits) — 43/43 (baseline held from CI/CD's own post-merge count,
no drop), 0/0 build warnings/errors, NFR-502 clean, `ProjectReference`
graph unchanged, RTVM already showed `Verified`/`15267cb`, `git status`
clean. Four-for-four now (issues #6, #7, #11, #8) on the "RTVM already
current → still route through the two-step handoff, don't skip it" pattern.
43 is now the current regression baseline (was 27 through issue #9, then
31 on issue-8's own branch before merge, then 39 after issue #11, now 43
after issue #8 merged — always recount from `main`, not the branch-only
number quoted mid-development).

**Regression baseline updated (issue #10, CORE-201/202, 2026-08-16):**
36/36 on branch `issue-10` (commit 98b4418) — 27 baseline + 9 new test
cases from `XicXioOteTests.cs` (6 `[TestMethod]`s, 3 of them
`[DataRow]`-parameterized ×2). Real `Xic`/`Xio`/`Ote` classes
(`SingleTagInstruction.Evaluate` now `virtual`, default still throws
`NotImplementedException` for the still-unimplemented `TON`/`TOF`/
`CTU`/`CTD`/`RES`) checked line-by-line against TP-201/TP-202 wording
in `docs/RTVM.md` (lines 129-130) — exact match, no drift. Straightforward
fill-in against the rung-state contract issue #9 established; when a
requirement is this cleanly scoped against a prior issue's interface,
reading the instruction classes directly (not just trusting the SE's
comment) took only a few minutes and is worth doing every time rather
than rubber-stamping the reported test count. (Merged into `main` on
issue-10's own trunk merge, 2026-08-17 — post-merge, `IInstruction.Evaluate`
picked up issue-11's 3-arg `elapsed` signature too, so the branch total of
36/36 became the shared post-merge regression baseline once combined with
issue-11/issue-8's later merges — see the 43/43 figure above, which is the
current number as of this file's last edit.)
