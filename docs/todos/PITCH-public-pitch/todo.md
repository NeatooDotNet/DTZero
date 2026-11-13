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
- [ ] **AC-2** · Required — A map ties each DDD concept to a DTZero component or marks it as not DTZero's job, and the user has confirmed it.
- [ ] **AC-3** · Required — The README states the problem, the audience and when not to use DTZero before any code.
- [ ] **AC-4** · Required — Every docs page says why its component exists and when to leave it out.
- [ ] **AC-5** · Required — The skill carries the same reasons as the docs.
- [ ] **AC-6** · Required — A launch plan lists where and how to generate interest.
- [ ] **AC-7** · Required — Every C# sample is still a Design-pinned snippet and both solutions are green.
- [ ] **AC-8** · Optional — Documentation issues #2, #37 and #39 are closed along the way.

## Out of Scope

- Library code changes beyond new Design sample regions and the tests that pin them.
- Carrying out the launch: posting, announcing, submitting anywhere.
- The package icon (#40) and the downstream migration (Neatoo#126).
- LazyLoad: nothing is written against it (CLAUDE.md, VisionAlignment R-K).

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-verdict](./plans/001-verdict.md) | The verdict: is DTZero useful to others | AC-1 | Done | #41 |
| 002 | [002-ddd-map](./plans/002-ddd-map.md) | Map DDD concepts to DTZero components | AC-2 | In Progress | — |
| 003 | [003-readme](./plans/003-readme.md) | Rewrite the README around the case | AC-3, AC-7 | Draft | — |
| 004 | [004-narrative-docs](./plans/004-narrative-docs.md) | Narrative docs carry the why | AC-4, AC-7 | Draft | — |
| 005 | [005-component-docs](./plans/005-component-docs.md) | Component pages: why and when not | AC-4, AC-7, AC-8 | Draft | — |
| 006 | [006-skill](./plans/006-skill.md) | The skill carries the same reasons | AC-5, AC-7 | Draft | — |
| 007 | [007-launch-plan](./plans/007-launch-plan.md) | A plan for generating interest | AC-6 | Draft | — |

---

## Punchlist

## Dismissed

## Parked

- NeatooDotNet/Neatoo#127 · Neatoo: re-run rules on the server before a write, by mechanism or option · PITCH-001 verdict, sixth ruling

---

## Discovery Log

---

## Skipped Steps

- PITCH-001 Step 5 test review — document-only plan with no behavioral bullets (2026-11-13).
- PITCH-002 Step 5 test review — document-only plan with no behavioral bullets (2026-11-13).
- Step 1 fan-out reconnaissance — the user asked for research inside each plan, not up front (2026-11-13).

---

## Sibling Todos

---

## Close-Out Audit

---

## Docs & Retro

---

## Results / Conclusions
