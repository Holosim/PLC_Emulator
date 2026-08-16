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
