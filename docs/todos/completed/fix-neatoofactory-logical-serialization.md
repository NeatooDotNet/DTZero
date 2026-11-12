# Fix DTZeroFactory.Logical - Remove Incorrect Serialization

## Status: Complete

**Completed:** 2026-01-24

## History

The three-mode enum was introduced in commit `dfe021ab` on May 13, 2025 (version 9.15.0). Before this, there were only two modes: `Local` and `Remote`.

The commit added the `Local` mode (later renamed to `Logical`) with this comment:
```csharp
else if (remoteLocal == DTZeroFactory.Local)
{
    // Client Only
    // We still Serialize the objects
    // but we don't need to make a call to the server
    services.AddScoped<IMakeRemoteDelegateRequest, MakeLocalSerializedDelegateRequest>();
}
```

The serialization was included from the very beginning - this was not a regression but an original (incorrect) design decision.

## The Issue

`DTZeroFactory.Logical` was performing JSON serialization when it should not.

### Intended Semantics

| Mode | Description | Serialization |
|------|-------------|---------------|
| `DTZeroFactory.Server` | Server in 3-tier architecture | Deserializes requests, serializes responses |
| `DTZeroFactory.Remote` | Client in 3-tier architecture | Serializes requests, deserializes responses |
| `DTZeroFactory.Logical` | Single-tier app, no server | **NO serialization** |

## Solution Implemented

1. **Removed serialization from `DTZeroFactory.Logical`** - Changed DI registration so Logical mode no longer registers `IMakeRemoteDelegateRequest`, causing generated factories to use the local constructor (direct execution).

2. **Deleted `MakeLocalSerializedDelegateRequest`** - This class was no longer needed after the fix.

3. **Added verification tests** - Created `ModeBehaviorTests.cs` with 8 tests to verify:
   - `DTZeroFactory.Server`: Does NOT register `IMakeRemoteDelegateRequest`
   - `DTZeroFactory.Logical`: Does NOT register `IMakeRemoteDelegateRequest` (fixed behavior)
   - `DTZeroFactory.Remote`: DOES register `IMakeRemoteDelegateRequest`
   - Server and Logical modes use local constructor (identical behavior)
   - All factory operations execute correctly in both modes

4. **Updated documentation**:
   - Updated DTZeroFactory enum XML docs
   - Updated `factory-modes.md` reference documentation
   - Updated `LogicalContainerBuilder` and `LocalContainerBuilder` remarks
   - Updated `LogicalModeTests` remarks

## Files Changed

### Modified
- `src/DTZero/AddDTZeroServices.cs` - Removed Logical mode serialization registration
- `docs/reference/factory-modes.md` - Updated Logical mode documentation
- `src/Tests/DTZero.UnitTests/TestContainers/LogicalContainerBuilder.cs` - Updated XML remarks
- `src/Tests/DTZero.IntegrationTests/TestContainers/LocalContainerBuilder.cs` - Updated XML remarks
- `src/Tests/DTZero.UnitTests/Logical/LogicalModeTests.cs` - Updated XML remarks

### Deleted
- `src/DTZero/Internal/MakeLocalSerializedDelegateRequest.cs` - No longer needed

### Created
- `src/Tests/DTZero.UnitTests/FactoryModes/ModeBehaviorTests.cs` - Verification tests for mode behavior

## Test Results

All 1,948 tests pass across all three target frameworks (net8.0, net9.0, net10.0):
- 440 UnitTests per framework
- 432 IntegrationTests per framework
- Plus other test projects

The new `ModeBehaviorTests` (8 tests) verify the fix works correctly.
