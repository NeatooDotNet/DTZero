## Code Review — PITCH-009 — [2026-11-16]

**Verdict: DECISION NEEDED — When a derived auth type restates an inherited check with a different operation set, should the restatement replace the inherited operations, leaving them unchecked with only a debug message, or must it never narrow them silently?**
**Logs:** build PASSED, both solutions, 0 errors; the 2 warnings are WASM native-reference warnings from the OrderEntry.BlazorClient example (`reviews/009-impl3-build.log`). Tests: 0 failed. UnitTests 889/889, IntegrationTests 686 passed / 5 skipped, Design.Tests 187 passed / 1 skipped, the same on net9.0 and net10.0 (`reviews/009-impl3-test.log`). The logs were written at 07:59, before `bfd7434` was committed at 08:00:57. The test review accepted them as the final state, and this review does the same.

### Direction & shape
The fix is in the right place. All three factory kinds take their checks from one function, `TypeAuthMethods` (`src/Generator/FactoryGenerator.Transform.cs:455`), so a single change there covers class, interface and static factories with no renderer edits. `GetAuthMethodsRecursive` (`:103`) reads the auth type's own members and every inherited one. It removes duplicates by name plus parameter types, and it sends uncallable checks to NF0108 instead of generating a call. Setting `authMethodInfo.ClassName = authSymbol.Name` (`:618`) is the smallest change that makes the renderers resolve and register only the named type. All six `ClassName` consumers (`ClassFactoryRenderer.cs:1417,1541,1578`, `InterfaceFactoryRenderer.cs:416,495,552`, `StaticFactoryRenderer.cs:197,375,407`) group, resolve, register or build a message by that name. Each one now sees a single auth type per factory, which is also the type the user wrote in `[AuthorizeFactory<T>]`, so it is in scope through the factory file's usings. The old code could put two simple names in one factory, for example a base and a derived type from different namespaces; that collision is gone. `TypeAuthMethodInfo` and `AuthMethodCall` equality include `ClassName`, but it is set before the record is stored and its value is deterministic, so incremental caching is unaffected. The pipeline is still `ForAttributeWithMetadataName` over the factory type, the same arrangement that already handled an auth type declared in another file. Before this change, NF0107 and NF0108 were a debug message and a CS1061 inside generated code. No reflection was added in the generator or the tests; a scan of the added lines found none. The generator still builds on netstandard2.0.

**Question 2 (`AllInterfaces` ordering).** The order is a documented contract. The XML doc for `ITypeSymbol.AllInterfaces` in Microsoft.CodeAnalysis.Common 5.0.0, the version this repo pins, says: "This list is topologically sorted by the inheritance relationship: if interface type A extends interface type B, then A precedes B in the list." The class walk lists the most-derived class first by construction (`GetMethodsRecursive`), and class members are read before any interface member. So "most-derived wins" holds wherever one declaration derives from the other. If the order were ever violated, the call would still bind through the auth type to the most-derived member and run once. Only the kept declaration's operation flags and `[Remote]` would come from the base. The contract does not order unrelated sibling interfaces; see Callout 1.

**Question 3 (`ClassName`).** No consumer breaks. See the paragraph above. Same-named auth types in different namespaces cannot collide, because one factory has one auth type. Emitting the simple name is the existing behavior: an aliased or fully-qualified `[AuthorizeFactory<Ns.Auth>]` without a using already failed before this plan.

### Veto-tier
None.

### Decision needed (detail)
- `FactoryGenerator.Transform.cs:149` and `:171`, High. The duplicate key is name plus parameter types and leaves out the operation flags, so the kept declaration's `AuthorizeFactoryOperation` replaces the other's.
  - The case: a derived declaration (a class restatement of an interface check, an override with the attribute restated, or a `new` member) carries `[AuthorizeFactory]` with different operations from the declaration it hides.
  - The effect: the hidden declaration's operations get no check. All that records it is the debug line "Skipping … a more-derived declaration … is already an authorization check".
  - This is pinned as intended. `AuthInheritanceTargets.cs:236-237,273-274` with `ImplementedInterface_ClassRestatedAttribute_Wins` asserts that a Fetch the interface guards succeeds once the class restates the check for Create. `InheritedAuthEmissionTests.ClassImplementingAttributedInterface_…` (`restatedOnClass`, interface Write and class Read) asserts the Write check disappears from the output.
  - Under the 2026-11-16 Plan Amendment ("kept once, the most-derived declaration that carries the attribute"), this is correct.
  - Under the plan's Intent, it is not: "A check is never dropped without a word". Nothing in the plan or todo shows the user ruling on narrowing.
  - The release notes do not mention it.

