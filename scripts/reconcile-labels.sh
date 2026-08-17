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

      if [[ -z "$AGENT_LABELS" && -z "$POSITION_LABELS" && -z "$HAS_IN_PROGRESS" ]]; then
        continue
      fi

      # Rule 1: intentional pauses are left completely alone.
      PAUSE_HIT=$(grep -E "$PAUSE_RE" <<< "$LABELS" | head -1 || true)
      if [[ -n "$PAUSE_HIT" ]]; then
        echo "#$NUMBER: skipped ($PAUSE_HIT -- intentional pause)" >> "$REPORT"
        continue
      fi

      LAST_TIME=$(gh issue view "$NUMBER" --repo "$REPO" --json comments \
        -q '.comments | if length > 0 then .[-1].createdAt else null end' 2>/dev/null)
      if [[ -z "$LAST_TIME" || "$LAST_TIME" == "null" ]]; then
        LAST_TIME="$UPDATED_AT"
      fi
      AGE=$(( NOW - $(date -u -d "$LAST_TIME" +%s) ))
      # Safety floor: even --force-immediate won't touch an issue that saw
      # activity in the last 5 minutes. Something that recent is very likely
      # a run still in flight, and retriggering it mid-execution is how
      # duplicate work and concurrency pile-ups start.
      if (( AGE < 300 )); then
        echo "#$NUMBER: skipped (active $(( AGE / 60 ))m ago -- likely mid-run)" >> "$REPORT"
        continue
      fi
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

      NOTES=()
      CONFLICT=false

      # The last comment's "**Next:**" line is the strongest signal there is:
      # the agent explicitly declared where this goes. If it's present, it
      # outranks whatever the labels say.
      LAST_BODY=$(gh issue view "$NUMBER" --repo "$REPO" --json comments \
        -q '.comments | if length > 0 then .[-1].body else "" end' 2>/dev/null)
      NEXT_LINE=$(grep -i '^\*\*Next:\*\*' <<< "$LAST_BODY" | tail -1 || true)
      DECLARED=$(grep -oE '`agent:[a-z-]+`' <<< "$NEXT_LINE" | tr -d '`' | head -1 || true)

      # Deliberately waiting on a human -- not stalled. Clear only the false
      # in-progress flag, leave ownership untouched, don't retrigger.
      if grep -qi 'waiting on human reply' <<< "$NEXT_LINE"; then
        if [[ -n "$HAS_IN_PROGRESS" ]]; then
          run gh issue edit "$NUMBER" --repo "$REPO" --remove-label "status:in-progress"
        fi
        echo "  -> waiting on human reply; cleared stale flags only, no retrigger" >> "$REPORT"
        continue
      fi

      # --- Choose ONE position label, and choose it together with the
      # owner rather than independently. Precedence:
      #   1. the position whose owner matches an explicit Next: declaration
      #   2. the position whose owner matches the current agent label
      #      (agreement between two independent signals is strong)
      #   3. the EARLIEST position present -- deliberately conservative.
      #      Redoing a completed step costs one cheap run; skipping an
      #      unfinished one commits or verifies work that never happened.
      KEEP_POSITION=""
      if (( POSITION_COUNT > 0 )); then
        if [[ -n "$DECLARED" ]]; then
          while read -r p; do
            [[ -z "$p" ]] && continue
            [[ "$(owner_for_position "$p")" == "$DECLARED" ]] && KEEP_POSITION="$p" && break
          done <<< "$POSITION_LABELS"
        fi
        if [[ -z "$KEEP_POSITION" && $AGENT_COUNT -eq 1 ]]; then
          while read -r p; do
            [[ -z "$p" ]] && continue
            [[ "$(owner_for_position "$p")" == "$AGENT_LABELS" ]] && KEEP_POSITION="$p" && break
          done <<< "$POSITION_LABELS"
        fi
        if [[ -z "$KEEP_POSITION" ]]; then
          BEST_RANK=99
          while read -r p; do
            [[ -z "$p" ]] && continue
            r=$(position_rank "$p")
            if (( r < BEST_RANK )); then BEST_RANK=$r; KEEP_POSITION="$p"; fi
          done <<< "$POSITION_LABELS"
        fi
      fi

      if (( POSITION_COUNT > 1 )); then
        CONFLICT=true
        NOTES+=("Found mutually exclusive status labels (${POSITION_LABELS//$'\n'/, }) -- these describe different points in the pipeline and can't both be true. Kept \`$KEEP_POSITION\` and cleared the rest.")
      fi

      # --- Choose the owner, consistently with the position just chosen.
      TARGET=""
      EXPECTED_OWNER=$(owner_for_position "$KEEP_POSITION")

      if [[ -n "$DECLARED" ]]; then
        TARGET="$DECLARED"
        if (( AGENT_COUNT == 1 )) && [[ "$DECLARED" != "$AGENT_LABELS" ]]; then
          CONFLICT=true
          NOTES+=("The last comment declared a hand-off to \`$DECLARED\` that never took effect -- the label stayed on \`$AGENT_LABELS\`. Routing to \`$DECLARED\`.")
        fi
        # A declaration outranks the position label, so if the position
        # label names a stage this target doesn't own, it's a leftover from
        # an earlier stage. Drop it rather than leave a contradictory pair;
        # the acting agent re-establishes the right one.
        if [[ -n "$KEEP_POSITION" && "$(owner_for_position "$KEEP_POSITION")" != "$TARGET" ]]; then
          CONFLICT=true
          NOTES+=("\`$KEEP_POSITION\` is a leftover -- it names a stage \`$TARGET\` doesn't own, and the declared hand-off to \`$TARGET\` is the newer signal. Cleared it; set the correct status yourself as part of this turn.")
          KEEP_POSITION=""
        fi
      elif [[ -n "$EXPECTED_OWNER" ]]; then
        TARGET="$EXPECTED_OWNER"
        if (( AGENT_COUNT >= 1 )) && ! grep -qx "$EXPECTED_OWNER" <<< "$AGENT_LABELS"; then
          CONFLICT=true
          NOTES+=("The \`agent:*\` label (${AGENT_LABELS//$'\n'/, }) disagreed with \`$KEEP_POSITION\`, which \`$EXPECTED_OWNER\` owns. Went with the status label, since redoing a step is safer than skipping one.")
        elif (( AGENT_COUNT == 0 )); then
          CONFLICT=true
          NOTES+=("This issue had no \`agent:*\` label at all. Inferred \`$TARGET\` from \`$KEEP_POSITION\`.")
        fi
      elif (( AGENT_COUNT == 1 )); then
        TARGET="$AGENT_LABELS"
      else
        CONFLICT=true
        NOTES+=("Found ${AGENT_COUNT} \`agent:*\` labels and no status label to resolve them against. Not guessing.")
        if ! $DRY_RUN; then
          gh issue comment "$NUMBER" --repo "$REPO" --body "Label reconciliation: ${NOTES[*]}" >/dev/null || true
        fi
        run gh issue edit "$NUMBER" --repo "$REPO" --add-label "status:needs-human"
        echo "  -> flagged status:needs-human (ambiguous, unresolvable)" >> "$REPORT"
        continue
      fi

      # --- Build the removal set: every label that isn't the chosen pair.
      REMOVE=()
      [[ -n "$HAS_IN_PROGRESS" ]] && REMOVE+=("status:in-progress")
      while read -r p; do
        [[ -z "$p" || "$p" == "$KEEP_POSITION" ]] && continue
        REMOVE+=("$p")
      done <<< "$POSITION_LABELS"
      while read -r a; do
        [[ -z "$a" || "$a" == "$TARGET" ]] && continue
        REMOVE+=("$a")
      done <<< "$AGENT_LABELS"

      if (( ${#REMOVE[@]} > 0 )); then
        ARGS=(); for l in "${REMOVE[@]}"; do ARGS+=(--remove-label "$l"); done
        run gh issue edit "$NUMBER" --repo "$REPO" "${ARGS[@]}"
      fi

      # Anything reconciled rather than simply retriggered gets a note on the
      # issue: a hand-off that left labels inconsistent may have left the
      # work itself partial, and the labels alone won't reveal that.
      if $CONFLICT; then
        BODY="Label reconciliation found an inconsistent state on this issue and corrected it:

$(printf -- '- %s\n' "${NOTES[@]}")

\`$TARGET\`: verify this issue's real state against the branch and the comment history before continuing -- don't assume the labels are accurate. They were left inconsistent by a hand-off that didn't finish, so the work it described may be partial too. If what you find doesn't match \`$KEEP_POSITION\`, say so and correct it rather than proceeding as if it does. If the step was in fact already completed, just hand off to the next role instead of redoing it."
        if $DRY_RUN; then
          echo "      would comment: (reconciliation notice + verify request)"
        else
          gh issue comment "$NUMBER" --repo "$REPO" --body "$BODY" >/dev/null || true
        fi
      fi

      run gh issue edit "$NUMBER" --repo "$REPO" --remove-label "$TARGET"
      run gh issue edit "$NUMBER" --repo "$REPO" --add-label "$TARGET"
      if $CONFLICT; then
        echo "  -> reconciled to $TARGET / $KEEP_POSITION, retriggered" >> "$REPORT"
      else
        echo "  -> retriggered $TARGET" >> "$REPORT"
      fi
    done

echo
echo "=== Summary ==="
cat "$REPORT"
if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  { echo "## Label reconciliation"; echo '```'; cat "$REPORT"; echo '```'; } >> "$GITHUB_STEP_SUMMARY"
fi
$DRY_RUN && echo "(dry run -- nothing was changed)"
rm -f "$REPORT"
