# Promoting to `master`: an enforced source branch and an automated version bump

**Date:** 2026-09-02
**Status:** **Accepted.** Nothing here is blocked. The three code changes (§4, §5, §6) are
independent and can land in any order; §7 is a one-off cleanup that §6 depends on, and §8 is the
first release run under the new flow.

## 1. The problem

Two parts of the release flow described in
[`2026-08-23-upm-package-registry-github-design.md`](./2026-08-23-upm-package-registry-github-design.md)
are written down but not enforced.

**The source branch is a convention.** `.agents/AGENTS.md` says `master` "moves solely via a release
PR from `dev`", and two guards protect the *tagging* step — `Tools/upm-release.mjs` refuses to tag
off `master`, and the `tag` job is conditioned on `github.ref == 'refs/heads/master'`. Neither one
stops the thing that causes the damage. Any feature branch can open a pull request into `master`
today, and merging it would publish whatever versions that branch happens to carry, from a base that
was never on `dev`. That would also break the invariant the branching model rests on: that every
commit on `master` also exists on `dev`.

**The version bump is manual and error-prone.** Releasing means, for every package with accumulated
`## [Unreleased]` entries: decide a semver bump by reading the entries, edit `version` in
`package.json`, and rename the `## [Unreleased]` heading to `## [x.y.z] - <date>`. Seventeen
publishable packages, done by hand, where a wrong version is *permanent* the moment the release PR
merges — OpenUPM picks the tag up and `<name>/<version>` can never be reissued.

A third, smaller problem sits alongside the second. Every CHANGELOG was seeded with an empty
`## [Unreleased]` heading on 2026-08-30. An empty heading is noise: it appears in the rendered
changelog on the package page, it says nothing, and it makes "has this package got unreleased work?"
a question you answer by reading rather than by looking.

## 2. What changes

| Change | Where | New? |
|---|---|---|
| `promotion-guard` job — a PR into `master` must come from `dev` | `Tools/promotion-check.mjs`, `release.yml` | new |
| `master` ruleset — require a PR, require the checks, no bypass | `.github/rulesets/master.json` | new |
| `prepare` subcommand — derive bumps, rewrite versions and changelogs | `Tools/upm-release.mjs` | new |
| `empty-unreleased` rule — an empty `## [Unreleased]` heading fails CI | `Tools/changelog-check.mjs` | new |
| `release-script-tests` job — `node --test` for the release tooling | `Tools/upm-release.test.mjs`, `release.yml` | new |
| Empty `## [Unreleased]` headings deleted from 13 packages | `Packages/*/CHANGELOG.md` | cleanup |

Untouched: the `tag` job and its `github.ref` condition, `validate`, `pack`, the `missing-entry` and
`frozen-section` rules, and everything in `tests.yml`.

## 3. The flow, after

```
feature branch ──PR──> dev ──┐
                             │  (repeat until you mean to release)
                             │
   node Tools/upm-release.mjs prepare
   on a branch off dev       │
                             ├──PR──> dev          (the version bumps land normally)
                             │
                             └──PR──> master       (the release PR; promotion-guard passes)
                                        │
                                        └─ merge = push to master = `tag` job = PUBLISH
```

`prepare` is a local command that edits files. It does not commit, push, tag, or open a pull
request. Publishing stays exactly where it is today — merging the release PR — and stays a
deliberate human act.

## 4. `promotion-guard`: only `dev` may open a release PR

### The rule

`Tools/promotion-check.mjs`, dependency-free Node, tests in `Tools/promotion-check.test.mjs`. It
reads the event name, base ref and head ref from the environment and decides:

| Event | Base | Head | Result |
|---|---|---|---|
| not `pull_request` | — | — | pass — "not a release PR" |
| `pull_request` | not `master` | — | pass — "not a release PR" |
| `pull_request` | `master` | `dev` | pass |
| `pull_request` | `master` | anything else | **fail**, naming the branch |

Exit codes match the repo's other tooling: `0` pass, `1` fail, `2` bad usage. `--json` for
machine-readable output, as `upm-release.mjs` and `changelog-check.mjs` both have.

### Why the logic is in the script and not in an `if:`

The obvious implementation is a job gated with
`if: github.base_ref == 'master'`. It is wrong here for the reason already recorded in `AGENTS.md`
for `changelog-check`: a job skipped by an `if:` reports `skipped`, and **a skipped required check
blocks the merge**. This job is intended to become a required check on `master` (§4.2), so it must
always run and always report a real result. The branch condition lives inside the script, which
prints "not a release PR" and exits 0 on the overwhelming majority of runs.

### Where the job lives

In `release.yml`, not a new workflow. A pull request into `master` *is* a release, and `release.yml`
already carries the branch-role commentary this rule belongs with. It runs on `ubuntu-latest` with
Node 22, alongside `validate` — no `needs:`, since it is independent of both.

