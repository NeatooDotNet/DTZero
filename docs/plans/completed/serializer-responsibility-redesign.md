# Serializer Responsibility Redesign

**Date:** 2026-03-20
**Related Todo:** [serializer-responsibility-redesign](../todos/serializer-responsibility-redesign.md)
**Status:** Requirements Documented
**Last Updated:** 2026-03-20

---

## Overview

Remove `ReferenceHandler` from `JsonSerializerOptions` entirely and provide `DTZeroReferenceResolver` through a static `AsyncLocal` accessor. Delete `IsNeatooType()`, `PlainOptions`, and `DTZeroReferenceHandler`. Return to a single `JsonSerializerOptions` instance. Converters that need reference tracking access the resolver directly via the `AsyncLocal` accessor instead of through `options.ReferenceHandler`.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/serializer-responsibility-redesign.md#requirements-review)

### Relevant Existing Requirements

#### Serialization Architecture

- **`DTZeroReferenceHandler` already uses `AsyncLocal` (`DTZeroReferenceHandler.cs:8`):** The proposed `AsyncLocal` accessor on `DTZeroReferenceResolver` is an evolution of the existing pattern, not a new mechanism. -- Relevance: validates the approach.

- **Only one DTZero converter accesses `options.ReferenceHandler` (`DTZeroInterfaceJsonTypeConverter.cs:46`):** The `Read()` method calls `options.ReferenceHandler!.CreateResolver().AddReference(id, result)`. However, as discovered during analysis, the `id` variable is initialized to `string.Empty` and never assigned, so this call is dead code -- the guard `if (!string.IsNullOrEmpty(id))` always prevents execution. -- Relevance: the only DTZero call site is dead code, simplifying the migration.

- **Downstream Neatoo converters are the primary consumer of `ReferenceHandler` (`isneatootype-missing-validatebase-check.md:32-33`):** Neatoo's `NeatooBaseJsonTypeConverter.Write()` calls `options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists)`. These call sites must migrate to the new `AsyncLocal` accessor.

- **`DTZeroJsonSerializer` constructor builds two `JsonSerializerOptions` (`DTZeroJsonSerializer.cs:76-113`):** The `Options` set has `ReferenceHandler`, `PlainOptions` does not. The redesign deletes `PlainOptions` and removes `ReferenceHandler` from `Options`, returning to a single options set.

- **`DTZeroJsonTypeInfoResolver` is independent of `ReferenceHandler` (`DTZeroJsonTypeInfoResolver.cs:24-39`):** This resolver provides DI-aware deserialization by overriding `CreateObject` for registered service types. It is unaffected by the redesign.

- **`DTZeroJsonConverterFactory` is the DI extension point for downstream converters (`DTZeroJsonConverterFactory.cs`):** Neatoo registers its own converter factories through this abstract base class. After the redesign, downstream converters must access the resolver via the `AsyncLocal` accessor rather than `options.ReferenceHandler`.

#### Anti-Patterns and Design Rules

- **Anti-Pattern 9 (`CLAUDE-DESIGN.md:378-419`):** "Do not mix Neatoo types with records in Interface Factory return types." The user-facing rule remains valid. The technical explanation changes: converters for Neatoo types add `$id`/`$ref` using the resolver directly; STJ's native record handling cannot process `$ref` in parameterized constructors. The rule itself is unchanged; only the mechanism description needs updating.

- **Quick Decisions Table record entry (`CLAUDE-DESIGN.md:153`):** "Records are serialized without `$id`/`$ref`; do not mix Neatoo types into record properties." Remains valid -- converters decide, not the serializer.

#### Existing Tests

- **Design serialization tests (`Design.Tests/FactoryTests/SerializationTests.cs`):** 6 tests verify Create, Fetch, value object, collection, nullable, and Save round-trips. Must pass after redesign.

- **Interface Factory record round-trip test (`Design.Tests/FactoryTests/InterfaceFactoryTests.cs:96-117`):** Validates `GetRecordByIdAsync()` returning an `ExampleRecordResult` record through client-server serialization. This is the scenario that prompted the original v0.21.3 fix. Must pass after redesign.

- **Interface Factory record serialization tests (`InterfaceFactoryRecordSerializationTests.cs`):** 6 tests including `InterfaceFactory_NonNeatooType_NoRefMetadata` which asserts that serialized JSON for non-Neatoo records contains no `$id` or `$ref` metadata. Must pass after redesign.

- **Interface collection serialization tests (`InterfaceCollectionSerializationTests.cs`):** 11 tests verifying round-trip serialization for single interface properties, concrete and interface collections with interface elements, nested structures, save operations, empty/null collections. Must pass after redesign.

- **Record serialization tests (`RecordSerializationTests.cs`):** 12 tests (plus 4 ClientServer tests) covering simple records, complex records with all primitive types, collections, nullables, nested records, equality preservation. Must pass after redesign.

### Gaps

1. **No documented contract for `AsyncLocal` resolver accessibility by third-party converters.** The architect must define visibility, thread safety, lifecycle, and null safety.

2. **No documented contract for `DTZeroInterfaceJsonTypeConverter` reference access after redesign.** The current call site is dead code (see analysis above). The redesign must decide whether to remove it or fix it.

3. **No documented migration path for Neatoo's converter call sites.** The coordinated Neatoo update must migrate `options.ReferenceHandler.CreateResolver()` calls to the new `AsyncLocal` accessor.

4. **No test for interface-typed property reference preservation with the new accessor.** The existing tests exercise the full pipeline but do not independently verify `$id`/`$ref` registration for interface-typed properties via the `AsyncLocal` path.

### Contradictions

None found. The redesign aligns with all documented requirements and existing anti-patterns.

### Recommendations for Architect

1. Define the `AsyncLocal` accessor API precisely (visibility, null safety, lifecycle).
2. Preserve `DTZeroReferenceResolver` lifecycle management in `DTZeroJsonSerializer`.
3. Address the dead code in `DTZeroInterfaceJsonTypeConverter.Read()` line 46.
4. Verify Anti-Pattern 9 remains valid.
5. Add a test for interface-typed property reference preservation.
6. Document the Neatoo migration path.
7. Keep the record round-trip bug visible.

---

## Business Rules (Testable Assertions)

### Serializer Configuration Rules

1. WHEN `DTZeroJsonSerializer` is constructed, THEN `Options.ReferenceHandler` IS `null`. A single `JsonSerializerOptions` instance is used for all types. -- Source: Todo clarification A6 (approved direction), Requirements Review item 7

2. WHEN `DTZeroJsonSerializer` is constructed, THEN only one `JsonSerializerOptions` instance exists (no `PlainOptions`). -- Source: Todo clarification A6, Requirements Review item 7

3. WHEN `DTZeroJsonSerializer` is constructed, THEN `IsNeatooType()` method does not exist. -- Source: Todo problem statement items 1-3, Requirements Review item 8

### Resolver Lifecycle Rules

4. WHEN `DTZeroJsonSerializer.Serialize()` is called, THEN a new `DTZeroReferenceResolver` is created, assigned to the `AsyncLocal`, and disposed after serialization completes. -- Source: Requirements Review recommendation 2, existing pattern at `DTZeroJsonSerializer.cs:156-158`

5. WHEN `DTZeroJsonSerializer.Deserialize()` is called, THEN a new `DTZeroReferenceResolver` is created, assigned to the `AsyncLocal`, and disposed after deserialization completes. -- Source: Requirements Review recommendation 2, existing pattern at `DTZeroJsonSerializer.cs:229-230`

