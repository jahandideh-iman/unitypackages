# Engineering rules

The short checklist every change is held to, for people and agents alike. Each rule links to the file that holds its detail. Where this list and that file disagree, the file is right, and this list gets fixed in the same commit.

## Before you start

- **Read the routing.** Read [`AGENTS.md`](../../AGENTS.md), then the rules file its "Where the rules live" table names for the area you are changing.
- **Check for a skill before acting.** When a superpowers workflow or a repo skill covers the task, invoke it before the first action. See [`process.md`](./process.md).
- **Features go spec → plan → code.** Chores, docs and CI changes skip the spec. See [`process.md`](./process.md).
- **Work in a worktree cut from `origin/dev`,** never with `git checkout -b`. See the [`feature-worktree`](../skills/feature-worktree/SKILL.md) skill.

## While you change code

- **Test first.** A feature or a fix starts with a failing test. See [`testing.md`](./testing.md).
- **Unity writes the asset files.** Never hand-edit `.unity`, `.prefab` or `.asset` YAML. Never delete or hand-create a `.meta`, and commit every asset together with its `.meta`. See the [`unity-asset-editing`](../skills/unity-asset-editing/SKILL.md) skill.
- **Shipped names stay put.** Don't rename an asmdef. Don't rename a shipped serialized field without `[FormerlySerializedAs]`. See [`packages.md`](./packages.md) and [`code-style.md`](./code-style.md).
- **Foundation packages stay free of `UnityEngine`.** `PackageBasics` and `ServiceLocating` remain plain C#. See [`code-style.md`](./code-style.md).
- **Fix a duplicate-assembly error in the asmdef.** Never turn `overrideReferences` off to make it go away. See [`testing.md`](./testing.md).
- **A shipped change gets a changelog bullet.** Add it under `## [Unreleased]` in the package's `CHANGELOG.md`. See [`releases.md`](./releases.md).
- **Unity code is not dead on a tool's word.** Don't delete Unity-facing code on a SharpLens unused-code result alone. See [`code-navigation.md`](./code-navigation.md).
- **Drive a live Editor with `unity command eval_file`,** never `eval --code`. Never drive a `menu --path` that opens a confirmation dialog. See [`code-navigation.md`](./code-navigation.md).

## Before you call it done

- **Run the checks the change touches.** Unity tests and `node --test Tools/*.test.mjs` run as [`testing.md`](./testing.md) describes. Formatting is checked with `npm run format:check`. Run `npm run check:docs` after editing anything under `.agents/` or `docs/`.
- **Exit code `6` is not a pass.** It means the run never produced results. Report it as "couldn't run," and report `8` as a red suite. See [`testing.md`](./testing.md).
- **Show the output.** Say what passed and what failed, with the output behind it. If a step was skipped, say so.
- **Write documents in the present tense.** A document describes the project as it is, not the change that produced it. See [`documentation-voice.md`](./documentation-voice.md).

## Git and pull requests

- **Use `git -C <path>`,** never `cd <dir> && git`. See [`git-workflow.md`](./git-workflow.md).
- **Use `gh`, not `glab`.** The `gitlab` remote is a read-only archive.
- **An agent opens its pull request into `dev` and stops.** It never merges. It never pushes to `master` or runs `tag --push`, because those publish, and a published id and version are permanent. See the [`releasing-packages`](../skills/releasing-packages/SKILL.md) skill.

## Keeping this list short

Add a rule here only when breaking it is expensive. Put its detail in the file it links to, so this checklist stays something an agent reads in full.
