# Narrative Docs Carry the Why

**Plan #:** 004
**Date:** 2026-11-13
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-7
**Status:** Draft
**Last Updated:** 2026-11-13
**Branch:** —
**PR:** —

---

## Scope

Revise the pages a newcomer reads in order (the docs landing page, `the-problem`, `decision-guide`, `getting-started`, `client-server-architecture`) so they argue the ruled case and place the DDD map. It does not touch the per-component pages or the skill.

## Carried from the verdict

- State early that the graph that crosses is the DDD conceptual graph, and that the persistence model stays on the server behind a `[Service]` parameter.
- The AI argument, with the skill presented as part of the library (R4).
- The public-API page or section, from the Design sample PITCH-003 adds.
- From the DDD map (PITCH-002): state "factory methods call factory methods" as the one idea, early, with fetch and save as its two cases and one call tree showing the single crossing. Say this is the work a repository does elsewhere. "Cascade" is not the public word; it may appear once as what an ORM calls it.
- From the DDD map: root and child are roles an entity takes, decided by the factory method's parameters, not kinds of class.
