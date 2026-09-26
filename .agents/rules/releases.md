# Releases

How a package version reaches OpenUPM, the release tooling, and the changelog rules CI enforces. The step-by-step procedure is the [`releasing-packages`](../skills/releasing-packages/SKILL.md) skill.

## Distribution and releases

The `version` field in each `Packages/<Dir>/package.json` is the **single source of truth**.

`npm pack` output (`*.tgz`) is a local verification aid, not a distribution channel — it is git-ignored by the `/PackageExports/` and `*.tgz` rules in `.gitignore`. Don't commit tarballs.

**No package carries a pre-release suffix.**

### Release tooling — `Tools/upm-release.mjs`

Dependency-free Node, used identically by a developer and by CI. Discovers packages by globbing `Packages/*/package.json` and skipping any manifest with `"private": true` (i.e. `PackageTemplate`).

```powershell
node Tools/upm-release.mjs validate            # is every package releasable?
node Tools/upm-release.mjs pack                # tarballs into PackageExports/ (git-ignored)
node Tools/upm-release.mjs tag --dry-run       # what would be tagged?
node Tools/upm-release.mjs tag --push          # create + push tags — THIS IS THE PUBLISH
node Tools/upm-release.mjs tag --push --only com.arman.service-locating   # one package
node Tools/upm-release.mjs prepare --dry-run    # what would each [Unreleased] section become?
node Tools/upm-release.mjs prepare              # rename the headings, bump the versions
```

### The whole flow in one go — `Tools/release.bat`

`Tools/release.bat` (a two-line wrapper over `Tools/release-flow.mjs`) runs an entire release and **takes no arguments**:

```powershell
Tools/release.bat            # or: node Tools/release-flow.mjs
```

Six steps, stopping at the first failure: preflight (`git` and `gh` present and authenticated, on `dev`, clean tree, not behind `origin/dev`) → `validate` → `prepare` → commit the bumps → push `dev` → `gh pr create --base master --head dev`. It prints the pull request URL and stops.

**It stops there deliberately.** Merging that pull request is the publish, and an OpenUPM tag is permanent, so the irreversible step stays a human click on a green PR. If no package has a populated `## [Unreleased]` section it says so and exits 0, having changed nothing. Re-running while a release PR is already open updates that PR rather than failing.

Passing it any argument is an error (exit 2) that points back at `upm-release.mjs` — that script is where single steps, `--dry-run`, `--only` and `--bump` live. Nothing forwards sub-commands; spell those `node Tools/upm-release.mjs <command>`. The flow's own tests are `Tools/release-flow.test.mjs`, run by `tooling-tests` in `tests.yml`.

> `release.bat` contains **no backslash at all**. One test pins that; two more pin that the `node` invocation is its only executable line and that it is CRLF. Tooling that eats backslashes turns a `Tools\release.bat` usage line into a bare `release.bat` command line, which cmd executes and which recurses forever when the working directory is `Tools/`. Keep it that way.

`--only` takes a package id or a folder name (`--only "UI Management"` works), is repeatable, and errors if it matches nothing. It is the way to release one package by hand without touching the others. Under `--only`, `validate` still resolves dependencies against _every_ package, not just the selected ones.

`prepare` turns every package's `## [Unreleased]` section into a version. Per package: no heading, or a heading with no entries, means skip; otherwise the `###` sub-headings with bullets under them decide the level — `Removed` is breaking, `Added`/`Changed`/`Deprecated` are features, `Fixed`/`Security` are fixes, highest wins — and **while the major is `0`, breaking and feature both land on the minor**. The heading is renamed to `## [X.Y.Z] - YYYY-MM-DD` with nothing left in its place, `package.json`'s `version` line is rewritten in place, and `validate` re-runs over the packages it touched.

`--bump <package>=<major|minor|patch>` overrides the derived level for one package and is repeatable; entries filed under no recognised `###` heading are an error rather than a guess. `prepare` refuses to run **on** `master` and refuses a dirty tree (`--allow-branch`, `--allow-dirty`), the inverse of `tag`'s guards. **It edits files and stops there** — it does not commit, push, tag, or open a pull request. Skipping it is not a quiet mistake: a PR into `master` carrying a surviving `## [Unreleased]` heading fails `unpromoted-unreleased` (see [the changelog rules](#changelogs--four-rules-enforced-in-ci)).