6. WHEN no serialization operation is in progress, THEN `DTZeroReferenceResolver.Current` returns `null`. -- Source: NEW (Gap 1: null safety contract)

7. WHEN a serialization operation completes (normally or via exception), THEN the `AsyncLocal` resolver is cleared (set to `null`). -- Source: NEW (Gap 1: lifecycle contract)

### Resolver Access Rules

8. WHEN a converter needs reference tracking during serialization or deserialization, THEN it accesses the resolver via `DTZeroReferenceResolver.Current` (static property). -- Source: Todo clarification A6 (approved direction), Requirements Review recommendation 1

9. WHEN `DTZeroReferenceResolver.Current` is accessed from a converter and a serialization operation is in progress, THEN a non-null `DTZeroReferenceResolver` is returned. -- Source: NEW (Gap 1: accessibility contract)

10. WHEN `DTZeroReferenceResolver.Current` is a `public` static property, THEN downstream converters in separate assemblies (e.g., Neatoo) can access it. -- Source: Requirements Review recommendation 1, Gap 1

### Record Serialization Rules

11. WHEN a plain record type (no `[Factory]`, not implementing `IOrdinalSerializable`) is serialized via `DTZeroJsonSerializer`, THEN the JSON output contains no `$id` or `$ref` metadata. -- Source: Anti-Pattern 9 (`CLAUDE-DESIGN.md:378-419`), Quick Decisions (`CLAUDE-DESIGN.md:153`), verified by `InterfaceFactory_NonNeatooType_NoRefMetadata` test

12. WHEN a plain record with a parameterized constructor is deserialized via `DTZeroJsonSerializer`, THEN deserialization succeeds without `ObjectWithParameterizedCtorRefMetadataNotSupported` error. -- Source: Original v0.21.3 fix motivation, verified by `InterfaceFactory_SimpleRecord_RoundTrip` test

### Interface Type Serialization Rules

13. WHEN an interface-typed property is serialized, THEN the `DTZeroInterfaceJsonTypeConverter.Write()` emits `$type` and `$value` wrapper properties (unchanged from current behavior). -- Source: Existing behavior at `DTZeroInterfaceJsonTypeConverter.cs:83-102`

14. WHEN an interface-typed property is deserialized, THEN the `DTZeroInterfaceJsonTypeConverter.Read()` reads `$type` and `$value`, resolves the concrete type, and deserializes the value (unchanged from current behavior). -- Source: Existing behavior at `DTZeroInterfaceJsonTypeConverter.cs:22-81`

15. WHEN `DTZeroInterfaceJsonTypeConverter.Read()` deserializes an interface-typed value, THEN it does NOT call `options.ReferenceHandler` (that code path is removed). -- Source: Dead code finding (see analysis), Requirements Review item 2

### Deletion Rules

16. WHEN the redesign is complete, THEN the file `DTZeroReferenceHandler.cs` is deleted. -- Source: Todo approved direction (remove `ReferenceHandler` from options entirely)

17. WHEN the redesign is complete, THEN `DTZeroJsonSerializer` has no `PlainOptions` property and no `IsNeatooType()` method. -- Source: Todo approved direction, Requirements Review items 7-8

### Backward Compatibility Rules

18. WHEN all existing serialization tests are run after the redesign, THEN they all pass. This includes Design project tests (6), interface factory record tests (6), interface collection tests (11), record serialization tests (16), and all other serialization-related tests. -- Source: Requirements Review items 11-12, existing test suites

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Construct serializer, verify no ReferenceHandler | `new DTZeroJsonSerializer(...)` | 1, 2 | `Options.ReferenceHandler == null`, no `PlainOptions` property |
| 2 | Serialize plain record | `InterfaceRecordResult("Test", 42)` via `Serialize()` | 4, 11 | JSON has no `$id`/`$ref`; resolver created and disposed |
| 3 | Deserialize plain record with parameterized ctor | JSON `{"Name":"Test","Value":42}` via `Deserialize<InterfaceRecordResult>()` | 5, 12 | `InterfaceRecordResult` with Name="Test", Value=42; no exception |
| 4 | Serialize record with collection | `InterfaceRecordWithCollection` with 3 items | 4, 11 | JSON has no `$id`/`$ref`; all items present |
| 5 | Resolver is null outside serialization | Access `DTZeroReferenceResolver.Current` with no operation active | 6 | Returns `null` |
| 6 | Resolver is non-null during serialization | Converter accesses `DTZeroReferenceResolver.Current` during `Serialize()` | 8, 9 | Returns non-null `DTZeroReferenceResolver` |
| 7 | Resolver is cleared after exception | `Serialize()` throws, then check `DTZeroReferenceResolver.Current` | 7 | Returns `null` |
| 8 | Interface-typed property serialization | Object with `ITestChild` property, serialize via client/server | 13, 14 | JSON contains `$type`/`$value`; deserializes to correct concrete type |
| 9 | No reference handler in options | After construction, inspect `Options` | 1, 15 | `Options.ReferenceHandler` is `null`; converter does not access it |
| 10 | Full client-server round-trip with Class Factory type | `ExampleClassFactory.Create("Test")` via client container | 18 | Object survives round-trip with all properties preserved |
| 11 | Full client-server round-trip with Interface Factory record | `IExampleRepository.GetRecordByIdAsync(42)` via client container | 11, 12, 18 | Record survives round-trip without `$id`/`$ref` error |
| 12 | Interface collection round-trip | `List<ITestChild>` with 2 elements via client container | 13, 14, 18 | All elements survive with correct concrete types |
| 13 | DTZeroReferenceHandler.cs is deleted | Check file system | 16 | File does not exist |
| 14 | IsNeatooType is deleted | Search `DTZeroJsonSerializer.cs` for `IsNeatooType` | 17 | Method does not exist |

---

## Approach

The redesign follows the approved direction from the comprehension check (clarification A6): remove `ReferenceHandler` from `JsonSerializerOptions` entirely and provide the resolver via `AsyncLocal`.

**Key insight from codebase analysis:** The `DTZeroInterfaceJsonTypeConverter.Read()` call to `options.ReferenceHandler` at line 46 is dead code. The `id` variable is initialized to `string.Empty` and never assigned, so the `AddReference` call is never reached. This means removing `ReferenceHandler` from `options` has zero behavioral impact on DTZero's own converters. The only consumers that will notice are downstream Neatoo converters that access `options.ReferenceHandler.CreateResolver()` -- and those are the ones that need the coordinated migration.

**Strategy:**

1. Add a static `AsyncLocal`-based `Current` property to `DTZeroReferenceResolver`
2. Modify `DTZeroJsonSerializer` to set/clear `DTZeroReferenceResolver.Current` instead of `DTZeroReferenceHandler.ReferenceResolver.Value`
3. Remove `ReferenceHandler` from `JsonSerializerOptions`, delete `PlainOptions`, delete `IsNeatooType()`
4. Remove dead reference handler code from `DTZeroInterfaceJsonTypeConverter.Read()`
5. Delete `DTZeroReferenceHandler.cs`
6. All existing tests must pass without modification

---

## Design

### AsyncLocal Accessor on DTZeroReferenceResolver

Add a static `AsyncLocal<DTZeroReferenceResolver?>` property to the existing `DTZeroReferenceResolver` class:

