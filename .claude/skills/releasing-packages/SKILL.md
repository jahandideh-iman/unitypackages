---
name: releasing-packages
description: Use when preparing, running or recovering a package release in this repo — filling a CHANGELOG's Unreleased section, running Tools/release.bat or upm-release.mjs prepare/tag, opening the dev → master release pull request, or fixing a failed changelog check.
---

# Releasing packages

A release publishes to OpenUPM by **creating a git tag** `<package-name>/<version>`; nothing is uploaded. The background, every flag, and why each guard exists are in [`releases.md`](../../../.agents/rules/releases.md). This skill is the procedure.

## The irreversible points

Two actions publish, and a published package name and version are **permanent** — OpenUPM builds a tag within 15–30 minutes and there is no unpublish:

1. **Merging the release pull request into `master`.** The `tag` job in `release.yml` runs on that push and tags every package whose version is not tagged yet. There is no confirmation step.
2. **`node Tools/upm-release.mjs tag --push`.** Without `--push` the tags stay local, and `git tag -d` removes them.

An agent does neither. It prepares the release, opens the pull request, and stops; a human merges.

## Procedure

1. **Record the changes.** On a feature branch, each package whose `Runtime/`, `Editor/` or `package.json` changed gets a bullet under `## [Unreleased]` in its `CHANGELOG.md`, filed under `### Added`, `### Changed`, `### Deprecated`, `### Removed`, `### Fixed` or `### Security`. Create the heading if it is absent. The branch merges into `dev` as usual.
2. **Run the flow from a clean, up-to-date `dev`:**

   ```powershell
   Tools/release.bat            # or: node Tools/release-flow.mjs
   ```

   It takes no arguments. It checks `git` and `gh`, runs `validate`, runs `prepare` (each `## [Unreleased]` becomes `## [X.Y.Z] - YYYY-MM-DD`, `package.json` is bumped, and dependents get a cascaded patch bump), commits, pushes `dev`, opens the pull request `dev` → `master`, **prints its URL and stops**. With no populated `## [Unreleased]` anywhere it changes nothing and exits 0.

3. **Check the pull request.** Every required check passes, `unity-tests` included. Read the version headings and bumps `prepare` produced; while a package's major is `0`, a breaking change lands on the minor.
4. **Hand over.** Report the pull request URL. A human merges it with a true merge, never a squash, and that merge is the publish.

To preview or release one package by hand: `node Tools/upm-release.mjs prepare --dry-run`, `prepare --only "<folder or id>"`, `prepare --bump <package>=<major|minor|patch>`, and `tag --dry-run` on `master`.

## When the changelog check fails

`.github/workflows/changelog.yml` runs `Tools/changelog-check.mjs` on every pull request. Rehearse it with `node Tools/changelog-check.mjs --base dev --head HEAD`; add `--base-branch master` to rehearse a release pull request.

| Rule                    | Fires when                                                             | Remedy                                                                                           | Waiver label        |
| ----------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ | ------------------- |
| `missing-entry`         | A package's shipped code changed with no `## [Unreleased]` entry       | Add the entry; the label is for a change no consumer can observe                                 | `no-changelog`      |
| `frozen-section`        | A version section whose tag exists was edited or deleted               | Revert the edit, and record the correction under `## [Unreleased]`                               | `changelog-rewrite` |
| `empty-unreleased`      | A `## [Unreleased]` heading has no bullet under it                     | Delete the heading or fill it in                                                                 | _none_              |
| `unpromoted-unreleased` | A pull request into `master` still carries a `## [Unreleased]` heading | Run `node Tools/upm-release.mjs prepare` on `dev`, commit, push — or re-run `Tools/release.bat` | _none_              |

The full rules, and which files count as shipped code, are in [`releases.md` § Changelogs](../../../.agents/rules/releases.md#changelogs--four-rules-enforced-in-ci).
