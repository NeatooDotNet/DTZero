# The Skill Carries the Same Reasons

**Plan #:** 006
**Date:** 2026-11-13
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5, AC-7
**Status:** Done
**Last Updated:** 2026-11-18
**Plan-review opt-in:** No
**Code-review opt-in:** No (prose and embeds of existing regions)
**Branch:** `PITCH-006-skill`
**PR:** —

---

## Scope

Bring `skills/DTZero/` in line with the revised docs: the same reasons, in the skill's own self-contained files, with every sample an embedded Design region. It does not change the skill's structure from its last rebuild, and it adds no Design code.

---

## Intent

- An assistant that loads the skill knows why the library exists, when not to propose it, and the three habits the docs now answer: where authorization goes, whether the sent object can be trusted, and what a public API looks like.
- The skill and the docs use the same names for the same ideas.

---

## Constraints & Invariants

- The skill references nothing outside its own directory.
- No hand-written C#. Embeds use regions that already exist and are already pinned by Design tests.
- Nothing against LazyLoad.

---

## Steps

1. Add the reasons and the limits to `SKILL.md`, before the model.
2. Add the three habit questions and their answers.
3. Add the shared authorization base, the inheritance rules, the stored-record rule and the public API to the authorization reference.
4. Replace "cascade", update the trimming table's interface factory row (#39), and set the skill version to 2.0.1.

---

## Acceptance

- [x] `SKILL.md` states why the library exists and when not to propose it `[explicit-skip: document]`
- [x] The skill answers where authorization goes, whether the sent object is trusted, and how outside callers are served, each with a compiled example `[explicit-skip: document]`
- [x] The skill's samples are all embedded Design regions and it links to nothing outside itself `[explicit-skip: document]`

---

## Current State (Pre-Flight)

- `SKILL.md` opened with what DTZero is and went straight to the model. It had no "when not", no public API guidance and nothing on trusting the sent object.
- Authorization lives in `references/advanced-patterns.md`. It had no shared base.
- "Cascade" named the save chain in one heading and its one link. The trimming table's interface factory row read "Not established".
- No file under `src` is touched; the PITCH-005 logs stand as the green baseline.

---

## Punchlist

---

## Test Evidence

None: no behavioral bullet. The seven regions embedded here (`authorization-base`, `authorization-base-derived`, `authorization-base-apply`, `authorization-stored-record`, `public-api-contract`, `public-api-endpoints`, `public-api-authorized`) were pinned in PITCH-003 and PITCH-005.

---

## Gate Record

- Step 5 test review skipped: document-only plan, no behavioral bullets, no source change (recorded in the todo).
- Checked by the orchestrator: mdsnippets ran clean; in every skill file the snippet open markers, close markers and anchors are equal in number; no link leaves the skill directory apart from the generated snippet-source attributions, which the skill already carried.

---

## Plan Amendments

- 2026-11-18: the public API section went into `references/advanced-patterns.md` and not a new file, to keep the structure.
- 2026-11-18: three trigger phrases were added to the skill's description for the three new habit questions.
- 2026-11-18: no fresh-session test of the revised skill was run.

---

## Notes
