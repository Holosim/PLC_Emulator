---
name: merge-required-member-break
description: A merge can surface a real build break (not a text conflict) when a `required` member is added on one branch and a bare object initializer for that type exists on another — always rebuild/retest after conflict resolution, not just after resolving <<<<<<< markers
metadata:
  type: project
---

Merging `issue-7` (DATA-IN-102, NETWORK JSON schema) to `main` produced
three textual conflicts (all mechanical: additive memory-list entries,
one field-rename collision) but resolving those alone left the build
broken. Issue #9's `ScanEngineTests.cs` — already merged to `main`,
untouched by `issue-7` — called `new NetworkDef()` with no initializer.
`issue-7` made `NetworkDef.Components` a `required` member. Neither
branch alone had a problem; git's conflict resolution doesn't (can't)
catch this because there's no textual overlap — it's a cross-file
semantic break only visible once both changes coexist.

**Fix applied:** updated the call site to
`new NetworkDef { Components = Array.Empty<NetworkComponentConfig>() }`
— no behavior change, just satisfies the new required member. Included
in the merge commit itself (not a separate commit) with an explanation
in the commit message.

**How to apply:** After resolving all `<<<<<<<`/`=======`/`>>>>>>>`
markers in any trunk merge, always do a full fresh `bin`/`obj` wipe +
`dotnet build` + `dotnet test` before committing the merge — never
assume "conflicts resolved" implies "build works." If the build breaks
in a file neither side of the merge touched, that's this pattern:
check for `required` members (or similarly non-optional constructor
requirements) added on one side that a call site on the other side
predates. Fix mechanically and document it in the merge commit message
so Test Engineer's regression pass knows it was intentional, not a
silent behavior change slipped in.

Related: [[feedback-git-merge-fast-forward]],
[[build-toolchain-shallow-clone]] — other merge-mechanics gotchas on
this project.

**2026-08-17, issue #10 (CORE-201/202) merge to `main` — same shape,
via an interface signature change instead of a `required` member:**
issue #11 (CORE-203/204, merged to `main` first) added a `TimeSpan
elapsed` third parameter to `IInstruction.Evaluate` after issue-10's
branch had already been cut with the old 2-arg signature. This time
git *did* flag a real conflict in `SingleTagInstruction.cs` (the
virtual base method both sides touched) — resolved by keeping the
newer 3-arg signature and updating `Xic`/`Xio`/`Ote` to match
(`elapsed` unused/ignored, not time-driven). But `XicXioOteTests.cs`
(new file, only on the `issue-10` side, so no textual overlap at all)
called the old 2-arg `.Evaluate(tags, rungState: ...)` directly on
instrument instances — auto-merged with zero conflict markers, and
would have failed to compile if I'd trusted "no `<<<<<<<` markers left
= done." Caught only by actually running the build after the conflict
markers were gone, per this memory's core rule. **Generalizes the
lesson:** it's not just `required` members — *any* interface/method
signature change landed by a sibling branch that merged first can
silently break a same-side-only file with no conflict markers at all.
Always build+test the *whole* merge, not just the files git flagged.