```
DTZeroReferenceResolver (public sealed class)
    EXISTING: AddReference(), GetReference(), ResolveReference(), AlreadyExists(), Dispose()
    NEW: static AsyncLocal<DTZeroReferenceResolver?> _current
    NEW: public static DTZeroReferenceResolver? Current { get => _current.Value; internal set => _current.Value = value; }
```

Design decisions:
- **Getter is `public`** -- downstream converters (Neatoo) in separate assemblies must read it
- **Setter is `internal`** -- only `DTZeroJsonSerializer` within DTZero should set/clear it
- **Nullable return** -- `null` means no serialization in progress; callers must null-check
- **`AsyncLocal` semantics** -- each async flow gets its own resolver, safe for concurrent operations

### DTZeroJsonSerializer Changes

**Constructor:** Remove `ReferenceHandler = this.ReferenceHandler` from `Options`. Delete `PlainOptions` entirely. Delete `ReferenceHandler` property. Delete `IsNeatooType()` method. Remove `IServiceAssemblies` dependency (only used by `IsNeatooType`). Single `Options` instance used for all serialization.

**Serialize/Deserialize methods:** Replace the conditional `IsNeatooType` branching:

```
BEFORE (each method):
    var options = IsNeatooType(type) ? this.Options : this.PlainOptions;
    if (options == this.Options) {
        using var rr = new DTZeroReferenceResolver();
        this.ReferenceHandler.ReferenceResolver.Value = rr;
    }
    JsonSerializer.Serialize/Deserialize(... options);

AFTER (each method):
    using var rr = new DTZeroReferenceResolver();
    DTZeroReferenceResolver.Current = rr;
    try {
        JsonSerializer.Serialize/Deserialize(... this.Options);
    } finally {
        DTZeroReferenceResolver.Current = null;
    }
```

Key change: the resolver is always created and always available. Whether a converter uses it is the converter's decision. Records and DTOs use no custom converter, so they serialize natively via STJ without any `$id`/`$ref` interference.

### DTZeroInterfaceJsonTypeConverter.Read() Cleanup

Remove the dead reference handler code (lines 44-48):

```
BEFORE:
    if (!string.IsNullOrEmpty(id))
    {
        options.ReferenceHandler!.CreateResolver()
                         .AddReference(id, result);
    }

AFTER:
    (removed entirely -- id was never assigned, this code was dead)
```

The `var id = string.Empty;` declaration at line 32 is also removed.

### DTZeroReferenceHandler.cs Deletion

The entire file is deleted. This class was a bridge between `JsonSerializerOptions.ReferenceHandler` and the `AsyncLocal<ReferenceResolver>`. With the new design, `DTZeroReferenceResolver.Current` provides direct access, eliminating the need for this intermediate class.

### Files NOT Changed

- `DTZeroOrdinalConverterFactory.cs` / `DTZeroOrdinalConverter<T>` -- does not use `ReferenceHandler`
- `DTZeroInterfaceJsonConverterFactory.cs` -- creates converters, does not use `ReferenceHandler`
- `DTZeroJsonTypeInfoResolver.cs` -- independent of `ReferenceHandler`
- `DTZeroJsonConverterFactory.cs` -- abstract base class, no `ReferenceHandler` access
- `AddDTZeroServices.cs` -- DI registration unchanged
- All test files -- no test changes required; all tests must pass as-is

### Neatoo Coordinated Migration (Out of Scope for This Plan)

Neatoo's converters will need to migrate from `options.ReferenceHandler.CreateResolver()` to `DTZeroReferenceResolver.Current`. This is a Neatoo-side change and is out of scope for this DTZero plan. However, the migration path is:

```
BEFORE (Neatoo converter):
    var resolver = options.ReferenceHandler!.CreateResolver();
    var id = resolver.GetReference(value, out var alreadyExists);

AFTER (Neatoo converter):
    var resolver = DTZeroReferenceResolver.Current;
    if (resolver != null) {
        var id = resolver.GetReference(value, out var alreadyExists);
        // ... reference handling logic
    }
```

The null check is important: when Neatoo types are serialized outside of `DTZeroJsonSerializer` (e.g., in unit tests using bare STJ), `Current` will be `null`. Neatoo's converters should gracefully skip reference handling in that case.

---

## Implementation Steps

### Phase 1: Add AsyncLocal Accessor to DTZeroReferenceResolver

1. Add `private static readonly AsyncLocal<DTZeroReferenceResolver?> _current = new();` field
2. Add `public static DTZeroReferenceResolver? Current` property with `public` getter and `internal` setter
3. Verify build succeeds

### Phase 2: Simplify DTZeroJsonSerializer

1. Remove `PlainOptions` property and its constructor initialization (lines 98-113)
2. Remove `ReferenceHandler = this.ReferenceHandler` from `Options` construction (line 78)
3. Remove `private DTZeroReferenceHandler ReferenceHandler { get; }` property (line 43)
4. Remove `IsNeatooType()` method (lines 123-136)
5. Remove `IServiceAssemblies serviceAssemblies` field (line 31) -- only used by `IsNeatooType`. Note: keep the `serviceAssemblies` parameter in the constructor if it is used elsewhere (check `FromObjectJson` and `DeserializeRemoteDelegateRequest`). **Update:** `serviceAssemblies` is used in `DeserializeRemoteDelegateRequest()` line 364 and `FromObjectJson()` line 408. The field must be kept.
6. Update all four `Serialize`/`Deserialize` method bodies to use the new pattern:
   - Remove `IsNeatooType` branching
   - Always create `DTZeroReferenceResolver`, set `DTZeroReferenceResolver.Current`, use `try/finally` to clear
   - Use single `this.Options` for all calls
7. Remove `PlainOptions`-related comments
8. Verify build succeeds

### Phase 3: Clean Up DTZeroInterfaceJsonTypeConverter

1. Remove `var id = string.Empty;` (line 32)
2. Remove the dead reference handler block (lines 44-48)
3. Remove `options.ReferenceHandler` -- the converter no longer accesses `ReferenceHandler` at all
4. Verify build succeeds

### Phase 4: Delete DTZeroReferenceHandler

1. Delete `src/DTZero/Internal/DTZeroReferenceHandler.cs`
2. Verify build succeeds

### Phase 5: Run All Tests

1. Run full test suite: `dotnet test src/DTZero.sln`
2. All tests must pass
3. Pay special attention to:
   - `InterfaceFactory_NonNeatooType_NoRefMetadata` -- verifies no `$id`/`$ref` for records
   - `InterfaceFactory_SimpleRecord_RoundTrip` -- verifies record round-trip works
   - All `InterfaceCollectionSerializationTests` -- verifies interface type serialization
   - All `SerializationTests` in Design project -- verifies Class Factory round-trips
   - `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` -- the original v0.21.3 test case

---

## Acceptance Criteria

- [ ] `DTZeroReferenceResolver.Current` is a `public static` property with `internal` setter
- [ ] `DTZeroJsonSerializer` has one `JsonSerializerOptions` instance (no `PlainOptions`)
- [ ] `DTZeroJsonSerializer` has no `IsNeatooType()` method
- [ ] `DTZeroJsonSerializer` has no `ReferenceHandler` property
- [ ] `Options.ReferenceHandler` is `null` (not set)
- [ ] `DTZeroReferenceHandler.cs` is deleted
- [ ] Dead code removed from `DTZeroInterfaceJsonTypeConverter.Read()`
- [ ] Resolver is always created and cleaned up in `try/finally` in all Serialize/Deserialize methods
- [ ] All existing tests pass without modification
- [ ] No `$id`/`$ref` metadata in serialized JSON for plain records

