---
name: concurrent-cicd-runs-same-day
description: Two CI/CD runs (different issues) can push to main within minutes of each other — always re-fetch and merge origin/main before pushing, even if your local main was current when you started
metadata:
  type: project
---

While merging `issue-7` (this issue, #7) to `main`, a second CI/CD run
finalizing issue #9 (CORE-200 regression sign-off: RTVM `Verified` +
commit SHA + a memory note) pushed to `main` between my `git pull
origin main --ff-only` at the start and my `git push` after resolving
issue-7's merge conflicts. `git push` was rejected (non-fast-forward);
`git fetch origin main && git merge origin/main --no-edit` picked up
the two extra commits cleanly (docs/memory only, no code overlap) and
the push succeeded on retry.

**How to apply:** Don't treat a rejected push as unusual or a sign of
something wrong — with multiple issues open to `agent:cicd`
concurrently, it's expected. Just fetch + merge (or rebase, but plain
merge is simpler and preserves both merge commits' independent
history) and push again. Re-run build/test after this second merge
too, not just after the first — see
[[merge-required-member-break]] for why a second merge can also
introduce a semantic break even without text conflicts, though in this
case it happened to be docs-only and safe.

Also relevant to [[release-versioning-tag-collision-same-day]]: the
concurrent issue #9 run already claimed `v1.0.1` for that day before I
got to my own tag step, which is exactly the collision that memory
describes — confirmed again here, same day, same resolution (verify
ancestry, don't re-tag, cite the merge SHA in the hand-off comment
instead).
