## Test Review — PITCH-003 — [2026-11-15] — Round 1

**Verdict: DECISION NEEDED — did the user's Step 2 outline ruling move "Who it is for" and "When not to use it" after the code? The README places them after the first code block, which fails AC-3 (Required) and plan bullet 4 as written, and no amendment records a change.**
**Logs:** build PASSED, both solutions, 0 errors (2 SDK `NativeFileReference` warnings from OrderEntry.BlazorClient, not CS warnings): `docs/todos/PITCH-public-pitch/reviews/003-impl-build.log`. Tests: 0 failed. UnitTests 871/871 and IntegrationTests 664 passed, 5 skipped, on each framework; Design.Tests 187 passed, 1 skipped, on each framework (baseline 180 passed, 1 skipped, in `003-test.log`, so the 7 new facts ran): `docs/todos/PITCH-public-pitch/reviews/003-impl-test.log`.

### Veto-tier
None. All three behavioral bullets are pinned at integration on the real server host (`DesignHttpHost`, an assembly fixture over `WebApplicationFactory<Program>`):
- Designed contract: `Get_StoredOrder_ReturnsTheDesignedContract` asserts the exact six-field set (so no `IsNew`/`IsDeleted`/line graph) and every value. `Get_MissingOrder_Is404` covers the miss.
- Verb changes the stored order: `Submit_DraftWithLines_ChangesTheStoredOrder` asserts 200 + "Submitted" in the contract, then reads the order back through the Remote-mode client factory as `Submitted` with its line intact.
- Refuses what the domain refuses: `Submit_EmptyDraft_IsRefused_LikeTheApplicationsCommand_AndStaysADraft` asserts 409, the stored order is still Draft, and `OrderCommands.SubmitOrder` returns `NotSubmittable` for the same order. `Submit_AlreadySubmitted_IsRefused_AndTheStoredOrderIsUnchanged` asserts 409 with status and lines unchanged. The endpoint reads `order.CanSubmit` (`Order.cs:432`) and has no copy of the rule.

Sacred tests: `git diff PITCH..HEAD --stat -- src` shows only `Design.Server/Program.cs` (+8, comment and `MapPublicOrderApi()`), the new `PublicApi/PublicOrderApi.cs` and the new `Http/PublicOrderApiHttpTests.cs`. No existing test file was modified. `git diff 376a326 HEAD --stat` shows only `README.md`, `docs/aspnetcore-integration.md`, `docs/getting-started.md` and `skills/DTZero/references/setup.md`. No .cs or .csproj file changed after the sample commit.

**Decision (document bullet, checked mechanically as asked):**
- Bullet 4 / AC-3, "states the problem, the audience and when not to use DTZero before its first code block". The first code block is `README.md:25` (`readme-client-usage`).
  - Problem: before it. Lines 5–17 ("Why DTOs and controllers existed").
  - Audience: only hinted before it ("your own application's client", line 7; "If you are choosing a client technology...", line 17). The "Who it is for" section is at line 195, after 7 C# blocks.
  - When not to use it: nothing before it. The section is at line 201.
  - The README's order is problem, code, the one idea, objections, authorization, audience, limits. No Plan Amendment, Discovery Log entry or Gate Record says the user ruled for this order at Step 2.

### Callouts (≤ 5 Optional)
None. The plan has no Optional bullets. The only path the plan added without a test is `Save(...) ?? throw` (see Theoretical).

### Tech-debt (untiered, ≤ 5, one line each)
- The missing-order tests use fixed ids 987654/987655 against the shared assembly-fixture store. They stay correct only while auto-increment ids stay below those values.

### Could not verify
- Whether the logs (written 16:32) match commit 376a326 (16:34) byte for byte. The Design.Tests count going from 181 to 188 shows the new tests and the endpoint mapping were in the build.

### Theoretical (not triaged)
- `PublicOrderApi.cs` `factory.Save(order, ct) ?? throw new InvalidOperationException(...)`: Save of a fetched, modified order does not return null on any input the tests or the domain produce. There is no live trigger.

**Document bullets 5–7, mechanical facts only:**
- Bullet 6, every C# block is an embed: confirmed. There are 10 ```` ```cs ```` blocks (lines 25, 42, 79, 115, 147, 168, 185, 239, 249, 282), with 10 `snippet:` / 10 `endSnippet` pairs and 10 "Snippet source file" attributions. The other fences are 1 ```` ```text ```` call diagram (line 69) and 2 ```` ```bash ```` install blocks. The `public-api-*` attributions (L69–L95, L101–L108) match the region lines in `PublicOrderApi.cs`.
- Bullet 5, objection order: not mechanically checked beyond presence. The order at lines 107, 109, 111 is persistence, "only matters if I already use WebAssembly", "DTOs do more". The plan says "only a niche"; the README's wording differs.
- Bullet 7: covered by the logs above.

### Read report
- Read beyond the brief: `README.md` in full (for the closed-world order claim); `Order.cs:432` via grep (to confirm `CanSubmit` is the rule the endpoint reads); `DesignHttpHost.cs` via grep (fixture wiring); `003-test.log` summary lines (baseline counts); `verdict.md` via grep for an outline or order ruling (only R2, which needs a "when not to use it" section and gives no position).
- Named but unused: none.
