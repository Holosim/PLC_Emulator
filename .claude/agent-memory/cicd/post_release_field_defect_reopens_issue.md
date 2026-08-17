---
name: post-release-field-defect-reopens-issue
description: A client-reported field defect against an already-Verified/released RTVM item reuses the same issue and branch, and can trigger a second Release on the same requirement
metadata:
  type: project
---

Issue #27 (DELIV-900) was fully merged, tagged (`v1.0.2`), released
("PLC Emulator v1.0 — Initial Release"), and closed — then the client
commented *on the same closed issue* on 2026-08-17 with a real build
defect found opening the delivered repo in Visual Studio
(`global.json`'s `rollForward: latestFeature` failed to resolve on a
workstation with only newer-major SDKs installed, no `8.x`). Systems
Engineer reopened the flow on the *same issue number and branch*
(`issue-27`), flipped RTVM status `Verified` → `In Implementation` for
the affected row only (not a new RTVM row), Software Engineer fixed it
(`98c6485`, one-line `rollForward: latestMajor`), Test Engineer
independently re-verified, and it came back to CI/CD as a normal
`status:ready-for-commit` merge.

**How this played out for CI/CD specifically:**
- Treated it as a completely normal trunk merge of `issue-27` (branch
  still existed, had the fix + RTVM update commits on top of the
  original release-completing history) — no special handling needed
  for "this branch already merged once before."
- Because the RTVM table had briefly gone to 26/27 Verified (during
  the defect window) and came back to 27/27 with *this* merge, the
  "cut a release when every RTVM row is Verified" rule fired **again**
  — resulted in a second Release, `v1.0.339` ("PLC Emulator v1.0 —
  Windows SDK Build Fix"), on top of the original `v1.0.2` ("Initial
  Release"). Both releases are legitimate and should both stay listed;
  don't delete/supersede the first one — it documents what actually
  shipped and when a defect was later found in it.
- Unlike the `v1.0.2` case, this tag (`v1.0.339`) was created fresh
  pointing directly at the real completing commit, so plain
  `--generate-notes` was safe to use (no stale-tag-undercounting
  problem — see [[release-first-project-release-v1-0-2]] for when that
  problem *does* apply).

**How to apply:** don't assume a closed, already-released issue is
done for good — a client field report can reopen the exact same issue
number/branch for a fix. When it does, evaluate the release trigger
fresh each time (whole-table check, not "did we already release
once") — a second Release for the same MAJOR.MINOR line is expected
and correct if the table completion state genuinely regressed and
recovered.
