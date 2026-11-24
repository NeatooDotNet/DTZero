## Test Review — PITCH-009 — [2026-11-16] — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED, both solutions, 0 errors (2 WASM native-reference warnings from the OrderEntry.BlazorClient example, outside this plan's code) — `reviews/009-impl3-build.log`; tests 0 failed: UnitTests 889/889, IntegrationTests 686 passed / 5 skipped, Design.Tests 187 passed / 1 skipped, the same on net9.0 and net10.0 — `reviews/009-impl3-test.log`. The skips are the 3 ShowcasePerformance tests and the 2 RelayTiming tests (integration) and `AuthorizedSave_NewAndDeleted_ReturnsNull` (Design); the skip counts match the baseline. The count increases (+18 unit, +22 integration) equal the new `[Fact]` counts (NF0107 6, NF0108 5, InheritedAuthEmission 7; AuthInheritanceTests 22).

### Veto-tier
None.

Every Required bullet is pinned at its declared tier by a test that asserts the behavior:
- Base-interface check refuses `[integration]`: `InterfaceChain_InheritedCheck_RefusesFetch` and `_RefusesCreate` assert a null result and exactly one `ChainedCheckAuth.CanReadKey:denied` entry in the server log. The target's auth interface declares nothing (`IChainedCheckAuth : IChainedCheckMiddle : IChainedCheckRoot`).
- Client `Can*` answers as the server does `[integration]`: `_ClientCanFetch_AnswersAsServer` checks the client's answer against the server factory's answer for both a denied and an allowed key, asserts 2 remote calls, and asserts the check never ran on the client. `_ClientCanCreate_` does the same for Create.
- Base-class check, only the derived type registered `[integration]`: `BaseClass_InheritedCheck_RefusesFetch`. `BaseClass_OnlyTheDerivedAuthTypeIsRegistered_AndTheFactoryWorks` asserts that `GetService<BaseClassCheckAuth>()` returns null on both tiers, that the derived type resolves, and that the factory still works.
- Two paths, runs once `[integration]`: `Diamond_CheckRunsOnce_PerFetch` and `_PerCreateAndPerCanCreate` assert exactly one log entry per unique key, read from the tier where each operation runs. `InheritedAuthEmissionTests.InterfaceDiamond_*` also pins that the generated factory is byte-equal to the one for a declared check, so a remote Fetch runs no check on the client side.
- Override runs the derived version `[integration]`: `Override_WithoutAttribute_` and `Override_WithRestatedAttribute_RunsTheDerivedVersion`. In both, the base version allows every key, so the test can only pass if the derived version ran. Each asserts the derived log entry is present and the base entry is absent.
- Referenced-assembly check fails the build `[unit]`: `NF0107_CheckInheritedFromInterfaceInReferencedAssembly_ReportsError` asserts a single Error with the full message naming `CanRead` and `SharedAuth.ISharedReadAuth`, located at `AuthorizeFactory<IAuth>`. `_AbstractBaseClass_` does the same for an abstract base. The declarations are emitted to a metadata image, so they really are outside the compilation's source.
- Interface a class auth type implements `[integration]`: `ImplementedInterface_Check_RefusesFetchAndCreate` (null results and log entries on both tiers), `_Checks_Allow_AndEachRunsOncePerOperation` (the exact set of checks per operation), `_ClassRestatedAttribute_Wins`.
- Not callable fails the build, no call generated `[unit]`: both NF0108 tests assert a single Error, its location, `LocalCreate` present and `CanRead` absent in the generated factory. `NF0108_AttributedInterfaceMethod_ImplementedImplicitly_NoDiagnostic` is the positive control: in the callable case it finds `auth.CanRead(key)` in the generated factory, which shows the `DoesNotContain` check is not vacuous.
- Existing tests unchanged; both solutions green (explicit-skip, logs): confirmed above and under Sacred below.

**Sacred tests:** `git diff --name-status -M f057499..HEAD -- src` shows one modified test file, `DiagnosticTestHelper.cs`, and it is +62/−0. It only adds `RunGeneratorWithReferences` and `EmitReference`; `RunGenerator` and `BuildReferences` are untouched. Every other test file is new (A). No test was deleted or renamed, and no existing assertion changed.

### Callouts (≤ 5 Optional)
None.

