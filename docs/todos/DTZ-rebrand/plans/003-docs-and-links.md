# Rebrand docs and remove old links

**Plan #:** 003
**Date:** 2026-11-06
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-3, AC-4, AC-5
**Status:** Done
**Last Updated:** 2026-11-07
**Plan-review opt-in:** No (mechanical, local and reversible, and the gate checks it independently)
**Code-review opt-in:** No (no behavior change; code edits are comments, skip reasons and package descriptions)
**Branch:** `DTZ-003-docs-and-links`, local only
**PR:** None, per the exception in the todo header

---

## Scope

Rebrand all prose, including the carried-over todos, plans and release notes, along with CLAUDE.md, agents and rules. Rewrite issue citations from the DTZ-002 map, turn PR references into plain text, remove every link that would break when the predecessor is deleted, regenerate snippets, and lead the README and NuGet descriptions with "Zero DTOs". This plan does not change code behavior, and it leaves `skills/` to DTZ-004, apart from what mdsnippets regenerates.

## Intent

A reader finds no old name and no dead link, and every issue citation lands on the right issue in DTZero.

## Framework & Architectural Alignment

Names follow DTZ-001. `src/Design` stays the single snippet source, so embedded C# comes from mdsnippets and is not edited by hand.

## Constraints & Invariants

- Names that mean the separate Neatoo library stay, including bare `Neatoo`.
- Files in other repos keep their real names there, so they are described in words rather than renamed to a path that does not exist.
- `#n` is a citation only from 48 up. Below that, these files use it for numbered list items such as "Risk #2".
- A predecessor PR becomes `old PR n`, so it can never link to the wrong item.
- Two-word "remote factory" stays. It means a factory method marked `[Remote]`, not the old name.
- `RFSK` stays, by the user's ruling.
- Lowercase `neatoo` in historical file names and region names stays, because no criterion covers it.
- The rewrite script stays out of the repo, because it contains the old name.

## Steps

1. Rewrite links, then citations, then names, across all tracked text outside `skills/`.
2. Rename the paths that carry the old name.
3. Lead the README and both package descriptions with "Zero DTOs".
4. Regenerate snippets and confirm nothing was swallowed.
5. Check relative links and anchors.
6. Run the build and test suites for both solutions.
7. Verify citations independently of the rewrite, and prove that check can fail.

## Acceptance

- [x] No file or path outside `skills/` contains the old name. `[explicit-skip: no behavior; proved by the search log]`
- [x] Every predecessor issue citation uses its new number, and every PR reference is plain text. `[explicit-skip: no behavior; proved by the citation log]`
- [x] No link points at the predecessor repo or a package version that does not exist under the new ID. `[explicit-skip: no behavior; proved by the search log]`
- [x] Relative links and anchors in the current docs resolve. `[explicit-skip: no behavior; proved by the link log]`
- [x] The README and both package descriptions lead with "Zero DTOs". `[explicit-skip: no behavior; proved by the search log]`
- [x] Both solutions build, and test counts equal the baseline. `[unit]`

## Current State (Pre-Flight)

**Surface:** 496 text files changed and 5 paths moved. That is the two root files and three agent files. The docs, `docs/todos/completed/` evidence files and 70 code comments or skip reasons are all in that count.

**Citations:** all 19 issue and PR URLs resolve to known numbers. From 48 up, `#n` is a predecessor issue or PR, with three exceptions: Bootstrap's `#37011`, which is out of scope, a sample order number, `Fetching order #123`, and citations of other repos. `Neatoo#103`, `Neatoo#95` and `dotnet/runtime#n` stay as written.

**Links:** `blob` and `tree` links move to the DTZero URL, because those paths exist there. `releases/tag` links are removed, because tags were not carried over. NuGet links for old versions are removed, because those versions do not exist under the new ID. The release notes template keeps its link.

**Old file names in other repos:** the Neatoo audit file, a Neatoo completed todo and a landing screen proposal are now described in words.

**Snippets:** mdsnippets changed only two skill files, so the rewrite already matched the embedded code. Its log shows no missing snippets.

## Punchlist

- [x] Update stale library names in markdown under src · `src/**/*.md` · done when no old names remain · AC-2

## Test Evidence

The rewrite touched no behavior, so most bullets are proved by searches rather than tests. Two checks cover citations. The citation check compares references file by file against the arc, but it shares the rewrite's number map, threshold and boundary rule, so it cannot find a form both of them miss. The residual scan has no boundary rule: it lists every `#n` above 39 whatever comes before it, and fails on anything not on a short allow list.

| Acceptance bullet | Evidence | Result |
|---|---|---|
| No old name outside `skills/` | `reviews/003-search.log`, `reviews/003-citations.log` | 0 files, 0 paths |
| Citations use new numbers, PRs are plain text | `reviews/003-citations.log`, `reviews/003-residual.log` | 1,029 files, 716 references, 0 problems. 29 hits above 39, 0 unexplained |
| No predecessor or dead package links | `reviews/003-search.log` | 0 predecessor links, 0 old package links, 0 tag links |
| Links and anchors resolve | `reviews/003-links.log` | 0 problems |
| "Zero DTOs" leads | `reviews/003-search.log` | README and both descriptions |
| Builds and test counts | `reviews/003-build.log`, `003-design-build.log`, `003-test.log`, `003-design-test.log` | 0 errors, 3,430 passed, 12 skipped, 0 failed, equal to the baseline |

**Flip:** planting a wrong citation and an old name made the citation check report both, in `reviews/003-flip-citations.log`. Planting two forms the rewrite missed and two close together made the residual scan report all four, in `reviews/003-flip-residual.log`. Before the Round 1 fixes, that scan also found all 18 real misses.

**Trimming not re-run:** code edits are comments, skip reasons and package descriptions, so the feature switch and the trimmed output are unaffected. DTZ-005 packs and exercises the packages.

**Test changes:** no assertion changed. Comments cite new issue numbers. The skip reason in `SaveTests` now says `#11`, and the one in `RelayTimingTests` says `old PR 69`, both justified by AC-4.

## Gate Record

- Round 1: CONCERNS. Three vetoes and three callouts, fixed or dismissed.
- Round 2: CONCERNS, last round. One veto left, a false claim about the residual scan, with no citation affected. Fixed after the round, not re-reviewed. See `reviews/003-test-review.md`.

## Plan Amendments

- **2026-11-07, Round 1:** the review found 18 citations the rewrite missed, because a letter, `-` or `/` came before the `#`, and one sample order number wrongly remapped. Both are fixed by hand. The citation check could not see them, so the residual scan was added, and the false evidence claims above were corrected. The Round 1 fixes also restored the blank line lost from the release notes and repaired a sentence in `v1.5.0.md`. All of these are markdown, so the build and test logs stand.

## Abandonment / Retirement Reason

## Notes
