---
name: gh-api-transient-503-silent-label-failures
description: gh CLI label edits can 503 transiently; the `2>/dev/null || true` pattern in the handoff steps swallows that silently — always verify labels after
metadata:
  type: known-issue
---

`gh issue edit --remove-label` / `--add-label` occasionally return `HTTP
503: No server is currently available` from the GitHub API/GraphQL
backend — unrelated to whether the label exists on the issue. This hit
issue #29's hand-off: the prescribed removal loop (`gh issue edit 29
--remove-label "$L" 2>/dev/null || true`) is written to tolerate "label
wasn't present" (a real 404-type no-op), but it *also* silently
swallows a genuine transient 503 the same way — so `status:ready-for-commit`
and `agent:cicd` were still on the issue after the loop reported success.

**Why:** The `|| true` in the handoff steps exists to make "label
didn't exist" non-fatal, per the workflow design (any one of several
possible inherited status labels may or may not be present). But it
can't distinguish that from a transient server error, and both look
identical from the shell's perspective.

**How to apply:** After running the label-removal/addition commands in
step 6 of "Working an issue," always run `gh issue view <N> --json
labels` once at the end to confirm the final label set actually
matches intent (no stale `status:*`, no lingering `agent:cicd`, correct
next-role label present) before considering the hand-off done. If a
503 shows up mid-sequence, just retry that one command after a short
sleep — it clears on retry, no special handling needed otherwise.
