---
name: workflows-permission-blocker
description: Pushing any file under .github/workflows/ is rejected for this pipeline's GitHub App token — stage new/changed workflow YAML under docs/ci/ instead
metadata:
  type: platform
---

Confirmed empirically on issue #5 (2026-08-16): `git push` of a new
file at `.github/workflows/build-and-test.yml` was rejected —
`refusing to allow a GitHub App to create or update workflow
".github/workflows/build-and-test.yml" without "workflows"
permission`. This is a real, repo-wide constraint on the relay
GitHub App installation, not project-specific and not limited to
`windows-verification.yml`'s own claim about itself — it blocks
*any* agent role from creating or editing *any* file under
`.github/workflows/`, full stop.

**How to apply:** if a task calls for a new or modified GitHub Actions
workflow, write/edit the YAML under `docs/ci/<name>.yml` instead (same
convention `docs/ci/windows-verification.yml` already uses), with a
short header comment stating it's not active where it sits and needs
a human (or a `workflows`-scoped token) to copy it into
`.github/workflows/`. Don't spend a retry attempting the direct push —
the rejection is deterministic, not transient. Existing files already
under `.github/workflows/` (added before this restriction was in
effect, or by a human) can still be read normally; this only blocks
writes.
