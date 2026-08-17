# Issue label convention

This is the contract the `agent-relay` workflow runs on. Keep it in sync
with `.github/workflows/agent-relay.yml` if you change it.

## Role labels — whose turn it is

Exactly one of these should be present on an issue that's currently
assigned to an agent. Adding one is what triggers that agent's workflow
run.

- `agent:product-manager`
- `agent:solutions-architect`
- `agent:systems-engineer`
- `agent:software-engineer`
- `agent:test-engineer`
- `agent:cicd`

Never leave two `agent:*` labels on the same issue at once. If it's
unclear who should act next, that's itself a question for
`agent:product-manager`.

## Status labels — modifiers, not triggers

- `status:in-progress` — an agent run is currently active on this issue
- `status:blocked` — paired with whichever `agent:*` label is receiving
  an escalation (e.g. `agent:solutions-architect`, `agent:product-manager`);
  marks this as an escalation rather than a fresh assignment
- `status:ready-for-test` — implementation complete, awaiting the Test
  Engineer
- `status:ready-for-rtvm-update` — a test passed; paired with
  `agent:systems-engineer`. Signals the fast path: update the RTVM
  status for the relevant requirement, then pass straight to
  `agent:cicd` — this is not a new requirement to define.
- `status:ready-for-commit` — RTVM is current and tests passed,
  awaiting CI/CD
- `status:verified` — the linked RTVM item is closed
- `status:cancelled` — a test procedure or requirement changed
  mid-test; the in-flight test iteration is void and will restart once
  a new build is ready
- `status:needs-human` — either an automated escalation path has been
  exhausted (e.g. five consecutive fail/rebuild/retest cycles on the
  same requirement, or three automatic retries of a rate-limit/overload
  error), or the run itself couldn't execute at all
  (credit balance exhausted, invalid or revoked API key, key lacks
  model access, or a missing OIDC permission) — nothing an agent could
  act on in any of these cases. The relay stops here on purpose. A
  human reviews the thread and either resolves it directly or
  manually relabels to
  resume.
- `status:waiting-on-lock` — this issue's agent backed off after
  failing to acquire a file lock (see `docs/LOCKING.md`). A scheduled
  sweep retries it periodically; no action needed unless it's stuck
  for an unusually long time.
- `status:retry-1` / `status:retry-2` / `status:retry-3` — a run
  failed with a rate-limit or service-overload error and is being
  retried automatically, with an increasing delay (30s, 60s, 120s).
  Purely informational; no action needed unless it escalates to
  `status:needs-human` after the third attempt.
- `status:on-hold` — this issue has declared dependencies that aren't
  satisfied yet, so it carries no `agent:*` label — it isn't anyone's
  turn. A scheduled sweep (`dependency-check.yml`) checks it
  periodically and releases it to `agent:software-engineer` once
  every dependency clears. No action needed unless it's stuck for an
  unusually long time, in which case check whether its declared
  dependency issues actually exist and are progressing.

## Type labels

- `type:requirement` — traces to a specific RTVM line item
- `type:blocker` — a question raised by an agent, not a client-facing ask
- `type:bug`

## Title convention

Issues that trace to a requirement start with the RTVM ID:

```
[RTVM-014] Short description of the requirement
```

This makes the RTVM ID searchable across issues, commits, and PRs without
needing a label per ID.

## Issue types

Every project runs through these at minimum — not an exhaustive list,
just the baseline. Each of the first five is a single issue producing
one artifact; the sixth is many issues, one per buildable feature:

1. **Project Kickoff** — triggers Product Manager's client
   interview; produces `docs/PROJECT_DEFINITION.md`.
2. **RTVM** — Systems Engineer breaks the Project Definition down into
   requirements; produces `docs/RTVM.md`'s line items.
3. **SDD** — Systems Engineer defines system architecture, with
   Solutions Architect and Software Engineer input as needed; produces
   `docs/SDD.md`.
4. **Implementation Plan** — Systems Engineer sequences the build with
   Solutions Architect and Product Manager, most-critical-first;
   produces
   `docs/IMPLEMENTATION_PLAN.md`, and is what actually creates the
   Generate Code Base issue and every `[RTVM-014]`-style issue below,
   each with its dependencies declared.
