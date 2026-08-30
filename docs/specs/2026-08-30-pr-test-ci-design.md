# Running Unity tests on pull requests

**Date:** 2026-08-30
**Status:** **Accepted.** Blocked on two things before it can go green — a self-hosted runner must be
registered (§3), and the Smart App Control block in §8 must be resolved. The workflow is correct and
committable before either happens; it will simply queue, then fail loudly and truthfully.

## 1. The problem

Pull requests in this repo are checked by `release.yml`, which runs `validate` and `pack` on
`ubuntu-latest` with Node only. Both are useful — they catch malformed `package.json` files, bad
version fields, and packaging mistakes — but neither one compiles a line of C# or runs a single test.

The repo has **34 assembly definitions across 18 packages, 9 of which are test assemblies**. Today
those tests run only when someone remembers to run them by hand, in an Editor that is already open.
A PR can turn every package red and still show two green checks.

This spec adds a Unity test job to the pull request path.

## 2. What runs where, before and after

| Check | Runner | Trigger | Status |
|---|---|---|---|
| `validate` | `ubuntu-latest` | every PR, push to `dev`/`master` | exists |
| `pack` | `ubuntu-latest` | every PR, push to `dev`/`master` | exists |
| **`test`** | **self-hosted Windows** | **same-repo PRs, push to `dev`/`master`, manual** | **new** |
| **`report`** | **`ubuntu-latest`** | **after `test`, always** | **new** |
| `tag` | `ubuntu-latest` | manual, from `master` only | exists, untouched |

The new workflow's triggers are `pull_request`, `push` to `dev` and `master`, and
`workflow_dispatch` — the manual trigger exists to carry the `clean_library` escape hatch in §4.

The new jobs live in a **new workflow file**, `.github/workflows/tests.yml`, rather than as jobs
added to `release.yml`. Three reasons: the runner and trigger surface are different; a red test
should not muddy the release workflow's status; and `release.yml` carries a deliberately staged
`tag` rollout gate that is best left alone.

## 3. Runner: self-hosted, forks excluded

The test job runs on a **self-hosted Windows runner on an already-licensed machine**, labelled
`unitypackages-windows`, concurrency 1.

Self-hosted is chosen over an ephemeral GitHub-hosted runner for three reasons. The machine already
holds an activated Unity seat, so no license secrets enter the repo. The Editor is already
installed. And `Library/` can persist between runs, which is the single largest lever on wall-clock
time (§4). The `unity test` invocations below are then exactly the ones already documented in
`.agents/AGENTS.md`, rather than a second, divergent CI-only path.

Concurrency must be 1: Unity refuses to open the same project twice, so two overlapping jobs would
fail on the second.

### The fork boundary

**This repo is public, and a self-hosted runner executes checked-out code on a physical machine with
a real filesystem and a real Unity license.** A `pull_request` event from a fork checks out the
contributor's code. Running that unguarded would be handing arbitrary write access to anyone who
opens a PR.

The test job is therefore gated:

```yaml
if: github.event_name == 'push' ||
    github.event.pull_request.head.repo.full_name == github.repository
```

Fork PRs fall through to the existing `ubuntu-latest` `validate` and `pack` jobs and get nothing
else. This is a correctness requirement, not a hardening nicety — without it the design is unsafe.

Two repository settings back it up, and both should be set when the runner is registered:

* Settings → Actions → General → **Require approval for all external contributors**.
* Register the runner **scoped to this repository**, not to the account or an organisation.

### Registration is a manual step

No runner is registered today. Registration needs an admin token from Settings → Actions → Runners,
run on the machine itself, and should be installed as a service so it survives a reboot. This cannot
be automated from inside the repo. Until it happens the test job queues indefinitely; the existing
ubuntu jobs are unaffected.

## 4. Keeping `Library/` warm

`Library/` in this project is **2.3 GB**. Rebuilding it from cold is a full asset import of the URP,
2D, Burst, Timeline, Input System and Visual Scripting packages in `Packages/manifest.json` — on the
order of twenty minutes, paid on every single job.

`actions/checkout` defaults to `clean: true`, which is `git clean -ffdx`. That deletes `Library/`.
So the checkout is done without cleaning and the clean is performed explicitly, with one exclusion:

```yaml
- uses: actions/checkout@v7
  with:
    clean: false
- name: Clean, preserving the asset import cache
  run: |
    git clean -ffdx -e Library/
    git reset --hard
```

**Only `Library/`.** Not `TestResults/`, not `Logs/`. Those are job *outputs*, not caches, and
preserving them creates a specific failure: a job that dies before writing its results would upload
the *previous* run's XML as this run's, reporting a stale pass. Everything this pipeline publishes
must have been produced by this pipeline. They stay wiped.

