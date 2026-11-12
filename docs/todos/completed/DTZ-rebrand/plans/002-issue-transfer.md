# Re-create all issues in DTZero

**Plan #:** 002
**Date:** 2026-11-06
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4
**Status:** Done
**Last Updated:** 2026-11-07
**Plan-review opt-in:** No (the user reads every rewritten issue before anything is posted)
**Code-review opt-in:** No (no code changes)
**Branch:** `DTZ-002-issue-transfer`, local only
**PR:** None, per the exception in the todo header

---

## Scope

Re-create all 39 predecessor issues in the public `NeatooDotNet/DTZero`, with their 9 comments, labels and open or closed state, and record a map from old number to new number for DTZ-003. This plan does not edit any citation in the repo, and it does not preserve original dates.

## Intent

The issue backlog survives the predecessor's deletion, and carries no trace of the old name, not even a transfer record or edit history.

## Framework & Architectural Alignment

The text follows the same naming as DTZ-001, so an issue names the same types, paths and routes the code now has. Mentions of the separate Neatoo library stay.

## Constraints & Invariants

- The user approves the rewritten text before anything is posted.
- Issues are created in ascending old-number order, so the new numbers are #1 to #39. Posting stops on any number that differs from the map.
- A `#n` reference to an issue moves to its new number. A reference to a predecessor PR becomes the plain text `old PR n`, so it can never link to the wrong item.
- The only changes are renames, reference updates, and dropping the name of one audit file. The text is otherwise unchanged.
- The rewrite scripts and exports stay out of the repo, because they contain the old name.

## Steps

1. Export the issues and rewrite their text outside the repo.
2. Check the result for the old name and for references outside #1 to #39.
3. Give the rewritten text to the user, and wait for approval.
4. Create the three missing labels.
5. Post the issues, then their comments, then close the ones that were closed.
6. Read everything back from GitHub and compare it with the approved text.
7. Commit the number map to `reviews/`.

## Acceptance

- [x] All 39 issues exist in DTZero as #1 to #39, in old-number order. `[explicit-skip: no code; proved by the read-back]`
- [x] Titles, bodies, comments, labels, state and close reason match the approved text. `[explicit-skip: no code; proved by the read-back]`
- [x] No issue title, body or comment contains the old name. `[explicit-skip: no code; proved by the read-back]`
- [x] The number map covers all 39 issues. `[explicit-skip: no code; proved by comparing the map with the export]`

## Current State (Pre-Flight)

All 39 issues and 9 comments are by the user's account, which is also the posting account. There are no attachments, milestones or assignees, and no links to the predecessor repo. 21 are open, 17 closed as completed and 1 closed as not planned. `critical`, `techdebt` and `skill` are missing from DTZero's labels. References to PRs 96, 127, 139 and 175 were confirmed as PRs.

Six cited paths do not exist in the current tree. One is in the Neatoo repo, and the others were moved or removed before the baseline. They stay as written.

## Punchlist

## Test Evidence

No code changed, so there are no tests to cite, and the build and test suites were not re-run. The proof is a read-back that compares every issue on GitHub with the text the user approved, field by field, for all 39 issues.

| Acceptance bullet | Evidence | Result |
|---|---|---|
| All 39 exist as #1 to #39, in order | `reviews/002-readback.log`, plus the posting script, which stops on any number mismatch | 39 issues, 0 failures |
| Text, labels, state and close reason match | `reviews/002-readback.log` | 21 open, 18 closed, 9 comments, 0 failures |
| No old name | `reviews/002-readback.log` | 0 failures |
| Map covers all 39 | `reviews/002-issue-map.md`, compared with the export and the live predecessor by the reviewer | Old numbers match, new numbers run 1 to 39 |

**Flip:** planting a changed title, labels, state and an old-name body made the read-back report all five failures, in `reviews/002-flip-readback.log`.

## Gate Record

- User approved the rewritten text before posting, 2026-11-07.
- Round 1: APPROVED, two callouts. One tag fixed, one punched. See `reviews/002-test-review.md`.

## Plan Amendments

## Abandonment / Retirement Reason

## Notes

Version numbers from before the rename stay as written, so an issue can read "DTZero 1.9.0" for a release published under the old name.
