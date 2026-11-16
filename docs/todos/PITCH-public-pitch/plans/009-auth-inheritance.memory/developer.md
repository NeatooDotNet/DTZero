# Developer — PITCH-009 Inherited Authorization Checks

Last updated: 2026-11-16
Current step: Implementation complete, both solutions green; awaiting test review / code review.

## Key Context
- Fix lives in one place: `src/Generator/FactoryGenerator.Transform.cs`. `TypeAuthMethods` feeds class, interface and static factories alike, so all three renderers were affected and all three are fixed by it.
  - New `GetAuthMethodsRecursive`: interface T -> own members + `AllInterfaces` members (diamond read once); class T -> existing `GetMethodsRecursive`. Attributed methods deduped by name + parameter display types, most-derived first wins.
  - `authMethodInfo.ClassName = authSymbol.Name` so resolution and registration go through T.
- Generated output of every pre-existing target in all 8 Generated folders (Design, Examples, Tests) is byte-identical to the pre-change snapshot; only 8 new files for the new targets.
- Unit-test harness (`DiagnosticTestHelper.RunGenerator`) lacks System.Text.Json refs, so output compilation of class factories always has errors there; do not assert error-free output in that harness.
- Generated folders are wiped by RemoveDir every build and only refilled when CoreCompile runs; an incremental build can leave them empty.

- Second pass (coordinator request): NF0107 (Error, `DiagnosticDescriptors.AuthCheckDeclarationNotInSource`) reported at the `[AuthorizeFactory<T>]` usage when a check has no source declaration (method without syntax, attribute without syntax, or T itself in metadata). Harness gained additive `RunGeneratorWithReferences` + `EmitReference` (emit image, not CompilationReference). Tests: `NF0107Tests` (5), and `InheritedAuthEmissionTests` helper asserts no NF0107. Logs `009-impl2-*.log`: Unit 882, Integration 681/5 skip, Design 187/1.
- Probe (deleted): explicit interface impl with the attribute on the class -> loud CS1061 in generated code (`auth.TestNamespace.IChecks.CanRead`). Attribute on an interface a CLASS auth type implements -> silently dropped, no check, no diagnostic (not fixed; reported).

- Third pass: class T now reads checks on every interface in T.AllInterfaces (after the class hierarchy, so class declarations win); interface checks T cannot call (explicit impl, attribute on explicit impl, default interface impl) -> NF0108 Error, no call generated. Tests: AuthInheritanceTests ImplementedInterface_* (5), InheritedAuthEmissionTests.ClassImplementingAttributedInterface_GeneratesAsCheckDeclaredOnClass, NF0108Tests (5), NF0107Tests class-implements-metadata-interface (1). Logs `009-impl3-*.log`: Unit 889, Integration 686/5, Design 187/1. Existing generated output byte-identical to the snapshot.

- Fourth pass (user ruling): restated checks COMBINE operations (union), never narrow. GetAuthMethodsRecursive groups declarations by nullable-insensitive signature (FullyQualifiedFormat), representative = most-derived declaration in source, others in `restatements`. [Remote] on any declaration -> combined check remote (OR, as the builder already ORs IsRemote across auth methods). Metadata restatement -> NF0107 unless source union covers every factory operation it names (Read covers Create, Fetch AND Execute). Callability (NF0108) now checked before grouping. Logs `009-impl4-*.log`: Unit 895, Integration 690/5, Design 187/1; pre-existing generated output byte-identical.

## Mistakes to Avoid
- Coverage fixtures: `Read` applies to Execute operations too (FactoryOperation.Execute = Read | Execute).
- Asserting zero errors on `RunGenerator` output compilation (see above).

## User Corrections
- None this run.

## Completion Evidence
- Logs: `docs/todos/PITCH-public-pitch/reviews/009-impl-build.log`, `009-impl-test.log` (full --no-incremental build).
- UnitTests 877/0/0 (was 871), IntegrationTests 681 passed 5 skipped (was 664/5), Design.Tests 187/1 unchanged; each per net9.0 and net10.0.
- Not run: a red run against the old generator.
