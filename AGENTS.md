# UnityPackages — Agent Guide

This is the entry point for developers and AI agents working on the **UnityPackages** repo: the rules that are expensive to break, and a routing table to the file that holds each area in full. OpenCode and other `AGENTS.md`-aware tools load it directly; `CLAUDE.md` imports it for Claude Code, and `QWEN.md` points to it.

**One feature, one session.** Every turn re-reads the whole conversation so far, so a turn late in a long session costs several times the same turn early in a short one. End the session when the feature lands instead of rolling into the next task.

**Push wide exploration into subagents.** Searching and reading files to locate something belongs in a search subagent where your tool has one (`Explore` or `general-purpose` in Claude Code): the subagent's tool output stays in its own context and only its report comes back.

**Compact deliberately.** Compact at a natural seam — a feature done, a review clean — rather than letting a session drift up to the auto-compaction ceiling. In Claude Code that is `/compact`, and `/context` itemises what is currently loaded.

## What this repo is

A Unity project that hosts **embedded UPM packages**: each folder under `Packages/` is a standalone, publishable package that compiles and tests in place. `Assets/` is only a scratch sandbox. Consumers install the packages from OpenUPM, never by copying folders — see [`.agents/rules/releases.md`](./.agents/rules/releases.md).

### Agent tool layout

Each agent tool reads its own folders, so some files exist twice. When you change one side, change its pair in the same commit:

| What        | Claude Code                             | OpenCode                                     |
| ----------- | --------------------------------------- | -------------------------------------------- |
| Rules       | `CLAUDE.md` (imports `AGENTS.md`)       | `AGENTS.md`                                  |
| Skills      | `.claude/skills/` (copy)                | `.agents/skills/` (source)                   |
| MCP servers | per user, `claude mcp add`              | per user, `~/.config/opencode/opencode.json` |
| Permissions | per user, `.claude/settings.local.json` | per user, your own `opencode.json`           |

- Skill folders use lowercase names, and a skill's `name:` must be lowercase-hyphenated and equal to its folder name. OpenCode rejects other names, and on Linux and macOS it cannot find a capitalised folder.
- OpenCode also reads `.claude/skills/`, so it finds each skill twice and logs a "duplicate skill name" warning. The copies are identical, so this is harmless. Set `OPENCODE_DISABLE_CLAUDE_CODE_SKILLS=1` to silence it.
- OpenCode names MCP tools `<server>_<tool>` (`sharplens_find_references`) where Claude Code uses `mcp__<server>__<tool>`.

## Non-negotiables

| Rule                                                                                               | Read before acting                                                               |
| -------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| Never hand-edit `.unity`, `.prefab` or `.asset` YAML                                               | [`unity-asset-editing`](./.agents/skills/unity-asset-editing/SKILL.md)           |
| Never delete, ignore or hand-create a `.meta`; commit an asset with its `.meta`                    | [`unity-asset-editing`](./.agents/skills/unity-asset-editing/SKILL.md)           |
| Start work in a worktree cut from `origin/dev`, never `git checkout -b`                            | [`feature-worktree`](./.agents/skills/feature-worktree/SKILL.md)                 |
| An agent opens the pull request and stops; it never merges                                         | [`feature-worktree`](./.agents/skills/feature-worktree/SKILL.md)                 |
| This repo is on GitHub — use `gh`, not `glab`; the `gitlab` remote is a read-only archive          | [`.agents/rules/git-workflow.md`](./.agents/rules/git-workflow.md)               |
| Never prefix a git command with `cd`; use `git -C <path>`                                          | [`.agents/rules/git-workflow.md`](./.agents/rules/git-workflow.md)               |
| Merging into `master` and `tag --push` publish; a published id and version are permanent           | [`releasing-packages`](./.agents/skills/releasing-packages/SKILL.md)             |
| Keep the `tag` job's `github.ref == 'refs/heads/master'` condition                                 | [`.agents/rules/releases.md`](./.agents/rules/releases.md)                       |
| `unity-tests` never runs on a fork PR: no `pull_request_target`, third-party actions pinned by SHA | [`.agents/rules/ci.md`](./.agents/rules/ci.md)                                   |
| Never rename an asmdef or tidy a known inconsistency                                               | [`.agents/rules/packages.md`](./.agents/rules/packages.md)                       |
| Keep `UnityEngine` out of `PackageBasics` and `ServiceLocating`                                    | [`.agents/rules/code-style.md`](./.agents/rules/code-style.md)                   |
| Never turn `overrideReferences` off to fix a duplicate-assembly error                              | [`.agents/rules/testing.md`](./.agents/rules/testing.md)                         |
| Prefer `sharplens` to `Grep`/`Glob` for C# navigation                                              | [`.agents/rules/code-navigation.md`](./.agents/rules/code-navigation.md)         |
| Never delete Unity-facing code on a SharpLens dead-code result alone                               | [`.agents/rules/code-navigation.md`](./.agents/rules/code-navigation.md)         |
| Documents describe the present, never the change                                                   | [`.agents/rules/documentation-voice.md`](./.agents/rules/documentation-voice.md) |