---

## Dependencies

- **No external dependencies.** All changes are within the `DTZero` project.
- **Neatoo coordinated update** is out of scope for this plan but must happen before Neatoo's next release. The migration path is documented in the Design section.

---

## Risks / Considerations

1. **Neatoo test failures.** Neatoo's converters currently access `options.ReferenceHandler.CreateResolver()`. After this change, `options.ReferenceHandler` will be `null`, causing `NullReferenceException` in Neatoo's converters. This is expected and intentional -- Neatoo must migrate to `DTZeroReferenceResolver.Current`. The 88-test failure documented in `isneatootype-missing-validatebase-check.md` will be resolved by the coordinated Neatoo update, not by this DTZero plan.

2. **Concurrent serialization.** `AsyncLocal` provides per-async-flow isolation, so concurrent serialization operations on different threads will each have their own resolver. This matches the existing behavior (the current `DTZeroReferenceHandler` also uses `AsyncLocal`).

3. **Resolver outside DTZeroJsonSerializer.** If someone constructs a `DTZeroReferenceResolver` manually or serializes Neatoo types using bare STJ (without `DTZeroJsonSerializer`), `DTZeroReferenceResolver.Current` will be `null`. Downstream converters must null-check. This is a new responsibility for Neatoo's converters.

4. **Breaking change for Neatoo.** The removal of `ReferenceHandler` from `options` is a breaking change for Neatoo's converters. This was acknowledged in clarification A7. The minimum DTZero version for the new Neatoo converters must be documented.

---

## Architectural Verification

**Scope Table:**

| Component | Affected? | Change |
|-----------|-----------|--------|
| `DTZeroReferenceResolver` | Yes | Add static `AsyncLocal` accessor |
| `DTZeroJsonSerializer` | Yes | Remove dual options, remove `IsNeatooType`, remove `ReferenceHandler`, simplify all methods |
| `DTZeroReferenceHandler` | Yes | Deleted entirely |
| `DTZeroInterfaceJsonTypeConverter` | Yes | Remove dead reference handler code |
| `DTZeroOrdinalConverterFactory` | No | Does not use `ReferenceHandler` |
| `DTZeroOrdinalConverter<T>` | No | Does not use `ReferenceHandler` |
| `DTZeroInterfaceJsonConverterFactory` | No | Does not use `ReferenceHandler` |
| `DTZeroJsonTypeInfoResolver` | No | Independent of `ReferenceHandler` |
| `DTZeroJsonConverterFactory` | No | Abstract base, no `ReferenceHandler` |
| `AddDTZeroServices` | No | DI registration unchanged |
| All test files | No | Must pass without modification |

**Verification Evidence:**

- `DTZeroInterfaceJsonTypeConverter.Read()` reference handler code is dead: `id` is initialized to `string.Empty` at line 32 and never assigned. The guard `!string.IsNullOrEmpty(id)` at line 44 always evaluates to `true` (isEmpty), so `AddReference` is never called. Evidence: `grep "id =" DTZeroInterfaceJsonTypeConverter.cs` returns only line 32.

- `DTZeroOrdinalConverterFactory` and `DTZeroOrdinalConverter<T>` do not reference `ReferenceHandler`: confirmed via grep -- zero matches.

- `DTZeroJsonTypeInfoResolver` does not reference `ReferenceHandler`: confirmed via grep -- zero matches.

- `serviceAssemblies` field in `DTZeroJsonSerializer` is used in `DeserializeRemoteDelegateRequest()` (line 364) and `FromObjectJson()` (line 408), so it must be retained even though `IsNeatooType()` is deleted.

**Breaking Changes:** Yes -- for Neatoo's converters (documented, acknowledged in A7). Not breaking for DTZero's own tests or public API.

**Codebase Analysis:**

Key files examined:
- `src/DTZero/Internal/DTZeroJsonSerializer.cs` -- dual options, `IsNeatooType`, resolver lifecycle
- `src/DTZero/Internal/DTZeroReferenceHandler.cs` -- `AsyncLocal<ReferenceResolver>` bridge class
- `src/DTZero/Internal/DTZeroReferenceResolver.cs` -- resolver implementation
- `src/DTZero/Internal/DTZeroInterfaceJsonTypeConverter.cs` -- dead reference handler code
- `src/DTZero/Internal/DTZeroOrdinalConverterFactory.cs` -- no `ReferenceHandler` usage
- `src/DTZero/Internal/DTZeroInterfaceJsonConverterFactory.cs` -- no `ReferenceHandler` usage
- `src/DTZero/Internal/DTZeroJsonTypeInfoResolver.cs` -- independent of `ReferenceHandler`
- `src/DTZero/AddDTZeroServices.cs` -- DI registration
- `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` -- 6 tests
- `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs` -- record round-trip test
- `src/Tests/DTZero.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs` -- 6 tests
- `src/Tests/DTZero.IntegrationTests/TypeSerialization/InterfaceCollectionSerializationTests.cs` -- 11 tests
- `src/Tests/DTZero.IntegrationTests/TypeSerialization/RecordSerializationTests.cs` -- 16 tests

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1-4: Core Changes | developer | Yes | All source changes are tightly coupled (4 files + 1 deletion), best done in a single pass | None |
| Phase 5: Test Verification | developer | No (same agent) | Same agent runs tests immediately after changes | Phases 1-4 |

**Parallelizable phases:** None -- all changes are sequential.

**Notes:** The changes touch only 4 files plus 1 deletion. The total scope is small enough for a single agent pass. A fresh agent for test verification is unnecessary since the developer needs the implementation context to diagnose any failures.

---

## Domain Model Behavioral Design

