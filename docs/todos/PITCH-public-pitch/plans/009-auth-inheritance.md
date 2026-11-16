# Inherited Authorization Checks Are Enforced

**Plan #:** 009
**Date:** 2026-11-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-7
**Status:** In Progress
**Last Updated:** 2026-11-16
**Plan-review opt-in:** No (the defect and its cause were located by the PITCH-005 probe)
**Code-review opt-in:** Yes (generator change on the authorization path)
**Branch:** `PITCH-009-auth-inheritance` (cut on top of `PITCH-004-narrative-docs` while PR #44 was open)
**PR:** —

---

## Scope

Make the generator honor authorization checks an auth type inherits. This is the library change the user approved on 2026-11-16 so that the authorization page (PITCH-005) can show a shared base honestly. It changes the generator and adds tests for it. It adds no Design sample and no documentation page; PITCH-005 does both once this lands.

---

## Intent

- A check an auth type inherits is enforced exactly as one it declares. A check is never dropped without a word.
- An auth type is resolved as the type the factory names. A base it inherits from is not a second service the application must register.
- Authorization that works today keeps working, with the same generated behavior.

---

## Framework & Architectural Alignment

- `verdict.md` section 8, fifth ruling: a base authorization class holds the broad read and write check.
- `docs/VisionAlignment.md` rulings on authorization.
- `CLAUDE.md` Planning Guidelines: generator tests, and client/server container tests for anything that crosses.

---

## Constraints & Invariants

- No existing test is changed or weakened. An existing test that fails after the change is reported before it is touched.
- No reflection.
- The generator stays on `netstandard2.0`.
- A check reachable by more than one inheritance path runs once.
- A check overridden or re-implemented in a derived auth class runs the derived version.
- No new public API and no new attribute.

---

## Steps

1. Read the inherited authorization methods of an auth interface, through every interface it inherits.
2. Resolve every authorization call through the auth type the factory names, wherever the method is declared.
3. Pin both with tests in the main solution: generated-code behavior on the client/server containers, for interface inheritance and for a base class.
4. Bump the version to 2.0.1 and write its release notes.

---

## Acceptance

- [ ] A check declared on a base interface refuses an operation on a type whose auth interface only inherits it `[integration]`
- [ ] The same inherited check is asked by the client through `Can*` and answers as the server does `[integration]`
- [ ] A check declared on a base class is enforced when the factory names the derived auth type, with only the derived type registered `[integration]`
- [ ] A check reachable through two inheritance paths runs once per operation `[integration]`
- [ ] A check a derived auth class overrides runs the derived version `[integration]`
- [ ] Every authorization test that existed before the change still passes unchanged `[explicit-skip: covered by the gate logs]`
- [ ] Both solutions build and test green `[explicit-skip: covered by the gate logs]`

---

## Current State (Pre-Flight)

- Baseline logs: `reviews/003-impl-build.log`, `reviews/003-impl-test.log`; no file under `src` has changed on this branch since.
- `GetMethodsRecursive` in `src/Generator/FactoryGenerator.Transform.cs` walks `BaseType` only. An interface has no base type, so methods from inherited interfaces are never seen.
- An auth method's owner is recorded as its declaring type (`MethodInfo.ClassName = methodSymbol.ContainingType.Name`, `Types.cs`), and the renderer resolves the service by that name (`ClassFactoryRenderer.cs`). A method on an abstract base is therefore resolved as the base.
- PITCH-005 probe results: base interface, check silently dropped; abstract base class, server fails at startup; restated on one interface, works; concrete base as its own auth type, works as a separate service.

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