## Where the rules live

| Touching                                                                 | Read first                                                                       |
| ------------------------------------------------------------------------ | -------------------------------------------------------------------------------- |
| Repo layout, package anatomy, asmdef names, the package catalogue        | [`.agents/rules/packages.md`](./.agents/rules/packages.md)                       |
| Unity tests, test doubles, Moq in a test asmdef                          | [`.agents/rules/testing.md`](./.agents/rules/testing.md)                         |
| `.github/workflows/`, `Tools/ci/`, the self-hosted runner                | [`.agents/rules/ci.md`](./.agents/rules/ci.md)                                   |
| C# navigation, refactoring, `sharplens`/`lifeblood`/`unity command`      | [`.agents/rules/code-navigation.md`](./.agents/rules/code-navigation.md)         |
| `Tools/upm-release.mjs`, `Tools/release.bat`, changelogs, OpenUPM        | [`.agents/rules/releases.md`](./.agents/rules/releases.md)                       |
| Branches, rulesets, required checks, `gh`                                | [`.agents/rules/git-workflow.md`](./.agents/rules/git-workflow.md)               |
| C# naming and style, CSharpier, Prettier, `.editorconfig`                | [`.agents/rules/code-style.md`](./.agents/rules/code-style.md)                   |
| Any document, README, skill or code comment                              | [`.agents/rules/documentation-voice.md`](./.agents/rules/documentation-voice.md) |
| Preparing or running a release                                           | [`releasing-packages`](./.agents/skills/releasing-packages/SKILL.md)             |
| A new package under `Packages/`                                          | [`adding-a-package`](./.agents/skills/adding-a-package/SKILL.md)                 |
| A scene, prefab, `.asset`, `.meta`, or adding/moving a file in a package | [`unity-asset-editing`](./.agents/skills/unity-asset-editing/SKILL.md)           |
| Starting or finishing a branch                                           | [`feature-worktree`](./.agents/skills/feature-worktree/SKILL.md)                 |
| Any feature's design or plan                                             | [`docs/INDEX.md`](./docs/INDEX.md)                                               |

## Commands

- **Unity tests, no Editor open (CI, a fresh checkout)** — `unity test` spawns its own batch instance, and fails with "another Unity instance is running with this project open" when an Editor already has the project open. It takes the Editor version from `ProjectSettings/ProjectVersion.txt`.

  ```powershell
  unity test --mode EditMode --output Library/editmode-results.xml
  unity test --mode PlayMode --output Library/playmode-results.xml
  ```

  Exit codes: `0` success, `8` tests ran and failed, `6` the run never produced results (compiler errors, a missing `--execute-method` target, a dead Editor). Treat `8` as a red suite and `6` as "couldn't run".

- **Unity tests, Editor already open** — runs in the live instance and returns per-test results as JSON: `unity command run_tests --mode EditMode`, `unity command run_tests --mode PlayMode`.
- **Tooling tests** — `node --test Tools/*.test.mjs`, and `powershell -NoProfile -File Tools/ci/Tests/Test-CiScripts.ps1` for the CI helpers.
- **Formatting** — `npm run format` rewrites every in-scope file; `npm run format:check` is the required `format` check.
- **Documentation** — `npm run check:docs` verifies that every cross-file link and heading anchor resolves, that `.claude/skills/` matches `.agents/skills/`, and that this file stays inside its byte budget. Run it after editing anything under `.agents/` or `docs/`. It reads tracked and staged files only, so `git add` a new file before running it.

## Documentation

- [`docs/INDEX.md`](./docs/INDEX.md) lists every feature's design spec (`docs/specs/`) and implementation plan (`docs/plans/`) with its status. Read the relevant document before changing a feature it covers; new work adds its row in the same commit as its spec.
- [`.agents/rules/`](./.agents/rules) holds the detailed rules, one file per area, reachable through the routing table above.
- Every document describes the project as it is now, never the change that produced it; the dated specs and plans are the exception, because they record a decision as it was made. [`.agents/rules/documentation-voice.md`](./.agents/rules/documentation-voice.md) carries the rule in full, and it governs code comments too.