### Callouts
1. `FactoryGenerator.Transform.cs:171` (with `:117`, `:179`), High. `Affects: AC-4`. `Reachable by:` a class auth type that implicitly implements two unrelated interfaces, each declaring `[AuthorizeFactory(...)] bool CanX(string key)` with different operations, for example `class OrderAuth : IReadChecks, IWriteChecks` with one public `CanX`.
   - The first interface in `AllInterfaces` order is kept, and the second interface's operations get no check.
   - Nothing reports it beyond the debug "Skipping" message. This is not even "most-derived wins": `AllInterfaces` does not order siblings.
   - The release notes' Known Limits (`docs/release-notes/v2.0.1.md:53`) and the 2026-11-16 amendment say this shape "is expected not to compile".
   - That is true only when T is an interface: `IOrderAuth : IA, IB` makes `iorderauth.CanX(key)` ambiguous (CS0121). When T is a class, a single implicit `public bool CanX` implements both interfaces, the generated `orderauth.CanX(key)` compiles, and the drop is silent.
   - This is the one remaining path I found from question 1: a method that carries `[AuthorizeFactory]` in the hierarchy, gets no generated check for some of its operations, and produces no diagnostic. The Decision above is the same mechanism, applied to a derived declaration.
2. `docs/release-notes/v2.0.1.md:17` and `:41-48`, High. `Affects: AC-4`. `Reachable by:` upgrading a project in either of two shapes that "Who is affected" does not name.
   - (a) An auth type that is itself in a referenced assembly. Before, none of its checks were generated, silently. Now each check raises NF0107, so the build fails (`NF0107Tests.NF0107_AuthTypeItselfInReferencedAssembly_ReportsError`). The page says "Generated code is unchanged for an auth type that declares all of its own checks". That sentence is literally true for this auth type, but its build now breaks. The What's New NF0107 bullet covers the diagnostic but not who it hits.
   - (b) A concrete, non-abstract base auth class. The plan's pre-flight records that this worked "as a separate service". The old code set `ClassName` to the declaring base and registered and resolved the base with `TryAddTransient<Base>()`. So the base implementation ran on a base instance even when the derived class overrode it. With an override that restated the attribute, both implementations ran.
   - Now the derived override runs alone. A caller the base refused can therefore be allowed. The direction is loosening, while the page's first bullet, "Those checks were not running. They run now. A caller they refuse is refused", describes only tightening.
   - The Overview at `:17`, "inherited … was not enforced", is also broader than the facts. A check on a concrete base ran, and a check on an abstract base stopped startup.
   - Shape (b) also bears on the plan's Intent line "Authorization that works today keeps working, with the same generated behavior". Its overridden form is the Decision above.

All other release-note claims match the code:
- Only the named auth type is registered.
- The NF0107 and NF0108 texts and their remedies match `DiagnosticDescriptors.cs`.
- A check reached two ways runs once, and an override runs the derived version once.
- The CS1061 case is now NF0108, located at the attribute.
- The fix covers all three factory kinds.
- A refusal names the auth type.
- The version is 2.0.1 in both properties, nav_order was renumbered over the 1.x pages, and the index and All Releases list are updated.

### Could not verify
- The amendment's claim that pre-existing targets produce byte-identical output: no snapshot artifact exists. The cover is that every pre-existing test passes unchanged.
- Callout 1 and Decision behavior at runtime: traced through the code, not executed. No test covers sibling interfaces on a class auth type.
- The old concrete-base behavior in Callout 2(b): traced from the pre-change code path (`MethodInfo.ClassName = ContainingType.Name`, `Types.cs:719`, plus the registrar) and the plan's probe note. No test pins it.

