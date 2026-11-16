# Authorization and Trust

**Plan #:** 005
**Date:** 2026-11-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-7
**Status:** In Progress
**Last Updated:** 2026-11-15
**Plan-review opt-in:** No
**Code-review opt-in:** No (Design samples only; the test review covers them)
**Branch:** `PITCH-005-authorization` (cut on top of `PITCH-004-narrative-docs` while PR #44 was open)
**PR:** —

---

## Scope

Make the authorization page carry the verdict's fifth ruling: authorization is a headline strength. Add the Design samples the page needs to say so honestly, and a section on trust. It does not touch the other component pages (PITCH-008) or the skill, and it changes no library or generator code.

---

## Intent

- A reader sees that authorization is one class with injected dependencies, shared across types through a base, enforced on every path into the server.
- A reader is told plainly that what the client sends is input, and sees a rule that decides from the stored record.
- Each claim is one a Design test pins.

---

## Framework & Architectural Alignment

- `verdict.md` section 8, fifth ruling: examples take the user by constructor; a base authorization class holds the broad read and write check.
- `docs/VisionAlignment.md` rulings on authorization; `.claude/rules/design-snippets.md`.
- The public API sample from PITCH-003.

---

## Constraints & Invariants

- No library or generator change. If a sample cannot be written without one, the work stops and the user decides.
- No existing test is changed or weakened.
- No reflection.
- The existing target-rule sample keeps working; the trust sample is added beside it.

---

## Steps

1. Add a shared authorization base to Design and an auth class that uses it, pinned by tests.
2. Reach an authorized aggregate through a public endpoint, and pin that the rule refuses there too.
3. Add a write rule that decides from the stored record, and pin it with a test that sends a tampered object.
4. Replace "cascade" as the name of the save chain in the Design source comment.
5. Revise `docs/authorization.md`: why, the base, trust, and when to leave each approach out.

---

## Acceptance

- [ ] A broad check declared once on a shared base refuses an operation on a type whose own auth class does not restate it `[integration]`
- [ ] A caller refused by an aggregate's authorization is refused through a public endpoint that uses the same factory `[integration]`
- [ ] A write is refused from the stored record's state even when the object the client sends claims otherwise `[integration]`
- [ ] `docs/authorization.md` opens with why authorization lives on the factory, shows the base, has a trust section, and says when to leave each approach out `[explicit-skip: document]`
- [ ] Both solutions build and test green `[explicit-skip: covered by the gate logs]`

---

## Current State (Pre-Flight)

- Baseline: PITCH-003's logs (`reviews/003-impl-build.log`, `reviews/003-impl-test.log`); no file under `src` has changed since. Design.Tests: 187 passed, 1 skipped per framework.
- `AuthorizedOrderAuth` takes `ICurrentUser` by constructor. No Design sample or test has an auth interface or class that inherits its methods.
- `ParamAuthOrderAuth.CanWrite(IParamAuthOrder target)` refuses a write when the sent object's `Status` is `Locked`. The status it reads is the one the client sent.
- The public endpoints use `Order`, which has no authorization.

---

## Punchlist

---

## Test Evidence

---

## Gate Record

---

## Plan Amendments

---

## Notes
