# Narrative Docs Carry the Why

**Plan #:** 004
**Date:** 2026-11-13
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-7
**Status:** Done
**Last Updated:** 2026-11-15
**Plan-review opt-in:** No (prose the user reads in the PR)
**Code-review opt-in:** No (no code)
**Branch:** `PITCH-004-narrative-docs`
**PR:** —

---

## Scope

Revise the pages a newcomer reads in order (`the-problem`, `getting-started`, `decision-guide`, `client-server-architecture`) so they argue the ruled case, and give the DDD map and the public API a public page each. It does not touch the per-component pages or the skill.

---

## Intent

- A newcomer who follows the README into the docs meets the same argument, at more length, and not an older one.
- The DDD reader has one page that shows where each concept lands and why.
- The public-API claim has a page that shows the sample and says when to leave it out.

---

## Framework & Architectural Alignment

- `verdict.md` rulings; `ddd-map.md` as confirmed; the README as merged in PITCH-003.
- `.claude/rules/design-snippets.md`: every C# block is an embedded Design region.
- DDD terms are used without definition.

---

## Constraints & Invariants

- No new Design code; only existing regions are embedded.
- No hand-written C#. The old hand-written "traditional approach" block on the problem page is removed, not relabelled.
- "Cascade" is not the public word for the load and save chain.
- No client technology is claimed beyond what is confirmed: Blazor WebAssembly and WPF.

---

## Steps

1. Rewrite `the-problem` around the README's argument.
2. Add `ddd-concepts` from the confirmed map and `public-api` from the PITCH-003 sample.
3. Bring `client-server-architecture`, `decision-guide` and `getting-started` in line: the one idea, roles, and the two new decisions.
4. List the new pages in the README; run mdsnippets and check the diff.

---

## Acceptance

- [x] `the-problem` states the cause, the conceptual-graph point, the one idea, the audience and when not to use it `[explicit-skip: document]`
- [x] A public page maps each DDD concept and each component, with reasons `[explicit-skip: document]`
- [x] A public page shows the public-API sample and says when to leave it out `[explicit-skip: document]`
- [x] The four narrative pages do not use "cascade" for the load and save chain and do not claim MAUI `[explicit-skip: document]`
- [x] Every C# block on the changed pages is an embedded Design region `[explicit-skip: document]`

---

## Current State (Pre-Flight)

- There is no docs landing page; the README's Documentation list is the entry.
- `the-problem` argued "single layer" and listed MAUI, WPF and console as clients. Its CSLA note compared on runtime reflection, which ruling R3 excludes.
- `client-server-architecture` already showed an entity as root and as child; it called the chain a "cascade" once.
- `decision-guide` had no entry for root or child, or for outside callers.

---

## Punchlist

- Moved to the todo's Punchlist: the word "cascade" in a Design source comment and in `factory-operations.md`.

---

## Test Evidence

Not applicable: no behavioral bullets and no source change. The Step 5 test review is skipped and recorded in the todo's Skipped Steps. The last green logs are PITCH-003's (`reviews/003-impl-build.log`, `reviews/003-impl-test.log`); no file under `src` changed since.

---

## Gate Record

Document-only plan. Checked mechanically on 2026-11-15: mdsnippets ran clean with every marker embedded; no file under `src` is in the diff; the narrative pages have no "MAUI", and "cascade" appears once, on `the-problem`, as what an ORM calls the save chain.

---

## Plan Amendments

- 2026-11-15: the stub's "docs landing page" does not exist, so it is not in scope.
- 2026-11-15: two new pages were added (`ddd-concepts`, `public-api`) instead of folding both into `the-problem`, which would have made that page too long to be read first.

---

## Notes
