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
