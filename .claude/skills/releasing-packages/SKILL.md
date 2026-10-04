---
name: releasing-packages
description: Use when preparing, running or recovering a package release in this repo — filling a CHANGELOG's Unreleased section, running upm-release.mjs prepare on a release branch or tag on master, opening the release pull requests (branch → dev, then dev → master), or fixing a failed changelog check.
---

# Releasing packages

A release publishes to OpenUPM by **creating a git tag** `<package-name>/<version>`; nothing is uploaded. The background, every flag, and why each guard exists are in [`releases.md`](../../../.agents/rules/releases.md). This skill is the procedure.

## The irreversible points

Two actions publish, and a published package name and version are **permanent** — OpenUPM builds a tag within 15–30 minutes and there is no unpublish:

1. **Merging the release pull request into `master`.** The `tag` job in `release.yml` runs on that push and tags every package whose version is not tagged yet. There is no confirmation step.
2. **`node Tools/upm-release.mjs tag --push`.** Without `--push` the tags stay local, and `git tag -d` removes them.

An agent does neither, and never pushes to `dev`. It prepares the release on a branch, opens each pull request, and stops; a human merges.

## Procedure

1. **Record the changes.** On a feature branch, each package whose `Runtime/`, `Editor/` or `package.json` changed gets a bullet under `## [Unreleased]` in its `CHANGELOG.md`, filed under `### Added`, `### Changed`, `### Deprecated`, `### Removed`, `### Fixed` or `### Security`. Create the heading if it is absent. The branch merges into `dev` as usual.
2. **Prepare on a branch off `dev`.** In a worktree cut from an up-to-date `origin/dev` (the [`feature-worktree`](../feature-worktree/SKILL.md) skill), on a branch such as `chore/prepare-release`:

   ```powershell
   node Tools/upm-release.mjs validate
   node Tools/upm-release.mjs prepare
   ```

   `prepare` turns each `## [Unreleased]` into `## [X.Y.Z] - YYYY-MM-DD`, bumps `package.json`, and gives dependents a cascaded patch bump; with no populated `## [Unreleased]` anywhere it changes nothing. Commit the result as `chore(release): promote <n> packages` (or `chore(release): <package>@<version>` for one package), push the branch, open its pull request into `dev`, report the URL and stop. A human merges it.

3. **Open the release pull request.** Once the prepare pull request is merged, open `dev` → `master`:

   ```powershell
   gh pr create --base master --head dev --title "Release: <n> packages" --body "<the packages and versions prepare bumped>"
   ```

   `promotion-check` accepts a pull request into `master` only from `dev`.

4. **Check the pull request.** Every required check passes, `unity-tests` included. Read the version headings and bumps `prepare` produced; while a package's major is `0`, a breaking change lands on the minor.
5. **Hand over.** Report the pull request URL. A human merges it with a true merge, never a squash, and that merge is the publish.

`Tools/release.bat` (`node Tools/release-flow.mjs`) runs the same steps for a maintainer: each run opens the next pull request — the preparation pull request into `dev`, then, once that is merged, the release pull request into `master`. It commits and pushes as the person running it, so an agent does not run it.

To preview or release one package by hand: `node Tools/upm-release.mjs prepare --dry-run`, `prepare --only "<folder or id>"`, `prepare --bump <package>=<major|minor|patch>`, and `tag --dry-run` on `master`.

## When the changelog check fails

`.github/workflows/changelog.yml` runs `Tools/changelog-check.mjs` on every pull request. Rehearse it with `node Tools/changelog-check.mjs --base dev --head HEAD`; add `--base-branch master` to rehearse a release pull request.

| Rule                    | Fires when                                                             | Remedy                                                                                           | Waiver label        |
| ----------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ | ------------------- |
| `missing-entry`         | A package's shipped code changed with no `## [Unreleased]` entry       | Add the entry; the label is for a change no consumer can observe                                 | `no-changelog`      |
| `frozen-section`        | A version section whose tag exists was edited or deleted               | Revert the edit, and record the correction under `## [Unreleased]`                               | `changelog-rewrite` |
| `empty-unreleased`      | A `## [Unreleased]` heading has no bullet under it                     | Delete the heading or fill it in                                                                 | _none_              |
| `unpromoted-unreleased` | A pull request into `master` still carries a `## [Unreleased]` heading | Run `node Tools/upm-release.mjs prepare` on a branch off `dev` and merge it into `dev` through a pull request (steps 2 and 3) | _none_              |

The full rules, and which files count as shipped code, are in [`releases.md` § Changelogs](../../../.agents/rules/releases.md#changelogs--four-rules-enforced-in-ci).
