# Make DTZero's Public Case

**ID:** PITCH
**Type:** Enhancement
**Status:** In Progress
**Created:** 2026-11-13
**Last Updated:** 2026-11-13
**Initial split:** 7 plans
**Plan cap:** 11
**Arc branch:** `PITCH`
**Related:** Adopts documentation issues #2, #37 and #39 under AC-8. No sibling arcs open (checked 2026-11-13). The verdict and the launch plan live in this tracked folder and so are public (Keith, 2026-11-13).
**Standing orders:** attended

---

## Goal

Decide, with evidence, whether DTZero is useful to anyone but its author now that AI writes boilerplate cheaply. If it is, make the README, the docs and the skill argue that case: why the library exists, why each component exists, how each DDD concept maps onto it, and when not to use it. Finish with a plan for generating interest. Research happens inside each plan, just before that plan's work, so questions reach the user one small set at a time.

## Acceptance Criteria

- [x] **AC-1** · Required — A written verdict names DTZero's audience, its alternatives and its value in AI-assisted development, and the user has ruled on it.
- [x] **AC-2** · Required — A map ties each DDD concept to a DTZero component or marks it as not DTZero's job, and the user has confirmed it.
- [x] **AC-3** · Required — The README states the problem, the audience and when not to use DTZero before any code.
- [x] **AC-4** · Required — Every docs page says why its component exists and when to leave it out.
- [x] **AC-5** · Required — The skill carries the same reasons as the docs.
- [ ] **AC-6** · Required — A launch plan lists where and how to generate interest.
- [ ] **AC-7** · Required — Every C# sample is still a Design-pinned snippet and both solutions are green.
- [ ] **AC-8** · Optional — Documentation issues #2, #37 and #39 are closed along the way.

## Out of Scope

- Library code changes beyond new Design sample regions and the tests that pin them. One exception, approved 2026-11-16: the generator fix in PITCH-009.
- Carrying out the launch: posting, announcing, submitting anywhere.
- The package icon (#40) and the downstream migration (Neatoo#126).
- LazyLoad: nothing is written against it (CLAUDE.md, VisionAlignment R-K).

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-verdict](./plans/001-verdict.md) | The verdict: is DTZero useful to others | AC-1 | Done | #41 |
| 002 | [002-ddd-map](./plans/002-ddd-map.md) | Map DDD concepts to DTZero components | AC-2 | Done | #42 |
| 003 | [003-readme](./plans/003-readme.md) | Rewrite the README around the case | AC-3, AC-7 | Done | #43 |
| 004 | [004-narrative-docs](./plans/004-narrative-docs.md) | Narrative docs carry the why | AC-4, AC-7 | Done | #44 |
| 005 | [005-authorization](./plans/005-authorization.md) | Authorization and trust | AC-4, AC-7 | Done | #46 |
| 006 | [006-skill](./plans/006-skill.md) | The skill carries the same reasons | AC-5, AC-7 | Done | #48 |
| 007 | [007-launch-plan](./plans/007-launch-plan.md) | A plan for generating interest | AC-6 | Draft | — |
| 008 | [008-component-docs](./plans/008-component-docs.md) | Component pages: why and when not | AC-4, AC-7, AC-8 | Done | #47 |
| 009 | [009-auth-inheritance](./plans/009-auth-inheritance.md) | Inherited authorization checks are enforced | AC-4, AC-7 | Done | #45 |

---

## Punchlist

- [x] Replace "cascade" as the name for the save chain in the `collection-factory-basic` source comment and in `docs/factory-operations.md` (four places) · `src/Design/Design.Domain/Entities/OrderLine.cs`, `docs/factory-operations.md` · the word appears only as "what an ORM calls it" · AC-4 (PITCH-005) · source comment in PITCH-005, docs and CLAUDE-DESIGN in PITCH-008; the skill is PITCH-006

- [x] Decide whether the public endpoints carry `RequireAuthorization`, and show an authorized aggregate reached through a public endpoint so "no entry point can miss the rule" has a test · `PublicApi/PublicOrderApi.cs` · decided with the user; if shown, an HTTP test pins it · AC-7 (fits PITCH-005, authorization) · done in PITCH-005: the authorized aggregate is reached through a public endpoint and pinned; `RequireAuthorization` is left to the application and the page says so

## Dismissed

## Parked

- NeatooDotNet/Neatoo#127 · Neatoo: re-run rules on the server before a write, by mechanism or option · PITCH-001 verdict, sixth ruling

---

## Discovery Log

- 2026-11-15 · Re-split · PITCH-005 became two plans: 005 (authorization and trust, with Design samples) and 008 (the remaining component pages, prose). The authorization work needs code and a test review; the rest does not. Eight plans of a cap of eleven.
- 2026-11-16 · Scope · The PITCH-005 probe found that the generator drops an authorization check an auth interface inherits, with no diagnostic, and cannot resolve one declared on an abstract base class. Keith ruled: fix the generator in this todo (PITCH-009), ahead of the authorization page. Out of Scope amended. Nine plans of a cap of eleven.

---

## Skipped Steps

- PITCH-001 Step 5 test review — document-only plan with no behavioral bullets (2026-11-13).
- PITCH-002 Step 5 test review — document-only plan with no behavioral bullets (2026-11-13).
- PITCH-004 Step 5 test review — document-only plan with no behavioral bullets and no source change (2026-11-15).
- PITCH-008 Step 5 test review — document-only plan with no behavioral bullets and no source change (2026-11-18).
- PITCH-006 Step 5 test review — document-only plan with no behavioral bullets and no source change (2026-11-18).
- Step 1 fan-out reconnaissance — the user asked for research inside each plan, not up front (2026-11-13).

---

## Sibling Todos

---

## Close-Out Audit

---

## Docs & Retro

---

## Results / Conclusions
