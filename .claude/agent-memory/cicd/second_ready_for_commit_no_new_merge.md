---
name: second-ready-for-commit-no-new-merge
description: A single issue can hand off to CI/CD twice — once to actually merge the branch, and again after the post-merge regression loop closes — the second time there's nothing new to merge
metadata:
  type: project
---

Issue #18 (DATA-OUT-300) looped: SE→TE→SE→**CI/CD** (merge to trunk,
`77336c5`, tagged v1.0.2) → SE→TE→SE→**CI/CD** again
(`status:ready-for-commit` a second time), per the standard "hand back
to Systems Engineer noting regression testing needed" step in
[[merge-required-member-break]]'s own workflow. On the second visit,
`git log origin/main..origin/issue-<N>` was empty — every commit on
the branch was already an ancestor of trunk. Nothing to commit or
merge; the branch's own content had been fully absorbed by the first
CI/CD turn.

**How to apply:** Before assuming a `status:ready-for-commit` hand-off
means "merge a branch," check `git log origin/main..origin/issue-<N>
--oneline`. If it's empty, this is the closing confirmation after a
post-merge regression pass, not a fresh merge — just re-verify build/
test on current trunk (cheap, worth doing anyway), confirm the RTVM
row and commit SHA are still correct, and hand straight back to
`agent:systems-engineer` with no new tag/release action. Don't
re-tag or re-merge; the version tag from the first visit
([[release-versioning-tag-collision-same-day]]) already covers this
work. No `status:*` label applies to this specific hand-back — omit
step (c) of the label sequence.

Related: [[build-toolchain-shallow-clone]] (still need to unshallow
to resolve older SHAs even on this confirmation-only pass).

**Don't confuse this with a fast-path RTVM commit on trunk (issue #23,
2026-08-17):** Systems Engineer can mark an RTVM row `Verified` via a
*direct single-line commit to `main`* (the fast-path) citing a commit
SHA that only exists on the feature branch, ahead of that branch
actually being merged. Seeing `docs/RTVM.md` already show `Verified`
on trunk does **not** mean this is the empty-diff case above — check
`git log origin/main..origin/issue-N --oneline` regardless; if it's
non-empty, this is still a real merge (issue #23 had 3 real commits
still to land). The RTVM state and "is there a branch left to merge"
are two independent questions — always check the branch diff, not the
docs.

**Second confirmed occurrence (issue #20/OUT-400, 2026-08-17):** same
pattern exactly — `git log origin/main..origin/issue-20` empty,
RTVM's `OUT-400` row already `Verified` with commit `40fa920`
recorded. Re-verified build (0/0) + test (108/108) on trunk anyway,
confirmed no open-requirement release trigger, then handed straight
to `agent:systems-engineer` with no `status:*` label added (omitted
step c) and no re-tag. This is turning into the normal shape of the
"trunk merge → regression pass → close-out" loop, not a one-off.