Not applicable -- this is an infrastructure/serialization change with no domain model impact.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-20

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `DTZeroJsonSerializer` constructor: remove `ReferenceHandler = this.ReferenceHandler` from `Options` initialization (line 78). STJ defaults `ReferenceHandler` to `null`. | `Options.ReferenceHandler == null` | Yes | |
| 2 | `DTZeroJsonSerializer` constructor: delete `PlainOptions` property (line 41) and initialization (lines 98-113). Only `this.Options` remains. | Single `JsonSerializerOptions` instance | Yes | |
| 3 | `DTZeroJsonSerializer`: delete `IsNeatooType()` method (lines 123-136). Replace 4 call sites (lines 152, 190, 225, 262) with unconditional `this.Options`. | `IsNeatooType()` does not exist | Yes | |
| 4 | `DTZeroJsonSerializer.Serialize(object?)` (line 138): replace lines 152-158 with `using var rr = new DTZeroReferenceResolver(); DTZeroReferenceResolver.Current = rr;` in `try/finally` that clears `Current` to `null`. | Resolver created, assigned to AsyncLocal, disposed after completion | Yes | Also incidentally fixes existing scoping bug where `using var rr` was inside `if` block and disposed before serialization. |
| 5 | `DTZeroJsonSerializer.Deserialize<T>(string?)` (line 213): same pattern as Rule 4, replacing lines 225-231. | Resolver created, assigned to AsyncLocal, disposed after completion | Yes | Same scoping fix applies. |
| 6 | `DTZeroReferenceResolver`: new `private static readonly AsyncLocal<DTZeroReferenceResolver?> _current = new()`. `Current` getter returns `_current.Value`. `AsyncLocal<T>` default for reference types is `null`. | `DTZeroReferenceResolver.Current` returns `null` | Yes | |
| 7 | `DTZeroJsonSerializer.Serialize`/`Deserialize`: `finally` block sets `DTZeroReferenceResolver.Current = null`. Executes on all exit paths including exceptions. | AsyncLocal cleared to `null` | Yes | |
| 8 | `DTZeroReferenceResolver`: `public static DTZeroReferenceResolver? Current` property with `public` getter. Converters call `DTZeroReferenceResolver.Current` instead of `options.ReferenceHandler.CreateResolver()`. | Converters access resolver via static property | Yes | |
| 9 | `DTZeroJsonSerializer.Serialize`/`Deserialize`: `DTZeroReferenceResolver.Current = rr` assigned before `JsonSerializer.Serialize`/`Deserialize` call. Converters execute within that scope. | Non-null resolver during serialization | Yes | |
| 10 | `DTZeroReferenceResolver` class is `public sealed` (line 8). `Current` getter is `public`. Both accessible from any assembly. | Downstream assemblies can access `Current` | Yes | |
| 11 | For plain records: `DTZeroOrdinalConverterFactory.CanConvert()` checks `IOrdinalSerializable.IsAssignableFrom(typeToConvert)` -- records do not implement this. `DTZeroInterfaceJsonConverterFactory.CanConvert()` checks `IsInterface \|\| IsAbstract` -- records are neither. With `Options.ReferenceHandler == null`, STJ default path never injects `$id`/`$ref`. | No `$id`/`$ref` in output | Yes | |
| 12 | For records with parameterized constructors: same converter bypass as Rule 11. No `$id` in JSON means no `ObjectWithParameterizedCtorRefMetadataNotSupported` error. STJ native parameterized constructor support handles deserialization. | Deserialization succeeds without error | Yes | |
| 13 | `DTZeroInterfaceJsonTypeConverter<T>.Write()` (lines 83-102): writes `$type` and `$value` explicitly. Does NOT use `options.ReferenceHandler`. No changes to this method. | `$type`/`$value` wrapper emitted | Yes | |
| 14 | `DTZeroInterfaceJsonTypeConverter<T>.Read()` (lines 22-81): reads `$type`/`$value`, resolves via `serviceAssemblies.FindType()`, deserializes via `JsonSerializer.Deserialize(ref reader, concreteType, options)`. Dead code removed but core logic unchanged. | Type resolution and deserialization unchanged | Yes | |
| 15 | `DTZeroInterfaceJsonTypeConverter<T>.Read()`: delete `var id = string.Empty` (line 32) and `if (!string.IsNullOrEmpty(id))` block (lines 44-48). `id` was never assigned from JSON stream, so `AddReference` was never called. | No `options.ReferenceHandler` call | Yes | Confirmed dead: grep shows `id` only assigned at line 32 (`string.Empty`), never reassigned. |
| 16 | Phase 4: delete file `src/DTZero/Internal/DTZeroReferenceHandler.cs`. Only 2 files reference `DTZeroReferenceHandler`: the file itself and `DTZeroJsonSerializer.cs` (which removes its reference in Phase 2). | File deleted, no dangling references | Yes | |
| 17 | Phase 2: delete `PlainOptions` property (line 41) + initialization (lines 98-113), delete `IsNeatooType()` (lines 123-136). All references confined to `DTZeroJsonSerializer.cs`. | Neither exists after redesign | Yes | |
| 18 | Phase 5: run `dotnet test src/DTZero.sln`. All existing tests pass. Critical tests: `InterfaceFactory_NonNeatooType_NoRefMetadata`, `InterfaceFactory_SimpleRecord_RoundTrip`, all `InterfaceCollectionSerializationTests`, Design project `SerializationTests`. | All tests pass | Yes | Depends on correct implementation of Rules 1-17. |

### Concerns

All concerns raised during review were acknowledged by the user:

1. **Existing scoping bug (acknowledged):** The BEFORE code disposes the resolver before serialization due to `using var` inside `if` block. The AFTER pattern fixes this as an intentional improvement.

2. **Stale test comment (acknowledged):** `InterfaceFactoryRecordSerializationTests.cs:133` references `PlainOptions`. Approved for comment-only update.

3. **`serviceAssemblies` field retention (acknowledged):** Keep the field (line 31), constructor parameters, and field assignment (line 71). Only delete `IsNeatooType()` and `PlainOptions`.

4. **Implementation ordering (acknowledged):** Steps 2.3 (remove `ReferenceHandler` property) and 2.6 (update method bodies) must happen together to avoid intermediate build failures.

5. **`ReferenceResolver` base class (acknowledged):** Inheritance is unnecessary after redesign but harmless. Future cleanup.

---

## Implementation Contract

**Created:** 2026-03-20
**Approved by:** dtzero-developer

### Verification Acceptance Criteria

No failing acceptance criteria from verification resources. All existing tests must continue to pass.

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | Verified by construction: inspect `DTZeroJsonSerializer` constructor | No dedicated test -- structural verification |
| 2 | `InterfaceFactory_NonNeatooType_NoRefMetadata` | Existing test validates no `$id`/`$ref` for records |
| 3 | `InterfaceFactory_SimpleRecord_RoundTrip` | Existing test validates parameterized ctor round-trip |
| 4 | `InterfaceFactory_RecordWithCollection_RoundTrip` | Existing test validates collection record round-trip |
| 5 | Verified by construction: `AsyncLocal<T>` default is `null` | No dedicated test -- language-level guarantee |
| 6 | Verified by construction: converter executes within `try` block scope | No dedicated test -- structural verification |
| 7 | Verified by construction: `finally` block always executes | No dedicated test -- language-level guarantee |
| 8 | `SingleInterface_SurvivesRoundTrip` + all `InterfaceCollectionSerializationTests` | Existing tests validate interface type serialization |
| 9 | Same as Scenario 8 | `$type`/`$value` wrapper tested by existing tests |
| 10 | Full test suite (`dotnet test src/DTZero.sln`) | All tests must pass |
| 11 | `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` | Design project record round-trip test |
| 12 | `ListOfInterface_JsonIncludesTypeInformation` | Validates `$type` presence in serialized JSON |
| 13 | File system check: `DTZeroReferenceHandler.cs` does not exist | Verified post-deletion |
| 14 | Grep check: no `IsNeatooType` in `DTZeroJsonSerializer.cs` | Verified post-deletion |

### In Scope

- [ ] `src/DTZero/Internal/DTZeroReferenceResolver.cs`: Add `private static readonly AsyncLocal<DTZeroReferenceResolver?> _current` field and `public static DTZeroReferenceResolver? Current` property (`public` getter, `internal` setter)
- [ ] `src/DTZero/Internal/DTZeroJsonSerializer.cs`: Remove `PlainOptions` property + initialization (lines 41, 98-113). Remove `ReferenceHandler` property + `Options` assignment (lines 43, 78). Remove `IsNeatooType()` method (lines 123-136). Remove `PlainOptions` comments (lines 37-40, 95-97). Update all four `Serialize`/`Deserialize` methods to use `DTZeroReferenceResolver.Current` in `try/finally` pattern with single `this.Options`. Keep `serviceAssemblies` field (line 31), all constructor parameters, and field assignment (line 71).
- [ ] `src/DTZero/Internal/DTZeroInterfaceJsonTypeConverter.cs`: Remove `var id = string.Empty` (line 32). Remove dead `if (!string.IsNullOrEmpty(id))` block (lines 44-48).
- [ ] `src/DTZero/Internal/DTZeroReferenceHandler.cs`: Delete entire file.
- [ ] `src/Tests/DTZero.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs`: Update comment at line 133 (replace `PlainOptions` reference with accurate description). Comment-only change, no test logic.

