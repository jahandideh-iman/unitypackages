---
name: adding-a-package
description: Use when creating a new UPM package under Packages/ in this repo — copying PackageTemplate, choosing the package id and asmdef names, and making the package publishable.
---

# Adding a package

Every package starts as a copy of `Packages/PackageTemplate/`. A package id is permanent once it has been tagged, so settle the name before the first release. The layout, the naming rules and the catalogue are in [`packages.md`](../../../.agents/rules/packages.md).

## Adding a new package

1. Copy `Packages/PackageTemplate/` to `Packages/<NewName>/`.
2. In its `package.json`: set `name` (kebab-case), `displayName`, a **real** `description`, `version`, and **remove `"private": true`** — the template carries it so the scaffold can never publish, and a copy inherits it.
3. Add `"license": "MIT"` plus a `LICENSE.md` and its `.meta`.
4. Rename the asmdefs to `Arman.<NewName>` (runtime, no suffix), `Arman.<NewName>.Editor` and `Arman.<NewName>.Tests.Editor`, and update their `name` fields.
5. Declare any `com.arman.*` dependencies with exact versions.
6. Write a `README.md` and a `CHANGELOG.md` with **no `## [Unreleased]` heading** — add one when you have an entry to put under it. See [the changelog rules](../../../.agents/rules/releases.md#changelogs--four-rules-enforced-in-ci).
7. Verify with `npm pack --dry-run` from the package folder.

## Names

- The package id is `com.arman.<kebab-case-name>`, in one flat namespace — [`packages.md` § Naming](../../../.agents/rules/packages.md#naming).
- The asmdef names are the three in step 4; the existing packages are not consistent, and are left that way — [`packages.md` § Assembly definitions](../../../.agents/rules/packages.md#assembly-definitions).
- Commit every new file and folder with its `.meta` — the [`unity-asset-editing`](../unity-asset-editing/SKILL.md) skill.

## Before the pull request

- The package has its row in the [package catalogue](../../../.agents/rules/packages.md#package-catalogue).
- `node Tools/upm-release.mjs validate` and `npm run format:check` pass.