The job is named `promotion-guard`. Not `guard`, not `check`: `AGENTS.md` records that two
identically-named jobs across workflows cannot be told apart in a PR's check list, which matters the
moment either becomes required. `changelog.yml` already owns `check` and `test`.

### 4.2 The `master` ruleset

A GitHub ruleset targeting `master`:

* require a pull request before merging
* require status checks: `promotion-guard`, `validate`, `pack`
* block force pushes
* block branch deletion
* **no bypass actors** — the repo owner included

The ruleset JSON is checked in at `.github/rulesets/master.json` and applied with `gh api`, so the
configuration is reviewable in the repo and reproducible after an accident, rather than existing
only as clicks in GitHub's settings UI. GitHub's rulesets API accepts this file directly.

**A ruleset cannot express the rule in §4.** There is no "restrict the source branch of a pull
request" primitive; rulesets target the *destination* ref only. That is precisely the division of
labour here: the script carries the rule, the ruleset makes the script unskippable.

## 5. `prepare`: derive the bump, rewrite the files

A fourth subcommand in `Tools/upm-release.mjs`, alongside `validate`, `pack` and `tag`.

```powershell
node Tools/upm-release.mjs prepare --dry-run          # what would be bumped?
node Tools/upm-release.mjs prepare                    # rewrite the files
node Tools/upm-release.mjs prepare --bump com.arman.package-basics=patch
node Tools/upm-release.mjs prepare --only "UI Management"
```

### Per package

Packages are discovered the same way every other subcommand discovers them — globbing
`Packages/*/package.json`, skipping any manifest with `"private": true`.

1. Locate the `## [Unreleased]` section in `CHANGELOG.md`. No heading → skip the package.
2. Collect the `###` subsections under it that have **at least one bullet**. A bare `### Added` with
   nothing beneath it does not count — the same rule `changelog-check.mjs` already applies, and it
   must stay the same rule in both places.
3. No entries → skip the package. Its `version` and its CHANGELOG are untouched, and it tags
   nothing on the next release.
4. Derive a bump level from the headings, highest wins:

   | Heading | Level |
   |---|---|
   | `Removed` | breaking |
   | `Added`, `Changed`, `Deprecated` | feature |
   | `Fixed`, `Security` | fix |

5. Map the level onto the current version. **While the major version is `0`, a breaking change bumps
   the minor**, not the major — sixteen of the seventeen publishable packages are `0.x`, and that is
   a deliberate open question per §5 of the GitHub spec, not an accident to be resolved by tooling.
   Once a package is `1.x` or beyond, breaking bumps the major.

   | Level | at `0.x` | at `>= 1.0` |
   |---|---|---|
   | breaking | minor | major |
   | feature | minor | minor |
   | fix | patch | patch |

6. Rewrite `CHANGELOG.md`: rename `## [Unreleased]` to `## [X.Y.Z] - YYYY-MM-DD`, leaving the
   entries beneath it exactly as they are. **Nothing is left in its place** — see §6.
7. Rewrite `version` in `package.json` with a targeted replacement of that one line, not a
   parse-and-reserialise. Reserialising would reformat files the tool has no business reformatting,
   and a churned `package.json` is a diff no reviewer can read.
8. Re-run the existing `validate` logic over the touched packages. `prepare` must not leave a tree
   that `validate` would reject.

`--bump <package>=<major|minor|patch>` overrides step 4 and 5 for one package, taking a package id
or a folder name, repeatable, and erroring if it matches nothing — identical in shape to `--only`,
which it composes with.

### Output

A table on stdout: package id, `0.1.0 → 0.2.0`, and the reason (`minor: Added, Changed`). `--json`
emits the same as structured data. `--dry-run` prints and writes nothing. Exit `0` with "nothing to
prepare" when no package has entries — an empty release is not an error.

### Guards

The inverse of `tag`'s. `prepare` **refuses to run on `master`** (`--allow-branch`) and **refuses to
run on a dirty tree** (`--allow-dirty`). A dirty tree matters more here than anywhere else in the
tooling: the whole value of the command is that you read its diff before you trust it, and a diff
mixed with unrelated edits is one you skim instead.

### Non-goal: cross-package dependency versions

`prepare` does not touch `com.arman.*` entries in any package's `dependencies`. A dependent pinning
`com.arman.package-basics: "0.1.0"` stays valid after PackageBasics moves to `0.2.0`, because
`validate` accepts a dependency at a version that is *either current or already tagged*, and `0.1.0`
is tagged. Auto-bumping dependents would churn every package on every release and turn one package's
patch into a repo-wide version wave, for no gain a consumer can observe.

Raising a dependency floor is a deliberate act — you do it when your package actually needs the new
API — and it belongs in the pull request that needs it, under that package's `## [Unreleased]`.

## 6. No empty `## [Unreleased]` heading

### The policy

An `## [Unreleased]` heading exists **only while it has entries under it**. It is created by the
contributor who has something to write under it, and it is renamed away by `prepare` at release
time. It is never seeded, never left behind empty, and never carried as a placeholder.

