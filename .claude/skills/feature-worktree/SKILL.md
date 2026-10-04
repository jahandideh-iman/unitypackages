---
name: feature-worktree
description: Use when starting or finishing any feature, fix, docs, chore or CI work in this repo — cutting a branch in a worktree from origin/dev, committing, pushing, and opening the pull request into dev.
---

# Feature worktree

Work happens on a short-lived branch in its own worktree, cut from `origin/dev`, and ends at an open pull request into `dev`. The branch model, rulesets and required checks are in [`git-workflow.md`](../../../.agents/rules/git-workflow.md).

## Start

Never `git checkout -b` in the main checkout: other sessions share it. Cut a worktree from the remote tip, with `--no-track` so the new branch does not take `dev` as its upstream:

```powershell
git -C <repo> fetch origin dev
git -C <repo> worktree add --no-track -b <prefix>/<name> .worktrees/<name> origin/dev
```

`<prefix>` is one of `feat/`, `fix/`, `docs/`, `chore/` or `ci/`. `.worktrees/` and `.claude/worktrees/` are git-ignored. A fresh worktree has no `Library/` and no generated `*.csproj` or `*.slnx`, so the first Editor open imports from scratch and `sharplens` has no solution to load until the Editor generates one.

Run git as `git -C <path> …`; never prefix it with `cd`.

## While working

- Commit messages follow Conventional Commits: `feat(ui-management): …`, `fix: …`, `docs: …`, `chore: …`, `ci: …`.
- Never run a bare `git stash`: every worktree shares the stash stack. Use `git stash push -u -m "<unique-tag>"`, restore that entry by its SHA with `git stash apply <sha>`, then drop it.
- A change to a package's shipped code needs a `## [Unreleased]` entry — the [`releasing-packages`](../releasing-packages/SKILL.md) skill.

## Finish

1. `npm run format:check`, `node --test Tools/*.test.mjs` and `npm run check:docs` pass, and so do the Unity suites when the change touches C#.
2. Push the branch: `git -C <worktree> push -u origin <prefix>/<name>`.
3. Open the pull request with `gh pr create`, which targets `dev`. Only a release pull request passes `--base master`, and only through the [`releasing-packages`](../releasing-packages/SKILL.md) procedure.
4. **Stop.** Report the pull request URL. An agent never merges, never pushes to `dev` or `master`, and never force-pushes; the merge is a human's.