### Theoretical (not triaged)
- `AuthMethodSignature` (`:198`) uses `ToDisplayString()`, which includes nullable annotations. If an override restates the attribute and its parameter is `string?` against the base's `string` (or differs in `#nullable` context), both declarations are kept, and the derived check runs twice.
- In an IDE where a C# project reference is a CompilationReference, a check inherited from that project has source syntax and is generated, while the command-line build raises NF0107 for it. The test helper's own remark about CompilationReference describes this difference. I have not confirmed which reference kind current IDEs supply.
- `ReportAtAuthorizeFactory` returns silently when `reportLocation` is null. That cannot happen, because the factory type is always in source.
- An `[AuthorizeFactory]` argument that the regex cannot read (a constant or an alias) leaves `Aggregate` with an empty list, which throws. This predates the plan.

### Read report
- Read beyond the brief:
  - `FactoryGenerator.Types.cs` (the `MethodInfo` constructor, `MethodParameterInfo`, auth-to-operation matching at `:629`).
  - `ClassFactoryRenderer.cs:1405-1600`, plus a grep of every `ClassName` consumer in the other renderers.
  - `FactoryModelBuilder.cs:530-560`.
  - The `FactoryGenerator.cs` pipeline.
  - The Roslyn 5.0.0 XML doc for `AllInterfaces` and `FindImplementationForInterfaceMember`.
  - `verdict.md:163` (the fifth ruling).
  - Release-notes nav_order values.
- Named but unused: `src/Design/CLAUDE-DESIGN.md`. The plan adds no Design sample, and nothing turned on it.

---

## Code Review — PITCH-009 — [2026-11-16] — Second pass (at `6259450`)

**Verdict: APPROVED**
**Logs:** build PASSED. Both solutions built with 0 errors. The 2 warnings are the OrderEntry.BlazorClient WASM native-reference warnings (`reviews/009-impl4-build.log`). Tests: 0 failed. UnitTests 895/895, IntegrationTests 690 passed / 5 skipped, Design.Tests 187 passed / 1 skipped, the same on net9.0 and net10.0 (`reviews/009-impl4-test.log`).
- The deltas match the diff. +6 unit tests: NF0107 +2, `RestatedCheck_*` +4. +4 integration tests: the `Union_*` tests.
- The logs were written at 12:49 and `6259450` was committed at 12:50:52. They are accepted as the final state, as in the first pass.

### Direction & shape
The ruling is implemented at the same seam as before. `GetAuthMethodsRecursive` (`FactoryGenerator.Transform.cs:106`) now groups the `[AuthorizeFactory]` declarations of one check by signature (`:137-150`). It returns one representative per group: the most-derived declaration in source (`:220`). The other declarations go back in `restatements` (`:222`).

`TypeAuthMethods` then works on each group:
- It ORs the operations of the in-source declarations into the representative's operations (`:677-702`).
- It marks the check `[Remote]` if any declaration carries `[Remote]` (`:686`, `:747-750`).
- It raises NF0107 for a declaration with no source syntax, unless the in-source union covers it (`:706-715`).

The callability test (NF0108) now runs before grouping (`:190-203`), so a check that cannot be called never joins a group. `MarkRemote` (`Types.cs:706`) changes the record before it is stored, so caching is unaffected.

Sacred tests: `git diff --name-status f057499..HEAD -- src/Tests` shows only `DiagnosticTestHelper.cs` modified, and it is additive. The three tests that changed between `bfd7434` and `6259450` were all written earlier in this plan. Each change has a justification line in Plan Amendments, so none of them was weakened without a word.

**Q1: the Decision case and Callout 1.** Both are closed for the shapes they named.
- Class restating an interface check: `Union_ClassRestatesInterfaceCheck_*` and `ImplementedInterface_ClassRestatement_CombinesOperations_RefusesBoth`.
- Derived interface restating a base interface check: `Union_DerivedInterfaceRestatesBaseInterfaceCheck_*`.
- Override restated for another operation: `Union_OverrideRestatedWithAnotherOperation_*`.
- `new` member: `RestatedCheck_GeneratesAsOneDeclarationNamingTheUnionOfOperations`, variant `newMemberRestated`.
- Sibling interfaces on a class auth type: `Union_ClassImplementsTwoUnrelatedInterfacesDeclaringTheCheck_*`.

One residual path remains (Callout 1 below): an operation named by an in-source declaration can still end up with no check and no diagnostic.