### Out of Scope

- All test assertions and test logic (no test behavior changes)
- `DTZeroOrdinalConverterFactory.cs` / `DTZeroOrdinalConverter<T>` (no `ReferenceHandler` usage)
- `DTZeroInterfaceJsonConverterFactory.cs` (no `ReferenceHandler` usage)
- `DTZeroJsonTypeInfoResolver.cs` (independent of `ReferenceHandler`)
- `DTZeroJsonConverterFactory.cs` (abstract base, no `ReferenceHandler` access)
- `AddDTZeroServices.cs` (DI registration unchanged)
- Neatoo coordinated migration (separate repo, separate plan)
- Documentation updates (handled in Step 9)
- `DTZeroReferenceResolver` base class removal (future cleanup, Concern 5)

### Verification Gates

1. After Phase 1 (AsyncLocal accessor): `dotnet build src/DTZero.sln` succeeds
2. After Phase 2 (DTZeroJsonSerializer simplification): `dotnet build src/DTZero.sln` succeeds
3. After Phase 3 (dead code removal from converter): `dotnet build src/DTZero.sln` succeeds
4. After Phase 4 (DTZeroReferenceHandler deletion): `dotnet build src/DTZero.sln` succeeds
5. Final: `dotnet test src/DTZero.sln` -- all tests pass, zero failures

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure (any test not listed in In Scope)
- Build failure after any phase that cannot be resolved by the planned changes
- Any test failure in Phase 5 that is not explained by the planned changes
- Discovery that `options.ReferenceHandler` is accessed by code not identified in the plan

---

## Implementation Progress

**Started:** 2026-03-20
**Developer:** dtzero-developer

### Milestones

| Phase | Description | Status | Build Result |
|-------|-------------|--------|-------------|
| 1 | Add `AsyncLocal` accessor to `DTZeroReferenceResolver` | Complete | 0 errors, 3 warnings (pre-existing WASM warnings) |
| 2 | Simplify `DTZeroJsonSerializer` (remove `PlainOptions`, `IsNeatooType()`, `ReferenceHandler` property; single options; `try/finally` resolver lifecycle) | Complete | 0 errors, 3 warnings |
| 3 | Remove dead code from `DTZeroInterfaceJsonTypeConverter` (`var id = string.Empty` and `if (!string.IsNullOrEmpty(id))` block) | Complete | 0 errors, 3 warnings |
| 4 | Delete `DTZeroReferenceHandler.cs` | Complete | 0 errors, 3 warnings |
| 5 | Update stale comment in `InterfaceFactoryRecordSerializationTests.cs:133`; run full test suite | Complete | All tests pass |

### Changes Made

**`src/DTZero/Internal/DTZeroReferenceResolver.cs`:**
- Added `using System.Threading;`
- Added `private static readonly AsyncLocal<DTZeroReferenceResolver?> _current = new();`
- Added `public static DTZeroReferenceResolver? Current` property with `public` getter and `internal` setter

**`src/DTZero/Internal/DTZeroJsonSerializer.cs`:**
- Removed `PlainOptions` property (line 41) and its comment block (lines 37-40)
- Removed `private DTZeroReferenceHandler ReferenceHandler` property (line 43)
- Removed `ReferenceHandler = this.ReferenceHandler` from `Options` construction (line 78)
- Removed `PlainOptions` construction block (lines 95-113) including ordinal converter and converter factory duplication
- Removed `IsNeatooType()` method (lines 117-136)
- Kept `serviceAssemblies` field (line 31), all constructor parameters, and field assignment (line 63)
- Updated `Serialize(object?)`: replaced `IsNeatooType` branching with `using var rr = new DTZeroReferenceResolver(); DTZeroReferenceResolver.Current = rr;` at method body scope, `try/finally` to clear `Current`
- Updated `Serialize(object?, Type)`: same pattern
- Updated `Deserialize<T>(string?)`: same pattern
- Updated `Deserialize(string?, Type)`: same pattern

**`src/DTZero/Internal/DTZeroInterfaceJsonTypeConverter.cs`:**
- Removed `var id = string.Empty;` (line 32)
- Removed dead `if (!string.IsNullOrEmpty(id))` block (lines 44-48) that called `options.ReferenceHandler!.CreateResolver().AddReference(id, result)`

**`src/DTZero/Internal/DTZeroReferenceHandler.cs`:**
- Deleted entire file

**`src/Tests/DTZero.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs`:**
- Updated comment at line 133 from `"serialize via the serializer that now selects PlainOptions for non-Neatoo types"` to `"serialize via the serializer (no ReferenceHandler in options, so records get no $id/$ref)"`

---

## Completion Evidence

**Reported:** 2026-03-20

- **Tests Passing:**
  - DTZero.UnitTests: 490 passed, 0 failed (net9.0); 490 passed, 0 failed (net10.0)
  - DTZero.IntegrationTests: 487 passed, 3 skipped, 0 failed (net9.0); 487 passed, 3 skipped, 0 failed (net10.0)
  - Design.Tests: 42 passed, 0 failed (net9.0); 42 passed, 0 failed (net10.0)
  - Total: 2,036 passed, 0 failed, 6 skipped (3 pre-existing ShowcasePerformanceTests skipped per TFM)
- **Verification Resources Pass:** Yes -- Design.Tests (42 tests x 2 TFMs) all pass
- **All Contract Items:** Confirmed 100% complete

### Contract Item Verification

| Contract Item | Status | Evidence |
|---------------|--------|----------|
| `DTZeroReferenceResolver.cs`: Add `AsyncLocal` field and `Current` property | Done | `private static readonly AsyncLocal<DTZeroReferenceResolver?> _current = new()` and `public static DTZeroReferenceResolver? Current` with `public` getter, `internal` setter |
| `DTZeroJsonSerializer.cs`: Remove `PlainOptions` property + initialization | Done | No `PlainOptions` in file |
| `DTZeroJsonSerializer.cs`: Remove `ReferenceHandler` property + `Options` assignment | Done | No `ReferenceHandler` property; `Options` has no `ReferenceHandler =` line |
| `DTZeroJsonSerializer.cs`: Remove `IsNeatooType()` method | Done | No `IsNeatooType` in file |
| `DTZeroJsonSerializer.cs`: Update all four Serialize/Deserialize methods | Done | All use `using var rr` + `DTZeroReferenceResolver.Current = rr` + `try/finally` pattern |
| `DTZeroJsonSerializer.cs`: Keep `serviceAssemblies` field and constructor params | Done | Field at line 31, assignment at line 63, used in `DeserializeRemoteDelegateRequest` and `FromObjectJson` |
| `DTZeroInterfaceJsonTypeConverter.cs`: Remove `var id = string.Empty` | Done | Line removed |
| `DTZeroInterfaceJsonTypeConverter.cs`: Remove dead `if (!string.IsNullOrEmpty(id))` block | Done | Block removed |
| `DTZeroReferenceHandler.cs`: Delete entire file | Done | File does not exist |
| `InterfaceFactoryRecordSerializationTests.cs`: Update stale comment at line 133 | Done | Comment updated, no test logic changed |

