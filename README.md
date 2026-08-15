# =TEMPLATE=

A template for building a semi-automated team of software engineering
AI agents — a Product Manager, a Solutions Architect, a Systems
Engineer, a Software Engineer, a Test Engineer, and CI/CD — using
GitHub as the distributor for the agents, handling communication via
Issue messages. No long-running process is required: every hand-off
is a fresh, ephemeral Claude Code session, triggered by a label
change, that reads the issue thread, does its work, and hands off to
the next role.

## Where to begin

Starting a brand-new project from this template:

- **Create a New Repo** - Duplicate this repo and give it a logical based on the Subject_Goals of the project.
- **Fill in Kickoff Runbook** - **[`KICKOFF_RUNBOOK.md`](./KICKOFF_RUNBOOK.md)** — the fill-in-the-blanks
questionnaire and step-by-step setup instructions. Start there, not
here.

## Where to go next — the project documents, in the order they're actually produced

Once a project is underway, these are the artifacts each role owns,
listed in the order the pipeline creates them — not the order they
happen to sit in alphabetically:

1. **[`docs/PROJECT_DEFINITION.md`](docs/PROJECT_DEFINITION.md)** —
   owned by **Product Manager**. Scope, stakeholder needs, and the MVP
   definition, gathered through the kickoff interview. Everything
   downstream traces back to this.
2. **[`docs/RTVM.md`](docs/RTVM.md)** — owned by **Systems Engineer**.
   The Requirements Traceability & Verification Matrix: every
   requirement broken into testable line items, each traced to a
   stakeholder need.
3. **[`docs/SDD.md`](docs/SDD.md)** — owned by **Systems Engineer**.
   The Software Design Document: system architecture, coding
   standards, and build/toolchain conventions. Comes *before* the
   implementation plan below, not after — you need the architectural
   decomposition before you can sensibly sequence a build around it.
4. **[`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md)** —
   owned by **Systems Engineer**, with Solutions Architect and Product
   Manager. Build sequencing, most-critical-first, and the dependency
   graph between features. This is also the step that actually
   creates every individual feature issue, dependency-gated where
   needed.

**Reference, not sequential** — consult as needed, not part of the
linear flow above:

- **[`docs/LOCKING.md`](docs/LOCKING.md)** — the symbolic file-locking
  convention agents use when editing shared documents or binary
  assets concurrently.

## Where the rules live

**[`.github/AGENT_LABELS.md`](.github/AGENT_LABELS.md)** — the full
label convention, the escalation ladder, the complete issue-type
reference, and the branch and comment-structure conventions every role
follows. This is the source of truth if something in the pipeline's
behavior doesn't match what you expected.