`prepare` also carries each bump into the manifests that pin it. When a package's version moves, every `com.arman.*` dependency range pointing at the old one is rewritten to the new one, and the dependent is added to the plan itself: a **patch** bump, plus a generated `### Changed` entry reading ``- Updated `com.arman.<dep>` to `X.Y.Z`.`` — filed under `## [Unreleased]`, creating that section if the package had none, and then promoted to a version heading like any other. This cascades transitively (a dependent of a dependent moves too) and runs over **every** publishable package even under `--only`, because the alternative is publishing a package whose siblings pin a version the repo no longer has. `--bump` still overrides the level for a package the cascade pulled in. A dependent that has entries of its own keeps its own derived level and simply gains the extra bullet.

Why bump the dependent at all, when `validate` accepts a dependency at a version that is either current or already tagged? Because the manifest change is a real change to a published artifact. Leaving it unversioned would make the repo's `0.1.0` differ from the `0.1.0` already tagged and consumed, and under the OpenUPM model that tag is permanent — there is no second chance to correct it.

`validate` checks, per package: parseable JSON; `name` matches `com.arman.<kebab-case-name>`; valid semver; `displayName`; a `description` that is not stock placeholder text; a `unity` minimum version; `license: "MIT"` plus a `LICENSE.md`; **a `.meta` file for every file and folder**; every `com.arman.*` dependency resolving to a non-private package in this repo at a version that is either current or already tagged; and `npm pack --dry-run` succeeding. Exit 0 = all valid, 1 = at least one failure.

`tag` refuses to run on a dirty tree or off `master` (`--allow-dirty`, `--allow-branch` override). It is idempotent — a package whose `<name>/<version>` tag already exists is skipped — and needs no topological sort, because tags are independent. Add `--json` to any subcommand for machine-readable output.

⚠️ **`--push` publishes.** OpenUPM picks the tag up within 15–30 minutes and the resulting name/version is permanent. Without `--push` the tags stay local and are removable with `git tag -d`.

