# Rebrand to DTZero

**ID:** DTZ
**Type:** Enhancement
**Status:** Complete
**Created:** 2026-11-06
**Last Updated:** 2026-11-12
**Initial split:** 5 plans
**Plan cap:** 8 — max(ceil(5 × 1.5), 5 + 2). Counts plan numbers issued, including Abandoned and Retired.
**Arc branch:** `DTZ`
**Related:** The baseline is an unmodified import of the predecessor repo's main, frozen 2026-11-06. No sibling arcs.
**Decisions (Keith, 2026-11-06):** Namespace, package ID and repo are `DTZero`, never `DT0`. Rename every `Neatoo`-prefixed public name, including the route, which becomes `/api/dtzero`. Old todos, plans and release notes come across and are renamed. First version is 2.0.0. `NF` diagnostic codes stay. The predecessor repo will be deleted, so nothing may link to it. The trace log category becomes `Trace.DTZero`, outside the `DTZero` filter prefix, so it behaves as it does today.
**Baseline (AC-1):** 3,430 passed, 12 skipped, 0 failed across net9.0 and net10.0. UnitTests 1,742 passed. IntegrationTests 1,328 passed, 10 skipped. Design.Tests 360 passed, 2 skipped. Logs: `reviews/baseline-*.log`.
**Naming rule for this todo:** These files call the old library "the predecessor". Writing its name here would fail AC-3.
**Standing orders:** attended
**Exception to the branch workflow (Keith, 2026-11-07, this todo only):** The arc and plan branches stayed local, with no pushes and no PRs after that date, and plans merged into the local `DTZ` after passing their gates. The public `NeatooDotNet/DTZero` repo held no code until the rebrand was finished, then received one clean first commit on `main`, so the old name never appears in its git history. Issues were re-created there, not transferred, so they carry no transfer record or edit history.
**Reading these files:** Commit hashes, branch names, "the arc", "the import commit" and `reviews/*.log` files refer to a private working history that no longer exists. It lived in a local clone and a temporary private repo, which was deleted after the clean commit. Logs were gitignored, as `docs/todos/CONVENTIONS.md` describes.

---

## Goal

DTZero contains everything the predecessor library had, rebranded, with no reference to the old name, so the predecessor repo can be deleted without losing anything. The library behaves exactly as it did, and ships to NuGet as `DTZero` and `DTZero.AspNetCore` 2.0.0.

## Acceptance Criteria

- [x] **AC-1** · Required — Both solutions build, and their tests pass with the same count as the baseline.
- [x] **AC-2** · Required — No `Neatoo`-prefixed namespace, type, method, assembly, package ID or route remains in the code.
- [x] **AC-3** · Required — A case-insensitive search for the old name finds nothing in any file or path.
- [x] **AC-4** · Required — All 39 issues are in DTZero, and every citation uses the new numbers.
- [x] **AC-5** · Required — The README and NuGet description lead with "Zero DTOs".
- [x] **AC-6** · Required — Neatoo's tests pass against locally packed DTZero packages on a throwaway branch.
- [x] **AC-7** · Required — CI is green on DTZero, and 2.0.0 of both packages is on NuGet.
- [x] **AC-8** · Required — A DTZero skill is installed in `~/.claude/skills`, and the repo holds only the DTZero skill.
- [ ] **AC-9** · Optional — A DTZero icon replaces `neatoo_icon.png`, and the demo GIF is re-recorded if it shows the old name.

## Out of Scope

- Migrating Neatoo to DTZero, which gets its own todo in the Neatoo repo.
- Migrating zCRM to DTZero.
- Deprecating the predecessor's NuGet packages.
- Deleting the predecessor repo, which is the user's call and comes after Neatoo moves.
- Carrying over tags, GitHub releases or PR history.
- Any behavior change beyond the renames.

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-code-rename](./plans/001-code-rename.md) | Rename code, projects and route | AC-1, AC-2, AC-3 | Done | local |
| 002 | [002-issue-transfer](./plans/002-issue-transfer.md) | Re-create all issues in DTZero | AC-4 | Done | local |
| 003 | [003-docs-and-links](./plans/003-docs-and-links.md) | Rebrand docs and remove old links | AC-2, AC-3, AC-4, AC-5 | Done | local |
| 004 | [004-dtzero-skill](./plans/004-dtzero-skill.md) | Replace the skill with DTZero's | AC-3, AC-4, AC-8 | Done | local |
| 005 | [005-package-proof-publish](./plans/005-package-proof-publish.md) | Prove packages against Neatoo, then publish | AC-1, AC-6, AC-7 | Done | local |

---

## Punchlist

- [x] Check the demo GIF for the old name · repo root · sampled frames show only the demo UI, so no re-record is needed · AC-9

## Dismissed

