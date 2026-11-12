# Replace the skill with DTZero's

**Plan #:** 004
**Date:** 2026-11-06
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4, AC-8
**Status:** Done
**Last Updated:** 2026-11-07
**Plan-review opt-in:** No (the same rules as DTZ-003, on 11 files)
**Code-review opt-in:** No (no code changes)
**Branch:** `DTZ-004-dtzero-skill`, local only
**PR:** None, per the exception in the todo header

---

## Scope

Rebrand the skill held in this repo, move it to `skills/DTZero`, and install it in `~/.claude/skills` beside the predecessor's skill. This plan does not remove the predecessor's user-level skill, which stays until Neatoo and zCRM move, and it does not edit the Neatoo or MudNeatoo skills.

## Intent

A session working on DTZero gets guidance with the names the code has, and a session in Neatoo or zCRM keeps guidance that matches the packages it still uses.

## Framework & Architectural Alignment

The skill stays self-contained, and its C# stays embedded from `src/Design` by mdsnippets.

## Constraints & Invariants

- The same naming, citation and link rules as DTZ-003.
- The installed copy matches the repo copy exactly.
- The skill's description leads with DTZero, to limit overlap with the predecessor's skill.

## Steps

1. Apply the DTZ-003 rules to the skill.
2. Move it to `skills/DTZero`, and remove the repeated name from its description.
3. Check its citations against the arc copy and the live issues.
4. Regenerate snippets.
5. Install it, and compare the installed copy with the repo.

## Acceptance

- [x] No file or path in the repo contains the old name. `[explicit-skip: no behavior; proved by a search]`
- [x] Every issue citation in the skill uses its new number and lands on the right issue. `[explicit-skip: no behavior; proved by the citation log]`
- [x] `~/.claude/skills/DTZero` matches `skills/DTZero`, and `~/.claude/skills` still holds the predecessor's skill. `[explicit-skip: no behavior; proved by a directory comparison]`

## Current State (Pre-Flight)

The skill is 11 files, and 10 changed. mdsnippets already updated its embedded code in DTZ-003, and it changed nothing here (`reviews/004-mdsnippets.log`). Issue citations are 14 linked pairs and one bare `#18`. There are no `RF` forms and no Neatoo-prefixed names apart from the org name.

## Punchlist

## Test Evidence

No code changed, so the build and test logs from DTZ-003 stand. DTZ-005 runs the full suites again.

| Acceptance bullet | Evidence | Result |
|---|---|---|
| No old name in the repo | `reviews/004-search.log` | 0 files, 0 paths |
| Citations | `reviews/004-citations.log`, which compares against the import commit and lists each cited issue's title | 14 linked and 1 bare citation, 9 issues, 0 problems |
| Installed copy | `reviews/004-install.log` | No differences, both skills present |

**Flip:** planting a wrong linked number and a wrong bare number made the citation check report both, in `reviews/004-flip-citations.log`.

## Gate Record

- Round 1: CONCERNS. One veto and one callout, both fixed.
- Round 2: APPROVED. See `reviews/004-test-review.md`.

## Plan Amendments

- **2026-11-07, Round 1:** the citation check compared against the arc, where the bare citation was already rewritten, so it proved nothing for that one. It now compares against the import commit. A pointer that quoted a heading in the Neatoo skill no longer quotes it, because the exact words cannot stay under AC-3 until that skill changes.

## Abandonment / Retirement Reason

## Notes

The skill was not re-tested in a fresh session, because its guidance did not change, only the names.
