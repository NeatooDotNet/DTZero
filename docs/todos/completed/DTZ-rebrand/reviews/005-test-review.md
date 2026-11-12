# DTZ-005 Test Review

**Date:** 2026-11-07
**Reviewer:** test-reviewer, tight budget

## Round 1 — CONCERNS, pre-publish checkpoint

The review covered the four checked bullets. The public history, CI and NuGet bullets follow the user's confirmation.

**Logs:** builds passed. Tests: 3,430 passed, 12 skipped, 0 failed, equal to baseline. Trimming passes. Neatoo: 2,030 passed and 2 skipped, plus 235 passed, equal to its baseline.

**Independent check:** every `DTZero.dll` in Neatoo's output matches a packed hash, and no old-package DLL remains. A replay of the rewrite shows only the recorded hand edits on top of it. No Neatoo assertion, skip or test attribute changed. Every name in the release note matches the code. The workflow's paths exist, and no build input needs git history.

| # | Finding | Disposition |
|---|---|---|
| V1 | The plan said Neatoo's tests run on one framework, when 55 ran on net10.0. | Fixed. |
| C1 | "One commit" conflicts with recording the result, and a slipped date would go stale. | Amended: a later clean commit closes the todo. |
| C2 | The publish key is not in place and its scope is unknown. | With the user before the tag. Later replaced by Trusted Publishing, so no key is stored. See the plan's amendments. |
| C3 | The push steps named no exact ref, so a wrong one could upload the old history. | Amended: every push names the public URL and an exact ref. |

## Round 2 — CONCERNS, last round

V1 is closed, with no veto. The numbers check out: 55 on net10.0 and 2,210 on net9.0.

| # | Finding | Disposition |
|---|---|---|
| C1 | The closing commit had no parent rule, ancestry check or user confirmation, so it could carry the old history. | Amended after the round: it is built in a fresh clone of the public repo, `--force` is forbidden, and the user confirms. Not re-reviewed. |
| C2 | The local-pack note was too broad. | Fixed: it now says "in anything packed". |
| C3 | Some todo lines are written for the published reader, so they are not true yet. | Accepted: these files go public only with the push, and the private repo's deletion follows it. |

## Tech-Debt

`AssemblyVersion` stays 1.0.0.0, and `build.yml` keeps a stale comment about history. Both predate this todo, and both are dismissed. The package scan's old-name check is weaker than the reviewer's, whose stronger scan passed.

## Could Not Verify

- The harness log shows no WSL marker.
- Release-configuration tests under the new names. CI's first run is the first.
- Any NuGet key scope, and organization-level secrets.

## Theoretical

Not triaged. .NET 9 reaches end of support soon. If CI's SDK then reports that as a warning, `TreatWarningsAsErrors` could fail the build.

## Parked

None.
