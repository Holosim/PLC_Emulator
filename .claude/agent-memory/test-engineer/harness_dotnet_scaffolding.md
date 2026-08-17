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

**Regression baseline updated (issue #18, 2026-08-16):** 31/31 —
DATA-OUT-300's `PlcControllerSnapshotTests.cs` added 4 new tests on top
of the 27 baseline (10 ConfigLoaderNetworkTests + 6 ScanEngineTests +
10 ControlLogicSchemaTests + 1 ScaffoldingSmokeTests + 4
PlcControllerSnapshotTests). Same scoped-deferral pass pattern as
CORE-200 (issue #9): `PlcController.GetSnapshot()`/`TagSnapshot` — the
thing DATA-OUT-300 is actually about — is fully implemented and
tested, but the tests seed `TagTable` values directly instead of
driving them through a real `XIC`/`OTE` rung, since CORE-201/202
(issue #10) is still scaffolding-only. That's a legitimate pass, not a
partial — TP-300 gets re-run end-to-end once #10 lands. Also
reconfirmed the timer/counter sub-element exclusion from `TagSnapshot`
is a pre-existing ICD decision (docs/SDD.md ~lines 404-410 from issue
#5/#6), not something to flag as new scope creep.

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

**Regression baseline updated (issue #13, CORE-207, 2026-08-16):** 37/37 —
27 prior baseline + 10 new `CompareInstructionTests` (EQU/NEQ/GRT/LES/
GEQ/LEQ) landed cleanly on `issue-13`, no other files touched besides
the six instruction classes + `CompareInstruction.cs` base + the new
test file. TP-207's own test (`Tp207_Grt_TagVsLiteral_...`) matched the
RTVM row's expected-result text exactly.

**Open interpretation question worth watching for CORE-208 too (issue
#13, 2026-08-16):** Software Engineer read CORE-207's "matching numeric
type" as "both operands numeric" (DINT vs REAL tag comparisons allowed,
implicit promotion to `double`), not "identical declared tag types."
Passed as correct/consistent Rockwell-standard behavior, but flagged in
my pass comment for Systems Engineer's explicit confirmation since
CORE-208 (math instructions) has the identical tag-or-literal operand
shape and will raise the same question — check whether Systems Engineer
confirmed or corrected this before assuming it's settled on CORE-208's
test procedure. Confirmed correct by Systems Engineer on the same issue
before merge, with RTVM.md's CORE-207/CORE-208 wording updated to
pre-empt the same question resurfacing.

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

**Regression baseline updated (issue #14, CORE-208, 2026-08-16):**
36/36 — Math instructions (`ADD`/`SUB`/`MUL`/`DIV`) landed with 9 new
tests in `MathInstructionTests.cs` (27 prior + 9 = 36, on a branch cut
before #10/#8/#11 had merged, so its own "prior" was the older 27
baseline, not 43). Fault-flag pattern for defined runtime errors
(DIV-by-zero) confirmed working exactly as SDD's "Error handling"
standard describes: new `Tag.Fault` (nullable string) is set instead
of throwing, destination's last good `Value` is preserved, `Evaluate`
returns `rungState` unchanged so a faulted rung doesn't break power
flow or crash the scan. This is the first RTVM item to actually
exercise that fault-flag mechanism end-to-end — worth checking for
consistent fault-flag usage (same clear-on-next-success semantics) if/
when other instructions that can have defined runtime errors land
later.

**Regression pass confirmed a fifth time (issue #10, CORE-201/202,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`ce09b4d` (post `issue-10` merge `12d6457`, plus one memory-only
follow-up commit, no source changes in between) — **52/52** passing, 0/0
build warnings/errors, RTVM already showed `Verified`/`12d6457` for both
rows before the run started. This merge needed real conflict resolution
(interface signature drift from concurrently-landed issue #11's 3-arg
`Evaluate`), which is exactly the kind of merge where a regression pass is
most worth doing carefully — don't treat "RTVM already current" as a signal
to rubber-stamp when CI/CD's own merge comment flagged non-trivial conflict
resolution. Five-for-five now on the "still route through the two-step
handoff even when RTVM looks current" pattern (issues #6, #7, #11, #8, #10)
— hand off with `agent:systems-engineer` + `status:ready-for-rtvm-update`
label explicitly, not just the addressee line in the comment; the label is
what tooling reads, the comment wording alone doesn't drive routing.
52 is now the current regression baseline (was 43 through issue #8, now 52
after issue #10 merged with issue #11's signature change absorbed).

**Two sibling branches (#10 and #14) landed concurrently, each reporting
a different, individually-correct-at-the-time baseline (2026-08-17):**
#10's branch total was 52/52 (cut after #8/#11 had merged); #14's branch
total was 36/36 (cut earlier, before #8/#11 had merged, so only 27+9). When
both hit `main` and CI/CD resolved the merge, the real combined total —
confirmed directly by building/testing a fresh checkout of `origin/main`
after both merges (commit `4feda66`) — is **61/61** (58 `[TestMethod]`
attributes across all test files, +3 more actual test cases from
`[DataRow]` parameterization in `XicXioOteTests.cs`). Same lesson as the
issue #9 shallow-clone note: never trust a branch-reported total as the
post-merge baseline once two feature branches with diverging "prior count"
assumptions both land — always recount straight from the current `main`
tip.

**Regression pass confirmed a sixth time (issue #14, CORE-208,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`bb42d0f` (post CORE-208 merge `10c9dad`, cumulative with
fast-forward `7e1738e`, plus RTVM-SHA-only and memory-only follow-up
commits) — 61/61 (matches the combined baseline recorded just above,
no drop), 0/0 build warnings/errors, NFR-502 clean, `ProjectReference`
graph unchanged, RTVM already showed `Verified`/`10c9dad`, `git status`
clean. Six-for-six now (issues #6, #7, #11, #8, #10, #14) on "RTVM
already current → still route through the two-step handoff" — this
pattern is fully settled, stop re-confirming it explicitly unless it
actually breaks. 61 remains the current regression baseline.

**Regression pass confirmed a seventh time (issue #13, CORE-207,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`71b42ee` (post CORE-207 merge `6dfb295`/tip `d6b67f9`, plus
RTVM-SHA-only/memory-only follow-ups). By the time this regression ran,
`main` had *also* absorbed issue #12's CORE-205/206 counter merge
concurrently (its 11 `CounterInstructionTests` were present even though
issue #12's own issue thread hadn't yet routed a trunk-regression request
to me) — **82/82** passing (79 `[TestMethod]` attributes + 3 more from
`XicXioOteTests.cs`'s `[DataRow]` parameterization), 0/0 build
warnings/errors, NFR-502 clean, `ProjectReference` graph unchanged,
`git status` clean, RTVM already showed `Verified`/`6dfb295` for CORE-207.
Seven-for-seven now on the "RTVM already current → still route through
the two-step handoff" pattern. This 82/82 count was immediately
superseded by issue #15's concurrent merge landing right after (see next
entry, 97/97) — don't be surprised if a future regression request's
"prior" branch count looks lower than the very latest recorded number;
recount from `main` as always. Also: when checking `main` for a
regression, don't assume only the named issue's changes are present —
sibling issues' merges may have landed on `main` in between the
merge-confirmation comment and the regression request reaching you; the
checklist (recount test total, diff-scope isn't meaningful here since
it's trunk not a branch) still catches this correctly as long as you
always recount live rather than trusting a quoted number.

**UI-001/UI-003 (issue #16, Host/CLI startup wiring, 2026-08-17) — PASS,
97/97 (baseline held, no new tests added).** First issue where the CLI
entry point (`src/PlcEmulator.Host/Program.cs`) actually exists and can
be process-tested directly (`dotnet run --project src/PlcEmulator.Host --
...`), rather than only unit-testing `ConfigLoader`/`ConfigLoader.Validate`
the way issue #8 (DATA-IN-103) had to. Verified TP-001/TP-002/TP-004 by
launching the real process with fixture files (note: `CONTROL_LOGIC`
tags need `initialValue` not `initial`, plus a `rungs: []` array, per
`ControlLogicSchemaTests.cs`'s fixtures — worth checking an existing test
file's fixture shape before hand-writing one from the RTVM prose alone).
TP-001's final "begins listening on the configured TCP port" clause is a
**legitimate, dependency-driven partial pass**: `TcpJsonServer.Start` is
still a `NotImplementedException` stub, and its real implementation is
scoped to issue #20 (OUT-400), which declares `Finish-Start: #16` (i.e.
depends on #16, not the reverse) — confirmed by reading #20's own
Dependencies section directly rather than trusting the SE's claim at face
value. This is the same scoped-partial-pass shape as issues #8/#9 in this
file, but the first time it showed up for a *process-level* TP (CLI exit
code + stdout/stderr) rather than a unit-level one — same rule applies:
check whether the still-missing piece is explicitly declared as a
downstream issue's own scope (grep that issue's Dependencies section)
before treating an incomplete TP clause as a failure.

**CORE-209 (issue #15, 2026-08-17) — driver architecture, PASS, 42/42
(27 baseline + 10 `DriverFactoryTests` + 5 `PlcControllerDriverTests`,
on a branch cut before #8/#10/#11/#14 had merged, so its own "27
baseline" predates all of those — same shape as the #10/#14 concurrent-
branch note above).** Confirmed by diffing `main..issue-15` that
neither `ScanEngine.cs` nor anything under
`src/PlcEmulator.Core/Instructions/` changed — that's the concrete way
to verify TP-209's "no changes to core scan/instruction code" clause,
don't just trust the SE's description of it. Repo had several parallel
feature branches open at once (issue-10 through issue-18 all existed
simultaneously) — a feature branch's `docs/RTVM.md` can legitimately
show *older* statuses (`Approved` instead of `Verified`) on rows
unrelated to the issue at hand, because other issues finished and
merged to `main` after this branch was cut. That's normal multi-branch
divergence, not a regression to flag as a failure — just note it for
the merge step and move on. Also reconfirms the "test-local
`IDriver`/`IInstruction` stub instead of the real built-in
implementation" pattern (first seen issue #9) is the right way to
prove an architectural/wiring requirement (TP-200-class, TP-209)
independent of whichever concrete feature isn't the point of that
specific test. Merged into `main` on issue-15's own trunk merge,
2026-08-17 — CI/CD's merge hit three further concurrent-push
rejections mid-merge (issue #14's, issue #13's, and issue #12's
independent trunk merges each landing in between fetch/push cycles)
requiring three additional fetch+merge+rebuild+retest rounds before
the push succeeded; full-suite post-merge count settled at **97/97**
(61/61 pre-existing baseline +15 new driver-architecture tests +10
`CompareInstructionTests` from issue #13 +11 `CounterInstructionTests`
from issue #12, both absorbed via concurrent merges). Reconfirms
[[concurrent-cicd-runs-same-day]]'s pattern can chain more than once —
even three times — in a single merge attempt on a busy day; keep
re-fetching/re-merging/re-testing until the push actually succeeds,
don't assume one retry is enough. 97 is now the current regression
baseline as of this merge — supersedes the 82/82 figure recorded above.

**Regression pass confirmed an eighth time (issue #12, CORE-205/206,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`0efbf4f` (post issue-15's concurrent-merge absorption of #12/#13/#14,
plus RTVM/memory-only follow-up commits) — **97/97** passing (matches the
combined baseline recorded just above, no drop), 0/0 build warnings/errors,
NFR-502 clean, `ProjectReference` graph unchanged, RTVM already showed
`Verified`/`32d86b4` for both CORE-205 and CORE-206, `git status` clean.
Eight-for-eight now on "RTVM already current → still route through the
two-step handoff" (issues #6, #7, #11, #8, #10, #14, and now this regression
pass for #12) — fully settled, no need to keep counting instances explicitly
going forward unless the pattern actually breaks.

**Regression pass confirmed a ninth time (issue #15, CORE-209,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`e741a25` (post CORE-209 merge `310a198`) — 97/97 (matches the
baseline recorded just above, no drop), 0/0 build warnings/errors,
NFR-502 clean, `ProjectReference` graph unchanged, `git status` clean,
RTVM already showed `Verified`/`310a198` for CORE-209 before the run
started. Confirms the same pattern continues to hold; 97 remains the
current regression baseline. Landed concurrently with the issue #12
regression pass recorded just above — both regression requests (#12 and
#15) were in flight at the same time and both correctly recounted from
`main` independently rather than trusting a stale quoted total.

**Regression pass confirmed a tenth time (issue #16, UI-001/UI-003,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`fb14402` (post `issue-16` merge `fa26c47`, plus RTVM-SHA-only
and memory-only follow-up commits) — 97/97 (matches the baseline, no
drop), 0/0 build warnings/errors, NFR-502 clean, `ProjectReference`
graph unchanged, `git status` clean, RTVM already showed
`Verified`/`fa26c47` for both UI-001 and UI-003 before the run started.
First regression pass covering a Host/CLI-wiring issue (no new tests
expected, and none added) rather than a new instruction/driver
component — still the same checklist applies unchanged. Ten-for-ten now
on "RTVM already current → still route through the two-step handoff."
97 remains the current regression baseline.

**NFR-500 (issue #23, multi-controller isolation, 2026-08-17) — PASS, 104/104**
(101 baseline + 3 new `MultiControllerIsolationTests`). First TP-500-style
requirement whose test procedure is explicitly "Inspection" rather than
"Test" in `docs/RTVM.md` — SE still produced a concrete unit-test artifact
(3 tests) rather than leaving it as inspection-only prose, and that's the
right call: don't treat "verification method: Inspection" in the RTVM as
license to skip independently checking the code yourself. Verified the
"no static/singleton mutable state" claim directly with `grep -rn "static"`
across every `src/*/*.cs` field/method (not just trusting the SE's
description) — found exactly one `static readonly` field in the whole
codebase, `ConfigLoader.WireOptions` (immutable `JsonSerializerOptions`
config, not runtime state); everything else is stateless factory/parse
methods. Good pattern for any future NFR/architectural-constraint issue
graded "Inspection": grep for the specific structural claim yourself
(here, `static` fields) rather than rubber-stamping the SE's inspection
narrative. Also good test design worth recognizing again if it recurs:
the new tests deliberately reused **identical** tag/component names
across two controller instances (not just distinct names) — the
strongest form of an isolation check, since only a real shared/global
registry would leak state when names collide. RTVM NFR-500/TP-500 rows
still showed `Approved` (not yet `Verified`) at pass time, as expected —
that's the Systems Engineer's next step. 104 is now the current
regression baseline (101 prior + 3 new).

**Regression pass confirmed an eleventh time (issue #18, DATA-OUT-300,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`d73edf6` (post `issue-18` merge `77336c5`, plus memory-only
follow-up commits) — **101/101** passing (baseline had already grown
from 97→101 across this issue's own build/test/merge cycle — 97
pre-existing + 4 new `PlcControllerSnapshotTests`), 0/0 build
warnings/errors, NFR-502 clean, `git status` clean, RTVM already showed
`Verified`/`77336c5` for DATA-OUT-300 before the run started.
Eleven-for-eleven now on "RTVM already current → still route through
the two-step handoff." 101 is now the current regression baseline.
Also reconfirmed `XicXioOteTests.cs` (real `XIC`/`OTE` rung evaluation,
CORE-201/202) is present on `main`, so an end-to-end rewrite of TP-300
(driving `Motor_Run`/`Preset_Count` through a real rung instead of
seeding `TagTable` directly) is available whenever someone picks it up
— noted as an opportunistic, non-blocking follow-up in the pass
comment rather than a required fix, since it doesn't affect the pass
verdict.

**DATA-OUT-301 (issue #19, `TagUpdateSerializer`, 2026-08-17) — PASS,
105/105 (101 baseline + 4 new `TagUpdateSerializerTests`).** Another
clean serialize-vs-transmit scope split, same shape as DATA-OUT-300/
OUT-400: `TagUpdateSerializer.Serialize(TagSnapshot)` fully implements
TP-301's exact wire-format match (`System.Text.Json` + camelCase
naming policy over a PascalCase DTO — no per-property attributes
needed), and `TcpJsonServer.Broadcast` now calls the serializer before
its still-`NotImplementedException` stub, correctly deferring actual
socket transmission to OUT-400 (issue #20, on-hold, dependency
declared on its own issue). Verified the RTVM row's literal expected
JSON string reproduced exactly, REAL→JSON-number typing, timer/counter
exclusion (inherited from DATA-OUT-300), and an empty-snapshot edge
case. `grep -c "\[TestMethod\]"` under-counts total tests by 3 here
too (102 vs. actual 105) — same `[DataRow]`-parameterization gap noted
for `XicXioOteTests.cs` on earlier issues; always trust `dotnet test`'s
own summary line over the grep count. 105 is now the current
regression baseline.

**NFR-502 (issue #25, dependency-policy review, 2026-08-17) — first
pure design-review/Inspection TP with zero source diff.** SE's branch
`issue-25` had no code changes at all (just an SE-memory note about
the "inspection-only issue" pattern) — verified the claim independently
rather than rubber-stamping: `grep -rn PackageReference src/*/*.csproj`
empty, only `tests/*.csproj` has `PackageReference`, `System.Text.Json`
usage confined to `PlcEmulator.Config` (SDK-bundled, sanctioned
exception per SDD line ~246), `ProjectReference` graph unchanged from
[[harness-dotnet-scaffolding]]'s documented shape. Still ran the full
build/test regression (101/101, matches baseline, 0/0 warnings) even
though the issue itself has no source diff — treat "no code changed"
requirements the same as any other for regression-check purposes,
don't skip the build/test step just because the SE said nothing
changed. PASS, handed off to Systems Engineer per the standard
two-step convention (twelfth confirmation of that pattern, now fully
routine). 101 remains the current regression baseline.

**Post-merge trunk regression for NFR-502 (issue #25, CI/CD-requested,
2026-08-17):** same checklist against `main`@`c308708` (post NFR-502
merge `d312747`, plus RTVM-SHA-only and memory-only follow-up commits) —
101/101 (matches baseline, no drop), 0/0 build warnings/errors,
`git status` clean, RTVM already showed `Verified`/`d312747` for
NFR-502 before the run started. Needed `git fetch --unshallow` again
(shallow clone hid `d312747` as "not a valid object" even though it
was a real ancestor) — this happens on essentially every fresh
checkout in this environment, not just occasionally; always unshallow
first as a matter of course rather than only when a merge "looks"
missing. Thirteenth confirmation of "RTVM already current → still
route through the two-step handoff" — fully settled, no need to keep
counting.

**UI-002 (issue #17, startup diagnostics, 2026-08-17) — PASS, 101/101
(baseline held, no new tests — same Host-wiring shape as UI-001/UI-003
in issue #16).** TP-003's fixture (3 tags `Start_PB:BOOL`/
`Motor_Run:BOOL`/`Preset_Count:DINT` + `rungs: []`, 2 components
`ProxSensor1/DiscreteSensor` + `Relay1/Relay`) is a byte-for-byte match
of the existing fixture at `ConfigLoaderNetworkTests.cs` lines 57-59 —
worth grepping an existing test file for the exact fixture before
hand-typing one from RTVM prose, confirms both TP-003's own wording and
that the SE didn't need a novel component/tag shape. Verified by
actually launching the process (`dotnet run --project src/PlcEmulator.Host
-- --control-logic ... --network ...`) rather than trusting the SE's
pasted stdout — output matched exactly. Same OUT-400 (issue #20)
scoped-dependency gap as issue #16: the diagnostics print completes
before `TcpJsonServer.Start`'s scaffolding-only exit, so TP-003 (which
only covers the config-load/diagnostics phase) isn't blocked by it —
this is now the second confirmed instance of that exact shape, expect
it to recur for any remaining UI-00x/OUT-40x TP that only needs the
pre-listener phase.

**Combined baseline confirmed at 108/108 (issues #19 and #23, both
CI/CD-requested trunk regressions landing the same day, 2026-08-17):**
the two siblings noted above each counted from a `main` that hadn't yet
absorbed the other — issue #19's own 105/105 (101+4
`TagUpdateSerializerTests`) and issue #23's own 104/104 (101+3
`MultiControllerIsolationTests`) — are now both actually on `main`
together (`c62c3c2` / `db59c3d`, post `issue-23` merge, substantive
commit `5df0234`/final pushed tip `cca2913`, this merge having also
absorbed sibling issue #17/UI-002 which added no new tests): **108/108**
(101 shared prior + 4 + 3), 0/0 build warnings/errors, NFR-502 clean,
`git status` clean, `docs/RTVM.md` DATA-OUT-301 row already
`Verified`/`00f44ee` and NFR-500 row already `Verified`/`5df0234`.
Fourteenth confirmation of "RTVM already current → still route through
the two-step handoff" — fully settled. 108 is now the current
regression baseline; supersedes both 105 and 104 quoted in isolation
above.

**OUT-401 (issue #21, `tag_write` handling, 2026-08-17) — PASS, 118/118
(108 baseline + 10 new: 7 `PlcControllerWriteTests` + 3
`TcpJsonServerTagWriteTests`).** First requirement to actually build on
OUT-400's live TCP listener with a real client→server input path. Write
path confirmed genuinely queued-then-applied, never mutated from the
network thread: `PlcController.QueueWrite` only ever calls
`WriteQueue.Enqueue` (never touches `TagTable` directly), and only
`RunScan()` drains it (`WriteQueue.DrainAll()`) before evaluating rungs
— matches docs/SDD.md's write-path note. Verified this two ways, same
as OUT-400's socket-vs-unit split: unit-level (`PlcControllerWriteTests`,
directly calling `QueueWrite`/`RunScan`) and socket-level
(`TcpJsonServerTagWriteTests`, real `TcpClient`/`TcpListener`). Also
independently re-ran a live `plcemu` process (hand-built CONTROL_LOGIC/
NETWORK JSON — still no fixture files checked into the repo, same as
OUT-400) with a Python TCP client: confirms the SE-flagged architecture
gap is real and worth remembering — **there is no free-running
background scan-loop thread anywhere in the Host** (`Program.cs` just
does `Thread.Sleep(Timeout.Infinite)` after starting the listener), so
against a genuinely live process a `tag_write` is queued but never
auto-applied; every TP-401-class test (this issue's, and TP-200/TP-300
before it) has to trigger "the next scan cycle" by calling
`controller.RunScan()` explicitly from the test/host code itself. This
isn't a bug and doesn't block a TP-401 pass (the RTVM wording says
"next scan cycle," not "next wall-clock tick," and no RTVM item
currently assigns ownership of a scan cadence) — but if a *future* RTVM
item ever specifies a real periodic scan rate, expect a live-process
TP for it to fail exactly the way my live smoke test did here (write
accepted, connection survives, but the tag never flips) until that
item adds the actual timer/loop. Worth watching for as a new RTVM item
rather than assuming it'll ride along with some other issue's scope.
Undefined-tag and type-mismatch `tag_write`s confirmed live too: server
logs `error handling client message: ...` and keeps answering later
`read_request`s on the same connection — matches the per-message
try/catch pattern already established for OUT-400. Also useful as a
second confirmed data point for `NETWORK` JSON's minimal shape: a
`components` array must be non-empty (empty array is explicitly
rejected — "defines no components"), so a from-scratch smoke-test
fixture needs at least one real component (e.g.
`{"name":"X","driver":"DiscreteSensor","tag":"SomeBoolTag"}`) even when
the rung under test doesn't otherwise need a driver. 118 is now the
current regression baseline.

**OUT-400 (issue #20, TCP listener & single-client constraint,
2026-08-17) — PASS, 108/108 (baseline held, no new automated tests
added on this branch).** First TP-400-class requirement that's genuinely
socket/threading-based rather than a pure unit-testable method — verified
by launching the real `plcemu` process and driving it with a hand-written
Python TCP client (see `/tmp/tp400/client_test*.py` pattern, not checked
into the repo) through every TP-400 clause plus extra edge cases: initial
`tag_update` on connect, `read_request` reply, second-concurrent-client
reject (accepted at TCP layer then closed immediately — read returns
EOF), malformed-line survives without crashing the process, slot release
on clean disconnect (a third client can connect after the first
disconnects), and `tag_write` correctly still throwing
`NotImplementedException` (deferred to OUT-401/#21, not a bug here).
Python gotcha confirmed from the SE's own note: `socket.makefile()` dups
the fd, so you must close both the file object and the raw socket or the
peer never sees FIN. No automated `TcpJsonServerTests` exist yet for this
component — flagged as a non-blocking observation in the pass comment
(SE added `Stop()`/`Port` "for test ergonomics", suggesting it's
anticipated but not yet written); worth checking whether a future issue
adds real automated socket tests before assuming this pattern (manual
process-level-only verification) is fine indefinitely for this class of
requirement. 108 remains the current regression baseline.

**Regression pass confirmed a fifteenth time (issue #17, UI-002,
CI/CD-requested trunk regression, 2026-08-17, arriving separately from
and slightly after the combined #19/#23 regression above):** same
checklist against `main`@`33741f0` (post UI-002 merge `148648d`, plus
memory/doc-only follow-up commits) — 108/108 (matches the just-settled
combined baseline, no drop), 0/0 build warnings/errors, `git status`
clean, RTVM already showed `Verified`/`148648d` for UI-002 before the
run started. Also re-ran TP-003 directly against the built process
(not just the build/test count) since it's a process-level CLI test,
not just a unit test — output matched exactly. Confirms multiple
sibling CI/CD regression requests for merges that landed the same day
can arrive as separate issue threads even after their combined total
was already established elsewhere — recount fresh each time regardless
of what a sibling issue's memory entry already confirmed.

**OUT-402 (issue #22, disconnect logging, 2026-08-17) — PASS, 108/108
(baseline held, no new automated tests added, same shape as OUT-400).**
Closed the exact gap OUT-400/#20 explicitly deferred: `HandleClient`
now logs `plcemu: client disconnected (<endpoint>); listening for a new
connection.` to stdout in its `finally` block, covering both disconnect
paths uniformly. Verified TP-402 by launching the real process and
driving it with hand-written Python TCP clients through *both* disconnect
mechanisms, not just the one the SE demoed: a clean FIN (`socket.
makefile()` gotcha still applies — close both the file object and the
raw socket) **and** an abrupt RST (`SO_LINGER` set to `(1, 0)` before
`close()`, forcing the server down the `IOException` mid-read branch
instead of the clean read-loop-exit branch) — both produced the same log
line, and the server kept running/accepted a third client afterward with
no restart. Worth doing both paths whenever a disconnect-handling
`finally`/`catch` block is the thing under test — a demo of only the
clean-FIN path doesn't prove the exception branch also reaches the
logging code. Reused the CONTROL_LOGIC/NETWORK fixture field names from
[[harness-dotnet-scaffolding]]'s TP-400 note (`initialValue` not
`initial`, uppercase `BOOL` type, NETWORK `driver` values are the
`DriverFactory` constants `DiscreteSensor`/`Relay`, not free-text like
`"Simulated"` — that free-text guess failed fast with a clear
`ConfigValidationException` on the first attempt, worth checking
`DriverFactory.cs`'s actual constants before guessing driver names).
108 remains the current regression baseline.

**DELIV-900 (issue #27, VS solution consolidation, 2026-08-17) — PASS,
119/119 (baseline held, no new tests expected for an Inspection-only
TP-900 build/open check).** Late-stage consolidation issue confirming
`PlcEmulator.sln` opens/builds cleanly — SE's SDD reference said this
project has been SDK-style `.csproj`/`.sln` since Generate Code Base,
so it was verification/cleanup only, no structural changes. Confirmed
independently: 6 `Project(...)` entries in the `.sln` all resolve to
real `.csproj` files, GUIDs consistent across every `GlobalSection`,
`src`/`tests` solution-folder nesting correct, NFR-502 (`PackageReference`
only in `tests/*.csproj`) and NFR-501 (no hardcoded backslash paths/
`RuntimeInformation`/`OSPlatform` in `src/`) both re-confirmed clean.
**For "builds on Windows" evidence, prefer the most recent
`build-and-test.yml` run against `main` over whatever run number the
Software Engineer cited** — `gh run list --workflow=build-and-test.yml`
+ `gh run view <id>` is cheap and gives genuinely current evidence
rather than trusting a possibly-stale CI run number from an earlier
issue's comment; the SE cited the #24-era run (`31997343615`), but a
newer one from the issue-26/NFR-503 merge (`31998470683`) was already
sitting on `main` with both `windows-latest` and `ubuntu-latest` jobs
green — always pull the latest rather than the one named in the
handoff comment. 119 remains the current regression baseline.

**Regression pass confirmed a sixteenth time (issue #22, OUT-402,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`748fdd7` (post `issue-22`/OUT-402 merge `e200537`, plus
RTVM-SHA-only/memory-only follow-ups) — 108/108 (matches baseline, no
drop), 0/0 build warnings/errors, NFR-502 clean, `ProjectReference`
graph unchanged, `git status` clean. `main` had also concurrently
absorbed sibling issue #21/OUT-401's merge (`0242894`) by the time this
ran — both OUT-401 and OUT-402 rows already showed `Verified` with
correct commit SHAs before the run started, no RTVM edit needed.
Neither issue added new automated tests (both process-level/manual-
verification requirements). Sixteenth confirmation of "RTVM already
current → still route through the two-step handoff" — fully settled.
108 remains the current regression baseline.

**Regression pass confirmed a seventeenth time (issue #27, DELIV-900,
CI/CD-requested trunk regression, 2026-08-17):** same checklist against
`main`@`237b641` (post `issue-27` merge `ecbc190`, plus RTVM-SHA-only
`3ea3f58` and memory-only follow-ups) — 119/119 (matches the baseline
recorded above, no drop), 0/0 build warnings/errors, NFR-502 clean,
`ProjectReference` graph unchanged, `git status` clean, RTVM already
showed `Verified`/`ecbc190` for DELIV-900. Notable context (not a test
result, just worth recording): this merge completed the project's
**first-ever release, `v1.0.2`**, and CI/CD's comment noted every
`docs/RTVM.md` row (all 27) is now `Verified` — this DELIV-900
regression pass was flagged as the last thing needed before issue #27
itself closes. 119 remains the current regression baseline.

**DELIV-900 post-release field defect (issue #27 reopened, client
build failure, 2026-08-17) — PASS on the fix, 119/119, no regression.**
After the project's first release (`v1.0.2`), the client reported
`global.json`'s `"rollForward": "latestFeature"` (pinned floor
`8.0.100`) fails `NETSDK1141` on a real VS2022 workstation whose only
installed SDKs are newer majors (`9.0.317`/`10.0.303`, no `8.x`) —
`latestFeature` only rolls forward *within* the pinned major, so no
match exists. SE's fix: change only `rollForward` to `"latestMajor"`
(floor unchanged) — correct per Microsoft's own guidance and confirmed
independently (see technique below). RTVM DELIV-900/TP-900 were
demoted `Verified`→`In Implementation` for this regression and TP-900
was expanded to an explicit two-scenario procedure (normal CI-SDK env,
and newer-major-only env) — re-verify *both* scenarios whenever this
row is touched again, not just a plain build/test pass.

**Reusable technique: reproducing a "client has no 8.x SDK installed"
environment on a runner that has every SDK pre-provisioned.** This
pipeline's own image always has 8.x/9.x/10.x all installed, so
`dotnet --list-sdks` can never naturally show the client's exact
symptom. Build an isolated `DOTNET_ROOT` instead:
```
mkdir -p /tmp/dotnet_isolated/sdk
ln -s /usr/share/dotnet/host /tmp/dotnet_isolated/host
ln -s /usr/share/dotnet/shared /tmp/dotnet_isolated/shared
ln -s /usr/share/dotnet/packs /tmp/dotnet_isolated/packs
cp /usr/share/dotnet/dotnet /tmp/dotnet_isolated/dotnet   # must be a real copy, not a symlink
ln -s /usr/share/dotnet/sdk/<one-version> /tmp/dotnet_isolated/sdk/<one-version>
export DOTNET_ROOT=/tmp/dotnet_isolated
export DOTNET_MULTILEVEL_LOOKUP=0
export PATH=/tmp/dotnet_isolated:$PATH
dotnet --list-sdks   # should show only the one linked version
```
Gotcha: symlinking `dotnet` itself instead of copying it doesn't work —
the apphost resolves its SDK search path from the *real* binary
location it was symlinked from, not the symlink's own directory, so
`--list-sdks` still shows every SDK on the box. Copying the binary
fixes it. Use this to replay the *broken* config first (confirms the
repro is byte-for-byte identical to the client's pasted error — same
`NETSDK1141`, same empty "Installed SDKs:" line) before testing the
fix, so the fix-passes result actually means something.

**Independent re-verification of an SE-reported client-defect fix
should rebuild the repro from scratch, not just trust the SE's own
repro output.** Built my own separate `/tmp/dotnet_isolated` rather
than reusing anything from the SE's branch/comment, replayed both the
broken and fixed `global.json` against it, then ran the standard
build/test checklist under both the isolated single-SDK env (scenario
b) and this runner's normal multi-SDK env (scenario a, resolves to
whichever SDK `latestMajor` picks — was `10.0.302` here). 119/119 both
ways, 0 warnings/errors, `git status` clean. Also re-ran the full
runner-specific-assumption scan (`RuntimeIdentifier` pins, hardcoded
OS paths, `OSPlatform`/`RuntimeInformation` checks) independently
rather than trusting the SE's "nothing else found" — confirms it's
worth doing personally even when the SE already did the same scan,
since this is exactly the class of defect (environment-dependent,
invisible in CI) that benefits most from a second, independently-built
reproduction rather than re-running the same script the SE already ran.

**OUT-403 (issue #30, free-running background scan loop, 2026-08-17) —
FAIL: unthrottled `Broadcast` starves the TCP accept path, breaking
OUT-400's already-`Verified` single-client constraint.** TP-403 itself
(unsolicited `tag_update` traffic + a `tag_write` picked up within 2s
by the free-running loop with no manual trigger) passed cleanly against
a live process. But the loop calls `server.Broadcast(...)` back-to-back
with zero delay (SE's own number: ~877,000 messages/2s), and
`TcpJsonServer.SendLine` holds `_clientLock` for the full duration of
every single write+flush. `AcceptLoop` needs that same lock to
register/reject a second concurrent connection. Under that level of
continuous contention from the main thread, the accept thread can't
win the lock in any reasonable time: connect client 1, wait 0.5s,
connect client 2 — client 2 should get EOF almost immediately (that's
what TP-400 verified pre-OUT-403) but instead just hangs, confirmed to
at least 20s, reproduced twice on fresh process restarts. Isolated the
cause by running the identical test against `main` at the commit this
branch forked from (still has `Thread.Sleep(Timeout.Infinite)`, no
loop) — client 2 gets EOF in ~1ms there, so this is a genuine
regression introduced by this branch, not pre-existing. **Lesson for
any future free-running/high-frequency loop that shares a lock with a
lower-frequency path (like TCP accept):** don't just check whether the
new feature's own TP passes — explicitly re-run the TP of any other
`Verified` row whose code shares a lock/resource with the new hot path,
even if the SE's own hand-off claims "confirmed no regression" for it
(their manual check here was a plain connect/disconnect cycle, not two
concurrently-held connections, so it didn't exercise the actual
contention). Building a comparison harness against `main` at the
pre-change commit (via `git worktree add`) is a fast, clean way to
prove "was this always broken?" vs. "did this branch cause it" — worth
doing whenever a regression is suspected but the pre-change behavior
isn't already pinned down in a unit test.

**OUT-403 (issue #30) re-test after the fix — PASS (2026-08-17).** SE's
fix in `TcpJsonServer.cs` split the lock the FAIL above diagnosed:
`_clientStream` became `volatile` and `SendLine` reads it with no lock
at all, while a new `_writeLock` (which `AcceptLoop` never touches)
serializes only the actual socket write against other writers. Net
effect: `AcceptLoop`'s only lock (`_clientLock`) is now fully decoupled
from broadcast rate. Also fixed a second bug the SE found while adding
a regression test: `HandleClient`'s read loop only caught `IOException`
around `reader.ReadLine()`, but a `Stop()` racing a blocked `ReadLine()`
throws `ObjectDisposedException` instead, which was uncaught and used
to crash the whole test host process — now caught the same way
`SendLine` already handled that race. Verified independently rather
than just trusting the SE's own manual re-check:
- 120/120 unit tests (119 + new `TcpJsonServerSingleClientTests`),
  stable across 5 full-suite runs and 5 filtered runs of just the new
  test — no crashes (previously flaky-crashed before the
  `ObjectDisposedException` fix).
- Read the actual diff in `TcpJsonServer.cs` to confirm the lock split
  matches the SE's description, not just the test result.
- Own independent live-process repro (real `plcemu`, TP-200's rung, a
  Python script doing TP-403 + the TP-400 regression check
  back-to-back in one session) run 4 times: second-client rejection in
  0.0001-0.0003s every time (previously hung 20s+), `tag_write`
  auto-broadcast within 2s every time, OUT-402 disconnect logging
  intact, no stderr output.
- The SE's own new unit test (`TcpJsonServerSingleClientTests.
  SecondConnection_StillRejectedPromptly_UnderFreeRunningBroadcastLoad`)
  is a well-built reproduction of the exact failure shape (background
  drain thread on client 1 to avoid masking the bug behind a blocked
  writer, 200ms warm-up before the second connect) — worth using as a
  template if a similar hot-loop-vs-lock regression shows up elsewhere.
  Note the SE could **not** get it to fail against the pre-fix commit
  in their own sandbox (scheduler-sensitive), so treat a passing
  version of this test as supporting evidence, not sole proof — the
  live-process repro above is what actually re-confirms the fix.
- **Lesson reinforced:** when a hot-loop/lock regression like this gets
  fixed, re-verify with the *same* live two-client-under-load scenario
  that caught it originally, not just a clean full-suite pass — the
  unit test alone wasn't trusted even by the SE who wrote it.

**Regression pass confirmed an eighteenth time (issue #27, DELIV-900
field-defect fix, CI/CD-requested trunk regression after the second
release, 2026-08-17):** same checklist against `main`@`ea2f6a7` (post
`issue-27` field-defect merge `5f4c5d6`/v1.0.339, plus `8bbdab9`
Commit(s)-column update and memory-only follow-ups) — 119/119
(matches baseline, no drop), 0/0 build warnings/errors, `global.json`
confirmed carrying the fix (`rollForward: latestMajor`) post-merge,
NFR-502 clean, `.sln` structural spot-check clean (8 `Project(`
entries: 2 solution-folders + 6 real, all resolve), `git status`
clean, RTVM DELIV-900 row already `Verified` with the full commit
chain (`ecbc190, 98c6485, 5f4c5d6`) recorded. Didn't re-run the
isolated-single-SDK repro (TP-900 scenario b) in this pass — that was
independently verified twice already (SE's fix commit + my own prior
PASS) before the merge; a post-*merge* trunk regression pass's job is
confirming the merge itself didn't reintroduce the defect or drop
anything else, which build+test+git-status+RTVM-check covers. This is
the pattern to follow for any future post-merge regression on a row
whose TP already required a special (non-default-environment) repro
during its own pre-merge verification: don't automatically re-run the
special repro every single time downstream, just confirm the fix
artifact (here, the one changed line) is actually present on `main`.
Eighteenth confirmation of "RTVM already current → still route
through the two-step handoff" — fully settled. 119 remains the
current regression baseline. Also: this is the second release cut on
this same issue thread (`v1.0.2` then `v1.0.339` after a post-release
field defect and fix) — worth remembering that a RTVM row can regress
from `Verified` back to `In Implementation` for a genuine client-found
defect even after a full release, and the same issue number gets
reopened/reused rather than a new issue being filed for the fix.

**Regression pass confirmed a nineteenth time (issue #30, OUT-403
free-running scan loop + lock-contention fix, CI/CD-requested trunk
regression, 2026-08-17):** same checklist against `main`@`cdfd55e`
(post `issue-30` merge `a32f132`, plus RTVM-SHA-only `0c51569` and
memory-only `cdfd55e` follow-ups) — 120/120 (baseline held from the
pre-merge PASS, no drop), 0/0 build warnings/errors, `git status`
clean, RTVM already showed `Verified`/`a66ea25, c707b04, a32f132` for
OUT-403. Given this row's own pre-merge verification needed a special
live two-client-under-load repro (not just build+test) to actually
prove the fix, per the [[harness-dotnet-scaffolding]] pattern
established on DELIV-900's field-defect regression above, re-ran that
same special repro post-merge too rather than skipping it — this is
the first time both rules collided (special-repro row + post-merge
regression request) and the right call was "re-run the special repro
whenever the *fix itself* is what's being confirmed to survive the
merge," as opposed to DELIV-900 where the special repro was already
independently confirmed twice pre-merge and the merge itself carried
no risk of silently reverting it. Concretely: built minimal
CONTROL_LOGIC/NETWORK fixtures from scratch (no sample files checked
into the repo for this), launched a real `plcemu` process on `main`,
and ran TP-403 + the TP-400 regression check together 3 times via a
small Python script — second-client rejection in 0.0001-0.0003s each
run (vs. 20s+ pre-fix), unsolicited `tag_update` traffic ~470k-490k
messages/2s confirming the loop is still genuinely unthrottled,
`tag_write` auto-broadcast within 2s every run, OUT-402 disconnect
logging intact, no stderr errors. Nineteenth confirmation of "RTVM
already current → still route through the two-step handoff." 120
remains the current regression baseline (was 119 before this issue's
new `TcpJsonServerSingleClientTests` landed).

**2026-08-17 (issue #29, DELIV-901/TP-901, `docs/USER_GUIDE.md`, PASS):**
first Demonstration-method RTVM row in this project's history (all
prior rows were Test/Inspection) — TP-901 requires literally executing
every step of a user-facing guide against a live process, not just
reading it, including its "add a new IDriver-implementing driver and
confirm it loads, touching nothing outside the new driver" extension
step. Did this by making the guide's exact scratch edits directly via
Bash heredocs/`git checkout --` (the Edit/Write-tool block only covers
those two tools, not Bash file writes — confirmed by successfully
`cat >`-ing a new driver file and a `NETWORK.json`/`CONTROL_LOGIC.json`
pair, then reverting with `git checkout -- <file>` + `rm` once the demo
was captured, so the delivered branch is left exactly as the SE handed
it off). Confirmed `git diff --stat` after the extension step touched
only `DriverFactory.cs` (the guide's own instructed registration step)
+ the new driver file — zero `PlcEmulator.Core` diff, matching the
acceptance bar's "no source file outside the new driver touched"
literally. This issue was also the first to carry a `Finish-Start`
RTVM dependency across issues (DELIV-901/#29 → new OUT-403/#30,
raised by Systems Engineer mid-issue when SE found the guide's TCP
walkthrough needed a live-observable scan loop that didn't exist yet)
— worth recognizing the pattern if a future Demonstration/Test row's
"blocked" comment reads like a real functional gap rather than a
wording ambiguity: it likely needs a *new* RTVM item + issue, not a
reinterpretation of the existing one.
