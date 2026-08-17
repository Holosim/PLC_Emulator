#!/usr/bin/env bash
# Reconcile agent/status labels on open issues, and retrigger stalled work.
#
# Why this exists: an issue's label set is the whole state machine. When a
# hand-off partially completes -- new label added, old one never removed, or
# the agent label moved but the status label didn't -- the issue lands in a
# state no agent will act on, and nothing self-corrects. This finds those,
# reconciles them by an explicit set of rules, and retriggers.
#
# Never filters by label in the API query. GitHub's label filter is AND
# semantics (`--label "a,b"` means "has BOTH"), which silently returns
# nothing for exactly the partial states this script exists to fix. It
# fetches all open issues and filters locally instead.
#
# Usage:
#   ./scripts/reconcile-labels.sh --dry-run            # show, change nothing
#   ./scripts/reconcile-labels.sh                      # 1h staleness cutoff
#   ./scripts/reconcile-labels.sh --force-immediate    # ignore age entirely
#   ./scripts/reconcile-labels.sh --repo owner/name    # explicit repo

set -uo pipefail

DRY_RUN=false
THRESHOLD_SECONDS=3600
REPO="${GH_REPO:-}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run)         DRY_RUN=true; shift ;;
    --force-immediate) THRESHOLD_SECONDS=0; shift ;;
    --threshold)       THRESHOLD_SECONDS="$2"; shift 2 ;;
    --repo)            REPO="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

if [[ -z "$REPO" ]]; then
  REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner) || {
    echo "Could not determine repo. Pass --repo owner/name." >&2; exit 2; }
fi

NOW=$(date -u +%s)

# --- Label taxonomy -------------------------------------------------------
#
# PAUSE states are intentional -- a human or a dependency is deliberately
# holding this issue. Never touched, never retriggered, no exceptions.
PAUSE_RE='^status:(blocked|needs-human|on-hold|waiting-on-lock|paused|cancelled)$'
#
# POSITION states mark where an issue sits in the pipeline. Mutually
# exclusive by definition: an issue is at exactly one point, never two.
# Rank = pipeline order, so the highest rank present is the furthest along.
POSITION_RE='^status:(ready-for-test|ready-for-rtvm-update|ready-for-commit|verified)$'

position_rank() {
  case "$1" in
    status:ready-for-test)        echo 1 ;;
    status:ready-for-rtvm-update) echo 2 ;;
    status:ready-for-commit)      echo 3 ;;
    status:verified)              echo 4 ;;
    *)                            echo 0 ;;
  esac
}

# Who owns an issue sitting at a given position, when the agent label is
# missing entirely and has to be inferred.
owner_for_position() {
  case "$1" in
    status:ready-for-test)        echo "agent:test-engineer" ;;
    status:ready-for-rtvm-update) echo "agent:systems-engineer" ;;
    status:ready-for-commit)      echo "agent:cicd" ;;
    status:verified)              echo "agent:systems-engineer" ;;
    *)                            echo "" ;;
  esac
}

run() {
  if $DRY_RUN; then
    echo "      would run: $*"
  else
    "$@" >/dev/null || echo "      WARNING: command failed: $*" >&2
  fi
}

REPORT=$(mktemp)

