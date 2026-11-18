# Developer — 003 README (Step 1: public-API Design sample)

Last updated: 2026-11-13
Current step: Step 1 implemented, reported to the orchestrator. Steps 2-4 (README) are not mine.

## Key Context
- Sample: src/Design/Design.Server/PublicApi/PublicOrderApi.cs (regions public-api-endpoints, public-api-contract).
- Program.cs: one using (outside region) + comment + `app.MapPublicOrderApi();` inside getting-started-server-program, after UseDTZero().
- Routes /api/public/orders/{id} and /{id}/submit. No `:int` constraint on purpose: with it, a non-numeric id falls to MapFallbackToFile and gets index.html 200 (proved: Get_NonNumericId_Is400 fails with the constraint).
- Order has no [AuthorizeFactory]; [Service] params (IOrderDbContext, IOrderLineListFactory) resolve from the request scope.
- Tests: Design.Tests/Http/PublicOrderApiHttpTests.cs, 7 tests. Design.Tests 180 -> 187 per TFM.

## Mistakes to Avoid
- AnalysisMode all: HttpClient string overloads trip CA2234; use Uri.
- No python on this machine; use Edit/sed.

## User Corrections
(none)

## Completion Evidence
- Logs: docs/todos/PITCH-public-pitch/reviews/003-impl-build.log, 003-impl-test.log (both solutions).
- UnitTests 871/871 x2; IntegrationTests 664 + 5 skipped x2; Design.Tests 187 + 1 skipped x2.