### Tech-debt (untiered, ≤ 5, one line each)
- `InheritedAuthEmissionTests` does not check that the output compilation is error-free, because the unit harness does not reference System.Text.Json (the test's own comment says so). Compilation of the inherited shapes is pinned only by the integration project's build.

### Could not verify
- The amendment's claim that generated output for pre-existing targets is byte-identical to a snapshot. No snapshot artifact is in the diff. The behavioral cover is that all pre-existing tests pass unchanged.

### Theoretical (not triaged)
- NF0108 raised from the `else` branch (an auth type in a referenced assembly that implements an attributed check explicitly) has no test. It is the same reporting helper the tested branch uses.
- NF0107 raised from the "No AttributeSyntax" branch (method syntax in source, attribute syntax not) has no test. No ordinary input seems to reach it.
- The NF0107/NF0108 report location falls back to the factory type when `[AuthorizeFactory<T>]` has no source syntax. That fallback has no test.

### Read report
- Read beyond the brief: the generated factory files for DiamondCheckTarget, searched under `obj` to settle whether the check also runs on the client. They are not emitted to disk, so this was settled from `InheritedAuthEmissionTests` instead.
- Named but unused: none.

---

## Test Review — PITCH-009 — [2026-11-16] — Round 2

Scope: `git diff bfd7434..6259450 -- src` (Step 7, a restated check combines operations).

**Verdict: APPROVED**
**Logs:** build PASSED, both solutions, 0 errors (the same 2 WASM native-reference warnings from the OrderEntry.BlazorClient example) — `reviews/009-impl4-build.log`; tests 0 failed: UnitTests 895/895, IntegrationTests 690 passed / 5 skipped, Design.Tests 187 passed / 1 skipped, the same on net9.0 and net10.0 — `reviews/009-impl4-test.log`. The skips are the same 3 ShowcasePerformance, 2 RelayTiming and `AuthorizedSave_NewAndDeleted_ReturnsNull` as round 1. The increases since round 1 (+6 unit, +4 integration) equal the new `[Fact]` counts: NF0107 +2 and InheritedAuthEmission +4 (`RestatedCheck_GeneratesAsOneDeclarationNamingTheUnionOfOperations`, `_DerivedInterfaceRestatesBaseInterface_`, `_RemoteOnAnyDeclaration_`, `_DifferingOnlyInNullableAnnotation_`); the four `Union_*` integration tests. The rename added no test.

### Veto-tier
None.

- New bullet, declared in more than one place runs for every declaration's operations, once per operation `[integration]`: pinned by the four `Union_*` tests on the client/server containers. `AssertUnionEnforced` asserts `Denied` is refused on Fetch and on Create, an allowed key passes both, and the server log for the Fetch key and the client log for the Create key each hold exactly one entry, for the derived version. Each target declares `CanRestated` once for Create and once for Fetch, in a different shape: a derived interface with `new`, an override in an auth class, and a class that implements two unrelated interfaces. Under the old rule (the most-derived declaration wins) one operation in each target goes unchecked and the `Assert.Null` for it fails, so the tests can tell the two rules apart. The override test also asserts that the base version never ran on either tier. `Union_ClassRestatesInterfaceCheck_` covers the class-restates-interface shape on `ImplementedChecksTarget`. `DeniedByRestatement` is refused only by `CanFetchRestatedOnClass`, so the two nulls come from the combined check. At unit level, `RestatedCheck_GeneratesAsOneDeclarationNamingTheUnionOfOperations` asserts that five restatement shapes generate the same code as one `Create | Fetch` declaration. It also has a discriminating control: Create-only generates different code.
- Changed tests now assert the ruled intent, and the amendment accounts for each change. The diff matches the amendment line by line. `ImplementedInterface_ClassRestatedAttribute_Wins` became `_ClassRestatement_CombinesOperations_RefusesBoth`, and Fetch of `DeniedByRestatement` went from `NotNull` to `Null`. In `_Checks_Allow_AndEachRunsOncePerOperation`, `CanFetchRestatedOnClass` was added to the Fetch set. That is stronger: the exact set now requires it once on the server. In `_ClientCanMethods_AnswerFromTheImplementedChecks`, `CanFetch(DeniedByRestatement)` went from `True` to `False`. `ClassImplementingAttributedInterface_GeneratesAsCheckDeclaredOnClass` lost only its `restatedOnClass` variant, and its other assertions are unchanged (the diff only shows them moved). The variant's case is now covered, under the new intent, by `RestatedCheck_GeneratesAsOneDeclarationNamingTheUnionOfOperations`. No change removes an assertion except that variant, which the amendment records.

**Sacred tests:** `git diff --name-status f057499..HEAD -- src/Tests` lists every file Step 7 touched (`AuthInheritanceTests.cs`, `AuthInheritanceTargets.cs`, `NF0107Tests.cs`, `InheritedAuthEmissionTests.cs`) as added (A) since the plan commit. Every changed test was written in this plan. `DiagnosticTestHelper.cs`, the only pre-existing test file the plan touched, was not modified after `bfd7434`. No test that existed before `f057499` was touched in this round.

### Callouts (≤ 5 Optional)
None.

### Tech-debt (untiered, ≤ 5, one line each)
- None new. The round-1 item still holds: the emission tests do not compile their output.

### Could not verify
- That the impl4 logs were produced from exactly the committed tree. The logs are stamped 12:49 and commit `6259450` is 12:50, and the tree is clean. The count increases match the new `[Fact]`s exactly, which places the new tests in the run.

### Theoretical (not triaged)
- `ReportCheckNotInSource` for the other declarations in a restatement group, when no declaration is in source (`FactoryGenerator.Transform.cs`, the "No MethodDeclarationSyntax" branch and the auth-type-in-a-referenced-assembly branch), has no test. The build already fails on the representative's NF0107, so the only effect is extra diagnostics.
- `CoversEveryOperation` when `ReadAuthorizeOperations` returns null (a referenced `[AuthorizeFactory]` with no operation argument) has no test. No ordinary input seems to reach it.

### Read report
- Read beyond the brief: `todo.md` Dismissed/Parked (both empty for this plan); the v2.0.1 release notes, to check the amendment's "two unrelated base interfaces" line against the new sibling-interface test. They agree: an interface auth type is unsupported, a class auth type is supported.
- Named but unused: none.
