# Close-Out Audit: PITCH

Written by the orchestrator from the auditor's returned report. The auditor read the arc at `785cb8b`, whose tree equals `origin/PITCH` after PR #49 merged.

## 2026-11-18, first audit: grade C

### Acceptance criteria

| AC | Word | Holds? | Evidence |
|---|---|---|---|
| AC-1 | Required | Yes | `verdict.md` sections 1 to 7; the rulings in section 8 |
| AC-2 | Required | Yes | `ddd-map.md` sections 1 to 3; confirmation in section 4 |
| AC-3 | Required | Yes, with callouts R1 and R3 | README: problem, audience and limits before the first code block; objections in the ruled order |
| AC-4 | Required | Yes for why and when-not; V1 open | Every page under `docs/` states both or points to a page that does. `events.md` and `VisionAlignment.md` exempt |
| AC-5 | Required | Partly | The skill carried the library-level reasons and the three habits. It lacked the per-component leave-outs and the trimming cost the docs gained |
| AC-6 | Required | Yes, with callout R4 | `launch-plan.md` |
| AC-7 | Required | Yes | 198 `cs` and 6 `razor` fences all inside snippet embeds; 28 `csharp` fences all begin `// WRONG`; the 7 new regions each pinned by a Design test; both solutions green |
| AC-8 | Optional | Yes | #2, #37, #39 closed 2026-11-18 |

### Veto-tier

- **V1.** `docs/save-operation.md` headed the save chain "Cascading to Children", against the ruling that "cascade" is dropped as the public word. Every earlier check searched for "cascade", which does not occur in "Cascading".

### Callouts

- **R1 (AC-3, AC-4).** README and `the-problem.md` gave "generated at compile time" as part of the difference from CSLA. The verdict records that CSLA has added a source generator, and ruling R3 names "no base class, any type" as the difference.
- **R2 (AC-4).** The release-notes index still said "No API change" for v2.0.1 and omitted NF0109.
- **R3 (AC-3).** README said "Authorization runs on the server for every call." A type with no authorization has none, and `[AuthorizeFactory<T>]` runs on the tier that runs the operation.
- **R4 (AC-4).** v2.0.1's notes carry a release date and a NuGet link, and 2.0.1 is not published. No row or issue holds the publish step.

### Could not verify

- "Used in production with WPF" (`the-problem.md`, `getting-started.md`). The verdict records WPF as a client DTZero has run with, not production use.
- A recorded ruling for naming the company in the README.
- That each of the roughly 87 regions that predate the arc is pinned by a Design test; only the 7 new ones were traced.
- The PITCH-003 gate was settled by editing the README, not by a ruling.

### Build and test

Both solutions built with 0 errors. UnitTests 901, IntegrationTests 691 passed and 5 skipped, Design.Tests 200 passed and 1 skipped, per framework. No assertion removed, no test skipped or disabled, no reflection added in the arc.

### Container

9 plans of a cap of 11. No open PR into the arc. Misses: Discovery Log entries named no AC; PITCH-002's bullets unticked on a Done plan; one PITCH-008 amendment inaccurate; `todo.md` Last Updated stale; AC-7 unticked.

### Former name

No occurrence in the arc's diff, commit messages or paths, by a case-insensitive scan with no word-boundary rule that was shown to fire on `main`.

## 2026-11-18, fixes on `PITCH-closeout`

- V1: the heading is "Saving the Children". A scan for "cascad" finds only the database's cascade delete and the ORM sentence.
- AC-5: `SKILL.md` gains "When to leave a piece out", one row per component; the skill's trimming reference gains "What it costs".
- R1: the compile-time clause is removed from both pages.
- R2: the index entries match the v2.0.1 page.
- R3: the README sentence now says the server runs a type's authorization rules before it runs the operation.
- R4 and the WPF claim: put to Keith in the arc PR.
- Container misses: fixed. The company-link ruling is recorded in the Discovery Log.
