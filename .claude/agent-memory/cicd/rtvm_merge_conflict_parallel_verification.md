---
name: rtvm-merge-conflict-parallel-verification
description: docs/RTVM.md conflicts when two sibling issue branches each verify different rows and one merges to main before the other — resolve by keeping every row's Verified status from both sides, never pick one side
metadata:
  type: project
---

Merging `issue-11` (CORE-203/204) to `main` produced a real conflict in
`docs/RTVM.md`: `main` had already merged `issue-10` (CORE-201/202,
verified independently) after `issue-11` branched off, so both branches
touched the same table rows — `issue-11`'s copy still showed
CORE-201/202 as `Approved` (stale, pre-dating issue-10's merge) while
showing CORE-203/204 `Verified`; `main`'s copy was the reverse.

**Resolution:** this is never an "either/or" pick — both sides'
`Verified` markings are independently true and both need to survive.
Kept all four rows `Verified` (union, not `--ours`/`--theirs`). Same
principle applies to any RTVM/memory conflict shaped this way: when
sibling issue branches each verify disjoint RTVM rows and merge to
trunk out of order, the conflict is almost always resolvable by
union — check the *content* of each side's row, not just resolve
mechanically.

**How to apply:** after resolving, still do the full rebuild/retest
per [[merge-required-member-break]] — a docs-only conflict resolution
doesn't guarantee the *code* underneath didn't also drift; in this
case it was clean but don't skip the check on the assumption that a
docs conflict implies a docs-only diff.

Also reconfirms [[release-versioning-scheme]]'s trigger: many sibling
`type:requirement` issues (#8, #10, #12-15, #18, on-hold #16-27) were
still open in parallel at merge time, so this stayed a plain build-
number bump (`v1.0.2`, no collision with `v1.0.1` since the day
advanced 2026-08-16 → 2026-08-17), not a release.

**Extends beyond `docs/RTVM.md`:** merging `issue-8` (2026-08-17,
commit `15267cb`) hit the identical shape of conflict in
`.claude/agent-memory/software-engineer/MEMORY.md` — two sibling
branches (`issue-8` and whatever landed CORE-203/204's timer-elapsed
memory entry) each appended a distinct bullet to the same "Architecture
patterns" list. Same resolution: union, keep both bullets, don't pick
a side. Any agent's `MEMORY.md` index file is exactly this
same shape (an append-mostly list) and should be treated the same way
on conflict — see [[release-versioning-tag-collision-same-day]] for
the paired tag-collision note from this same merge.
