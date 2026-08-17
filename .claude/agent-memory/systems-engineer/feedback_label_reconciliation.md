---
name: feedback-label-reconciliation
description: How to handle a "label reconciliation" comment that flags mutually-exclusive status labels on an issue
metadata:
  type: feedback
---

When a `status:*` label-reconciliation bot/owner comment appears (e.g. both
`status:ready-for-rtvm-update` and `status:ready-for-commit` present at once
because a prior hand-off didn't complete), don't trust the kept label at face
value and don't assume the work is partial either — **read the full comment
thread and verify against real artifacts** (git log/merge-base on the actual
repo, `docs/RTVM.md` contents) before acting.

**Why:** on issue #10 (CORE-201/202), the labels were left inconsistent by an
incomplete hand-off, but the underlying work was actually 100% complete —
RTVM already had the commit SHA recorded, and Test Engineer's post-merge
regression pass had already come back clean. Blindly redoing "ready-for-commit"
steps would have been wasted/duplicate work; blindly trusting the label without
checking would have risked missing a genuinely partial state on some other
issue.

**How to apply:** on any label-reconciliation flag, run the same checks used
here: `git merge-base --is-ancestor <sha> main` to confirm a claimed merge
commit is real and present, `grep` the RTVM for the item's current
status/commit column, and read the last few comments in order. If everything
checks out as already done, comment explaining what you verified and why the
work is genuinely complete (not just "trusting the label"), then close per the
normal closeout pattern — same "already current" pattern as the
Documentation-index entries for issues #6/#7/#8/#11/#12/#14/#16 (see MEMORY.md).