**Q2: a declaration in a referenced assembly.** I found no path where a referenced declaration's operation is neither enforced nor reported.
- If no declaration of the check is in source, every declaration is reported (`:650-654`, and `:784-788` for an auth type that is itself referenced).
- If a referenced declaration is grouped with in-source ones, it is reported unless `CoversEveryOperation` holds. That test uses the same `&` matching that assigns checks to factory methods (`Types.cs:629`). The six `FactoryOperation` values are the whole enum.
- When the test holds, the covered operations are checked by the same call, so they are enforced.
- `ReadAuthorizeOperations` can return null: the attribute has one constructor, taking an int-backed enum (`FactoryAttributes.cs:155`), so a null only arises when the argument cannot be read. A null is reported, not skipped.
- `[Remote]` on a referenced declaration is still ORed in (`:686` runs before the syntax test).

**Q3: `[Remote]` combination.** This cannot make a check weaker. A remote check runs on the server, and the renderer guards a remote method as server-only (`isServerOnly: IsInternal || IsRemote`, `ClassFactoryRenderer.cs:420` and the equivalents).

It does move work that used to run locally. For every operation of the combined check, the factory method becomes remote and `Task`-returning (`FactoryModelBuilder.cs:351-357`), and its `Can*` asks the server (`:820-822`, `:930-943`). That covers the operations named only by a non-`[Remote]` restatement too. See Callout 2.

`IsInternal` comes from the representative alone (`Types.cs:728`). That matters only when a `new` member changes accessibility (see Theoretical).

**Q4: the signature key.** `SymbolDisplayFormat.FullyQualifiedFormat` lacks `IncludeNullableReferenceTypeModifier`. The generator's own `FullyQualifiedFormatWithNullable` (`Types.cs:35-38`) exists to add that option. So `string?` and `string`, and nested annotations such as `List<string?>`, now share a key. `int?` stays distinct, and `RestatedCheck_DifferingOnlyInNullableAnnotation_IsOneCheck` pins this.
- Apart from that annotation, `FullyQualifiedFormat` qualifies at least as much as the default format did. So this commit introduces no new collapse and no new split.
- The key still leaves out ref-kind and method arity, which is unchanged from `bfd7434` (see Theoretical).

**Q5: Callout 2.**
- Shape (a), an auth type in a referenced assembly, is closed (`v2.0.1.md:52`).
- Shape (b), a concrete base with an overriding derived class, is closed (`:53`).
- The sentence at `:42` is now scoped to "declared in the project".
- The Overview sentence at `:17`, which the first pass flagged as broader than the facts, is unchanged.
- One claim the first pass did not test, "No API change" (`:13`, `:27`), does not hold. See Callout 3.
- These claims match the code: the restated-check bullet (`:35`), subject to Callout 1; the referenced auth type remedy (`:52`), which `CoversEveryOperation` makes true; and Known Limits (`:58`), where an interface auth type inheriting from two bases gives an ambiguous call and a class auth type gets the union.

### Veto-tier
None.

### Callouts
1. `FactoryGenerator.Transform.cs:698-701` (against `:672`), High. `Affects: AC-4` (plan Acceptance: "runs for the operations of every declaration"). `Reachable by:` a restated check where a declaration that is not the representative writes its operations in a form the regex `AuthorizeFactoryOperation\.(\w+)` cannot read.
   - Examples: a base `[AuthorizeFactory(Op.Fetch)]` under `using Op = DTZero.AuthorizeFactoryOperation;`, a `const AuthorizeFactoryOperation` field, or a cast. Then a derived type restates the check as `[AuthorizeFactory(AuthorizeFactoryOperation.Create)]`.
   - `ParseAuthorizeOperations` returns an empty list for that declaration. It adds nothing to the union, and nothing reports it, so Fetch goes unchecked.
   - The same form on the representative makes `Aggregate` throw, so no factory is generated and the build fails loudly.
   - So this is the one remaining shape where an in-source declaration's operation is silently lost. A base declaration in that form was never read before this plan, so a project can already contain one.
   - One option: `ReadAuthorizeOperations` (`:240`), which reads the bound constant, already exists and would read any form.
