# Working process

How a piece of work runs from idea to pull request: the superpowers workflows, where their documents go, and the project subagents they dispatch.

## Superpowers workflows

**Use the superpowers skill workflow wherever one applies**, rather than working ad hoc:

| Situation                            | Skill                                                                    |
| ------------------------------------ | ------------------------------------------------------------------------ |
| Creative work: a feature, a new API  | `brainstorming`                                                          |
| A feature or a bug fix               | `test-driven-development`                                                |
| A bug, a failing test, odd behaviour | `systematic-debugging`                                                   |
| Multi-step work                      | `writing-plans`, then `subagent-driven-development` or `executing-plans` |
| A branch ready for review            | `requesting-code-review`                                                 |
| About to say something is done       | `verification-before-completion`                                         |

Check for an applicable skill before the first action of a task, not after, and invoke it with the skill tool instead of recalling what it says — the workflows change between versions.

**New feature work goes spec → plan → implementation.** Write the design spec and the implementation plan before touching code. Documentation, tooling and CI chores that change no package behaviour skip the spec.

## Where superpowers meets this repo's rules

Where a superpowers default and a rule of this repo disagree, the repo rule wins:

- **Documents.** A spec goes to `docs/specs/YYYY-MM-DD-<topic>-design.md` and a plan to `docs/plans/YYYY-MM-DD-<topic>.md`, not under `docs/superpowers/`. Each carries a `**Status:**` line and a row in [`docs/INDEX.md`](../../docs/INDEX.md), per [`documentation-voice.md`](./documentation-voice.md#keeping-the-routing-honest).
- **Worktrees.** The worktree is cut by the [`feature-worktree`](../skills/feature-worktree/SKILL.md) skill: from `origin/dev`, under `.worktrees/`, on a `feat/`, `fix/`, `docs/`, `chore/` or `ci/` branch.
- **Finishing.** `finishing-a-development-branch` offers to merge locally or keep the branch. Neither applies here: a branch always ends at a pushed branch and an open pull request into `dev`, and the agent stops there.
- **Tests.** "The suite is green" means the commands in [`testing.md`](./testing.md#test-commands) and `node --test Tools/*.test.mjs`; a Unity exit code `6` is "couldn't run", never a pass.

## Project subagents

Three subagents carry this repo's conventions into the superpowers dispatches, so a fresh agent starts from the rules rather than from a generic prompt. When a superpowers skill dispatches one of these roles, pass the project agent as the subagent type instead of `general-purpose`, and keep the skill's prompt template as the prompt:

| Role                                   | Subagent        | Dispatched by                                                       |
| -------------------------------------- | --------------- | ------------------------------------------------------------------- |
| Implements one task of a plan          | `implementer`   | `subagent-driven-development`, `executing-plans`, review fix rounds |
| Reviews one task's diff                | `task-reviewer` | `subagent-driven-development`                                       |
| Reviews the whole branch before the PR | `code-reviewer` | `subagent-driven-development`, `requesting-code-review`             |

Each exists twice, as `.claude/agents/<name>.md` (Claude Code, keyed by `name:`) and `.opencode/agents/<name>.md` (OpenCode, `mode: subagent`). The two copies share their description and body; change both in the same commit. `npm run check:docs` fails a pair that has drifted.
