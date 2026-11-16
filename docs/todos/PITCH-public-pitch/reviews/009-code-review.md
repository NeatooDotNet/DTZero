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
