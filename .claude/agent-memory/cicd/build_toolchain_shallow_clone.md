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

**Also breaks BUILD-number computation, not just merges (issue #16,
2026-08-17):** a fresh shallow session showed only 7 commits and a
first-commit date one day later than reality, which would have
produced a wrong BUILD number for the `v{MAJOR}.{MINOR}.{BUILD}` tag
(see [[release-versioning-tag-collision-same-day]]). Run the
shallow-repo check (and `--unshallow` if needed) before *any*
git-history-derived computation this session — not just before
merges — since `git log --reverse --format=%cd | head -1` is exactly
this same failure mode.
