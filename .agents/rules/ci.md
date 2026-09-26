# Continuous integration

The GitHub Actions workflows, what each job checks, and the self-hosted runner.

## CI

`.github/workflows/tests.yml` runs the test suites on every same-repo pull request and on pushes to `dev` and `master` — both the Unity suites and, in a single `tooling-tests` job, the tests for the repo's own scripts. Every job that needs Node reads the version from `.nvmrc` via `node-version-file`, so a bump is one edit rather than seven. `.github/workflows/release.yml` separately runs `validate` and `pack`, and `.github/workflows/changelog.yml` enforces [the changelog rules](./releases.md#changelogs--four-rules-enforced-in-ci). `.github/workflows/format.yml` runs the [formatters](./code-style.md#formatting) in check mode. Design notes: [`docs/specs/2026-08-30-pr-test-ci-design.md`](../../docs/specs/2026-08-30-pr-test-ci-design.md).

| Job             | Runner              | Notes                                                                                                                            |
| --------------- | ------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `tooling-tests` | `ubuntu-latest`     | The repo's own tooling tests — the `Tools/ci/` PowerShell helpers, the changelog check, and the release flow. Runs on forks too. |
| `unity-tests`   | self-hosted Windows | EditMode + PlayMode. **Never runs on fork PRs** — see below.                                                                     |
| `report`        | `ubuntu-latest`     | Turns the JUnit XML into PR annotations.                                                                                         |
| `format`        | `ubuntu-latest`     | `npm run format:check`: CSharpier and Prettier. Runs on forks too.                                                               |

Job names are unique across all four workflows on purpose: two identically named entries in a PR's check list cannot be told apart, which matters the moment either becomes a required check. Hence `unity-tests` rather than `test`, and one `tooling-tests` job rather than one per tool. For the same reason the report step runs with `annotate_only: true`: creating a check run gives GitHub no way to say which check suite it belongs to, so it can file the result under another workflow, such as _changelog_, and a red Unity suite would point the reader at the wrong place.

Three rules that are load-bearing rather than stylistic:

- **The `unity-tests` job must never run on a fork PR.** This repo is public and the runner is a physical machine with a live Unity licence. The job's `if:` condition is the only thing preventing a drive-by PR from executing code there. Never add a `pull_request_target` trigger to this workflow, and never pin a third-party action by tag instead of commit SHA.
- **CI runs the Editor in `ProjectVersion.txt`, or fails.** No `-e`, no `--allow-install`. `Tools/ci/Resolve-UnityEditor.ps1` enforces this.
- **Only `Library/` survives between runs.** `TestResults/` and `Logs/` are wiped every job, so everything the pipeline publishes was produced by that run. The `clean_library` input on a manual dispatch forces a cold run, which is how you tell a poisoned cache from broken code.

The helper scripts under `Tools/ci/` have their own tests, which need no Unity and no runner:

```powershell
powershell -NoProfile -File Tools/ci/Tests/Test-CiScripts.ps1   # locally (Windows PowerShell 5.1)
pwsh -File Tools/ci/Tests/Test-CiScripts.ps1                    # in CI (PowerShell Core)
```

The `tooling-tests` job runs `pwsh`, because it is on `ubuntu-latest`. The self-hosted `unity-tests` job runs **Windows PowerShell 5.1**, via an explicit shell string set as a job default — PowerShell Core is not installed on the runner, so `shell: pwsh` there fails with `pwsh: command not found` before any step does work.

That shell string spells out three things the built-in `shell: powershell` would not give it: `-NoProfile`, an execution-policy override (the runner account's policy is Restricted and otherwise refuses the `.ps1` GitHub generates per `run:` block), and a trailing `exit $LASTEXITCODE`. The last is load-bearing — GitHub appends that epilogue to its _built-in_ shells only, and without it a step whose final act is a failing script reports success.

Lint the workflows with [`actionlint`](https://github.com/rhysd/actionlint); `.github/actionlint.yaml` declares the self-hosted runner's label so a typo in it is still caught.

`Tools/ci/Publish-UnityLog.ps1` recognises one environment failure by signature: Windows Smart App Control blocking the Editor's `Bee.Tools.dll` (`0x800711C7`), which Unity misreports as `Scripts have compiler errors.`. Smart App Control is off on the runner; the detector exists because that misreported message costs an afternoon to diagnose from cold. Section 8 of the design doc has the background.
