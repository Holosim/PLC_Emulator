---
name: release-first-project-release-v1-0-2
description: PLC_Emulator's first-ever GitHub Release (v1.0.2, issue #27/DELIV-900) — how to cut a Release when the day's BUILD tag already exists pointing at an earlier, non-completing commit
metadata:
  type: project
---

Issue #27 (RTVM-DELIV-900) merge to `main` (`ecbc190`) brought every row
in `docs/RTVM.md` (all 27 requirements) to `Verified` — the release
trigger per [[release-versioning-scheme]]'s "## Versioning and
releases" section. First release ever cut on this project.

**The wrinkle:** by the time this merge landed, `v1.0.2` (today's
day-based BUILD number, per [[release-versioning-tag-collision-same-day]])
had *already* been created and pushed hours earlier by a sibling
same-day merge, pointing at commit `2e107fa` (CORE-203/204, mid-day,
nowhere near RTVM completion). `gh release create <tag>` **uses the
existing tag's commit as-is** if the tag already exists — `--target`
is silently ignored in that case (confirmed via `gh release create
--help`: "If a matching git tag does not yet exist, one will
automatically get created... Use --target to point to a different
[commit] for the *automatic tag creation*" — i.e. `--target` only
matters when the tag doesn't exist yet).

**Resolution used:** did not force-move the existing `v1.0.2` tag
(consistent with [[release-versioning-tag-collision-same-day]]'s
"never force-move a pushed tag" rule — this generalizes to Releases,
not just tags). Instead:
1. Confirmed `git merge-base --is-ancestor v1.0.2 HEAD` still held
   against the actual completing commit (`ecbc190`) — the tag's
   commit, though not the tip, is still a real ancestor, so the
   version number itself isn't wrong, just coarse-grained.
2. Used `--notes-file` with **hand-written** release notes (not
   `--generate-notes`) listing all 27 `docs/RTVM.md` requirement IDs
   with plain-language descriptions, per the alternate "## Versioning"
   section's instruction to pull RTVM IDs/descriptions rather than
   trust auto-generated commit-range notes. This mattered here
   specifically: `--generate-notes` diffs from the tag's actual commit
   (`2e107fa`) which would have **silently undercounted** — missing
   CORE-208 through DELIV-900, everything that shipped later the same
   day. Auto-notes are only safe when the tag points at the completing
   commit itself.
3. Explicitly cited the real completing commit SHA (`ecbc190`) inside
   the release notes body and the hand-off comment, so the precise
   pointer survives even though the tag ref itself is day-granular by
   design.

**How to apply:** Any time a release-triggering merge lands on a day
where that day's tag already exists (increasingly likely as more
`type:requirement` issues land per day) — don't try to repoint the
tag; check ancestry, write the release notes by hand from
`docs/RTVM.md` (not `--generate-notes`), and cite the actual
completing commit SHA explicitly in both the release body and the
hand-off comment.

**Label convention used for hand-off:** RTVM's DELIV-900 row was
already `Verified` via SE's fast-path commit (`5b1b4de`) *before* this
branch merge, but `git log origin/main..origin/issue-27` was
non-empty (2 real commits, memory-only) — the established
[[second-ready-for-commit-no-new-merge]] pattern: real merge, RTVM
already correct → hand off with **no `status:*` label** (omit step c),
straight to `agent:systems-engineer`. Thirteenth+ confirmation of this
exact shape.
