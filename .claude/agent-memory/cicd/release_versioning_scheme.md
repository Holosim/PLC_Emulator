---
name: release-versioning-scheme
description: Which of the two versioning schemes in cicd.md's instructions is authoritative — RESOLVED 2026-08-17, no longer a live conflict
metadata:
  type: project
---

**UPDATE 2026-08-17 (issue #27, field-defect-fix merge, commit `5f4c5d6`,
tag `v1.0.339`):** the conflict described below is **resolved**. The
repo owner pushed directly to `main` (commit `1227973`, "Deploy
toolchain-detecting Windows verification") and rewrote
`.claude/agents/cicd.md` down to a single, non-conflicting "##
Versioning and releases" section. Current, live rule: **BUILD =
`git rev-list --count HEAD` after your merge lands** (a running total
of *all* commits on trunk, not date-based) — this jumped straight from
`2` (the old date-based value) to `339` on the very next tag, because
this repo's commit history includes a memory/index commit from every
agent turn, not just substantive ones. That jump is expected and
correct under the new scheme, not a bug — don't be alarmed by a big
delta between consecutive tags. Old tags `v1.0.1`/`v1.0.2` (cut under
the previous date-based scheme) are not renumbered or invalidated.
Re-check `.claude/agents/cicd.md`'s own "## Versioning and releases"
section each time before computing BUILD, in case it changes again —
don't trust this memory's account of the formula over the file itself.

**Original historical note (pre-resolution, kept for context):**
cicd.md's instructions used to contain **two different versioning
schemes** back to back (one titled "## Versioning" near the top, one
titled "## Versioning and releases" further down). They disagreed on
format details (simple incrementing BUILD counter vs. BUILD =
days-since-first-commit; 3-number vs. 2-number VERSION file; release
trigger = "no open type:requirement issues" vs. "every RTVM line item
Verified"). Step 5 of "Working an issue" named the *second* section
exactly, so that one was treated as authoritative while both existed.
Applied on issue #6 (2026-08-16): `VERSION` created as `1.0`
(first-ever release cycle), BUILD = days-since-first-commit = 1 →
`v1.0.1`. See [[build-toolchain-shallow-clone]] and
[[feedback-git-merge-fast-forward]] for mechanical issues hit while
producing that merge.