### Stop Condition Checks

- No out-of-scope test failures: zero test failures across all projects
- No unexpected build failures: all four phase builds succeeded with zero errors
- No undocumented `options.ReferenceHandler` access sites: grep confirms no remaining references outside the deleted file

---

## Documentation

**Agent:** business-requirements-documenter
**Completed:** 2026-03-20

### Expected Deliverables

- [x] `docs/serialization.md` -- Update "Scope: Neatoo Types Only" section to describe the new mechanism (converter-level reference handling, not serializer-level type classification)
- [x] `src/Design/CLAUDE-DESIGN.md` -- Update Anti-Pattern 9 "Why it matters" explanation to describe converter-level mechanism instead of dual-options split
- [x] `src/Design/CLAUDE-DESIGN.md` -- Quick Decisions Table record entry (line 153): no change needed (already accurate: "Records are serialized without `$id`/`$ref`")
- [x] `src/Design/CLAUDE-DESIGN.md` -- Common Mistakes item 10: no change needed (already accurate)
- [x] `src/Design/CLAUDE-DESIGN.md` -- design_version bumped to 1.2, last_updated to 2026-03-20
- [ ] Neatoo migration path documentation: Create a brief document or todo describing the required Neatoo converter changes (before/after pattern, minimum DTZero version) -- **Developer Deliverable** (see below)
- [x] Skill updates: N/A (no skill content references `IsNeatooType` or `PlainOptions`)
- [x] Sample updates: N/A

### Files Directly Updated

1. **`src/Design/CLAUDE-DESIGN.md`**
   - Updated `design_version` from 1.1 to 1.2, `last_updated` from 2026-03-08 to 2026-03-20
   - Updated Anti-Pattern 9 "Why it matters" explanation (line 419): replaced dual-options/type-classification description with converter-level mechanism description. Now explains that `JsonSerializerOptions` has no `ReferenceHandler`, converters access `DTZeroReferenceResolver.Current` via `AsyncLocal`, and STJ's native record deserialization cannot process `$ref` in parameterized constructors.
   - No changes to Quick Decisions Table (line 153), Common Mistakes item 10 (line 753), or Design Debt table -- all remain accurate as-is.

2. **`docs/serialization.md`**
   - Updated "Scope: Neatoo Types Only" section (lines 120-124): renamed heading to "Scope: Converter-Level, Not Serializer-Level". Replaced `IOrdinalSerializable` / factory assembly type classification description with converter-level mechanism. Now explains that `JsonSerializerOptions` has no `ReferenceHandler`, Neatoo converters access `DTZeroReferenceResolver.Current`, and plain records/DTOs are serialized natively by STJ.

### Developer Deliverables

1. **Neatoo migration path documentation**: Create a todo or brief document (in the Neatoo repo, not DTZero) describing the required Neatoo converter migration:
   - **Before**: `var resolver = options.ReferenceHandler!.CreateResolver();`
   - **After**: `var resolver = DTZeroReferenceResolver.Current; if (resolver != null) { ... }`
   - **Call sites**: `NeatooBaseJsonTypeConverter.Write()` and any other Neatoo converter accessing `options.ReferenceHandler.CreateResolver()`
   - **Null check**: Required because `DTZeroReferenceResolver.Current` is `null` when serialization is not in progress (e.g., bare STJ usage in unit tests)
   - **Minimum DTZero version**: v0.21.4

### General Documentation (Step 8B)

**Agent:** dtzero-docs-writer
**Completed:** 2026-03-20

- [x] `docs/release-notes/v0.21.4.md` -- Created release notes with overview, breaking changes, bug fixes, migration guide (before/after converter code), and commit summary
- [x] `docs/release-notes/index.md` -- Added v0.21.4 to highlights table and all releases list
- [x] `docs/release-notes/v0.21.3.md` -- Adjusted nav_order from 1 to 2
- [x] `src/Directory.Build.props` -- Bumped `PackageVersion` from 0.21.3 to 0.21.4
- [x] Skill files -- Verified no updates needed (no references to `IsNeatooType`, `PlainOptions`, `DTZeroReferenceHandler`, or `ReferenceHandler`)
- [x] `docs/serialization.md` -- Already updated by business-requirements-documenter (heading renamed, content reflects converter-level mechanism)

Step 8 documentation is complete.

---

## Architect Verification

**Verified:** 2026-03-20
**Verdict:** VERIFIED

### Independent Build Results

- `dotnet build src/DTZero.sln` -- 0 errors, 3 warnings (pre-existing WASM warnings in OrderEntry.BlazorClient)

### Independent Test Results

- DTZero.UnitTests: 490 passed, 0 failed (net9.0); 490 passed, 0 failed (net10.0)
- DTZero.IntegrationTests: 487 passed, 3 skipped, 0 failed (net9.0); 487 passed, 3 skipped, 0 failed (net10.0)
- Design.Tests: 42 passed, 0 failed (net9.0); 42 passed, 0 failed (net10.0)
- **Total: 2,038 passed, 0 failed, 6 skipped** (3 pre-existing ShowcasePerformanceTests skipped per TFM)

### Design Match Verification

Each acceptance criterion verified by reading the actual source files:

| Criterion | Verified? | Evidence |
|-----------|-----------|----------|
| `DTZeroReferenceResolver.Current` is `public static` with `internal` setter | Yes | Lines 19-23: `public static DTZeroReferenceResolver? Current { get => _current.Value; internal set => _current.Value = value; }` |
| `DTZeroJsonSerializer` has single `JsonSerializerOptions` (no `PlainOptions`) | Yes | Grep for `PlainOptions` returned zero matches in DTZeroJsonSerializer.cs |
| `DTZeroJsonSerializer` has no `IsNeatooType()` method | Yes | Grep for `IsNeatooType` returned zero matches in DTZeroJsonSerializer.cs |
| `DTZeroJsonSerializer` has no `ReferenceHandler` property | Yes | Grep for `ReferenceHandler` returned zero matches in DTZeroJsonSerializer.cs |
| `Options.ReferenceHandler` is `null` (not set) | Yes | Options construction (lines 68-73) has no `ReferenceHandler =` assignment |
| `DTZeroReferenceHandler.cs` is deleted | Yes | File read returned "File does not exist" |
| Dead code removed from `DTZeroInterfaceJsonTypeConverter.Read()` | Yes | No `var id = string.Empty` or `AddReference` call; grep confirmed zero matches |
| Resolver is always created and cleaned up in `try/finally` | Yes | All four methods confirmed: `Serialize(object?)` lines 100-120, `Serialize(object?, Type)` lines 136-156, `Deserialize<T>` lines 169-189, `Deserialize(string?, Type)` lines 204-224 |
| All existing tests pass without modification | Yes | 2,038 tests passed, 0 failures |
| `serviceAssemblies` field retained | Yes | Field at line 31, used in `DeserializeRemoteDelegateRequest` (line 306) and `FromObjectJson` (line 350) |
| No `DTZeroReferenceHandler` references remain | Yes | Grep for `DTZeroReferenceHandler` across `src/DTZero/` returned zero matches |
| Comment-only change in `InterfaceFactoryRecordSerializationTests.cs` | Yes | Line 133 updated to `"serialize via the serializer (no ReferenceHandler in options, so records get no $id/$ref)"` -- no test logic changed |

