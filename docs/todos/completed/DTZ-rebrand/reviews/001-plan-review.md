# DTZ-001 Plan Review

**Date:** 2026-11-06
**Reviewer:** plan-reviewer, tight budget
**Verdict:** DECISION NEEDED — the trace log category
**Veto-tier findings:** None

`{old}` stands for the predecessor's one-word name.

## Decision

The library logs under two separate roots today, `Neatoo.{old}` and `{old}.Trace`, and `ServerServices.cs` documents that a filter on the first leaves the second off. The mapping sends both to `DTZero`, so `DTZero.Trace` becomes a child of `DTZero`. That conflicts with the Goal's "behaves exactly as it did". Keeping the old name fails AC-3. No test catches it.

## Callouts

All affect a Required criterion.

| # | Finding | Affects |
|---|---|---|
| 1 | Steps 4 and 5 are in the wrong order, so factory names are mangled before the class rename | AC-1, AC-3 |
| 2 | `TestNamespace.Neatoo` and `TestAssembly.Neatoo` shadowing decoys have no mapping row and would pass vacuously | AC-1 |
| 3 | The header bullet cannot fail, because tests read the constants, and the new values are not stated | AC-2 |
| 4 | The trimming gate omits the harness run, and CI publishes for linux-x64 | AC-1 |
| 5 | The must-stay list has wrong entries, and bare-word `Neatoo` lines split both ways with no check on over-renaming | AC-2 |
| 6 | `NeatooStyle_*` test methods conflict with AC-2 as worded | AC-2 |
| 7 | Mapping rows miss real text: the generator's `Factory` class, regex-escaped forms, the `api/neatoo` client literal, and two lower-case `{old}` forms | AC-1, AC-2, AC-3 |
| 8 | The combination suffix is set in `CombinationInfo.ClassName`, not `TargetClassGenerator` | AC-1, AC-3 |
| 9 | 37 matching non-markdown files under `docs/todos/completed/` are owned by neither plan, and the demo GIF's file name is on no list | AC-3 |
| 10 | Test namespaces would nest under the library namespace, so they stop proving the consumer position | AC-1 |
| 11 | Silent consumer breaks trace to no todo entry beyond zCRM's route | AC-6 |

Also noted: Step 3's "child as it is today" is wrong, because the generator's namespace is today the parent. Step 7 has nothing to regenerate. `NeatooBaseJsonTypeConverter` and `NeatooListBaseJsonTypeConverter` live in the Neatoo repo, so they stay.

## Could not verify

- "About 70" combination classes. They are emitted at build time, and tracked text holds 48 distinct tokens.
- The count of 170 distinct tokens.
- Any compile or runtime outcome, because no build ran.
- That the two converters exist in the Neatoo repo.

## Dispositions

Keith, 2026-11-06: option A for the decision, so the trace category becomes `Trace.DTZero`. All rows accepted.

| # | Disposition |
|---|---|
| 1 | Amend: rename `Remote`-suffixed classes before any `{old}` rule |
| 2 | Amend: decoys become `TestNamespace.DTZero` and `TestAssembly.DTZero` |
| 3 | Amend: new values stated, proven by search so the test count holds |
| 4 | Amend: gate runs publish, `verify-trimmed.sh` and the harness locally |
| 5 | Amend: bare-word lines classified, with every changed line reviewed |
| 6 | Amend: `NeatooStyle_*` methods become `BaseClassStyle_*` |
| 7 | Amend: mapping rows added |
| 8 | Amend: suffix changes at `CombinationInfo.ClassName` |
| 9 | Amend: DTZ-001 excludes `docs/`, and DTZ-003 owns those files and the GIF name |
| 10 | Amend: test namespaces move to a `DTZeroTests.*` root |
| 11 | Park: Parked row widened to cover both consumers |
