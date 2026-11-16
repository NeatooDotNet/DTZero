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
5. Report a check the generator cannot enforce as a build error, where it is now dropped without a word.
6. Enforce checks declared on the interfaces a class auth type implements.

---

## Acceptance

- [x] A check declared on a base interface refuses an operation on a type whose auth interface only inherits it `[integration]`
- [x] The same inherited check is asked by the client through `Can*` and answers as the server does `[integration]`
- [x] A check declared on a base class is enforced when the factory names the derived auth type, with only the derived type registered `[integration]`
- [x] A check reachable through two inheritance paths runs once per operation `[integration]`
- [x] A check a derived auth class overrides runs the derived version `[integration]`
- [x] A check declared in a referenced assembly fails the build with a diagnostic that names it `[unit]`
- [x] A check declared on an interface a class auth type implements is enforced through that class `[integration]`
- [x] A check the auth type cannot call fails the build with a diagnostic, and no call is generated for it `[unit]`
- [x] Every authorization test that existed before the change still passes unchanged `[explicit-skip: covered by the gate logs]`
- [x] Both solutions build and test green `[explicit-skip: covered by the gate logs]`

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

Logs of the final state: `reviews/009-impl3-build.log`, `reviews/009-impl3-test.log`. UnitTests 871 to 889, IntegrationTests 664 to 686 (5 skipped), Design.Tests 187 unchanged, per framework. Integration tests are in `src/Tests/DTZero.IntegrationTests/Authorization/AuthInheritanceTests.cs` on the client/server containers; unit tests are under `src/Tests/DTZero.UnitTests`.

| Acceptance bullet | Test | Tier |
|---|---|---|
| Base-interface check refuses | `InterfaceChain_InheritedCheck_RefusesFetch`, `InterfaceChain_InheritedCheck_RefusesCreate`, `InterfaceChain_InheritedCheck_AllowsFetch` | integration, confirmed |
| Client `Can*` answers as the server | `InterfaceChain_InheritedCheck_ClientCanFetch_AnswersAsServer`, `InterfaceChain_InheritedCheck_ClientCanCreate_AnswersAsServer` | integration, confirmed |
| Base-class check, only the derived type registered | `BaseClass_InheritedCheck_RefusesFetch`, `BaseClass_OnlyTheDerivedAuthTypeIsRegistered_AndTheFactoryWorks` | integration, confirmed |
| Two paths, runs once | `Diamond_CheckRunsOnce_PerFetch`, `Diamond_CheckRunsOnce_PerCreateAndPerCanCreate` | integration, confirmed |
| Override runs the derived version | `Override_WithoutAttribute_RunsTheDerivedVersion`, `Override_WithRestatedAttribute_RunsTheDerivedVersion` | integration, confirmed |
| Referenced-assembly check fails the build | `NF0107_CheckInheritedFromInterfaceInReferencedAssembly_ReportsError`, `NF0107_CheckInheritedFromAbstractBaseClassInReferencedAssembly_ReportsError` (`Diagnostics/NF0107Tests.cs`) | unit, confirmed |
| Interface a class auth type implements | `ImplementedInterface_Check_RefusesFetchAndCreate`, `ImplementedInterface_Checks_Allow_AndEachRunsOncePerOperation`, `ImplementedInterface_ClassRestatedAttribute_Wins` | integration, confirmed |
| Not callable fails the build, no call generated | `NF0108_AttributedInterfaceMethod_ImplementedExplicitly_ReportsError_AndGeneratesNoCall`, `NF0108_AttributeOnExplicitImplementation_ReportsError_AndGeneratesNoCall` (`Diagnostics/NF0108Tests.cs`) | unit, confirmed |

Also added: interface-factory and static-factory coverage, and `InheritedAuthEmissionTests` (an inherited check generates the same factory code as a declared one).

---

## Gate Record

---

## Plan Amendments

- 2026-11-16, Steps 1 and 2: one change in the transform covers class, interface and static factories; no renderer changed. A check found twice by name and parameter types is kept once, the most-derived declaration that carries the attribute. A refusal message now names the auth type the factory uses.
- 2026-11-16, Step 5 added: a check with no source declaration (a base in a referenced assembly) was still dropped with only a debug message. It is now error NF0107.
- 2026-11-16, Step 6 added: the implementer found a third silent case, a class auth type whose checks sit on an interface it implements. Those are now enforced; a check the class cannot be called for (explicit or default implementation) is error NF0108, replacing a CS1061 in generated code.
- 2026-11-16: generated output for every target that existed before the plan was compared with a snapshot and is byte-identical (implementer's report; not re-checked by the orchestrator).
- 2026-11-16: the new tests were not run against the old generator. The PITCH-005 probe observed the old behavior.
- 2026-11-16: not supported and not tested: a generic base auth interface; two unrelated base interfaces declaring the same check. Both are expected to fail at compile time. Recorded in the release notes.
- 2026-11-16: NF0107 and NF0108 are not yet in `docs/` or the skill; PITCH-005 documents them on the authorization page.

---

## Notes