5. **Generate Code Base** — Software Engineer's first task: the actual
   project scaffolding (a Visual Studio solution, an Unreal project,
   whatever the platform needs). No dependencies of its own; almost
   everything else depends on it.
6. **`[RTVM-014] ...`** — one issue per atomic, buildable, testable
   feature. This is where Software Engineer, Test Engineer, and CI/CD
   all work via comments and hand-offs on the *same* issue — never a
   new issue per action taken on one feature.

The first five each close themselves out and create the next one in
the chain — they don't relabel forward the way the sixth type does.

## Declaring dependencies

When Systems Engineer creates the Generate Code Base issue and the
`[RTVM-014]`-style issues during the Implementation Plan step, any
issue that isn't immediately ready to start needs its dependencies
declared in its body, under a `## Dependencies` heading:

```
## Dependencies
- Finish-Start: #12
- Start-Start: #15
```

**Finish-Start** — the referenced issue must be closed first.
**Start-Start** — the referenced issue must have started (moved past
`status:on-hold`), but doesn't need to be finished — the two can
progress concurrently once both are underway.

An issue with any declared dependency gets `status:on-hold` instead of
`agent:software-engineer` when created — `dependency-check.yml` checks
it periodically and releases it automatically once every dependency
clears. An issue with no dependencies (like Generate Code Base itself)
gets `agent:software-engineer` immediately.

## Escalation ladder

No role is a transparent pass-through for a question it can't answer.
The ladder: `cicd` → `test-engineer` → `software-engineer` →
`systems-engineer` → `solutions-architect` → `product-manager` → user.
Product Manager is the only rung that talks to the human client
directly — every request for client input reaches the user through
here, regardless of which rung it originated at. When a role can't
resolve something itself:

1. **Try to resolve it first.** Don't escalate reflexively — the next
   rung up isn't always more qualified, just next in line.
