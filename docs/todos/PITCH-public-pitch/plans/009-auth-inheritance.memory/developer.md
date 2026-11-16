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

## Mistakes to Avoid
- Asserting zero errors on `RunGenerator` output compilation (see above).

## User Corrections
- None this run.

## Completion Evidence
- Logs: `docs/todos/PITCH-public-pitch/reviews/009-impl-build.log`, `009-impl-test.log` (full --no-incremental build).
- UnitTests 877/0/0 (was 871), IntegrationTests 681 passed 5 skipped (was 664/5), Design.Tests 187/1 unchanged; each per net9.0 and net10.0.
- Not run: a red run against the old generator.