This reverses the 2026-08-30 decision to seed all 18 CHANGELOGs with an empty heading.

### The rule: `empty-unreleased`

A third rule in `Tools/changelog-check.mjs`, reported by the existing `check` job in
`changelog.yml`:

> A `## [Unreleased]` heading with no entries under it is a failure. Delete the heading, or put
> something under it.

"No entries" reuses `entriesOf` — the bare-heading rule from step 2 of §5, in the one place it is
already implemented.

**Scope: repo-wide, not diff-scoped.** The two existing rules examine only the packages a pull
request touched, because both are claims *about the change*. This one is a claim about the state of
the repository, so it walks every publishable package's CHANGELOG at the head commit. Diff-scoping
it would let an empty heading sit forever in a package nobody edits. Repo-wide cannot produce false
blame here because §7 cleans all thirteen before the rule ships — after that, an empty heading can
only arrive in a pull request that put it there.

It also catches the failure mode that costs the most: a release pull request that renamed some
sections and left an orphan heading behind.

**No waiver label.** The two existing waivers exist because "this change needs no entry" and "I may
rewrite what `0.1.0` says it shipped" are genuine judgement calls a reviewer should weigh. "You left
an empty heading" has exactly one correct resolution, and it is deleting two lines.

Private packages are skipped, consistent with the other two rules.

### Knock-on: the `missing-section` message

`changelog-check.mjs` already reports `missing-section` when a package's shipped code changed and
its CHANGELOG has no `## [Unreleased]` heading at all. That stays — it is now the *common* path
rather than an oddity, since the heading no longer pre-exists. Its message needs rewording: today it
reads "Add one above the newest version and record the change under it", which is phrased for a
world where the heading was seeded and you forgot to fill it in.

### Tests

Into the existing `Tools/changelog-check.test.mjs` (31 tests today, building throwaway git repos and
running the real script inside them): empty heading; heading with only a bare `###`; heading with
entries; heading absent entirely; a private package with an empty heading; and a pull request that
deletes an empty heading.

`prepare`'s bump derivation and changelog rewrite get a new `Tools/upm-release.test.mjs` — the first
tests that file has had. Both suites run in CI via `node --test`; the new one needs a job in
`release.yml`, named `release-script-tests` for the same collision reason as §4.

## 7. Cleanup: thirteen headings

Thirteen packages carry an empty `## [Unreleased]` heading and lose it in this change: the twelve
publishable packages with no unreleased work, plus `PackageTemplate`.

Removing it from the template is not incidental. A newly scaffolded package's CHANGELOG documents an
initial release; shipping it a placeholder section guarantees every new package starts by violating
§6.

The five packages that *do* have entries — InGameMessageLogging, PackageBasics,
PersistentDataManagement, UnityUtilities, UpdateManagement — keep their headings, which §8 then
renames.

This cleanup is Markdown-only. It triggers no `missing-entry` (every `*.md` is exempt from that
rule) and touches no tagged version section, so `frozen-section` stays quiet.

## 8. The first release under the new flow

Once §4–§7 have landed on `dev`, `prepare` bumps five packages from `0.1.0` to `0.2.0`:

| Package | Bump | From the headings |
|---|---|---|
| `com.arman.in-game-message-logging` | minor | Changed |
| `com.arman.package-basics` | minor | Changed |
| `com.arman.persistent-data-management` | minor | Added, Changed |
| `com.arman.unity-utilities` | minor | Changed |
| `com.arman.update-management` | minor | Changed |

The other twelve stay at `0.1.0` and tag nothing — `tag` is idempotent, so a push to `master` that
changes no version tags nothing.

Three of those five describe namespace and folder flattening that is **breaking** for consumers.
Under §5's `0.x` rule that still lands on a minor bump, which is the correct signal for `0.x`: minor
is where breakage lives before `1.0`.

**Ordering.** The flattening refactor those entries describe must be committed and merged into `dev`
first. A release PR that promotes changelog entries describing code that is not on `master` would
publish a tarball that disagrees with its own changelog.

**The merge is the publish.** Merging the release PR pushes to `master`, runs `tag`, creates five
`<name>/0.2.0` tags, and OpenUPM builds them within 15–30 minutes. The name and version are then
permanent. There is no dry run in front of that step and no undo behind it.

## 9. What this does not do

* **Does not auto-merge or auto-release.** No schedule, no `workflow_dispatch` that publishes. The
  human act stays the release PR merge, exactly as today.
* **Does not bump dependency ranges** (§5).
* **Does not resolve the `0.x` question.** Whether these packages should be `1.0` is §5 of the
  GitHub spec and stays open; this spec only makes sure the tooling does not answer it accidentally.
* **Does not restrict who may push to `dev`.** `dev` is the development branch; it is protected by
  the existing PR checks and nothing more.
* **Does not add a pre-release channel.** No package carries a pre-release suffix and none gains
  one here.
