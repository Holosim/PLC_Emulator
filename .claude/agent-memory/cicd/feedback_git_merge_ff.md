---
name: feedback-git-merge-fast-forward
description: git merge silently fast-forwards and drops a custom -m message when the target is already an ancestor — use --no-ff if the crafted merge commit message must land
metadata:
  type: feedback
---

`git merge <branch> --no-edit -m "<crafted Summary/Source/Testing
message>"` does **not** create a merge commit — and silently discards
the `-m` message — if the target branch is already a fast-forward
target (i.e. the branch being merged already contains all of the
current branch's history). Git just moves the ref forward to the
other branch's tip.

**Why:** Discovered merging issue-6 to `main` on issue #6 — `main`
had already been merged *into* `issue-6` first, so `main`'s own merge
into `issue-6`'s tip was a pure fast-forward. The carefully-formatted
commit message (Summary/Source/Testing per the required commit format)
never actually landed anywhere; the tip commit on trunk kept whatever
message the feature-branch tip already had.

**How to apply:** If a merge to trunk needs its own message carrying
the Summary/Source/Testing writeup (per cicd.md's commit message
format), pass `--no-ff` explicitly so git creates a real merge commit
even when a fast-forward is possible. Otherwise, don't rely on the
merge commit for that documentation — put the full writeup in the
issue hand-off comment instead (referencing the substantive commit
SHA(s) from the feature branch), which is what actually happened here
and is an acceptable fallback when a fast-forward already got pushed
before you notice.
