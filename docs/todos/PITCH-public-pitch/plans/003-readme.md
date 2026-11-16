# Rewrite the README Around the Case

**Plan #:** 003
**Date:** 2026-11-13
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-7
**Status:** Done
**Last Updated:** 2026-11-13
**Plan-review opt-in:** No (the user reads the outline before the prose is written)
**Code-review opt-in:** No (one small Design sample; the test review covers it)
**Branch:** `PITCH-003-readme`
**PR:** —

---

## Scope

Rewrite `README.md` from the ruled verdict and the confirmed DDD map: the problem, the audience and when not to use DTZero come before any code, and the first screen has to earn a stranger's next minute. Add the one Design sample the README's public-API claim needs. It does not touch `docs/` or the skill.

---

## Intent

- A .NET developer who has never heard of DTZero knows within the first screen what it removes, why that was ever needed, and whether it is for them.
- The reader who objects (as the verdict's author did) finds each objection answered in the order it occurs to them.
- Every claim the README makes is one the repository can show: a Design sample with a test, or a link to a page that has one.

---

## Framework & Architectural Alignment

- `verdict.md` rulings R1 to R5 and the six dated rulings in its section 8.
- `ddd-map.md`: "factory methods call factory methods" is the named idea; root and child are roles; "cascade" is not the public word.
- `.claude/rules/design-snippets.md`: every C# sample is an embedded Design region with a test; region names are unique with a topic prefix.
- DDD terms are used without definition.

---

## Constraints & Invariants

- No hand-written C# in the README.
- The public-API sample is a hand-written endpoint on the Design server that calls the generated factory. It adds no library code and no generator change.
- The public contract in that sample is a designed type, not the domain object, because that is the claim: the contract is designed, the logic is the one copy.
- No comparison on trimming or AOT with CSLA (R3). No download or star predictions.
- Nothing written against LazyLoad. The old library name appears nowhere.

---

## Steps

1. Add the public-API sample to Design: an endpoint for outsiders that loads an aggregate through its factory, runs a domain verb on it and saves it, returning a designed contract. Pin it with an HTTP test on the real server host.
2. Put the README outline to the user and settle its order and its first screen.
3. Write the README from the outline, embedding Design regions only.
4. Run mdsnippets, check the diff for swallowed sections, and build and test both solutions.

---

## Acceptance

- [x] A caller of the public endpoint gets the designed contract for a stored order `[integration]`
- [x] A domain verb called through the public endpoint changes the stored order by the same rule the client's call uses `[integration]`
- [x] The public endpoint refuses what the domain refuses, without a second copy of the rule `[integration]`
- [x] The README states the problem, the audience and when not to use DTZero before its first code block `[explicit-skip: document]`
- [x] The README answers the three objections in order: persistence in the entity, "only a niche", "DTOs do more" `[explicit-skip: document]`
- [x] Every C# block in the README is an embedded Design region `[explicit-skip: document]`
- [x] Both solutions build and test green after the change `[explicit-skip: covered by the gate logs]`

---

## Current State (Pre-Flight)

- The README opens with a tagline and a paragraph, then "The Opportunity" with the "changes everything" line the verdict dropped, then a list comparison, then 125 lines of sample before any audience or limit is stated. It has no "when not to use it", no mention of CSLA, authorization only as a feature bullet, and "Zero boilerplate" as the first feature.
- Design.Server has no endpoint besides `UseDTZero()`. Its composition is `AddDesignServer()` in `ServerServices.cs`; HTTP tests host the real `Program` through `Design.Tests/Http/DesignHttpHost.cs`.
- `Order` has `Submit()` and `CanSubmit`, so a verb with a rule already exists to reuse.

---

## Punchlist

- Moved to the todo's Punchlist (cross-plan): authorization on the public endpoints.

---

## Test Evidence

All in `src/Design/Design.Tests/Http/PublicOrderApiHttpTests.cs`, on the real server host. Logs: `reviews/003-impl-build.log`, `reviews/003-impl-test.log` (Design.Tests 180 to 187 per framework).

| Acceptance bullet | Test | Tier |
|---|---|---|
| Designed contract for a stored order | `Get_StoredOrder_ReturnsTheDesignedContract`, `Get_MissingOrder_Is404` | integration, confirmed |
| Verb through the public endpoint changes the stored order by the same rule | `Submit_DraftWithLines_ChangesTheStoredOrder` (read back through the DTZero client factory) | integration, confirmed |
| Refuses what the domain refuses, no second copy | `Submit_EmptyDraft_IsRefused_LikeTheApplicationsCommand_AndStaysADraft`, `Submit_AlreadySubmitted_IsRefused_AndTheStoredOrderIsUnchanged` | integration, confirmed |

Also added, beyond the bullets: `Get_NonNumericId_Is400`, `Submit_MissingOrder_Is404`.

---

## Gate Record

- Round 1, 2026-11-15, `reviews/003-test-review.md`: no veto-tier finding on tests, logs or existing tests; the three behavioral bullets are pinned at integration tier. Verdict DECISION NEEDED on one document point: the audience and "when not to use it" came after the first code block, against AC-3.
- Resolved without a ruling by meeting the criterion: an "Is it for you?" section now states the audience and the four limits before the first code block, and links to the detailed section. No second round was run; the change is markdown only and was checked by heading and code-fence line numbers.
- Not triaged (reviewer listed as tech-debt or theoretical): fixed ids in the missing-order tests; the `Save ... ?? throw` branch.

---

## Plan Amendments

- 2026-11-13, Step 1: the route has no `:int` constraint. With one, a non-numeric id matched no endpoint and the SPA fallback answered 200 with `index.html`; without it the request reaches the endpoint and gets 400. `Get_NonNumericId_Is400` pins this.
- 2026-11-13, Step 1: `app.MapPublicOrderApi()` and its comment sit inside the `getting-started-server-program` region, so the README and getting-started embeds gain two lines at the next mdsnippets run.
- 2026-11-13, Step 1: `Order` has no authorization, so the sample shows none. `[Service]` parameters resolve from the request scope when server code calls the factory.

---

## Notes
