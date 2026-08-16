---
name: gotcha-trunk-lag-behind-dependency
description: What to do when the branch convention says "branch from trunk" but trunk hasn't actually received a closed dependency issue's code yet
metadata:
  type: project
---

On PLC_Emulator, issue #5 ("Generate Code Base") was closed and fully
signed off (Software Engineer → Test Engineer → Systems Engineer, all
PASS) but its code was never actually merged to `main` by CI/CD — the
final hand-off comment said "closing this issue, no further hand-off
needed" instead of relabeling to `agent:cicd`, so nothing triggered the
merge. `dependency-check.yml` only checks whether a dependency issue is
**closed**, not whether its branch reached trunk — so issue #6 got
released to `agent:software-engineer` while `main` still had zero
`src/` files.

**Why this matters:** `git checkout -b issue-<N> main` is the default
per `.github/AGENT_LABELS.md`'s branch convention, but blindly doing
that here would have started issue #6 with no scaffolding to build on.

**How to apply:** before branching, check whether `main` actually
contains what the dependency issue's hand-off comments claim it does
(`git log --oneline main`, compare against the dependency's branch,
e.g. `git diff origin/main origin/issue-<dep> --stat`). If trunk is
missing merged work a closed dependency produced, branch from that
dependency's own branch instead (`git checkout -b issue-<N>
origin/issue-<dep>`), merge in any trunk-only commits on top, and say
so plainly in the hand-off comment so Systems Engineer/CI/CD can
reconcile the missing trunk merge. Don't merge the dependency branch
into trunk yourself — merging to trunk is CI/CD's job, not
Software Engineer's, even to fix a gap like this.

See also [[gotcha-shallow-clone-merge]] (already-known: `git log`
looks like a 1-commit repo until `git fetch --unshallow`, which is
also what surfaces `origin/issue-<N>` branches that a shallow clone's
default fetch doesn't bring down).
