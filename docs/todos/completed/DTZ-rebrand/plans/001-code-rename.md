# Rename code, projects and route

**Plan #:** 001
**Date:** 2026-11-06
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2, AC-3
**Status:** Done
**Last Updated:** 2026-11-06
**Plan-review opt-in:** Yes (public API, and the generator matches names as strings)
**Code-review opt-in:** No (mechanical rename, covered by the test suites and the trimming check)
**Branch:** `DTZ-001-code-rename`
**PR:** One PR on the private working repo, since deleted

In this plan `{old}` stands for the predecessor's one-word name, per the todo's naming rule.

---

## Scope

Rename namespaces, assemblies, package IDs, projects, solutions, source paths, `Neatoo`-prefixed public names, generator string literals and generated-code output, and rename every wire and configuration name the library owns. The test count must match the baseline exactly. This plan covers all tracked files except markdown, `skills/` and `.claude/`. It does not touch prose docs, issue citations or the skill, it keeps the `NF` diagnostic codes, and it does not bump the version.

---

## Intent

- Every name a DTZero consumer types or configures says DTZero.
- The library behaves exactly as before, apart from the names themselves.
- References to the separate Neatoo library stay accurate, because that library still exists and still uses this one.

---

## Framework & Architectural Alignment

- Tier composition, `[Factory]` attributes and generated factory shapes are unchanged. The generator emits the same code under new names.
- `src/Design/` stays the single source of snippets. Region names do not change.
- The two-container client/server test pattern is untouched.

---

## Constraints & Invariants

- No test is removed, skipped or weakened. Test edits are renames only, and each kind is reported at the gate.
- A name stays only if it refers to the separate Neatoo library, the `NeatooDotNet` GitHub org, or the `neatoo_icon.png` file that AC-9 owns.
- Client and server in this repo agree on the route, the headers and the feature switch.
- `NF` codes are unchanged.
- `Directory.Build.props` keeps version 1.10.9. Plan DTZ-005 sets 2.0.0.
- Replacement is scripted and ordered, so it can be re-run from the baseline rather than patched by hand.

---

## Steps

1. Protect every name that means the separate Neatoo library before any replacement runs.
2. Rename the compound brand forms first, so that later single-word rules cannot split them.
3. Rename the generator's namespace and assembly, keeping the generator's namespace a child of the library's namespace as it is today.
4. Rename the remaining `{old}` and `Neatoo` identifiers, covering types, members, locals, the wire names and the configuration names.
5. Rename test target classes whose generated factory name would otherwise contain `{old}`, including the classes the combination test generator emits.
6. Rename files, folders, projects and solutions, and update every path that points at them, including `build.yml`, `verify-trimmed.sh` and `InternalsVisibleTo`.
7. Regenerate anything generated that is tracked in the repo.
8. Run both solutions and the trimming check, then compare results to the baseline.

---

## Acceptance

- [x] Both solutions build with no new warnings `[explicit-skip: meta-bullet, build log]`
- [x] Test totals equal the baseline of 3,430 passed, 12 skipped and 0 failed `[explicit-skip: meta-bullet, test log]`
- [x] A client call reaches the server over HTTP on `/api/dtzero` `[integration]`
- [x] The format and not-authorized headers round-trip under their new names `[integration]`
- [x] Generated factories register and resolve under the new attribute and registrar names `[integration]`
- [x] A trimmed client publish strips server-only code under the renamed feature switch `[integration]`
- [x] A search of this plan's files finds no brand name beyond the allowed list `[explicit-skip: search result recorded in Test Evidence]`

---

## Current State (Pre-Flight)

**Surface:** 443 tracked files outside markdown, `skills/` and `.claude/` match. That is 415 `.cs`, 16 `.csproj`, 4 `.razor`, 2 `.sln` and one each of `.yml`, `.xml`, `.sh`, `.props`, `.http` and `LICENSE`. There are 170 distinct name tokens.

**Mapping, applied in this order:**

| From | To |
|---|---|
| `Neatoo.{old}` | `DTZero` |
| `AddNeatoo{old}`, `AddNeatoo{old}Core` | `AddDTZero`, `AddDTZeroCore` |
| Prose `Neatoo {old}` | `DTZero` |
| `Neatoo.Generator` | `DTZero.Generator` |
| `namespace Neatoo;` in `src/Generator` | `namespace DTZero.Generator;` |
| `{old}` in names and paths | `DTZero` |
| `Neatoo` as a type or member prefix | `DTZero` |
| `neatoo` in camelCase names | `dtZero` |
| `/api/neatoo` | `/api/dtzero` |

