---
name: pattern_nfr501_consolidation_review
description: NFR-501 late-stage consolidation pass (issue #24) — code review found clean, but CI-matrix deployment is still blocked by the workflows-permission wall
metadata:
  type: project
---

Issue #24 was the SDD-designated single late-stage consolidation issue
for NFR-501 (see `docs/SDD.md`'s "Target-platform verification
strategy," revised 2026-08-16 on issue #5). By the time it ran, every
functional RTVM item (`UI`/`DATA-IN`/`CORE`/`DATA-OUT`/`OUT`/NFR-500/
NFR-502) was already `Verified`, so this was genuinely the "full
functional feature set exists" trigger point the SDD describes — not
a premature run.

**Code review (my actual scope): clean, no fix needed.** Audited
`src/` for hardcoded path separators, `#if WINDOWS`/`RuntimeInformation`/
`OSPlatform` conditionals, platform-specific socket/file-locking
options, and RID-pinned `.csproj` files — none found. `dotnet build
-c Release` + `dotnet test -c Release --no-build` both clean on
Ubuntu (118/118 passing), which is the Linux half of TP-501's evidence.

**Confirmed (again) the [[workflows_permission_blocker]] still applies
at consolidation time, not just during Generate Code Base.** TP-501's
own steps explicitly call for `docs/ci/build-and-test.yml`/
`docs/ci/windows-verification.yml` to be "promoted to
`.github/workflows/` at consolidation time" — I attempted exactly
that push on `issue-24` and got the identical rejection first seen on
issue #5 (`refusing to allow a GitHub App to create or update
workflow ... without "workflows" permission`). Reverted the local
commit immediately (never reached origin) rather than leaving a
dangling unpushable commit on the branch. **This is a structural,
repo-wide constraint on every agent role's GitHub App token — CI/CD's
own role file has no elevated permission either** — so escalating
through the agent ladder (Solutions Architect, Systems Engineer, etc.)
would not help; every rung hits the same wall. It needs a human with
repo-admin access to either grant the App `workflows` permission or
copy the two files into `.github/workflows/` by hand, once.

**Decision made here: hand off to Test Engineer as `status:ready-for-
test` anyway, not `status:needs-human`.** My own run completed and
produced real evidence (clean review + Ubuntu build/test); only one
specific git write operation was rejected, not the run itself — that
doesn't match AGENT_LABELS.md's `status:needs-human` carve-out (budget/
API-key/OIDC failures that prevent a run from executing at all). Left
the call about whether Ubuntu-only evidence + a clean code review is
sufficient, or whether this needs to climb further before NFR-501 can
be marked `Verified`, to the Test Engineer — that's a verification-
sufficiency judgment, not mine to make per "never mark your own work
verified."

**Also re-flagged (still unresolved, second time now):**
`windows-verification.yml`'s actual build/test steps are still a
leftover C++/MSBuild toolchain (SudokuSolver.exe/dumpbin/vstest
native-desktop workload) from whichever project it was templated
from — flagged on
issue #5, still not rewritten to a `dotnet build`/`dotnet test`
equivalent for this project as of issue #24. Didn't rewrite it myself:
it's unexercisable (can't be pushed, so can't be test-run) until the
permission gap clears, and editing untestable CI YAML blind isn't
worth the risk of stacking a second latent mistake on the first.

**How to apply:** if a future issue needs this workflow file to
actually run, the permission grant (or manual copy) has to happen
first — don't spend another attempt re-proving the block exists, and
don't treat the C++-leftover content as fixed just because the paths/
SOLUTION customization note says "customized."
