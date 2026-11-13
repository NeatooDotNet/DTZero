# The Verdict: Is DTZero Useful to Others

**Plan #:** 001
**Date:** 2026-11-13
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1
**Status:** Done
**Last Updated:** 2026-11-13
**Plan-review opt-in:** No (a document the user rules on directly)
**Code-review opt-in:** No (no code)
**Branch:** `PITCH-001-verdict`
**PR:** #41

---

## Scope

Write an evidence-based verdict on whether DTZero is worth another team's adoption: who it is for, what they would use instead, what it saves in a real application, and whether cheap AI-written boilerplate removes or strengthens the case. The verdict is `verdict.md` in this todo's folder and the user rules on it. This plan does not edit the README, the docs or the skill. If the ruling is "personal tool", plans 003 to 007 shrink to an honest README and the todo closes early.

---

## Intent

- The user gets a frank answer they can disagree with, built from numbers and sources rather than from the library's own pitch.
- Every later plan has one ruled statement of audience, claim and non-claim to write from.
- The weak points a public reader will raise are named here first, with the answer or the admission.

---

## Framework & Architectural Alignment

- DTZero's own claim is `docs/VisionAlignment.md` D15 and section 2 statement 1; the verdict tests that claim, it does not restate it.
- Evidence is measured, with the command that produced each number, so the user can re-check it.
- Facts about other libraries carry a URL. A fact that could not be verified is marked so.

---

## Constraints & Invariants

- No tracked file names the library's former name.
- No private application's source, names of its domain types, or business detail is copied in; only counts.
- The verdict states what DTZero does not do and who should not use it.

---

## Steps

1. Measure a production application's use of the library: factory types, operations, what still needed hand-written endpoints or transfer types, and the size of the generated code.
2. Survey the alternatives a .NET team would reach for, with current status and what each leaves to write by hand.
3. Gather the strongest arguments on both sides of the AI question.
4. Write `verdict.md`: audience, claim, alternatives table, the AI argument, weaknesses, and a recommended ruling.
5. Put the ruling to the user as a small set of questions and record the answers in the verdict.

---

## Acceptance

- [x] `verdict.md` names the audience, the alternatives and the value under AI-assisted development `[explicit-skip: document]`
- [x] Every number cites its command and every outside fact cites a URL `[explicit-skip: document]`
- [x] The verdict names who should not use DTZero `[explicit-skip: document]`
- [x] The user's ruling is recorded in the verdict `[explicit-skip: document]`

---

## Current State (Pre-Flight)

- `README.md` and `docs/the-problem.md` argue from "Blazor WebAssembly changes everything" and a bullet list of saved layers. Neither names an alternative other than CSLA, a cost, or a reader who should look elsewhere.
- `docs/VisionAlignment.md` section 3 already holds a DDD concept table with compiled examples; PITCH-002 starts from it.
- The repository is days old publicly (first commit 2026-11-12), so stars and downloads say nothing yet.

---

## Punchlist

---

## Test Evidence

Not applicable: no behavioral bullets. The Step 5 test review is skipped and recorded in the todo's Skipped Steps.

---

## Gate Record

- 2026-11-13: test review skipped (document-only plan); the user ruled on the verdict in six entries — Done

---

## Plan Amendments

---

## Notes