`.github/workflows/release.yml` runs `validate` + `pack` on every PR and on every push to `dev` or `master` — so a change is checked when it merges to `dev` and again when it is promoted. The `tag` job runs only from `master` (see [Branching](./git-workflow.md#branching)); its only permission is `contents: write`, and there is no registry secret anywhere in the pipeline.

⚠️ **Merging a release PR into `master` publishes.** The `tag` job runs on that push — `if: github.event_name == 'push' && github.ref == 'refs/heads/master'` — and creates and pushes a tag for every package whose current version is not tagged yet. It is idempotent, so a push to `master` that changes no version tags nothing, but there is no confirmation step and no dry run in front of it. Bump versions on `dev` and leave them there until you actually mean to release.

**Keep the `github.ref == 'refs/heads/master'` condition:** it is the only thing stopping a routine push to `dev` from publishing.

The registry-hosting design is specced in [`docs/specs/`](../../docs/specs/):

| Document                                                                                                                           | Contents                                                                                               |
| ---------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| [`docs/specs/2026-08-22-upm-package-registry-design.md`](../../docs/specs/2026-08-22-upm-package-registry-design.md)               | GitLab + npmjs.com variant. Superseded, but still holds the shared problem statement and cleanup list. |
| [`docs/specs/2026-08-23-upm-package-registry-github-design.md`](../../docs/specs/2026-08-23-upm-package-registry-github-design.md) | **Current direction** — GitHub + OpenUPM.                                                              |

The GitHub spec's §3 carries the OpenUPM submission table (ids and `gitTagPrefix` bases). The [package catalogue](./packages.md#package-catalogue) and each `package.json` are the source of truth if the two disagree.

Under the current direction: releasing is **creating a git tag**, not uploading. OpenUPM's build pipeline watches tags and builds versions itself, so a per-package tag `<package-name>/<version>` (matched by OpenUPM's `gitTagPrefix`) is the entire publish step. Bump `version` on `dev`, promote it with a release PR `dev` → `master`, then tag from `master` — see [Branching](./git-workflow.md#branching).

**A published package name and version are permanent.** Verify both before a first publish.

### Changelogs — four rules, enforced in CI

A package CHANGELOG carries a `## [Unreleased]` heading **only while it has entries under it**. The contributor with something to record creates the heading; `upm-release.mjs prepare` renames it to a version heading and leaves nothing in its place. An empty heading is a CI failure — see `empty-unreleased` below. `.github/workflows/changelog.yml` runs `Tools/changelog-check.mjs` on every PR into `dev` or `master` — dependency-free Node, same as the release tooling, and runnable locally:

```powershell
node Tools/changelog-check.mjs --base dev --head HEAD
node Tools/changelog-check.mjs --base dev --head HEAD --json
node --test Tools/changelog-check.test.mjs    # the check's own tests, 50 of them
```

| Rule                    | What it enforces                                                                                                                                                                                                                                                                                                                                                                                              | Waiver label        |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------- |
| `missing-entry`         | A change to a package's **shipped code** must be recorded under that package's `## [Unreleased]` heading. Reported as `missing-changelog` or `missing-section` when the file or the heading is what is absent; the waiver covers all three. Opening a **new version section** satisfies the rule in place of an entry, so a release PR — where `prepare` has renamed every heading — passes without a waiver. | `no-changelog`      |
| `frozen-section`        | A version section whose `<package-name>/<version>` tag **already exists** must not be edited or deleted.                                                                                                                                                                                                                                                                                                      | `changelog-rewrite` |
| `empty-unreleased`      | A `## [Unreleased]` heading must have at least one entry under it. Checked **repo-wide** at the head commit, not just on the packages the PR touched.                                                                                                                                                                                                                                                         | _none_              |
| `unpromoted-unreleased` | On a PR **into `master`** only: no `## [Unreleased]` heading may survive at all. Also repo-wide.                                                                                                                                                                                                                                                                                                              | _none_              |

The two waivers are deliberately separate — "this change needs no entry" is not the same claim as "I may rewrite what `0.1.0` says it shipped". Labels are read _inside_ the script rather than gating the job with `if:`, so the check always reports a real success instead of `skipped`; that matters if it is ever made a required check, because a skipped required check blocks the merge.

**Shipped code** triggers `missing-entry`, and only that: anything under `Runtime/` or `Editor/`, plus `package.json`. `Tests/`, `Samples/`, `Documentation/`, every `*.md`, and every `*.meta` are exempt — none of them reach a consumer of the published tarball, so a doc fix or a GUID churn never demands an entry. `frozen-section` looks at the CHANGELOG regardless, precisely because Markdown is otherwise exempt and released history could be rewritten unseen.

Two packages are skipped by `missing-entry` and `frozen-section`: one with `"private": true` (i.e. `PackageTemplate`), and one that is **new** in the pull request — its CHANGELOG documents an initial release, not an unreleased delta. A private package is exempt from all four rules; a new one is exempt from three, but not from `unpromoted-unreleased`.

Details worth not re-deriving:

- The diff is taken against the **merge base**, so commits landing on `dev` after you branched are never blamed on your PR.
- A bare `### Added` with no bullet under it does not count as an entry.
- Trailing whitespace inside a frozen section is ignored — no reader can see it.
- **The release PR is not a false positive.** Renaming `## [Unreleased]` to `## [0.2.0]` satisfies `missing-entry` on its own, because opening a version section that did not exist at the base is exactly what a release does. That version has no tag yet, so `frozen-section` does not fire on it either; the tag comes after the merge.
- `frozen-section` reads `git tag`, so CI checks out with `fetch-depth: 0`. A shallow fetch would leave the tag list empty and silently disable the rule.
- `empty-unreleased` has **no waiver label**, deliberately: "I need an empty heading" is not a claim worth being able to make. Delete the heading or fill it in.
- It and `unpromoted-unreleased` are the two rules that are not diff-scoped. Every publishable package's CHANGELOG is read at the head commit.
- `unpromoted-unreleased` is **the release gate**. Without it, a release PR (dev → master) that skipped `prepare` would pass every check: no `package.json` version moves, so `tag` tags nothing, and the work lands on `master` still labelled unreleased. The remedy the failure names is the missing step: run `node Tools/upm-release.mjs prepare` on `dev`, commit, push.
- It needs the base **branch name**, which `--base` (a SHA in CI) cannot supply, so `changelog.yml` passes `--base-branch "$BASE_REF"` from `github.event.pull_request.base.ref`. Omit the flag and the rule is inert — a local `node Tools/changelog-check.mjs --base dev --head HEAD` never fires it. To rehearse a release PR locally, add `--base-branch master`.
- It supersedes `empty-unreleased` rather than compounding with it: an empty heading is unpromoted too, and one remedy deserves one diagnostic.
- Unlike every other rule, a package **new in the PR is not exempt** from it. A new package legitimately carries an empty scaffold heading while it is being written, but one crossing into `master` for the first time still has to name the version it publishes as.
