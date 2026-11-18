## Test Review — PITCH-005 — 2026-11-17 — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED: `reviews/005-impl2-build.log` has both solutions, 0 errors. Its 2 warnings are the existing `NativeFileReference` warnings from `OrderEntry.BlazorClient`, not in this plan's scope. `reviews/005-impl3-build.log` (Design only) has 0 warnings and 0 errors. Tests: `reviews/005-impl2-test.log` shows UnitTests 901/0 failed, IntegrationTests 691 passed with 5 skipped, and Design.Tests 200 passed with 1 skipped, on both net9.0 and net10.0. `reviews/005-impl3-test.log` shows Design.Tests 200 passed with 1 skipped on both frameworks. No failures. Design.Tests went from 187 to 200, which matches the 13 tests added.

### Veto-tier
None.

- **Bullet 1 (a broad check on the shared base refuses, and the derived class does not restate it).** Three tests pin this, all through the client container and its serializing stand-in.
  - `SignedOut_IsRefusedFetch_ByTheInheritedReadCheck`: Fetch returns null after one remote call, and Create returns null. Only `IsSignedIn` covers Read, so this isolates the inherited read check.
  - `ReadOnlyCaller_CanFetch_ButIsRefusedSave_ByTheInheritedWriteCheck`: an Update is refused when only Sales is removed. `CanDelete` does not apply to an Update, so this isolates the inherited `CanWrite`.
  - `NonManager_SavesAnEdit_ButIsRefusedDelete_ByTheDerivedClassCheck`: the derived class's own check still refuses.
  - `QuoteAuth` declares only `CanDelete`.
- **Bullet 2 (a public endpoint refuses the same caller the factory refuses).** `Get_CallerTheRulesRefuse_Is403_LikeTheApplicationsFetch` runs on the real `Program` host. It asserts a 403 with no contract body for a Manager, and asserts that the application's own client, signed in as the same Manager, gets null from Fetch. `Get_AnonymousCaller_Is403` covers an anonymous caller.
- **Bullet 3 (a write is refused from the stored record whatever the sent object says).** Two tests pin this.
  - `LockedInStore_SentAsActive_SaveIsRefused_AndTheStoredRowIsUnchanged`: Save throws `NotAuthorizedException`, and the stored status and name are unchanged.
  - `LockedInStore_SentAsNew_SaveIsRefused_AndTheStoredRowIsUnchanged`: `TrySave` returns no access, and `Assert.Same` shows the stored row was not replaced.
  - `ActiveInStore_SaveIsAllowed_AndTheStoredRowChanges` is the positive control.
- **Sacred tests.** In `AggregateTests.cs` and `OrderLineDualityTests.cs`, only comments changed (the "cascade" wording). `ParamAuthorizationTests.cs` gained one test and lost nothing. `DesignClientServerContainers.cs` gained only the `ILockableOrderStore` registrations. No assertion, case or expected value was changed.

### Callouts (≤ 5 Optional)
None.

### Tech-debt (untiered, ≤ 5, one line each)
- `src/Design/CLAUDE-DESIGN.md` still says "cascade" in five places. Already recorded in the plan's amendments for PITCH-008.

### Could not verify
- The logs were written 1 to 3 minutes before the commits they cover (`005-impl3-*` at 22:12, the docs commit with the `PublicOrderApi.cs` wrap at 22:13). Nothing on disk proves that the committed tree is the tree that was built. The timestamps fit a build-then-commit order. The branch was rebased onto the #44 and #45 merges while this review ran, and the tree is identical to the pre-rebase head (`git diff b587aba HEAD` is empty).

### Theoretical (not triaged)
- `PublicAuthorizedOrderApi`'s `NotFound` branch. `AuthorizedOrder.Fetch` never fails once `CanFetch` has passed, so no input reaches it.
- `LockableOrder.Fetch` returning false for a missing row, and `LockableOrder.Update` throwing for a missing row. Nothing in the repository calls either with an unknown id.
- Two `Quote` claims in `docs/authorization.md` (line 181) are not shown by a `Quote` refusal:
  - An Insert runs `CanWrite`.
  - A Delete also runs `CanWrite`.

  Both follow from the Write flag, which the Operation Flags table states and the existing tests cover.
- `docs/authorization.md` line 358 ("an Update that must not change a field takes that field from the stored row") is advice. No sample shows it. `LockableOrder.Update` maps Status from the sent object, which is the design: locking is a change the client saves.

### Document check (requested by the brief)
- **No hand-written C#.** `docs/authorization.md` has 16 C# fences and `docs/public-api.md` has 4, and every one sits between a `snippet:`/`endSnippet` pair. Neither file has a fence of another language. The `public-api-authorized` source link (`PublicOrderApi.cs#L134-L153`) matches the region's lines, and the embedded text matches the wrapped source.
- **Prose claims match the tests and samples:**
  - **A Shared Authorization Base.** Fetch runs `IsSignedIn` and Update runs `CanWrite` (`AuthorizationBaseTests`). Delete needs `CanDelete` (NonManager test). Only the named class is registered and the base is not (`OnlyTheNamedAuthClass_IsRegistered`).
  - **What Is Inherited.** The interface chain, the base class, implemented interfaces, overrides running once, restatements combining operations, `[Remote]` on either declaration making the check remote, and NF0107, NF0108 and NF0109 are pinned by PITCH-009's tests:
    - `src/Tests/DTZero.IntegrationTests/Authorization/AuthInheritanceTests.cs`
    - `src/Tests/DTZero.UnitTests/FactoryGenerator/Authorization/InheritedAuthEmissionTests.cs`, including `RestatedCheck_RemoteOnAnyDeclaration_MakesTheCombinedCheckRemote`
    - `src/Tests/DTZero.UnitTests/Diagnostics/NF0107Tests.cs`, `NF0108Tests.cs` and `NF0109Tests.cs`

    "since v2.0.1" matches `src/Directory.Build.props` and `docs/release-notes/v2.0.1.md`. The page and the release notes both state that a generic base interface is not supported.
  - **What the Client Sends Is Input.** "A client that changes Locked to Active … passes the rule" is pinned by `ParamAuth_Save_DecidesFromTheStatusTheClientSends`. "A locked order sent as Active is still refused" is pinned by `LockedInStore_SentAsActive_…`. "CanSave(target) asks the server" is pinned by `CanSaveTarget_AnswersFromTheStoredRow` (the remote call count goes up by 1).
  - **Behind a Public Endpoint.** "A refused Fetch returns null to server code" is pinned by the existing `AuthorizedOrder_Fetch_ReturnsNull_WhenDenied` (local container). The 403 for a refused caller and for an anonymous caller is pinned by the HTTP tests. The endpoint calls `CanFetch` and does not restate the rule (sample).

### Read report
- **Read beyond the brief:**
  - `DesignClientServerContainers.cs` and `TestUser.cs`, to confirm one shared user and its default roles.
  - `DesignHttpHost.cs`, for the test scheme and `CreateClientScope`.
  - `AuthorizedOrderAuth.cs`, for which rules refuse a Manager.
  - The PITCH-009 test names, to check the "What Is Inherited" claims.
  - `src/Directory.Build.props` and `docs/release-notes/v2.0.1.md`, for the version claim.
  - `AuthorizationTests.AuthorizedOrder_Fetch_ReturnsNull_WhenDenied`, for the server-code null claim.
- **Named but unused:** none.