2. `FactoryGenerator.Transform.cs:686`, `:747-750`, with `FactoryModelBuilder.cs:351-357`, `:820-822`. High. `Affects: AC-4` (plan Intent: "Authorization that works today keeps working, with the same generated behavior"). `Reachable by:` upgrading a project in which one declaration of a check carries `[Remote]` and a restatement that names other operations does not.
   - Example: a concrete base auth class with `[Remote][AuthorizeFactory(Fetch)] public virtual bool CanX(...)`, and a derived class overriding it with `[AuthorizeFactory(Create)]` only.
   - At v2.0.0 the derived check ran alone for Create, locally and synchronously.
   - Now the combined check is `[Remote]`. The local `Create` becomes a remote, `Task`-returning factory method whose body runs on the server, and `CanCreate` asks the server.
   - The same happens when a class restates an interface's `[Remote]` check without `[Remote]`. The check is never weaker. What changes: a local root `Create` moves to the server, which is against what the restating declaration says (VisionAlignment D14 makes a root Create local by default). Call sites may stop compiling, and a check that reads client-side state now reads the server's.
   - Plan Amendments record the OR rule. The user's ruling covered operations, not remoteness.
   - One option: emit a check per declaration for the operations only that declaration names, keeping each declaration's `[Remote]`.
3. `docs/release-notes/v2.0.1.md:13`, `:27`, `:40-53`. High. `Affects: AC-4`. `Reachable by:` upgrading with an inherited check that returns `Task<bool>`/`Task<string>` or carries `[Remote]`, and that applies to a factory operation that was synchronous because the check was dropped.
   - "No API change" does not hold for that project. The newly enforced check makes the factory method and its `Can*` `Task`-returning (`FactoryModelBuilder.cs:354-357`, `:822`), and a `[Remote]` check also makes them remote. The generated factory interface changes signature, and callers that used the result synchronously stop compiling.
   - This was true at `bfd7434` as well. Callout 2's shape adds a case that worked at v2.0.0.
   - Neither appears under "Who is affected".
   - The Overview at `:17` still says an inherited check "was not enforced". That is broader than the facts, given that `:53` now says a concrete base's check ran.

### Could not verify
- Callouts 1 and 2 at runtime. Both were traced through the transform and the model builder, not executed.
  - `RestatedCheck_RemoteOnAnyDeclaration_MakesTheCombinedCheckRemote` pins the generated code for a class restating an interface's `[Remote]` check.
  - No test covers the concrete-base override shape or a `Create` that becomes `Task`-returning.
- No test-review round covers the impl4 tests. `009-test-review.md` has Round 1 only, at impl3. I read the changed and added tests where a finding or a closure depended on them.
- The amendment's claim that pre-existing output is byte-identical. It is the implementer's report, with no snapshot artifact. The cover is that every pre-existing test passes unchanged.

### Theoretical (not triaged)
- A `new` member hiding an inherited check takes over the hidden declaration's operations, and the hidden implementation never runs. The same goes for an interface implemented by a base-class method that a derived `new` member hides. Because `IsInternal` comes from the representative, a `public new` over an `internal` check also drops the server-only guard on the hidden declaration's operations.
- The signature key ignores ref-kind and method arity (unchanged from `bfd7434`). So `CanX(int)` and `CanX(ref int)`, or `CanX<T>(string)` and `CanX(string)`, would collapse into one check. An `object`/`dynamic` override, or differing tuple names on a `new` member, would split into two checks: the check runs twice and no operation is lost.
- With default metadata import, a non-public `[AuthorizeFactory]` member of a referenced base is not visible, so it is neither enforced nor reported. It would not be callable anyway, unless InternalsVisibleTo applies.

### Read report
- Read beyond the brief:
  - `FactoryGenerator.Types.cs:35-38`, `:600-839`.
  - `FactoryModelBuilder.cs:340-380`, `:800-950`.
  - A grep of `IsRemote`/`IsInternal` across the generator.
  - `AuthorizeFactoryOperation.cs` and `FactoryOperation.cs`.
  - `FactoryAttributes.cs` (attribute usage and constructor).
  - `Design.Domain/Aggregates/AuthorizedOrderAuth.cs` and `AuthorizedOrder.cs` comments on `[Remote]` auth.
  - The test diff `bfd7434..HEAD`, `009-test-review.md`, and `todo.md` (ACs, Dismissed, Parked).
- Named but unused: none.
