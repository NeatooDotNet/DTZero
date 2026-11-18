# Component Pages: Why and When Not

**Plan #:** 008
**Date:** 2026-11-13
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-7, AC-8
**Status:** Done
**Last Updated:** 2026-11-18
**Plan-review opt-in:** No
**Code-review opt-in:** No (prose only)
**Branch:** `PITCH-008-component-docs`
**PR:** #47

---

## Scope

Give every remaining docs page a statement of why its component exists and when to leave it out, drawing on the DDD map, and act on issues #2, #37 and #39 where the page is already open. It does not restructure the reference content, and it changes no code and no snippet.

---

## Intent

- A reader of any component page learns when not to reach for that component, from the page itself.
- The save chain has one name, "factory methods call factory methods", and "cascade" is only what a database or an ORM calls it.
- Trimming is described with its real cost.

---

## Constraints & Invariants

- No C# is hand-written; no snippet marker is added, moved or removed.
- Nothing is claimed about which trimming gaps closed in 2.0.0: the verdict did not establish it.
- Figures from the private production application are counts only.

---

## Steps

1. Add a "when to leave it out" section to each component page that lacks one.
2. Replace "cascade" as the name of the save chain in the docs and in `src/Design/CLAUDE-DESIGN.md`.
3. State what trimming costs, and when to leave it out.
4. Act on #37 (getting-started wording), #39 (interface factory trimming) and #2 (interface factory documentation).

---

## Acceptance

- [x] Each component page says when to leave its component out, or points to the page that does `[explicit-skip: document]`
- [x] "Cascade" names only a database's or an ORM's behavior `[explicit-skip: document]`
- [x] The trimming page states its cost and when to leave trimming out `[explicit-skip: document]`
- [x] `getting-started.md` says `[Remote]` marks where a call crosses, not where code lives (#37) `[explicit-skip: document]`
- [x] The trimming page's interface factory entry states the measured condition (#39) `[explicit-skip: document]`

---

## Current State (Pre-Flight)

- Pages with no "when not": factory-operations, save-operation, service-injection, serialization, aspnetcore-integration, factory-modes, factory-events, trimming. `interface-factory.md` had "When to Use"; the two reference pages had neither.
- "Cascade" as the chain's name: `factory-operations.md` twice (a third use is the database's cascade delete), `CLAUDE-DESIGN.md` five times.
- `events.md` is a redirect stub for an API removed in v1.5.0.
- No file under `src` other than `CLAUDE-DESIGN.md` is touched, so the PITCH-005 logs stand as the green baseline.

---

## Punchlist

---

## Test Evidence

None: no behavioral bullet.

---

## Gate Record

- Step 5 test review skipped: document-only plan, no behavioral bullets, no source change (recorded in the todo).
- Checked by the orchestrator: snippet open and close markers balance in every docs page; `git diff --stat` shows 81 insertions and 9 deletions over 13 files, each deletion a line this plan rewrote.

---

## Plan Amendments

- 2026-11-18: no re-split was needed; the work was one section per page.
- 2026-11-18: the two reference pages (attributes, interfaces) point to the DDD concepts table and the decision guide instead of repeating why and when-not per entry.
- 2026-11-18, #39: the docs entry is done. The skill's trimming table still says "Not established"; PITCH-006 changes it. The issue's note that `dotnet clean` leaves a stale publish is on the page under "What It Costs". The measurement is from a production application, not a Design test, and the page says so.
- 2026-11-18, #2: `docs/interface-factory.md` documents the feature and predates this todo. Nothing was needed beyond one "leave it out" line.
- 2026-11-18: "MAUI" removed from the client list in `factory-modes.md`, as PITCH-004 did in getting-started.
- 2026-11-18: the skill still uses "cascade" (`references/class-factory.md` heading, `anti-patterns.md`). PITCH-006.

---

## Notes
