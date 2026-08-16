---
name: build-toolchain-shallow-clone
description: Local checkout may be a shallow git clone, breaking merge-base/merge across branches with divergent fetch depths
metadata:
  type: project
---

The CI/CD working checkout can be a **shallow clone** (`git rev-parse
--is-shallow-repository` → `true`). When two refs were fetched to
different depths (e.g. `origin/main` only 2 commits deep vs. a feature
branch fetched much further back), `git merge-base` returns nothing
and `git merge` fails with `fatal: refusing to merge unrelated
histories` — even though the branches share real history once you can
see it.

**Why:** Shallow fetch boundaries are per-ref, not per-repo; a commit
that's a real ancestor can be "invisible" to `merge-base` if the
boundary was cut before it on one ref's fetch but not the other's.
This hit issue #6's trunk merge: `origin/main` looked unrelated to
`issue-6` until `git fetch --unshallow origin` pulled full history,
after which `merge-base` immediately found the real common ancestor.

**How to apply:** Before merging any branch to trunk (or merging
trunk into a feature branch), run `git rev-parse
--is-shallow-repository` and, if true, `git fetch --unshallow origin`
first. Don't trust "unrelated histories" as necessarily meaning the
branches truly don't share history — check shallow status before
concluding that.