### Issues Found

None. Implementation matches the design precisely across all 5 files (4 modified + 1 deleted).

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer
**Verified:** 2026-03-20
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| **Serialization round-trip for Class Factory types** (Design.Tests SerializationTests, 6 tests) | SATISFIED | 42 Design.Tests pass per TFM. `DTZeroJsonSerializer` methods all use single `this.Options` with `try/finally` resolver lifecycle. Round-trip path unchanged: `ToObjectJson` -> `Serialize` -> resolver set -> converters run -> resolver cleared. |
| **Interface Factory record round-trip without `$id`/`$ref`** (v0.21.3 fix, `InterfaceFactoryTests.cs:96-117`) | SATISFIED | `Options.ReferenceHandler` is `null` (no assignment in constructor, lines 68-73). STJ does not inject `$id`/`$ref` when `ReferenceHandler` is null. Records bypass all custom converters (`DTZeroOrdinalConverterFactory` checks `IOrdinalSerializable`, `DTZeroInterfaceJsonConverterFactory` checks `IsInterface || IsAbstract` -- records match neither). Test `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` passes. |
| **No `$id`/`$ref` metadata in serialized JSON for plain records** (Anti-Pattern 9, `CLAUDE-DESIGN.md:378-419`) | SATISFIED | Verified via `InterfaceFactory_NonNeatooType_NoRefMetadata` test (6 tests in `InterfaceFactoryRecordSerializationTests`). With `Options.ReferenceHandler == null`, STJ's native path serializes records without reference metadata. Anti-Pattern 9 rule text remains accurate: "plain records/DTOs are serialized without reference handling." |
| **Records with parameterized constructors deserialize without error** (Quick Decisions Table, `CLAUDE-DESIGN.md:153`) | SATISFIED | No `$id` in JSON means no `ObjectWithParameterizedCtorRefMetadataNotSupported` error. 12 `RecordSerializationTests` plus 4 ClientServer tests pass. |
| **Interface-typed property serialization emits `$type`/`$value`** (`DTZeroInterfaceJsonTypeConverter`) | SATISFIED | `Write()` method (lines 76-95) writes `$type` and `$value` explicitly, independent of `ReferenceHandler`. `Read()` method (lines 22-74) reads `$type`/`$value` and resolves concrete type. Dead code removed (lines 31-48 in old file), core logic unchanged. 11 `InterfaceCollectionSerializationTests` pass. |
| **`AsyncLocal` resolver accessor: public getter, internal setter, nullable** (Gap 1 from requirements review) | SATISFIED | `DTZeroReferenceResolver.cs` lines 19-23: `public static DTZeroReferenceResolver? Current { get => _current.Value; internal set => _current.Value = value; }`. Downstream converters can read; only `DTZeroJsonSerializer` can set. Null when no operation in progress. |
| **Resolver lifecycle: created before, cleared after (including exceptions)** (Business Rule 7) | SATISFIED | All four Serialize/Deserialize methods follow identical pattern: `using var rr = new DTZeroReferenceResolver(); DTZeroReferenceResolver.Current = rr; try { ... } finally { DTZeroReferenceResolver.Current = null; }`. The `finally` block guarantees cleanup on all exit paths. |
| **Single `JsonSerializerOptions` instance (no `PlainOptions`)** (Business Rule 2) | SATISFIED | Grep for `PlainOptions` in `src/DTZero/` returns zero matches. Constructor (lines 68-84) creates one `Options` instance. |
| **No `IsNeatooType()` method** (Business Rule 3) | SATISFIED | Grep for `IsNeatooType` in `src/DTZero/` returns zero matches. |
| **`DTZeroReferenceHandler.cs` deleted** (Business Rule 16) | SATISFIED | File glob for `**/DTZeroReferenceHandler.cs` returns no matches. Grep for `DTZeroReferenceHandler` in `src/DTZero/` returns zero matches. |
| **Properties need public setters** (Critical Rule 5, `CLAUDE-DESIGN.md:603-610`) | NOT AFFECTED | No property visibility changes in this implementation. |
| **`[Remote]` only for aggregate root entry points** (Critical Rule 1, `CLAUDE-DESIGN.md:425-442`) | NOT AFFECTED | No changes to factory patterns or `[Remote]` behavior. |
| **All existing tests pass without modification** (Business Rule 18) | SATISFIED | 2,038 tests passed, 0 failures, 6 skipped (pre-existing). No test assertions or logic modified; only one comment updated in `InterfaceFactoryRecordSerializationTests.cs:133`. |
| **`serviceAssemblies` field retained** (Plan Concern 3) | SATISFIED | Field at `DTZeroJsonSerializer.cs:31`, assignment at line 63, used in `DeserializeRemoteDelegateRequest` (line 306) and `FromObjectJson` (line 350). |
| **Files NOT changed that should not be changed** | SATISFIED | Verified zero `ReferenceHandler` matches in: `DTZeroOrdinalConverterFactory.cs`, `DTZeroInterfaceJsonConverterFactory.cs`, `DTZeroJsonTypeInfoResolver.cs`, `AddDTZeroServices.cs`. No generator code references changed components. |

### Unintended Side Effects

**None found.** Specifically verified:

1. **Generated code patterns (Design projects):** No impact. The generator does not reference `DTZeroReferenceHandler`, `PlainOptions`, or `IsNeatooType`. Generated `IOrdinalSerializable` implementations are unaffected. The `DTZeroOrdinalConverterFactory` and `DTZeroOrdinalConverter<T>` do not use `ReferenceHandler`.

2. **Serialization contracts:** No change to what survives the client/server round-trip. The `$type`/`$value` wrapper for interface-typed properties is emitted by `DTZeroInterfaceJsonTypeConverter.Write()` independent of `ReferenceHandler`. The `$id`/`$ref` metadata for Neatoo types is emitted by downstream Neatoo converters (out of scope), not by DTZero's converters. Plain records/DTOs continue to serialize without reference metadata.

3. **Design project tests:** All 42 tests pass per TFM. The Design projects demonstrate correct patterns and remain accurate as the source of truth.

4. **Published docs accuracy:** `docs/serialization.md` lines 120-124 ("Scope: Neatoo Types Only") describes the user-facing behavior correctly -- plain records/DTOs are serialized without reference handling. The internal mechanism description is slightly stale (refers to the old approach conceptually) but the plan correctly tracks this as a Step 9 documentation deliverable. No user-facing behavioral inaccuracy.

5. **Anti-Pattern 9 (`CLAUDE-DESIGN.md:378-419`):** The rule and its "Why it matters" explanation remain behaviorally accurate. The statement "the record is serialized without reference handling, but the embedded Neatoo type expects it" is still correct. The plan tracks the mechanism description update as a Step 9 deliverable.

6. **Breaking change for downstream Neatoo converters:** Acknowledged in plan Risks section (item 1 and 4). Neatoo's converters that call `options.ReferenceHandler.CreateResolver()` will get `NullReferenceException` until migrated to `DTZeroReferenceResolver.Current`. This is intentional and documented. The migration path is documented in the plan's Design section (lines 247-264).

7. **Multi-targeting (net9.0/net10.0):** Tests pass on both frameworks. `AsyncLocal<T>` is available in both. No framework-specific concerns.

### Issues Found

None.
