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
