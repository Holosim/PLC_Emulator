---
name: workflows-permission-resolution-plc-emulator
description: GitHub App used by this pipeline cannot be granted `workflows` scope (doesn't declare the permission) — manual human deployment is the only route for files under .github/workflows/ on this project
metadata:
  type: project
---

On PLC_Emulator, every agent role's git identity is blocked from
creating/updating anything under `.github/workflows/` — confirmed
independently by Software Engineer, Test Engineer, and Systems
Engineer across issues #5 and #24 (all got the identical `refusing to
allow a GitHub App to create or update workflow ... without
'workflows' permission` rejection). On 2026-08-17 the client
(Holosim, issue #24) confirmed this is permanent, not a
to-be-fixed-later gap: the App **doesn't declare** the `workflows`
permission at all, so it can never be granted it. Manual deployment by
a human with repo-admin access is the only route for workflow files on
this project, indefinitely.

**Why:** saves a future agent from re-attempting the push (it will
fail identically every time) or waiting on a permission grant that
isn't coming.

**How to apply:** if a task calls for adding/editing a file under
`.github/workflows/`, stage the change under `docs/ci/` (the existing
convention on this project) and ask a human to copy it over manually —
don't push to `.github/workflows/` directly, and don't escalate up the
agent ladder expecting the permission to eventually be granted.

Related, same issue: `docs/ci/windows-verification.yml` (C++/MSBuild
scaffolding inherited from the project template — `dumpbin`,
`SudokuSolver.exe`, native-desktop `vstest.console.exe`) was **deleted
by client instruction, not rewritten** — TP-501's Windows leg is
satisfied entirely by `.github/workflows/build-and-test.yml`'s
`windows-latest`/`ubuntu-latest` dotnet matrix. Don't recreate or
rewrite `windows-verification.yml` for this project; it has no role in
a .NET codebase.

NFR-501 verified Verified on issue #24, CI run `31997343615` (both
`ubuntu-latest` and `windows-latest` legs green, 118/118 tests, 0
build warnings/errors on each). See [[feedback-platform-verification-schedule]].