2. **If you can't, escalate to the next rung up — in your own words.**
   Summarize, rephrase, or reference ("see Systems Engineer's
   questions 1–3 above") rather than forwarding verbatim. If you're
   relaying a question that already climbed from further down the
   ladder, say so, so the next rung knows this didn't originate with
   you.
3. **When an answer comes back down to you, relay it to whoever
   escalated to you** — don't just resolve your own concern and move
   on. The role that originally asked should get an answer via the
   same chain it went up, not silence.

Two deliberate exceptions skip most of the ladder:

- **Test Engineer's 5-consecutive-failure escalation** lands on
  `agent:product-manager` directly. Every rung already had its shot at
  this exact problem, repeatedly, and failed; climbing it again would
  repeat a demonstrated failure rather than add fresh consideration.
- **Infrastructure failures** (budget exhaustion, an invalid API key,
  a missing permission — see "Persisting your work" and the workflow's
  own failure handling) go straight to `status:needs-human` with no
  `agent:*` label at all, bypassing Product Manager too. These aren't
  requests for client input; they're operational problems no role,
  including Product Manager, can act on. Routing them through Product
  Manager first would just add a hop for something it can't resolve
  any faster than the human can.

Every other escalation — including Product Manager asking the
user — climbs one rung at a time.

## Notify vs. hand off

These are different actions and shouldn't be conflated:

- **Notify** — a comment addressed to a role by name, for their
  awareness. No relabel. Use this when the next *action* isn't theirs
  but they need to know something changed (e.g. Solutions Architect
  telling Systems Engineer about a scope refinement nobody asked for;
  Systems Engineer telling Software Engineer an RTVM item changed).
- **Hand off** — a comment plus a relabel, because the next action
  genuinely is theirs.

When a rule says "notify X, then notify Y" and both are real actions
someone has to take (not just awareness), treat it as two sequential
handoffs — X acts and relabels to Y — rather than trying to address two
roles' turns at once. See `status:ready-for-rtvm-update` above for the
concrete example.

## Persisting your work

Every job starts from a fresh `git checkout` and its container is
destroyed the moment the job ends — nothing local carries over to the
next run, for any role. Writing a file with Edit/Write isn't enough by
itself; if you don't commit and push it before you finish, it's gone,
not just uncommitted. This applies equally to documents (RTVM, SDD,
PROJECT_DEFINITION) and to your own `MEMORY.md` — a memory update that
happens after your last commit in a run is lost exactly the same way
a code change would be. Make committing and pushing everything you
touched the last thing you do, every run, regardless of what else
you've already committed earlier in that same run.

## Branch convention

Every `[RTVM-014]`-style feature issue's work happens on a branch
named `issue-<number>` — issue #5 uses `issue-5`, always, no
variation. This is deterministic on purpose: every role can compute
the branch name directly from the issue number and check it out
before starting, without needing to parse it out of a comment
anywhere. Software Engineer creates it; Test Engineer and CI/CD check
it out before doing anything else on that issue. Nothing gets merged
to trunk except by CI/CD, and only once Test Engineer has signed off.

## Hand-off mechanics

Two rules, both learned from real stalls rather than theory:

**Clear the status label you inherited.** A `status:ready-for-*` label
describes one point in the pipeline. When you hand off, the one that
routed work *to* you is no longer true — remove it before setting the
next one. Skipping this doesn't fail loudly; it accumulates, and an
issue carrying both `status:ready-for-rtvm-update` and
`status:ready-for-commit` is telling two different stories about where
it actually is.

**Never combine label edits into one `gh issue edit` call.** A single
command with several flags fails as a whole if any one label isn't
present — and `gh` does error on removing a label that isn't there.
When that happens, the `--add-label` that would have triggered the
next agent never runs, and the issue stalls with no error anywhere
visible. Use separate, individually tolerant commands
(`2>/dev/null || true` on removals), and always perform the
`agent:<next-role>` add **last**, since that's what actually fires the
relay — everything else must already be correct by the time it lands.

## Comment structure

Every comment carries two fixed elements, regardless of role or
outcome — not organic habit, a fixed structure, because tooling now
depends on it and not just readability.

**First line: addressee, then speaker.** `Systems Engineer, Test
Engineer:` means Test Engineer is speaking *to* Systems Engineer, not
just introducing itself. (Convention borrowed from Navy watch-standing
communication — "Conn, Sonar" identifies who needs the attention
before who's asking for it.) When the primary intended reader is the
human client rather than another role, use `Client, <Speaker>:`.

**Last line: an explicit, structured next-status line.** This is what
`stall-recovery.yml` actually parses to recover a stalled hand-off —
get the format right and recovery is reliable; get it wrong and
recovery is back to guessing. Exactly one of:

```
**Next:** `agent:<role-name>`
```
for a genuine hand-off,
```
**Next:** Continuing — <what happens next, briefly>
```
if you're keeping the issue and moving to your own next step, or
```
**Next:** Waiting on human reply
```
for the one case where nobody should be triggered until the client
answers.

## Document locations

Every role's file references these; keep the paths consistent across
projects built from this template:

- `docs/PROJECT_DEFINITION.md` — Product Manager's scope
  definition: business analysis, stakeholder needs, MVP definition
- `docs/RTVM.md` — Systems Engineer's requirements traceability and
  verification matrix (plain markdown table: ID, category, requirement,
  verification method, test procedure reference, status)
- `docs/SDD.md` — Systems Engineer's software design document and
  system architecture
- `docs/IMPLEMENTATION_PLAN.md` — Systems Engineer's build sequence,
  most-critical-first, ideally with a Mermaid diagram (renders natively
  on GitHub, is close enough to UML for this purpose without needing
  separate tooling)
- `docs/LOCKING.md` — the symbolic file-locking convention; read this
  before editing any binary asset or shared document
- `VERSION` — repo root, just `MAJOR.MINOR` (e.g. `1.0`). CI/CD reads
  this and appends its own computed build number; only Systems
  Engineer or Product Manager edits it, deliberately, at the start of
  a new release cycle. See "Versioning and releases" in cicd.md.

## Handoff protocol

Every handoff:
1. Removes the acting role's `agent:*` label
2. Adds exactly one new `agent:*` label for the next role
3. Adds a relevant `status:*` label alongside it when the handoff is
   anything other than the normal next step (an escalation, a
   cancellation, an RTVM-update fast path)

The workflow's job only reacts to label-*add* events, so a handoff always
means adding the next label — removing one on its own does nothing.
