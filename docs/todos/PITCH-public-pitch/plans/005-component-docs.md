# Component Pages: Why and When Not

**Plan #:** 005
**Date:** 2026-11-13
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-7, AC-8
**Status:** Draft
**Last Updated:** 2026-11-13
**Branch:** —
**PR:** —

---

## Scope

Give every remaining docs page a statement of why its component exists and when to leave it out, drawing on the DDD map, and close issues #2, #37 and #39 where the page is already open. It does not restructure the reference content. Likely to re-split by page group once the pages are read.

## Carried from the verdict

- Authorization is a headline strength: one rule class with injection, enforced on the server and asked by the client through `Can*`.
- A second authorization example: a base auth class holding the broad read and write check. Needs a Design sample and test first; inherited auth methods are unpinned today.
- A trust section: what the client sends is input. A target rule that reads the sent object's state is checking input; show the rule reading the stored record.
- Trimming: state the real cost and which gaps are closed in 2.0.0.
- From the DDD map (PITCH-002): replace "cascade" wording in the component pages and the skill with "factory methods call factory methods"; describe root and child as roles.
