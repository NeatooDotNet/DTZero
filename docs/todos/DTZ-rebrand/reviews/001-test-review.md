# DTZ-001 Test Review

**Date:** 2026-11-06
**Reviewer:** test-reviewer, tight budget

## Round 1 — CONCERNS

**Logs:** build, test and trimming logs are green and match the baseline: 3,430 passed, 12 skipped, 0 failed, with the same warnings.

**Veto:** the "Generated factories register and resolve" row cited a unit-tier test and an unnamed suite. Fixed by citing `Order_SavesAndFetches_OverHttp` and `ExecCtorEntity_IsAutoRegisteredByGenerator_ClientSide`. No test changed.

**Weakening check:** the reviewer mapped brand tokens to one placeholder and diffed all 280 renamed or modified test files against baseline. 274 are identical. The other 6 are expected: two assertion changes, two comments, the `GetType` string in the test helper, and the planned `CombinationInfo` suffix. Test attribute and assertion counts match baseline.

**Namespaces:** test declaration roots map exactly to `DTZeroTests`, apart from the three `DTZero.AspNetCore.TestLibrary` files, which were nested the same way at baseline.

**Tech-debt:** a README under `src` still documents old names. Punched to the todo.

## Round 2 — APPROVED

The veto is closed. The source tree is unchanged since the logs were taken. All four flips are confirmed from their logs.

**Left open, no action needed:**

- Flip 4 proved four of the five decoys. The fifth, `EventPreservation_EmittedNamespaceIsShadowed`, failed for a different reason, because `EventPreservationRenderer` emits no bare root reference for that decoy to catch. The test calls itself a smoke test, and it has the same shape as baseline, so the rename did not cause this.
- The harness log does not show that it ran in WSL. The plan states it did.

## Punched

- Stale library names in markdown under `src`.

## Parked

None.