Examples: `AddNeatooAspNetCore` becomes `AddDTZeroAspNetCore`, `UseNeatoo` becomes `UseDTZero`, `NeatooFactory` becomes `DTZeroFactory`, and `NeatooRuntime` becomes `DTZeroRuntime`.

**Must stay, because they mean the separate Neatoo library:**

- `AddNeatooServices` and `Neatoo's` in comments.
- `A Neatoo base class` and `A Neatoo server` in comments.
- Test names `CtorTarget_NeatooStyle`, `NeatooStyle_*`, `AddLikeNeatoo`, `ShowcasePerformanceNeatooObj`, `InterfaceFactory_NonNeatooType_NoRefMetadata`.
- `NeatooBaseJsonTypeConverter` and `NeatooListBaseJsonTypeConverter` need a check at the keyboard, because they may name Neatoo library types.
- `NeatooDotNet`, the GitHub org, including the `Copyright` line in `LICENSE`.
- `neatoo_icon.png`, which AC-9 owns.
- The test value `"Neatoo"` in `InterfaceFactoryTests`, which is arbitrary data.

**Generated-name trap:** the generator appends `Factory` to a class name. So `ClassExecRemote`, `ShowcaseAuthRemote`, `LogicalModeTarget_Remote` and about 70 `Comb_*_Remote` classes get factory interfaces whose names contain `{old}` without being the brand. A text replacement breaks them, yet AC-3's search matches them. The fix is to rename those classes so their suffix becomes `RemoteMode`, giving `IClassExecRemoteModeFactory`. The combination generator appends the suffix in `TargetClassGenerator`, and `CombinationDimensions.json` keeps its `"Remote"` execution mode value.

**Wire and configuration names that change:**

- Route, in `WebApplicationExtensions` and `MakeRemoteDelegateRequestHttpCall`.
- `X-Neatoo-Format` and `X-Neatoo-Not-Authorized`.
- Log categories such as `Neatoo.{old}.Server`.
- Feature switch `Neatoo.{old}.IsServerRuntime`, in the library, `ILLink.Descriptors.xml`, `Person.Client` and the trimming project.
- The `NeatooHttpClient` key.
- Diagnostic categories `{old}.Usage` and `{old}.Configuration`.

**Name-sensitive code:** the generator matches `ForAttributeWithMetadataName` strings and `ToDisplayString()` comparisons. A missed literal compiles in the generator and silently generates nothing. `StringComparer.Ordinal` sorting in the generator keys on consumer names, so the library rename does not reorder it.

**Trimming check:** `dotnet test` does not exercise the feature switch. The gate runs the trimmed publish and `verify-trimmed.sh` steps from `build.yml`.

---

## Punchlist

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Tier confirmed |
|---|---|---|---|---|
| Both solutions build | Required | `[explicit-skip]` | `reviews/001-build.log`, `reviews/001-design-build.log`: 0 errors, same warning count as baseline | ✓ |
| Totals equal baseline | Required | `[explicit-skip]` | `reviews/001-test.log`, `reviews/001-design-test.log`: 3,430 passed, 12 skipped, 0 failed | ✓ |
| Client reaches server on `/api/dtzero` | Required | `[integration]` | `Design.Tests.Http.*` over real HTTP, and `DTZeroTests.UnitTests.Internal.MakeRemoteDelegateRequestHttpCallTests.Call_PostsToTheEndpoint_WithTheCorrelationIdHeader` | ✓ |
| Headers use their new names | Required | `[explicit-skip]`, per amendment | Search: `X-DTZero-Format` and `X-DTZero-Not-Authorized` are the constants, and the old header prefix has 0 matches | ✓ |
| Generated factories register and resolve | Required | `[integration]` | `Design.Tests.Http.AspNetCoreHttpTests.Order_SavesAndFetches_OverHttp` and `Design.Tests.FactoryTests.SerializationTests.ExecCtorEntity_IsAutoRegisteredByGenerator_ClientSide`, with `AssemblyAttributeEmissionTests` as supporting evidence | ✓ |
| Trimmed client strips server-only code | Required | `[integration]` | `reviews/001-trim-publish.log`, `reviews/001-trim-verify.log`, `reviews/001-trim-harness.log`. Same steps as `build.yml`, with the linux-x64 harness run in WSL | ✓ |
| No brand name beyond the allowed list | Required | `[explicit-skip]` | Search of scope: `{old}` has 0 matches. `Neatoo`-prefixed names left: `NeatooBaseJsonTypeConverter`, `NeatooListBaseJsonTypeConverter`, `NeatooDotNet`, `neatoo_icon` | ✓ |

