---
name: concurrent-cicd-runs-same-day
description: Two CI/CD runs (different issues) can push to main within minutes of each other — always re-fetch and merge origin/main before pushing, even if your local main was current when you started
metadata:
  type: project
---

While merging `issue-7` (this issue, #7) to `main`, a second CI/CD run
finalizing issue #9 (CORE-200 regression sign-off: RTVM `Verified` +
commit SHA + a memory note) pushed to `main` between my `git pull
origin main --ff-only` at the start and my `git push` after resolving
issue-7's merge conflicts. `git push` was rejected (non-fast-forward);
`git fetch origin main && git merge origin/main --no-edit` picked up
the two extra commits cleanly (docs/memory only, no code overlap) and
the push succeeded on retry.

**How to apply:** Don't treat a rejected push as unusual or a sign of
something wrong — with multiple issues open to `agent:cicd`
concurrently, it's expected. Just fetch + merge (or rebase, but plain
merge is simpler and preserves both merge commits' independent
history) and push again. Re-run build/test after this second merge
too, not just after the first — see
[[merge-required-member-break]] for why a second merge can also
introduce a semantic break even without text conflicts, though in this
case it happened to be docs-only and safe.

Also relevant to [[release-versioning-tag-collision-same-day]]: the
concurrent issue #9 run already claimed `v1.0.1` for that day before I
got to my own tag step, which is exactly the collision that memory
describes — confirmed again here, same day, same resolution (verify
ancestry, don't re-tag, cite the merge SHA in the hand-off comment
instead).

**Escalated on issue #12 (2026-08-17):** with 5+ sibling instruction-
group issues (#10, #11, #13, #14, #12 itself) all merging to `main`
same-day, `git push origin main` was rejected **three times in a row**
after resolving issue-12's own merge conflicts — each retry needed a
fresh `git fetch origin && git merge origin/main --no-edit` (each pass
picking up 1-2 more commits from sibling CI/CD runs), then a rebuild/
retest before pushing again. Don't be alarmed by more than one
rejection in a row; keep the fetch→merge→build→test→push loop going
until it succeeds. Also hit [[build-toolchain-shallow-clone]] again
mid-loop: `git merge-base --is-ancestor` returned a false "not an
ancestor" for a tag I knew was upstream, because the working clone was
still shallow — `git fetch --unshallow origin` fixed it before I
concluded anything was actually wrong. Check
`git rev-parse --is-shallow-repository` first if an ancestry check
gives a surprising answer, rather than trusting the negative result.

**Reconfirmed on issue #23 (2026-08-17), 3 rejections in a row:**
merging `issue-23` (NFR-500) to `main` was rejected 3 times by sibling
CI/CD runs finishing issues #17 (UI-002) and #19 (DATA-OUT-301) mid-
loop. Two of the three catch-up merges were clean docs/memory-only
auto-merges; one had a real conflict in
`.claude/agent-memory/test-engineer/MEMORY.md` (two sibling branches
each rewrote the *same* index bullet's baseline test count — 104/104
vs. 105/105 — rather than appending a new bullet) — resolved by
merging the prose into one bullet describing both counts drifted in
parallel, not a pick-a-side, then re-verified the real post-merge
count with `dotnet test` (108/108) rather than trusting either side's
stale number. Full rebuild/retest after every one of the 4 merge
rounds (initial `--no-ff` + 3 catch-ups), per
[[merge-required-member-break]] — worth it here since round 3 (issue
#19) actually did carry real source changes (`TagUpdateSerializer.cs`,
`TcpJsonServer.cs`), not just docs.

**Reconfirmed on issue #15 (2026-08-17), even worse contention:** `git
push origin main` was rejected **four times in a row** merging
`issue-15` (CORE-209) — issues #14, #13, and #12 each landed on `main`
independently mid-loop, plus one more docs/memory-only push. Same
fetch→merge→resolve→rebuild→retest→push loop, just run four times
instead of three; nothing new mechanically, but worth noting the
rejection count keeps climbing as more sibling `type:requirement`
issues (#16-27) queue up behind #10-15 — don't be surprised if it hits
5+ on a future merge. `docs/RTVM.md` conflicts resolved by union each
time (per [[rtvm-merge-conflict-parallel-verification]]); non-RTVM
`MEMORY.md`/memory-file conflicts (software-engineer, systems-engineer,
test-engineer) also resolved by straight union (append both sides'
bullets/paragraphs) every time — this is now a fully settled pattern
for this project, not something to re-derive.