gh issue list --state open --limit 200 --repo "$REPO" \
  --json number,title,labels,updatedAt \
  --jq '.[] | [.number, .updatedAt, (.labels | map(.name) | join(",")), .title] | @tsv' \
  | while IFS=$'\t' read -r NUMBER UPDATED_AT LABELS_CSV TITLE; do

      [[ -z "$NUMBER" ]] && continue
      LABELS=$(tr ',' '\n' <<< "$LABELS_CSV")

      AGENT_LABELS=$(grep '^agent:' <<< "$LABELS" || true)
      POSITION_LABELS=$(grep -E "$POSITION_RE" <<< "$LABELS" || true)
      HAS_IN_PROGRESS=$(grep -x 'status:in-progress' <<< "$LABELS" || true)

      # Nothing agent-related on this issue at all -- not ours to touch.
      if [[ -z "$AGENT_LABELS" && -z "$POSITION_LABELS" && -z "$HAS_IN_PROGRESS" ]]; then
        continue
      fi

      # Rule 1: intentional pauses are left completely alone.
      PAUSE_HIT=$(grep -E "$PAUSE_RE" <<< "$LABELS" | head -1 || true)
      if [[ -n "$PAUSE_HIT" ]]; then
        echo "#$NUMBER: skipped ($PAUSE_HIT -- intentional pause)" >> "$REPORT"
        continue
      fi

      # Staleness check. An issue only counts as stalled if nothing has
      # happened on it recently -- otherwise a run may be legitimately
      # mid-flight right now.
      LAST_TIME=$(gh issue view "$NUMBER" --repo "$REPO" --json comments \
        -q '.comments | if length > 0 then .[-1].createdAt else null end' 2>/dev/null)
      if [[ -z "$LAST_TIME" || "$LAST_TIME" == "null" ]]; then
        LAST_TIME="$UPDATED_AT"
      fi
      AGE=$(( NOW - $(date -u -d "$LAST_TIME" +%s) ))
      if (( AGE < THRESHOLD_SECONDS )); then
        continue
      fi

      AGENT_COUNT=0
      [[ -n "$AGENT_LABELS" ]] && AGENT_COUNT=$(grep -c . <<< "$AGENT_LABELS")
      POSITION_COUNT=0
      [[ -n "$POSITION_LABELS" ]] && POSITION_COUNT=$(grep -c . <<< "$POSITION_LABELS")

      echo "#$NUMBER ($TITLE)" >> "$REPORT"
      echo "  idle $(( AGE / 60 ))m | agents: ${AGENT_LABELS//$'\n'/, } | positions: ${POSITION_LABELS//$'\n'/, }" >> "$REPORT"
      echo "--- #$NUMBER: idle $(( AGE / 60 ))m"

      REMOVE=()
      NOTES=()
      CONFLICT=false

      # Rule 2: mutually exclusive position labels. Progress only moves
      # forward, so the furthest-along label is the live one and anything
      # behind it is a leftover from a hand-off that didn't finish cleaning
      # up. Keep the highest rank, drop the rest -- but flag it, because a
      # partial hand-off means the work itself may also be half-done.
      KEEP_POSITION=""
      if (( POSITION_COUNT > 1 )); then
        CONFLICT=true
        BEST_RANK=0
        while read -r p; do
          [[ -z "$p" ]] && continue
          r=$(position_rank "$p")
          if (( r > BEST_RANK )); then BEST_RANK=$r; KEEP_POSITION="$p"; fi
        done <<< "$POSITION_LABELS"
        while read -r p; do
          [[ -z "$p" || "$p" == "$KEEP_POSITION" ]] && continue
          REMOVE+=("$p")
        done <<< "$POSITION_LABELS"
        NOTES+=("Found mutually exclusive status labels (${POSITION_LABELS//$'\n'/, }). Kept \`$KEEP_POSITION\` as the furthest along and cleared the rest.")
      elif (( POSITION_COUNT == 1 )); then
        KEEP_POSITION="$POSITION_LABELS"
      fi

      # Rule 3: status:in-progress on an issue this stale is by definition
      # false -- the run it referred to is long gone. Always clear it; it's
      # what blocks a clean retrigger.
      if [[ -n "$HAS_IN_PROGRESS" ]]; then
        REMOVE+=("status:in-progress")
      fi

      # Rule 4: decide who should act next.
      TARGET=""
      if (( AGENT_COUNT > 1 )); then
        # Genuinely ambiguous. If exactly one of them matches the owner the
        # position label implies, that's strong enough to act on. Otherwise
        # don't guess -- hand it to a human.
        CONFLICT=true
        EXPECTED=$(owner_for_position "$KEEP_POSITION")
        if [[ -n "$EXPECTED" ]] && grep -qx "$EXPECTED" <<< "$AGENT_LABELS"; then
          TARGET="$EXPECTED"
          while read -r a; do
            [[ -z "$a" || "$a" == "$TARGET" ]] && continue
            REMOVE+=("$a")
          done <<< "$AGENT_LABELS"
          NOTES+=("Found more than one \`agent:*\` label (${AGENT_LABELS//$'\n'/, }). Kept \`$TARGET\`, which is the owner \`$KEEP_POSITION\` implies.")
        else
          NOTES+=("Found more than one \`agent:*\` label (${AGENT_LABELS//$'\n'/, }) and no way to tell which is live from the status labels. Not guessing -- flagging for a human.")
          if ! $DRY_RUN; then
            gh issue comment "$NUMBER" --repo "$REPO" --body "Label reconciliation: ${NOTES[*]}"
          fi
          run gh issue edit "$NUMBER" --repo "$REPO" --add-label "status:needs-human"
          echo "  -> flagged status:needs-human (ambiguous agent labels)" >> "$REPORT"
          continue
        fi
      elif (( AGENT_COUNT == 1 )); then
        # A "**Next:**" line naming a different role means the hand-off was
        # declared but the relabel never landed. Trust the declaration.
        LAST_BODY=$(gh issue view "$NUMBER" --repo "$REPO" --json comments \
          -q '.comments | if length > 0 then .[-1].body else "" end' 2>/dev/null)
        NEXT_LINE=$(grep -i '^\*\*Next:\*\*' <<< "$LAST_BODY" | tail -1 || true)
        DECLARED=$(grep -oE '`agent:[a-z-]+`' <<< "$NEXT_LINE" | tr -d '`' | head -1 || true)

        if grep -qi 'waiting on human reply' <<< "$NEXT_LINE"; then
          # Not stalled -- deliberately waiting on you. Clear the false
          # in-progress flag and leave everything else untouched.
          if (( ${#REMOVE[@]} > 0 )); then
            ARGS=(); for l in "${REMOVE[@]}"; do ARGS+=(--remove-label "$l"); done
            run gh issue edit "$NUMBER" --repo "$REPO" "${ARGS[@]}"
          fi
          echo "  -> waiting on human reply; cleared stale flags only, no retrigger" >> "$REPORT"
          continue
        fi

        if [[ -n "$DECLARED" && "$DECLARED" != "$AGENT_LABELS" ]]; then
          TARGET="$DECLARED"
          REMOVE+=("$AGENT_LABELS")
          NOTES+=("The last comment declared a hand-off to \`$DECLARED\` that never took effect. Routing there.")
        else
          TARGET="$AGENT_LABELS"
        fi
      else
        # Rule 5: no agent label at all, but the issue is clearly parked at
        # a pipeline position. Infer the owner from the position.
        TARGET=$(owner_for_position "$KEEP_POSITION")
        if [[ -z "$TARGET" ]]; then
          echo "  -> no agent label and no inferable owner; skipped" >> "$REPORT"
          continue
        fi
        CONFLICT=true
        NOTES+=("This issue had no \`agent:*\` label at all. Inferred \`$TARGET\` from \`$KEEP_POSITION\`.")
      fi

      # Apply removals.
      if (( ${#REMOVE[@]} > 0 )); then
        ARGS=(); for l in "${REMOVE[@]}"; do ARGS+=(--remove-label "$l"); done
        run gh issue edit "$NUMBER" --repo "$REPO" "${ARGS[@]}"
      fi

      # Rule 6: when anything was reconciled rather than simply retriggered,
      # say so on the issue and ask the acting agent to confirm real state
      # before trusting the labels. A partial hand-off can mean partial
      # work, and the label alone won't reveal that.
      if $CONFLICT; then
        BODY="Label reconciliation on this issue found an inconsistent state and corrected it:

$(printf -- '- %s\n' "${NOTES[@]}")

\`$TARGET\`: before continuing, verify the issue's real state against the branch and the comment history rather than trusting the labels. These labels were left inconsistent by a hand-off that didn't complete, so the work it described may also be partial. If what you find doesn't match \`$KEEP_POSITION\`, say so and correct it rather than proceeding on the assumption it's accurate."
        if $DRY_RUN; then
          echo "      would comment: (reconciliation notice + verify request)"
        else
          gh issue comment "$NUMBER" --repo "$REPO" --body "$BODY" >/dev/null || true
        fi
      fi

      # Retrigger: remove then re-add, which is what actually fires the relay.
      run gh issue edit "$NUMBER" --repo "$REPO" --remove-label "$TARGET"
      run gh issue edit "$NUMBER" --repo "$REPO" --add-label "$TARGET"
      echo "  -> retriggered $TARGET$($CONFLICT && echo ' (after reconciliation)')" >> "$REPORT"
    done

echo
echo "=== Summary ==="
cat "$REPORT"
if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  { echo "## Label reconciliation"; echo '```'; cat "$REPORT"; echo '```'; } >> "$GITHUB_STEP_SUMMARY"
fi
$DRY_RUN && echo "(dry run -- nothing was changed)"
rm -f "$REPORT"