**Flips, each restored afterwards:**

- Client route set back to the old value: the unit test above failed, and all 16 `Design.Tests.Http` tests failed.
- Feature switch name broken in the trimming project: `verify-trimmed.sh` failed 43 checks.
- Generator attribute literal broken: 33 compile errors in `DTZero.UnitTests`.
- `global::` stripped from every generator reference: 20 of 40 `AssemblyAttributeEmissionTests` failed, including each `*_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles` decoy test.

Flip logs are `reviews/001-flip*.log`. The trimming harness ran in WSL Ubuntu-24.04.

**Test changes, by kind. No claim changed in any of them:**

| Kind | Claimed before | Claims now | Justification |
|---|---|---|---|
| Namespaces, usings and type names | Same behavior under old names | Same behavior under new names | AC-2, user decision |
| Expected strings in assertions for generated code, log categories and the route | Old literal | New literal | AC-2, user decision |
| `Remote`-suffixed target classes become `RemoteMode` | Same | Same | AC-3 |
| `NeatooStyle_*` methods become `BaseClassStyle_*` | Same | Same | AC-2 |
| Shadowing decoys move to the `DTZero` root | Shadow the generated root | Shadow the generated root | Plan review row 2 |

---

## Gate Record

- Round 1 (2026-11-06): CONCERNS — 1 veto addressed, a citation fix with no test change; 1 tech-debt line punched; could-not-verify resolved with flip logs and a fourth flip — `reviews/001-test-review.md`
- Round 2 (2026-11-06): APPROVED — Done

---

## Plan Amendments

### 2026-11-06 — Plan review corrections

- **Section affected:** Scope, Steps, Acceptance, Current State
- **Original said:** See the sections above, which stay frozen.
- **What changed:**
  - Scope excludes everything under `docs/`, which DTZ-003 owns along with the demo GIF's file name.
  - The `Remote`-suffix class rename runs first, before any `{old}` rule, and only for classes whose factory name would contain `{old}`.
  - The suffix changes at `CombinationInfo.ClassName`, keeping the JSON value `"Remote"`.
  - Trace category becomes `Trace.DTZero`, a decision by Keith.
  - Headers become `X-DTZero-Format` and `X-DTZero-Not-Authorized`. That bullet is proven by search, not `[integration]`, because tests read the constants and adding one would change the count.
  - `TestNamespace.Neatoo` and `TestAssembly.Neatoo` decoys become `TestNamespace.DTZero` and `TestAssembly.DTZero`, so they keep shadowing the generated root.
  - Test namespaces move to a `DTZeroTests.*` root, keeping them outside the library namespace. Project and assembly names are `DTZero.*`.
  - `NeatooStyle_*` test methods become `BaseClassStyle_*`.
  - `ShowcasePerformanceNeatooObj` is this library's own name and renames. `NeatooBaseJsonTypeConverter` and `NeatooListBaseJsonTypeConverter` stay.
  - Bare-word `Neatoo` defaults to staying, with an explicit list of the lines that mean this library. Every changed bare-word line is reviewed.
  - Added rows: the generator's `Factory` class and `TrackingNames`, regex-escaped forms, `api/neatoo` without its slash, and the lower-case `{old}` forms.
  - The trimming gate runs publish, `verify-trimmed.sh` and the harness locally.
  - Step 3 wording is wrong, because the generator's namespace is today the parent. The target `DTZero.Generator` stands.
  - Step 7 is dropped, because no generated code is tracked.
  - The region constraint applies to snippet regions only.
- **Why:** Plan review, `reviews/001-plan-review.md`.
- **Discovery Log:** 2026-11-06 / DTZ-001

---

## Abandonment / Retirement Reason

---

## Notes

**Silent breaks for consumer migrations.** These produce no compile error when an app moves to DTZero, so the Neatoo and zCRM todos must handle them:

- An old feature switch name is ignored, so server-only code stays in the trimmed client.
- Old log category names in app configuration stop matching.
- Any app middleware keyed on the old route or headers stops matching.
