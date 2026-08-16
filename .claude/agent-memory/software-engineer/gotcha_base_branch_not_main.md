---
name: gotcha-base-branch-not-main
description: as of issue #7, origin/main still doesn't contain the Generate Code Base (issue #5) scaffolding — branch feature work off origin/issue-5, not origin/main, until CI/CD actually merges it
metadata:
  type: project
---

`origin/main` was verified clean (PASS) on the "Generate Code Base"
issue (#5) and the issue was closed, but as of 2026-08-16 CI/CD had not
yet merged `issue-5` into `main` — `git ls-tree origin/main` has no
`PlcEmulator.sln`/`src/`/`tests/` at all, only docs and agent-memory.
The actual scaffolding (`PlcEmulator.sln`, all five `src/*` projects,
`tests/PlcEmulator.Tests`) only exists on `origin/issue-5`.

**Why:** the branch convention says every feature issue's work happens
on `issue-<number>` branched off... implicitly `main`, but `main`
doesn't have anything to build on top of yet. Branching a NETWORK
schema (issue #7) or CONTROL_LOGIC schema (issue #6) feature off
`origin/main` directly would mean starting from zero — no `.sln`, no
`PlcEmulator.Config` project, nothing.

**How to apply:** until `main` actually contains the scaffolding (check
`git ls-tree origin/main --name-only | grep -c '\.sln'` — 0 means not
merged yet), create feature branches from `origin/issue-5` instead of
`origin/main`, then merge `origin/main` on top for any doc/memory
updates that landed there since (`git merge origin/main --no-edit`,
remembering to `git fetch --unshallow` first per
[[gotcha-shallow-clone-merge]] so the merge isn't spuriously refused as
unrelated histories). Re-check this note's premise each time — once
CI/CD actually merges `issue-5` into `main`, branch off `origin/main`
normally again and this workaround stops applying.