- DTZ · Eleven non-main predecessor branches could hold unmerged work · All have zero commits beyond main
- DTZ · Predecessor's local stash would be lost on deletion · Lists no tracked-file changes
- DTZ-002 · The `RFSK` todo ID stands for the old name · User ruled 2026-11-07 that it stays, as a historical ID
- DTZ-003 · PR numbers in this todo have no owner · DTZ-005 owns them, and the reviewer withdrew this
- DTZ-003 · Removed API names read with the new prefix, though they never shipped under it · History renamed by decision
- DTZ-003 · New `#1` to `#39` citations share a number range with list items · The context tells them apart
- DTZ-003 · Quoted commit subjects were altered · They had to lose the old `#n` form
- DTZ-004 · The check script cannot be repeated from the repo · It was a one-off check for this rebrand
- DTZ-004 · The installed skill's attribution links lead nowhere outside the repo · They predate this todo
- DTZ-001 · One shadowing decoy test failed its flip for another reason · It has the baseline's shape, so the rename did not cause it
- DTZ-005 · `AssemblyVersion` stays 1.0.0.0 and `build.yml` keeps a stale history comment · Both predate this todo

## Parked

- Neatoo#126 · Neatoo and zCRM migration · Update feature switch, log categories, headers and route, including zCRM's hard-coded route in its server middleware · DTZ-001 plan review
- Neatoo#126 · Neatoo and zCRM migration · Remove the predecessor's skill from `~/.claude/skills` once both have moved · DTZ-004 user decision
- Neatoo#126 · Neatoo and zCRM migration · A table from old names to new names, which cannot live here under AC-3 · DTZ-005 user decision
- Neatoo#126 · Neatoo and zCRM migration · `NeatooBaseJsonTypeConverter`, `NeatooJsonConverterFactory`, `NeatooListBaseJsonTypeConverter` and `NeatooTraceLog` need `using Neatoo;`, and `FluentRuleTests` uses a partly qualified name · DTZ-005 package proof
- #40 · Replace `neatoo_icon.png` when a DTZero icon exists · AC-9, user has no icon yet

---

## Discovery Log

### 2026-11-06 — DTZ-001 · serves AC-1, AC-2, AC-3
- **Finding:** Plan review found the step order, mapping rows, must-stay list, test namespaces and trimming gate needed correcting.
- **Decision:** Amend
- **Follow-up:** n/a

### 2026-11-07 — DTZ-001 · serves AC-3, AC-7
- **Finding:** The baseline import put the old name in the public repo's history, and a force-push could not remove it.
- **Decision:** Amend. The public repo was replaced with an empty one, and all work stays local until one clean commit is pushed. See the exception in the header.
- **Follow-up:** DTZ-005 owns the clean commit and the first CI run.

### 2026-11-07 — DTZ-005 · serves AC-7
- **Finding:** The clean commit cannot record its own CI run or publish, and the migration rows need an owner.
- **Decision:** Amend. The first commit is the clean one, and a later clean commit closes this todo. The user ruled that the migration rows go to the Neatoo migration work, which also carries zCRM.
- **Follow-up:** DTZ-005 owns the closing commit.

### 2026-11-07 — DTZ-005 · serves AC-7
- **Finding:** A stored NuGet key would have to be entered again on the public repo.
- **Decision:** Amend. The user chose Trusted Publishing, so the publish job gets a one-hour key from GitHub's OIDC token.
- **Follow-up:** The user creates the policy on nuget.org before the tag.

### 2026-11-07 — DTZ-004 · serves AC-8
- **Finding:** Neatoo and zCRM still use the predecessor's packages, and the `neatoo` skill expects the predecessor's skill beside it, so removing that skill now would give those sessions the wrong names.
- **Decision:** Amend. The user changed AC-8: the repo holds only the DTZero skill, and the predecessor's user-level skill stays until both projects move.
- **Follow-up:** Parked row.

### 2026-11-07 — DTZ-003 · serves AC-4
- **Finding:** The gate found 18 citations the rewrite missed and one sample order number wrongly remapped, and the citation check shared the rewrite's blind spot.
- **Decision:** Amend. All are fixed, and a scan with no boundary rule now covers citations.
- **Follow-up:** n/a

---

## Skipped Steps

- Step 1 Explore fan-out: recon was a handful of targeted searches, run inline.

---

## Sibling Todos

---

## Close-Out Audit

- 2026-11-07, pre-publish: C, on AC-9's icon row, which had no issue.
- 2026-11-11, re-audit: **B**. AC-9's icon half is parked as #40. Acknowledged by Keith, 2026-11-12. See `reviews/close-out-audit.md`.

---

## Docs & Retro

**Documentation:** The docs, README and skill were renamed in DTZ-003 and DTZ-004, and the release note shipped in DTZ-005. No internal-contradiction callouts were parked. The only doc debt is the icon, #40.

**Retro:** 5 plans were issued, against a cap of 8. 11 findings were dismissed, 1 punched, and 5 parked as two issues, #40 and Neatoo#126. None were queued. The first lesson is about history: when the Goal includes a clean public history, decide where the baseline lives before the first commit. Here an import commit reached the public repo, and that forced a switch to a private working repo. The second is about checks: a check that shares the rewrite's matching rule shares its blind spots. DTZ-003's gate found 18 missed citations, and only a scan with no boundary rule caught them. Where these lessons go is the user's call.

---

## Results / Conclusions

DTZero 2.0.0 shipped on 2026-11-11 as `DTZero` and `DTZero.AspNetCore`. It was built from one clean commit on the public `main` and published with Trusted Publishing. Behavior and test counts match the predecessor, the 39 issues were re-created, and the DTZero skill replaced the old one. Neatoo and zCRM move in Neatoo#126, and the icon is #40.
