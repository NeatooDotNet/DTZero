# Authorization and Trust

**Plan #:** 005
**Date:** 2026-11-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-7
**Status:** In Progress
**Last Updated:** 2026-11-17
**Plan-review opt-in:** No
**Code-review opt-in:** No (Design samples only; the test review covers them)
**Branch:** `PITCH-005-authorization` (now on top of `PITCH-009-auth-inheritance`, PR #45, itself on PR #44)
**PR:** —

---

## Scope

Make the authorization page carry the verdict's fifth ruling: authorization is a headline strength. Add the Design samples the page needs to say so honestly, and a section on trust. It does not touch the other component pages (PITCH-008) or the skill, and it changes no library or generator code.

---

## Intent

- A reader sees that authorization is one class with injected dependencies, shared across types through a base, enforced on every path into the server.
- A reader is told plainly that what the client sends is input, and sees a rule that decides from the stored record.
- Each claim is one a Design test pins.

---

## Framework & Architectural Alignment

- `verdict.md` section 8, fifth ruling: examples take the user by constructor; a base authorization class holds the broad read and write check.
- `docs/VisionAlignment.md` rulings on authorization; `.claude/rules/design-snippets.md`.
- The public API sample from PITCH-003.

---

## Constraints & Invariants

- No library or generator change. If a sample cannot be written without one, the work stops and the user decides.
- No existing test is changed or weakened.
- No reflection.
- The existing target-rule sample keeps working; the trust sample is added beside it.

---

## Steps

1. Add a shared authorization base to Design and an auth class that uses it, pinned by tests.
2. Reach an authorized aggregate through a public endpoint, and pin that the rule refuses there too.
3. Add a write rule that decides from the stored record, and pin it with a test that sends a tampered object.
4. Replace "cascade" as the name of the save chain in the Design source comment.
5. Revise `docs/authorization.md`: why, the base, trust, and when to leave each approach out.

---

## Acceptance

- [x] A broad check declared once on a shared base refuses an operation on a type whose own auth class does not restate it `[integration]`
- [x] A caller refused by an aggregate's authorization is refused through a public endpoint that uses the same factory `[integration]`
- [x] A write is refused from the stored record's state even when the object the client sends claims otherwise `[integration]`
- [x] `docs/authorization.md` opens with why authorization lives on the factory, shows the base, has a trust section, and says when to leave each approach out `[explicit-skip: document]`
- [x] Both solutions build and test green `[explicit-skip: covered by the gate logs]`

---

## Current State (Pre-Flight)

- Baseline: PITCH-003's logs (`reviews/003-impl-build.log`, `reviews/003-impl-test.log`); no file under `src` has changed since. Design.Tests: 187 passed, 1 skipped per framework.
- `AuthorizedOrderAuth` takes `ICurrentUser` by constructor. No Design sample or test has an auth interface or class that inherits its methods.
- `ParamAuthOrderAuth.CanWrite(IParamAuthOrder target)` refuses a write when the sent object's `Status` is `Locked`. The status it reads is the one the client sent.
- The public endpoints use `Order`, which has no authorization.

---

## Punchlist

---

## Test Evidence

Logs: `reviews/005-impl2-build.log` and `reviews/005-impl2-test.log` (both solutions, after Step 1), then `reviews/005-impl3-*.log` (Design only, after a formatting change to one region). Design.Tests 187 to 200 per framework, 1 skipped; UnitTests 901 and IntegrationTests 691 (5 skipped) as PITCH-009 left them. All tests are in `src/Design/Design.Tests`, on the real client/server containers or the real server host.

| Acceptance bullet | Test | Tier |
|---|---|---|
| Broad check on a shared base refuses, not restated | `SignedOut_IsRefusedFetch_ByTheInheritedReadCheck`, `ReadOnlyCaller_CanFetch_ButIsRefusedSave_ByTheInheritedWriteCheck`, `NonManager_SavesAnEdit_ButIsRefusedDelete_ByTheDerivedClassCheck` (`FactoryTests/AuthorizationBaseTests.cs`) | integration, confirmed |
| Refused through a public endpoint that uses the same factory | `Get_CallerTheRulesRefuse_Is403_LikeTheApplicationsFetch`, `Get_AnonymousCaller_Is403` (`Http/PublicAuthorizedOrderApiHttpTests.cs`) | integration, confirmed |
| Write refused from the stored record when the sent object claims otherwise | `LockedInStore_SentAsActive_SaveIsRefused_AndTheStoredRowIsUnchanged`, `LockedInStore_SentAsNew_SaveIsRefused_AndTheStoredRowIsUnchanged` (`FactoryTests/StoredRecordAuthorizationTests.cs`) | integration, confirmed |

Also added: `SalesManager_PassesEveryCheck`, `OnlyTheNamedAuthClass_IsRegistered`, `Get_CallerTheRulesAllow_GetsTheContract`, `ActiveInStore_SaveIsAllowed_AndTheStoredRowChanges`, `CanSaveTarget_AnswersFromTheStoredRow`, and `ParamAuth_Save_DecidesFromTheStatusTheClientSends`, which pins the contrast the trust section describes.

---

## Gate Record

---

## Plan Amendments

- 2026-11-17, Step 1: the base is an abstract class, `AuthorizationBase`, and `Quote` names the derived class `QuoteAuth` in `[AuthorizeFactory<T>]`, where the other Design samples name an interface. An interface cannot inherit from a class. The base uses `OrderRoles` for its role names; no role constant was added.
- 2026-11-17, Step 5: one line in region `public-api-authorized` was over the snippet width limit and was wrapped (formatting only). `docs/public-api.md` gained a section on authorization. Embeds of the server setup and service registration regions in other pages gained lines, as expected.

- 2026-11-17, Step 1 unblocked. Keith ruled on 2026-11-16 to fix the generator; PITCH-009 (PR #45) did, and this branch is rebased onto it. The two Parked rows are removed: PITCH-009 did that work. The page also documents NF0107, NF0108 and NF0109, and the rule that a restated check combines operations.

- 2026-11-15, Step 1 stopped. Probes showed the generator does not support a shared base as the step assumed. (A) A base interface: the inherited check is dropped silently. (B) An abstract base class: the server fails at startup. (C) One interface restating the broad check, with an abstract class implementing it: works, but restates. (D) A concrete base class used as its own auth type: works, as a separate service. No sample was written. Awaiting the user's ruling; both defects are on the todo's Parked list as proposed.
- 2026-11-15, Step 2: a refused Fetch returns null to server code, the same as not found. The endpoint asks `CanFetch` first and answers 403; `Results.Forbid` is not used because Design.Server registers no scheme. `app.MapPublicAuthorizedOrderApi()` is inside the embedded server setup region.
- 2026-11-15, Step 3: the stored-record rule is on a new aggregate, `LockableOrder`, because `ParamAuthOrder` stores nothing. The rule is `[Remote] internal`, so `CanSave(target)` asks the server; `CanSave()` with no argument returns allowed. The contrast test pins that a rule reading the sent object follows what the client sends.
- 2026-11-15, Step 4: comment wording changed in `OrderLine.cs`, `Order.cs` and two test files (comments only). `src/Design/CLAUDE-DESIGN.md` still says "cascade" in five places; left for PITCH-008.

---

## Notes
