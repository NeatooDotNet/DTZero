# Prove packages against Neatoo, then publish

**Plan #:** 005
**Date:** 2026-11-06
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-6, AC-7
**Status:** In Progress
**Last Updated:** 2026-11-07
**Plan-review opt-in:** No (the user confirms the clean push and the publish before each happens)
**Code-review opt-in:** No (the only code change is the version)
**Branch:** `DTZ-005-package-proof-publish`, local only
**PR:** None, per the exception in the todo header

---

## Scope

Set the version to 2.0.0, write the release note, pack DTZero locally, and run Neatoo's tests against those packages in a throwaway clone. Then remove PR numbers from the todo files, build one orphan commit from the finished tree, push it to `main` on the public repo as the first CI run, and publish 2.0.0 of both packages from a `v2.0.0` tag. A later commit, also clean, closes this todo. The user then deletes `DTZero-work`. This plan does not migrate Neatoo for real.

## Intent

The packages are proven against their largest consumer before anything goes public, and the public repo starts from one commit with no trace of the old name.

## Framework & Architectural Alignment

The release follows `CLAUDE.md`: version in `src/Directory.Build.props`, notes in `docs/release-notes/`, and a `v*` tag that triggers the publish job in `build.yml`.

## Constraints & Invariants

- The real Neatoo repo is not touched. The proof runs in a clone in the scratchpad.
- Local 2.0.0 packages use a separate NuGet cache, so they cannot shadow the real release.
- The release note uses no old name, so it lists the new names and the migration table moves to the Neatoo migration todo.
- The user confirms before the clean push, the tag and the closing commit's push, because none of them can be taken back.
- The closing commit is built in a fresh clone of the public repo, never on the local history, and its ancestry is checked before the push: the public `main` is its parent, and the commit count is exactly one more.
- The clean commit is built from the finished tree, with no parent, and its tree is checked for the old name first.
- Every push names the public URL and an exact ref, such as `<commit>:refs/heads/main`, and the tag names that commit. `--all`, `--tags`, `--force` and a branch name are never used, because they could upload the old history.
- The release date is set to the day of the push, in the release note and its index rows, before the commit is built.

## Steps

1. Set the version, and write the release note and index rows.
2. Pack the way CI does, and inspect the packages.
3. Record a baseline of Neatoo's tests in a clean clone.
4. Point a throwaway clone at the local packages, then build and test it.
5. Run both DTZero solutions and the trimming check at 2.0.0.
6. Remove PR numbers from the todo files, and run the close-out audit.
7. Build the orphan commit, and confirm with the user.
8. Push to public `main`, and watch CI.
9. With the user's confirmation and the Trusted Publishing policy in place, tag `v2.0.0`, and confirm both packages on NuGet.
10. Close the todo in a later commit, built in a fresh clone of the public repo: record the results, move the folder to `docs/todos/completed/`, check the commit and its ancestry, and confirm with the user before the push.

## Acceptance

- [x] Both DTZero solutions build at 2.0.0, and test counts equal the baseline. `[unit]`
- [x] The trimming check passes at 2.0.0. `[integration]`
- [x] Both packages carry the right ID, version, description and generator, with no old name. `[explicit-skip: no behavior; proved by the package inspection]`
- [x] Neatoo's tests pass against the local packages, with the same counts as its baseline. `[integration]`
- [ ] The public repo's first commit has no parent, and no commit in its history has the old name. `[explicit-skip: no behavior; proved by a clone of the public repo]`
- [ ] CI is green on the public repo. `[integration]`
- [ ] 2.0.0 of both packages is on NuGet. `[explicit-skip: external; proved by the package pages]`

## Current State (Pre-Flight)

**Packing:** `dotnet pack` fails with NU5026 unless Release is built first. CI builds, then packs with `--no-build`, and that works.

**NuGet publishing:** the predecessor repo held a stored key. The public repo uses Trusted Publishing instead, so it stores no key. See Plan Amendments.

**Neatoo:** it pins 1.9.0, so the proof also covers everything in 1.10.x. It has two solutions, `src/Neatoo.sln` and `src/Design/Design.sln`.

**Migration findings for the Neatoo todo:** four Neatoo files under its old nested namespace relied on sitting inside `Neatoo`, so each needs a `using Neatoo;` once that namespace becomes `DTZero.Internal`. One test used a partly qualified name. A local helper named after the old library can keep or change its name.

## Punchlist

## Test Evidence

| Acceptance bullet | Evidence | Result |
|---|---|---|
| DTZero builds and tests at 2.0.0 | `reviews/005-build.log`, `005-design-build.log`, `005-test.log`, `005-design-test.log` | 0 errors, 3,430 passed, 12 skipped, 0 failed, equal to the baseline |
| Trimming check | `reviews/005-trim-publish.log`, `005-trim-verify.log`, `005-trim-harness.log` | Publish, verifier and harness pass. The harness ran in WSL Ubuntu-24.04 |
| Packages | `reviews/005-packages.log` | IDs, 2.0.0, "Zero DTOs" descriptions, generator under `analyzers/dotnet/cs`, no old name in names, nuspec or DLL bytes |
| Neatoo proof | `reviews/005-neatoo.log` | `Neatoo.sln` 2,030 passed and 2 skipped, `Design.sln` 235 passed, 0 failed. Counts equal a clean clone's baseline. Unit test output holds the packed `DTZero.dll` by hash |
| Public history, CI, NuGet | Pending the user's confirmation | Not yet run |

**Neatoo adaptation:** names came from DTZero's own source. Four files gained `using Neatoo;` and one test lost a partial qualifier. No Neatoo test or assertion changed otherwise.

**Not proven by the Neatoo run:** each Neatoo test project runs one framework: 55 tests ran on net10.0, and the other 2,210 on net9.0. The run did not exercise a trimmed publish. The local pack came from an uncommitted tree that differed from its commit, in anything packed, only by the version, and CI packs the release itself.

## Gate Record

- Round 1, pre-publish: CONCERNS. One veto and three callouts, fixed, amended or with the user.
- Round 2, pre-publish: CONCERNS, last round. No veto. One route to the old history through the closing commit, closed after the round and not re-reviewed. See `reviews/005-test-review.md`.

## Plan Amendments

- 2026-11-07, after the gate: the user chose NuGet Trusted Publishing over a stored key. The publish job now has `id-token: write`, and `NuGet/login@v1` swaps GitHub's OIDC token for a one-hour key. `NUGET_USER` in the public repo's `nuget` environment holds the nuget.org profile name. The user creates the policy on nuget.org for repo `NeatooDotNet/DTZero`, workflow `build.yml` and environment `nuget`. The change was not re-reviewed, and the push to `main` does not run the publish job, so the tag is its first run. A login failure stops the job before anything is published.

## Abandonment / Retirement Reason

## Notes
