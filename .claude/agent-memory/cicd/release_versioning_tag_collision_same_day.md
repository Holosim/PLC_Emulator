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
