---
name: release-versioning-scheme
description: Which of the two versioning schemes in cicd.md's instructions is authoritative, and how it was applied for the project's first tag (v1.0.1)
metadata:
  type: project
---

cicd.md's own instructions contain **two different versioning
schemes** back to back (one titled "## Versioning" near the top, one
titled "## Versioning and releases" further down, under
"Responsibilities"). They disagree on format details (simple
incrementing BUILD counter vs. BUILD = days-since-first-commit;
3-number VERSION file vs. 2-number MAJOR.MINOR-only VERSION file;
release trigger = "no open type:requirement issues" vs. "every RTVM
line item Verified").

**Resolution used:** step 5 of "Working an issue" explicitly says
"check whether it completes a release per 'Versioning and releases'
above" — that phrase names the *second* section exactly, so that's
the one actually wired into the workflow steps. Treat "## Versioning
and releases" (BUILD = days since first commit via `git log --reverse
--format=%cd --date=short | head -1`; VERSION file holds only
`MAJOR.MINOR`; tag every trunk merge with `v{MAJOR}.{MINOR}.{BUILD}`;
cut a GitHub Release only when *every* `docs/RTVM.md` line item is
Verified) as authoritative if the two ever conflict again.

**Applied on issue #6 (2026-08-16):** no `VERSION` file existed yet →
created it as `1.0` (first-ever release cycle, per the "create it as
1.0" instruction). First commit date `2026-08-15`, merge date
`2026-08-16` → BUILD = 1 → tagged `v1.0.1`, pushed. Most `docs/RTVM.md`
line items were still `Approved`/`In Test` (only DATA-IN-100/101 `In
Test`, nothing `Verified` yet), so no GitHub Release was cut — tag
only. See [[build-toolchain-shallow-clone]] and
[[feedback-git-merge-fast-forward]] for the mechanical issues hit
while producing this merge.
