---
name: feedback-platform-verification-schedule
description: Client overrode a per-feature dual-platform CI matrix in favor of one-time late-stage consolidation — apply this default going forward
metadata:
  type: feedback
---

When the pipeline's native execution environment (Ubuntu) differs from
a deliverable's target platform (Windows/VS here), don't default to
gating every feature issue on a second-platform CI matrix just because
the runner itself is "nearly free" (e.g. GitHub-hosted `windows-latest`
alongside `ubuntu-latest`). The client corrected exactly this on
PLC_Emulator issue #5 (2026-08-16): even a free runner adds a
recurring integration/permission-surface cost per feature (this
project hit it concretely — pushing workflow files needs a
`workflows`-scoped token, discovered on the very first feature issue).
That recurring cost, multiplied across every RTVM feature, outweighs
catching an OS-specific bug slightly earlier.

**Why:** client's own words — "development happens entirely in-pipeline
… Windows/VS verification is a one-time final consolidation step, not
a continuous target." This matches the systems-engineer role
instructions' own guidance about target-platform verification
decisions being deliberate, not default — but the first pass on this
project defaulted to per-feature gating anyway because the runner cost
looked negligible in isolation.

**How to apply:** when writing the "Target-platform verification
strategy" section of a future `docs/SDD.md`, default toward **one late
consolidation pass**, not a per-feature matrix, unless there's a
concrete reason to expect early, hard-to-bisect platform divergence
(native interop, platform-specific APIs, etc.). If unsure, ask — don't
assume "the second runner is free" settles it on its own. See
[[sdd-decisions-plc-emulator]] for how this played out concretely.