The cost of preserving `Library/` is that a corrupt cache can now outlive the job that created it.
The escape hatch is a `workflow_dispatch` input, `clean_library`, which when set runs a full
`git clean -ffdx` — that is what distinguishes "the code is broken" from "the cache is poisoned"
when a run goes red for no visible reason.

## 5. Editor version: use the project's, fail if absent

CI runs **the Unity version the project declares**, and errors out if that version is not installed
on the runner. It does not pin, override, or substitute a different editor.

`unity test` already defaults to the version in `ProjectSettings/ProjectVersion.txt`, so the correct
behaviour is the default. Two things make it strict:

* **`--allow-install` is never passed.** With it, a missing editor triggers a silent multi-gigabyte
  download in the middle of a job. Without it, a missing editor is an error.
* **A preflight step names the problem.** Rather than letting the CLI fail in its own words, the job
  checks first and emits a single clear line.

```powershell
$declared = (Select-String 'ProjectSettings/ProjectVersion.txt' `
             -Pattern '^m_EditorVersion:\s*(\S+)').Matches[0].Groups[1].Value

$editors = (unity editors --json | ConvertFrom-Json).data
$editor  = $editors | Where-Object { $_.version -eq $declared }

if (-not $editor -or -not $editor.location) {
  Write-Host "::error::Unity $declared (from ProjectSettings/ProjectVersion.txt) is not installed on this runner."
  Write-Host "Installed: $(($editors | Where-Object location | ForEach-Object version) -join ', ')"
  exit 1
}
Write-Host "Using Unity $declared at $($editor.location)"
```

`unity editors --json` returns a `location` field **only for installed editors** — verified against
CLI 1.0.0-beta.3 on 2026-08-30. Its presence is the installed check; matching on `version` alone
would pass for an editor that is merely known to the Hub.

This choice means CI always tests what the repo declares. Its limitation is documented in §8: an
editor can be installed and still be unable to compile, and this check will not catch that.

## 6. Test execution

Two steps in one job, EditMode then PlayMode. **Both run even if the first fails.** A red EditMode
suite should not hide an independent PlayMode failure — surfacing one failure per push, when two
exist, costs an extra round trip for no saving worth having.

```powershell
unity test --mode EditMode `
  --output TestResults/editmode-results.xml `
  --report-format nunit,junit --junit-output TestResults/editmode-junit.xml `
  --timeout 1800
```

...then the same for `--mode PlayMode`, writing `playmode-results.xml` and `playmode-junit.xml`.

### PlayMode is included ahead of need

All 9 test assemblies currently carry `"includePlatforms": ["Editor"]`, so **there are no PlayMode
tests today**. The step is included anyway so that the first PlayMode test written is covered by CI
automatically, rather than silently unrun until someone notices.

This had an unresolved edge: whether an empty PlayMode run exits `0` or `6`. The step was drafted
defensively, treating *ran, produced a results file, zero tests* as a pass and failing only on a
genuine `6` with no results file.

**Resolved on the first green run.** An empty PlayMode suite exits `0` and writes a well-formed
`<testsuites tests="0"/>` to both the NUnit and JUnit paths. The defensive branch was therefore
unreachable, and has been removed: had a real PlayMode failure ever left a stale results file
behind, that branch would have reported it green — the precise failure this pipeline exists to
prevent. Any nonzero exit is now treated as a real failure.

The remaining consequence is that `require_tests` on the report action stays `false`, since one of
the two XML files legitimately reports zero tests. That is the setting which would otherwise catch
a suite silently not running, so it is worth flipping to `true` once PlayMode assemblies exist.

### Exit codes are distinguished, not collapsed

Per `.agents/AGENTS.md`, observed on CLI 1.0.0-beta.3:

| Code | Meaning | Reported as |
|---|---|---|
| `0` | success | pass |
| `8` | **tests ran and failed** | red suite — read the test report |
| `6` | **the run never produced results** | build/environment failure — read the editor log |

Collapsing `6` and `8` into "nonzero" is what sends someone debugging a test that never ran. The job
reports them differently and says which artefact to open.

### Log capture

`unity test` **streams nothing to stdout and exits silently even on success.** Without explicit
capture, this job's log would contain two command lines and nothing else.

Unity writes to `Logs/Editor.log`, and **the PlayMode editor overwrites what the EditMode editor
left there**, so each step's log is copied aside immediately rather than captured once at the end. A
`Publish-UnityLog` helper does this — copy the log, filter it for
`error CS\d+|Exception:|Unhandled Exception|\[Error\]|Aborting batchmode|Fatal error`, echo a bounded
excerpt inline, dump the tail on failure, and leave the complete file for the artefact upload.

`Exception:` with the colon is deliberate: a bare `Exception` also matches every Mono stack frame
whose signature mentions `System.Exception`, which buries the real lines under hundreds of noise.

Both steps hold their exit code and re-raise it after the capture, rather than letting it abort the
step — a red run is precisely the one whose log is worth having.

