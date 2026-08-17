---
name: release-versioning-tag-collision-same-day
description: What to do when the days-since-first-commit BUILD number collides with an already-pushed tag from an earlier merge the same day
metadata:
  type: project
---

Building on [[release-versioning-scheme]]: the "## Versioning and
releases" formula (`BUILD` = days since first commit) explicitly says
"Two merges on the same day get the same build number — that's fine."
Taken literally this is a **git tag name collision** — `git tag
v{MAJOR}.{MINOR}.{BUILD}` fails with `fatal: tag 'X' already exists` if
you try to create the same tag twice, even pointing at a different
commit.

**Resolution used (issue #9, 2026-08-16):** merged `issue-9` to `main`
(commit `49d5150`) on the same calendar day `v1.0.1` was already tagged
(issue #6's merge). Recomputing gave the same `v1.0.1` string. Rather
than force-moving the existing tag (a destructive rewrite of an
already-pushed ref, against the git safety protocol) or inventing an
extra disambiguating digit not in the spec, I:
1. Confirmed with `git merge-base --is-ancestor v1.0.1 <new-SHA>` that
   the existing tag's commit is a real ancestor of the new merge — i.e.
   the new work is strictly cumulative on top of what `v1.0.1` already
   marks, nothing is lost or contradicted.
2. Left the existing `v1.0.1` tag in place, did not attempt to
   recreate or move it.
3. Recorded the actual merge commit SHA explicitly in the hand-off
   comment instead, since that's the precise pointer for this specific
   merge; the tag only has day-granularity by design.

**How to apply:** Before tagging any trunk merge, check whether the
computed tag name already exists (`git tag <name>` and check for the
"already exists" error, or `git rev-parse <name>` first). If it does
and the day hasn't advanced, don't force-move it — verify ancestry with
`--is-ancestor`, skip re-tagging, and lean on the commit SHA in the
hand-off comment for precision. Only escalate if ancestry *doesn't*
hold (i.e. the existing tag points somewhere that isn't a real ancestor
of the new merge) — that would mean the day-based BUILD math itself is
producing a wrong/misleading tag, which is a genuine ambiguity worth
raising rather than guessing past.

**Reconfirmed (issue #8, 2026-08-17, merge `15267cb`):** `v1.0.2` was
already tagged earlier the same day by issue #11's merge (per
[[rtvm-merge-conflict-parallel-verification]]). Recomputed BUILD (days
since first commit `2026-08-15`) still gave `2`. Confirmed
`git merge-base --is-ancestor v1.0.2 HEAD` held, skipped re-tagging,
cited the merge SHA in the hand-off comment instead. Third occurrence
of this exact pattern — with several sibling `type:requirement`
branches (#10/#12-15/#18, etc.) all landing on the same calendar day,
expect this to keep recurring; it is not a sign of anything wrong.

**Fourth occurrence (issue #14, 2026-08-17, merge `10c9dad` +
follow-up `7e1738e`):** same result — `v1.0.2` already tagged, BUILD
recomputed still `2`, ancestry confirmed, skipped re-tag, cited SHAs
in hand-off comment. Also hit a same-day *push* rejection mid-merge
(second CI/CD run landed issue #10's RTVM-SHA follow-up between my
fetch and push) — see [[concurrent-cicd-runs-same-day]], same
fetch+merge+retest+push-again resolution, no new pattern.

**Fifth occurrence (issue #13, 2026-08-17, merge `6dfb295`, final
pushed tip `d6b67f9` after two rounds of catching up to `origin/main`):**
same result again — `v1.0.2` still ancestor-confirmed, skipped re-tag,
cited both the substantive merge SHA and the final pushed SHA in the
hand-off comment. Two separate push rejections in this one merge (see
[[concurrent-cicd-runs-same-day]]) — issue #14 and its RTVM-SHA/memory
follow-ups both landed on `origin/main` while this merge was in
progress. Confirms this is now the steady-state shape for any day with
several sibling `type:requirement` branches landing — not worth
re-deriving each time.

**Sixth occurrence (issue #16/UI-001/UI-003, 2026-08-17, merge
`fa26c47`):** same result — session started from a **shallow clone**
(`git rev-parse --is-shallow-repository` → `true`, only 7 commits
visible, first-commit date wrongly read as `2026-08-16`); ran `git
fetch --unshallow` first (per [[build-toolchain-shallow-clone]]), which
corrected the visible history to 166 commits and the true first-commit
date `2026-08-15`, giving the same BUILD=2 as prior occurrences today.
`v1.0.2` ancestor-confirmed, skipped re-tag, cited merge SHA in
hand-off. No open-issue-count release trigger either (several
`type:requirement` issues still open/on-hold). **Always check
shallowness before trusting `git log --reverse` for the BUILD-number
date — a shallow clone's "first" visible commit is not the repo's
actual first commit and will silently under- or over-count days.**
