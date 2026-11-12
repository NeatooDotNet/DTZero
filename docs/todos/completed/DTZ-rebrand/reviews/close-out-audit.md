# DTZ Close-Out Audit

## 2026-11-07 — Pre-Publish

**Grade: C, on one item. AC-7 is pending and was not graded.**

The audit ran before the public push, so that its findings could be fixed before the clean commit existed.

**The C item:** AC-9's icon half is unmet, and its Parked row had no issue number. The user chooses between fixing it, parking it, or closing as Blocked.

There are no veto-tier findings.

| AC | Word | Evidence | Holds? |
|---|---|---|---|
| AC-1 | Required | `reviews/005-*.log`: 0 errors, 3,430 passed, 12 skipped, 0 failed, equal to baseline. Test attribute, assertion and skip counts match the baseline. | Yes |
| AC-2 | Required | Project files and renamed wire names traced to source. A token census outside the docs finds only Neatoo references, stay-list test names, the org name and the icon file. | Yes |
| AC-3 | Required | Byte scan of all 1,108 tracked files: 0 plain, 0 UTF-16, 0 split, 0 paths. | Yes |
| AC-4 | Required | 39 live issues, 21 open, 9 comments, 1 not planned, no old name. All map rows match the predecessor. 14 sampled citations land on the right issue. | Yes |
| AC-5 | Required | README, both project files and both nuspecs. | Yes |
| AC-6 | Required | `reviews/005-neatoo.log`: 2,030 passed and 2 skipped, plus 235 passed, equal to baseline, and the DLL hash matches. | Yes |
| AC-7 | Required | Public repo empty. Both NuGet IDs return 404. | Pending |
| AC-8 | Required | `skills/DTZero` matches the installed copy. `skills/` holds only `DTZero`. | Yes |
| AC-9 | Optional | GIF checked. Icon unmet. | No, icon only |

### Callouts and Dispositions

| # | Finding | Disposition |
|---|---|---|
| C1 | The migration rows had no existing target, no attributed ruling and no zCRM owner, and their detail sat in session-only places. | Amended: rows name both projects and the files, the ruling is logged, and the old-to-new table is recorded outside the repo. A durable target is with the user. |
| C2 | The clean commit could not record its own CI run or publish, and no step closed the todo. The hard-coded release date could go stale. | Amended: a later clean commit closes the todo, and the date is set on the day of the push. |
| C3 | Statements about the clone, hashes, branches and the import commit would dangle after the switch. | Amended: the header is in the past tense, and one note explains these references. |
| C4 | One PR stayed open on the private repo. | Closed as superseded. |
| C5 | Six dismissals existed only in review files, and two items were left open in one. | Added to Dismissed. |
| C6 | The plan said Neatoo's tests run only on net9.0. | Fixed. |

### Could Not Verify

- The trim logs show no version or WSL marker. The publish output is dated from the 2.0.0 run.
- NuGet: a 404 shows no package under either ID, but it does not rule out a reserved prefix.
- zCRM's own todos, which were outside the audit's reach.

### Theoretical

Not triaged. Three gitignored baseline logs hold the old name and do not reach the clean commit. Relative links outside the current docs: 131 broken at baseline, 130 now, none new.

### Container

Plans 5 of a cap of 8. Discovery Log conforms. Build and tests green.

## 2026-11-11 — Re-Audit

**Grade: B.** Every Required criterion traces, and there are no veto-tier findings. The C item is closed: AC-9's icon half is still unmet, and its Parked row now carries #40. AC-7 now traces in full.

| AC | Word | Evidence | Holds? |
|---|---|---|---|
| AC-1 | Required | Both CI runs on the clean commit, `reviews/final-ci-main.log` and `final-ci-tag.log`: 0 errors, 3,430 passed, 12 skipped, 0 failed, equal to baseline. | Yes |
| AC-2 | Required | Code unchanged since the last audit, apart from `build.yml`. The only `Neatoo` hits are the org URL and a comment naming Neatoo's own converters. | Yes |
| AC-3 | Required | 0 matches and 0 paths among 1,110 files. The public tree equals the finished tree. The scan's 17 hits are the two-word phrase DTZ-003 kept, and the scanner fires on the baseline. | Yes |
| AC-4 | Required | 40 issues: the 39 re-created plus #40. | Yes |
| AC-5 | Required | `README.md`, and both NuGet descriptions start "Zero DTOs." | Yes |
| AC-6 | Required | `reviews/005-neatoo.log`, unchanged. | Yes |
| AC-7 | Required | Both runs green on the one public commit, which has no parent. The tag run exchanged the OIDC token and pushed both packages. Both 2.0.0 packages are listed, owned by `NeatooDotNet`, and the GitHub Release carries both. | Yes |
| AC-8 | Required | `skills/` holds only `DTZero`, and the skill is installed. | Yes |
| AC-9 | Optional | GIF checked. The icon is unmet. | No, icon only: #40 |

### Callouts and Dispositions

| # | Finding | Disposition |
|---|---|---|
| C1 | The plan branch was not merged into the local arc, so the closing commit could take stale todo files. | Fixed: the arc was fast-forwarded to the plan branch, and the closing commit is built from it. |
| C2 | The header says the private working repo was deleted, and it still existed. | The user deletes it before the closing commit is pushed. |

### Could Not Verify

- The nuget.org policy itself. The successful token exchange shows it exists and works.
- zCRM's own todos, carried over.

### Theoretical

Not triaged. The Trusted Publishing change was not gate-reviewed. The auditor read it and found nothing to raise, and it ran green on the tag.