### Burst is disabled for this job

`UNITY_BURST_DISABLE_COMPILATION: "1"` is set on the test job. Burst's asynchronous JIT compilation
can raise errors that land inside a PlayMode test's SetUp window, which makes identical commits pass
and fail at random. Nothing in this repo needs Burst AOT — it produces packages, not players — so
removing the JIT step entirely costs nothing and removes a whole class of flake.

Burst reads this from the environment in `BurstCompilerOptions`' static constructor, not from the
command line, so it cannot be silently dropped by a CLI change.

## 7. Reporting

Test results are surfaced by `mikepenz/action-junit-report`, which produces per-test annotations on
the Checks tab and inline on changed lines.

**It runs in a separate `ubuntu-latest` job**, not on the self-hosted runner. The test job uploads
`TestResults/` as an artefact; the report job downloads it and annotates. This keeps a third-party
action with `checks: write` off the physical machine — which matters more than usual given a public
repo and a self-hosted runner. The cost is roughly twenty seconds.

Two constraints on that job:

* **Pin the action by commit SHA, not by tag.** A tag can be moved; in a public repo with write
  permissions that is a supply-chain seam.
* **Narrow `permissions:`** to `contents: read`, `checks: write`, `pull-requests: write`. Nothing
  else.

Both jobs' upload and report steps run `if: always()`, so a red suite still reports rather than
vanishing with the job.

Artefacts uploaded: `TestResults/` and `Logs/`, 30-day retention. The editor logs are worth keeping —
the more obscure failures show up in the asset import and package manager logs rather than the test
XML.

## 8. Known blocker: Smart App Control

**On the current development machine this pipeline will fail, and §5's check will not be what
catches it.**

`ProjectVersion.txt` declares `6000.5.10f1`. That editor **is installed** — it has a `location`, so
the preflight in §5 passes. The run then fails with `Scripts have compiler errors.`, which is false.

The real cause is Windows Smart App Control blocking
`6000.5.10f1/Editor/Data/Tools/BuildPipeline/Bee.Tools.dll` with `0x800711C7`. `BuildProgram` exits
before any script compiles. The only evidence is in `Logs/Editor.log`; the CLI's message is
misleading.

The job therefore carries a **failure-path diagnostic**: on a failed run it scans the captured log
for that signature and emits

```
::error::Unity could not compile because Smart App Control blocked Bee.Tools.dll (0x800711C7).
This is an environment block on the runner, not a compile error in this PR.
```

This costs nothing on a green run and prevents the next person losing an afternoon to a lying error
message.

Three ways out, in the order they should be considered:

1. **Resolve Smart App Control on the runner.** This is the owner's decision and is deliberately not
   made here: Smart App Control cannot be re-enabled without reinstalling Windows, so disabling it
   is not a step this spec recommends taking casually.
2. **Move `ProjectVersion.txt` back to `6000.5.0f1`,** which is installed and unaffected. This also
   resolves the stale claim in `.agents/AGENTS.md` (§11). It costs the editor bump from `b5975d6`.
3. **Host the runner on a machine without Smart App Control enforcing.**

Until one of these happens the pipeline is red. That is the intended behaviour of a design that runs
the version the project declares: it reports the environment as genuinely broken rather than quietly
testing something else.

## 9. Out of scope

* **No build job.** This repo produces packages, not players. `pack` in `release.yml` already covers
  the artefact that matters.
* **No changes to `release.yml`,** in particular not to the staged `tag` gate.
* **No per-package test jobs.** One Editor launch covers all 9 test assemblies; nine launches would
  multiply the dominant cost by nine to gain isolation nobody has asked for.
* **No code coverage.** Worth revisiting once the suite runs green in CI at all.

## 10. Verification

A workflow cannot be test-driven, so it is verified by deliberate failure on a throwaway branch:

1. Open a PR into `dev`. Confirm the test job is picked up by the runner and that §5 logs the
   resolved editor path.
2. Confirm the empty PlayMode run is reported as a pass, and record whether it exits `0` or `6`
   (§6). Amend the step if the assumption was wrong.
3. Add a deliberately failing EditMode test. Confirm the check goes red, that the failing test is
   named in the PR annotations, and that it is reported as a red suite (exit `8`).
4. Introduce a deliberate compile error. Confirm it is reported as a build failure (exit `6`), not
   as a test failure.
5. Open a PR from a fork, or simulate one. Confirm the test job does not run.
6. Delete the branch.

Steps 3 and 4 are the ones that matter: a CI pipeline that has never been observed failing correctly
has not been verified.

## 11. Follow-up

`.agents/AGENTS.md` states the sandbox Editor is `6000.5.0f1`. `ProjectVersion.txt` has been
`6000.5.10f1` since commit `b5975d6`. The document is stale and should be corrected alongside this
work, together with a short section describing the CI added here.
