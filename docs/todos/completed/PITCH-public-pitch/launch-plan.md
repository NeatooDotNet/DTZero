# Launch Plan: Generating Interest in DTZero

**Date:** 2026-11-18
**Status:** Draft for Keith's ruling
**Source:** [verdict.md](verdict.md), sections 1, 3, 5, 6 and 7

This is a plan, not a record of anything done. Nothing in it has been posted, submitted or announced. Channel details come from general knowledge of the .NET community, not from a fresh check: confirm each channel's current rules before using it.

---

## 1. What success means

The verdict's answer was "yes, to a narrow audience". The plan aims at that audience and nothing wider.

**The audience:** teams with a .NET client (Blazor WebAssembly or WPF) in front of ASP.NET Core, one team owning both tiers, and a domain with behavior. The nearest existing community is CSLA's.

**Success is evidence that someone else is evaluating it.** In rough order of weight:

1. A stranger opens an issue or asks a question that shows they ran it.
2. A stranger objects in public, and the docs already answer the objection.
3. A second team puts it in an application.

No download or star figure is a target. Those numbers move with a single link from a popular account and say little about use.

**"A tool for me" is a legitimate result.** If three months of the work below produce none of the three signals, the honest reading is that DTZero is a library its author uses and publishes. The docs, the fix in 2.0.1 and the skill are worth having either way.

---

## 2. Before anything is announced

A reader who arrives from a link decides in a minute. Each item below is something that reader checks. None is done by this todo.

