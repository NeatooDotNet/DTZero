# Map DDD Concepts to DTZero Components

**Plan #:** 002
**Date:** 2026-11-13
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2
**Status:** Done
**Last Updated:** 2026-11-13
**Plan-review opt-in:** No (a document the user confirms directly)
**Code-review opt-in:** No (no code)
**Branch:** `PITCH-002-ddd-map`
**PR:** —

---

## Scope

Produce `ddd-map.md` in this todo's folder: each DDD concept, the DTZero component that carries it and the reason, or a mark that the concept is not DTZero's job; and each DTZero component with the reason it exists. The user confirms it. It does not rewrite any page; later plans draw on it.

---

## Intent

- Every later page has one confirmed reason per component to write from, so the "why" is not reinvented page by page.
- A DDD reader can find their concept and see where it lands, including the ones DTZero leaves alone.

---

## Framework & Architectural Alignment

- Starts from `docs/VisionAlignment.md` section 3 and the rulings D3, D11 to D15, R-C to R-H.
- DDD terms are used without definition (the repository's documentation rule).
- Every cited example is a file in `src/Design`.

---

## Constraints & Invariants

- No row contradicts a ruling in `docs/VisionAlignment.md` section 0.
- Nothing is written against LazyLoad.

---

## Steps

1. Re-check each "Compiled example" cell of the VisionAlignment table against Design as it stands.
2. Add the reason for each row, and the rows the verdict surfaced: persistence model, polymorphism, public API.
3. List the concepts that are not DTZero's job and where each lives.
4. Add the component table: what would be written without each component, and why it exists.
5. Put the map to the user and record the confirmation.

---

## Acceptance

- [ ] Each DDD concept maps to a component with a reason, or is marked not DTZero's job `[explicit-skip: document]`
- [ ] Each DTZero component has a stated reason `[explicit-skip: document]`
- [ ] Every cited Design example exists `[explicit-skip: document]`
- [ ] The user's confirmation is recorded in the map `[explicit-skip: document]`

---

## Current State (Pre-Flight)

- The VisionAlignment table's four "none" examples now exist: `Contexts/OrderCheckout.cs`, `ReadModels/OrderSummary.cs`, `Services/CustomerDirectory.cs`, and `OpenDraft` on `Order`. `Money` is a plain record and `TaxRate` carries `[Factory]`.
- The table has no reason column and no component-first view.
- `docs/factory-modes.md` lists MAUI among Remote clients; the verdict confirmed only Blazor WebAssembly and WPF. Left for PITCH-005.

---

## Punchlist

---

## Test Evidence

Not applicable: no behavioral bullets. The Step 5 test review is skipped and recorded in the todo's Skipped Steps.

---

## Gate Record

Document-only plan; Step 5 test review skipped (todo, Skipped Steps). The gate was the user's confirmation, given 2026-11-13 in two rounds and recorded in `ddd-map.md` section 4.

---

## Plan Amendments

---

## Notes
