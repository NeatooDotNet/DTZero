# DTZ-003 Test Review

**Date:** 2026-11-07
**Reviewer:** test-reviewer, tight budget

## Round 1 — CONCERNS

**Logs:** both builds passed with the baseline's 2 warnings. Tests: 3,430 passed, 12 skipped, 0 failed, equal to baseline. No assertion changed.

**Corruption:** none across all 503 changed files. No Neatoo-defined name was rewritten, among the 28 suspects checked.

| # | Finding | Disposition |
|---|---|---|
| V1 | 18 citations were missed, because a letter, `-` or `/` came before the `#`. | Fixed. |
| V2 | A sample order number, `#123`, was remapped as if it were an issue. | Restored. |
| V3 | The evidence overclaimed the citation check, whose flip could not find a missed form, and two Current State claims were false. | Corrected. A scan with no boundary rule was added. |
| C1 | The NuGet removal took a blank line from the release notes. | Restored in 40 files. |
| C2 | A sentence in `v1.5.0.md` lost its meaning when its link was cut. | Fixed. |
| C3 | PR numbers in this todo's own files have no owner. | Dismissed: DTZ-005's Scope owns it. The reviewer withdrew this in Round 2. |

## Round 2 — CONCERNS, last round

V1, V2 and all three callouts are closed. No citation is wrong or hidden: the reviewer's own scan of every `#n` found the same 29 hits, and at the pre-fix commit it reports exactly the 18 misses.

**Veto, one left:** the residual scan used a greedy match, so a second `#n` within its window hid the first. Its claim to list every `#n` was false, though no live case was affected.

**Closed after Round 2:** the scan now matches each `#n` alone and reads the preceding text separately. It reports 29 hits and 0 unexplained, and its flip reports all four planted forms, including a pair close together. No citation changed, so the reviewer's result holds. This fix was not re-reviewed, because Round 2 is the last round.

## Tech-Debt

| Item | Disposition |
|---|---|
| `v1.5.0.md` linked a plan at a path that never held it | Fixed: the link now names `docs/plans/completed/`. |
| Removed API names now read with the new prefix, though they never shipped under it | Dismissed: history renamed by decision. |
| New `#1` to `#39` citations share a number range with list items | Dismissed: the context tells them apart. |
| Quoted commit subjects were altered | Dismissed: they had to lose the old `#n` form. |

## Could Not Verify

- The docs renderer. The tree has no site configuration.
- Link and anchor checks cover current docs, not `docs/todos/`, `docs/plans/` or individual release notes.
- Citation meaning was sampled, apart from `.cs` files and `docs/VisionAlignment.md`, which were read in full.

## Theoretical

Not triaged. The citation check compares against `DTZ`, so it stops being repeatable once this plan merges.

## Parked

None.
