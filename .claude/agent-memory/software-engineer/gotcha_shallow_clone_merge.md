---
name: gotcha-shallow-clone-merge
description: git merge from origin/main can falsely report "unrelated histories" if the checkout is shallow — fetch full history before concluding branches actually diverged
metadata:
  type: feedback
---

When merging `origin/main` into a feature branch and git reports
`fatal: refusing to merge unrelated histories`, don't assume trunk was
actually reset/rewritten. Check first whether the local clone is
shallow (`git log origin/main --oneline` showing suspiciously few
commits, e.g. one big squashed-looking commit with hundreds of
insertions). Run `git fetch origin main --unshallow` (or `--depth=0`)
and re-check `git merge-base origin/main origin/issue-N` — it will
very likely resolve to a real common ancestor once the full history is
present.

**Why:** hit this on issue #5 (Generate Code Base) — `origin/main`
initially looked like a single root commit unrelated to the `issue-5`
branch (which held all the scaffolding work), which would have implied
trunk got force-reset. It was actually just a shallow fetch; the true
`main` had 20+ commits sharing ancestry with `issue-5`, and a normal
`git merge origin/main --no-edit` on the feature branch worked cleanly
with zero conflicts once fetched in full.

**How to apply:** before concluding a branch needs a risky
`--allow-unrelated-histories` merge or manual file-by-file
reconciliation, unshallow the fetch and re-run `git merge-base`. Only
treat it as genuinely unrelated history if that still comes back
empty.