| # | Item | Why it matters |
|---|---|---|
| 1 | Merge the `PITCH` branch to `main` and publish 2.0.1 | The README and docs a visitor sees are still the old ones until this happens. 2.0.1 is a security fix; it should not sit unpublished behind an announcement. |
| 2 | Repository description, topics and homepage | The description still says "data mapper", which the README no longer claims. There are no topics, so the repository is not found by search. |
| 3 | NuGet page: readme, tags, project URL, icon (#40) | Many .NET developers meet a library on nuget.org first. |
| 4 | A published docs site | `docs/` is written for one. A visitor should not read documentation as raw files. |
| 5 | A sample a stranger runs in five minutes | `src/Examples/OrderEntry` exists. It needs a README with the three commands, and a check that it runs from a clean clone. |
| 6 | GitHub Discussions on | A question is a lower bar than an issue. Without it, curious readers leave no trace. |
| 7 | `SECURITY.md` and a short `CONTRIBUTING.md` | 2.0.1 fixed a silent authorization gap. Saying how to report the next one is part of being trusted with authorization. |
| 8 | Open issues read as a known-limits list | A visitor reads the issue list. Label each one, close what is stale, and link the real limits from the docs. |
| 9 | How a stranger installs the skill | The skill is part of the pitch. "Copy this folder" should be one documented step. |
| 10 | The Neatoo story | Neatoo is the editing-session half and is mid-migration (Neatoo#126). The README should say in one line what a reader can use today. |

Items 1 to 6 are the gate. Announcing before them wastes the one first impression.

---

## 3. What to make

Each artefact answers one objection from the verdict's section 7. An objection answered in public is worth more than a feature list.

| # | Artefact | Answers | Notes |
|---|---|---|---|
| A | **"Why are we still writing DTOs for a .NET client?"** A post. | The whole case | The flagship. The problem is older than the tools; the layers were a workaround for JavaScript. Shows one operation written the way Microsoft's guidance for WebAssembly asks (an interface, two implementations, an endpoint, transfer types) beside the same operation as one `[Remote]` method. |
| B | **The same feature, two ways.** A small repository or a folder in this one. | "Show me" | Microsoft's documented pattern and DTZero, for one aggregate with a child list and one rule. Count files and lines honestly, including DTZero's setup. A third column for CSLA only if someone who knows CSLA well reviews it. |
| C | **"A public API over the same model."** A post. | "DTOs do more than cross a boundary" | Concedes versioning, payload shape and what a caller may set, for outside callers. Claims only the one-application case. The sample is already in Design. |
| D | **"What the client sends is input."** A post. | "What stops tampering?" | The stored-record rule beside the rule that trusts the sent object. Includes the 2.0.1 fix as an example of a gap found and closed. |
| E | **"Teaching an agent a library it was not trained on."** A post. | The AI question | The skill as part of the product. Strongest with a small experiment: the same task given to an agent with and without the skill, and what each wrote. |
| F | **"Coming from CSLA."** A docs page. | "This is CSLA" | Says so first. A mapping of terms, and the one real difference: no base class, any type. No trimming or AOT comparison. Written to be read by CSLA's community without offence. |
| G | **The production account.** A post. | "One maintainer, a new repository" | 74 factory types, 233 operations, 3 hand-written controllers, and what trimming cost. The costs make the numbers believable. |
| H | **A five-minute screen recording.** | Readers who do not read | Add a field to an aggregate; watch it appear on the client with no other file touched. |

A and B come first. C, D and F answer the objections that A will draw, so they should exist before A is widely shared. E, G and H follow over the next months.

---

## 4. Where to take it

Ordered from least risk to most. The early channels are forgiving and give feedback; the late ones give one chance.

### Owned

- The README, the NuGet page and the docs site. Everything else links here.
- A place for posts. The company site or a personal blog; cross-post to dev.to for reach.

### Lists and indexes

- **Awesome Blazor** and **awesome-dotnet**: a pull request each. Slow, steady discovery.
- Documentation indexes that AI tools read, such as Context7, and whichever skill or plugin directories exist when this is carried out.

### Link blogs and newsletters

- Daily and weekly .NET link collections, such as The Morning Dew and C# Digest, take submissions or pick up posts from feeds. A post with a clear title is what they want; a repository link alone is not.

### Communities

- **CSLA's discussions.** The closest audience and the one most able to judge it. Arrive as related work and ask for criticism, not as a replacement. Artefact F should exist first.
- **Blazor and .NET chat communities** (Discord servers, the DDD community's chat). Answer questions where DTZero is a fair answer; do not post announcements.
- **r/dotnet and r/Blazor.** Both tolerate a project post when it is a write-up with substance and the author stays to answer. Post A, not the repository.

### One-chance channels

- **Hacker News, "Show HN".** Only after the feedback from the earlier channels has been absorbed into the docs. The audience will open with "persistence in the entity" and "what stops tampering"; both answers must already be one click away.

### Talks and audio

- A local .NET user group or meetup talk built from A and B. Low stakes, real questions.
- Community slots at larger events, and .NET podcasts, once there is a second user or a notable write-up to point to. Before that there is little for a host to book.

---

## 5. Order of work

**Phase 0, readiness.** Section 2, items 1 to 6. Then artefacts A and B.

**Phase 1, quiet.** Lists and indexes. Ask three to five developers who owe no kindness to read the README cold and say where they stopped. Fix what they report. Write C, D and F.

**Phase 2, the post.** Publish A. Submit it to the link blogs. Post it to r/dotnet or r/Blazor, one of them, and answer every comment for two days. Open the CSLA conversation.

**Phase 3, the wide channel.** Show HN, once Phase 2's objections are answered in the docs.

**Phase 4, sustained.** One artefact every month or so (E, G, H, then whatever the questions ask for). Answer issues and discussions within days; for a one-maintainer library, response time is the trust signal.

Check the three signals from section 1 at the end of each phase. Phase 3 is optional if Phase 2 produced none.

---

## 6. What to say, and what not to

**Say:**

- DTOs and controllers were a workaround for a language boundary that a .NET client does not have.
- One definition, both tiers, checked by the compiler.
- A public API reuses the same model; the contract is designed, the logic is the one copy.
- It is CSLA's idea, without the base class.
- When not to use it, before anyone asks.

**Do not say:**

- "Zero boilerplate" or anything that sells saved typing. AI made typing cheap; the claim that survives is about drift, not keystrokes.
- Anything comparing trimming or AOT with CSLA. No source was found.
- That Blazor WebAssembly is large or growing. No credible figure was found.
- That it suits public APIs, non-.NET clients, Blazor Server alone, or thin CRUD.

**Expect, and answer plainly:**

- "One maintainer." True. The counter is a production application, the test suite, and how fast issues are answered.
- "Persistence in the entity." The graph that crosses is the model, not the tables. Link the section.
- "This is niche." Yes.

---

## 7. Risks

- **A hostile first thread.** The persistence objection reads as a design sin to part of the DDD community. The answer exists; the risk is a reader who does not click. Lead with it in A.
- **A security finding after attention arrives.** Authorization is now a headline claim. `SECURITY.md`, the 2.0.1 notes and a fast fix are the mitigation. A second silent gap found by a stranger would cost more than the first found by us.
- **Time.** Every channel above pays only if the author stays to answer. A post with no replies from its author does harm.
- **Neatoo.** A reader who follows the link to a library mid-migration may stop there.

---

## 8. Decisions for Keith

1. **How much time, for how long?** The plan assumes a few hours a week for three months. With less, do Phase 0 and Phase 1 only.
2. **Whose name is on it?** A personal voice reads better in these communities than a company one. The production account (G) names the company either way.
3. **Where do posts live?** The company site, a personal blog, or the docs site's own blog section.
4. **Is the CSLA conversation wanted?** It is the best audience and the most exposed one.
5. **Is a talk on the table?** If yes, B is built to be demonstrated live.
6. **Which readiness items become issues or a follow-on todo?** None is filed by this plan.
