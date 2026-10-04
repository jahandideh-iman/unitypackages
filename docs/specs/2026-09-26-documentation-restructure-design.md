# Agent documentation: a lean `AGENTS.md`, rules by area, and a docs index

**Date:** 2026-09-26
**Status:** Implemented
**Branch:** `docs/agent-docs-restructure`, cut from `origin/dev`, landing as one pull request into
`dev`.

## 1. The problem

Two agents work on this repo: Claude Code, and OpenCode driving Qwen 3.8 27B. Both load
`AGENTS.md` in full at the start of every session — Claude Code through the `@AGENTS.md` import in
`CLAUDE.md`, OpenCode directly — and every later turn re-reads it as part of the conversation.

`AGENTS.md` is 57,794 bytes and 537 lines. Most of it is reference an agent needs only when it
touches one area: the release tooling and changelog rules (about 110 lines), the MCP tools and the
Unity CLI (about 90), testing and Moq (about 50), branching and rulesets (about 45), formatting
(about 40), CI (about 35). A session that edits one package README pays for all of it, and a
27B-parameter model with a small context window loses the few rules that are expensive to break
among the ones that are not.

The design history has the opposite problem: nothing routes to it. `docs/specs/` holds six design
documents and `docs/plans/` four plans, with no index. Each spec's `**Status:**` line is free text
("Accepted — current direction. Rollout in progress"), none records whether the work shipped, and
the plans carry no status at all. Four live Markdown links in the specs point at
`../../.agents/AGENTS.md`, a path that does not exist.

## 2. Goals

- `AGENTS.md` holds only what every session needs: the non-negotiable rules, a routing table to
  the file that holds each area, and the everyday commands. It stays between 6,000 and 12,000
  bytes.
- Every rule and fact in the current `AGENTS.md` survives, in exactly one home.
- An agent that is about to do something irreversible or consumer-breaking — release, add a
  package, touch an asset or `.meta`, start a branch — has a skill that loads on that task.
- `docs/INDEX.md` routes from a feature, or from a file, to the spec and plan that explain it, and
  each document's Status line says whether it shipped.
- A check in CI keeps links, anchors, the skill mirror and the size budget honest.

## 3. Non-goals

- Rewriting the bodies of the dated specs and plans. They record decisions as they were made; only
  their Status lines and broken Markdown links change.
- Moving `docs/specs/` and `docs/plans/`. The paths stay; the index is added beside them.
- Changing the four vendored skills (`unity-cli`, `unity-package-management`, `lifeblood-mcp`,
  `sharplens-mcp`).
- Adding rules. Every rule below already exists in this repo, with one exception, stated in §5.4.
- Committing an `opencode.json`, a `.mcp.json` or a `.claude/settings.json`. Both MCP servers and
  all permissions stay registered per user.

## 4. The always-loaded core: `AGENTS.md`

`AGENTS.md` keeps these sections, in this order.

**Header.** One paragraph on what the file is and which tool loads it how, then three short
paragraphs on keeping sessions cheap: one feature per session, wide exploration in a search
subagent, compaction at a natural seam. They carry no measured figures.

**What this repo is.** Three lines: a Unity project that hosts embedded UPM packages under
`Packages/`, `Assets/` a scratch sandbox, consumers installing from OpenUPM.

**Agent tool layout.** Which file each tool reads — rules (`CLAUDE.md` importing `AGENTS.md`
versus `AGENTS.md`), skills (`.claude/skills/` copy versus `.agents/skills/` source), MCP servers
and permissions (per user in both) — with the skill-folder naming rules and the duplicate-skill
warning OpenCode logs.

**Non-negotiables.** A two-column table, rule and "read before acting":

| Rule                                                                                               | Read before acting                     |
| -------------------------------------------------------------------------------------------------- | -------------------------------------- |
| Never hand-edit `.unity`, `.prefab` or `.asset` YAML                                               | `unity-asset-editing` skill            |
| Never delete, ignore or hand-create a `.meta`; commit an asset with its `.meta`                    | `unity-asset-editing` skill            |
| Start work in a worktree cut from `origin/dev`, never `git checkout -b`                            | `feature-worktree` skill               |
| An agent opens the pull request and stops; it never merges                                         | `feature-worktree` skill               |
| This repo is on GitHub — use `gh`, not `glab`; the `gitlab` remote is a read-only archive          | `.agents/rules/git-workflow.md`        |
| Never prefix a git command with `cd`; use `git -C <path>`                                          | `.agents/rules/git-workflow.md`        |
| Merging into `master` and `tag --push` publish; a published id and version are permanent           | `releasing-packages` skill             |
| Keep the `tag` job's `github.ref == 'refs/heads/master'` condition                                 | `.agents/rules/releases.md`            |
| `unity-tests` never runs on a fork PR: no `pull_request_target`, third-party actions pinned by SHA | `.agents/rules/ci.md`                  |
| Never rename an asmdef or tidy a known inconsistency                                               | `.agents/rules/packages.md`            |
| Keep `UnityEngine` out of `PackageBasics` and `ServiceLocating`                                    | `.agents/rules/code-style.md`          |
| Never turn `overrideReferences` off to fix a duplicate-assembly error                              | `.agents/rules/testing.md`             |
| Prefer `sharplens` to `Grep`/`Glob` for C# navigation                                              | `.agents/rules/code-navigation.md`     |
| Never delete Unity-facing code on a SharpLens dead-code result alone                               | `.agents/rules/code-navigation.md`     |
| Documents describe the present, never the change                                                   | `.agents/rules/documentation-voice.md` |

In the file itself every entry in the second column is a relative link.

**Where the rules live.** A routing table, "touching" to "read first", with one row per rules
file and skill, plus `docs/INDEX.md` for any feature's design or plan.

**Commands.** The two ways to run Unity tests (`unity test` with no Editor open, `unity command
run_tests --mode …` with one open) and the exit codes `0`/`8`/`6`; `npm run format` and
`npm run format:check`; `npm run check:docs`; the tooling tests.

**Documentation.** `docs/INDEX.md` is the entry point to every spec and plan; `.agents/rules/`
holds the rules by area; documents describe the present, and the dated specs and plans are the
exception because they record decisions.

## 5. Where the rest goes

### 5.1 `.agents/rules/`

Each current `AGENTS.md` section moves whole, with its prose unchanged except for links that
pointed at a sibling section, which now point at the file that holds it.

| File                     | Takes these `AGENTS.md` sections                                                                                                             |
| ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `documentation-voice.md` | Writing documentation, plus a new closing section on keeping the routing honest (§7)                                                         |
| `packages.md`            | Repo layout, Package anatomy, Assembly definitions, Package catalogue, Naming, Known inconsistencies                                         |
| `testing.md`             | Test Commands in full (flags, CI report format, JSON shape), Test doubles                                                                    |
| `ci.md`                  | CI                                                                                                                                           |
| `code-navigation.md`     | MCP Tool Usage & Unity CLI with its subsections: Skills, `sharplens`, `lifeblood`, What belongs to `lifeblood`, `unity command`, Permissions |
| `releases.md`            | Distribution and releases, Release tooling, The whole flow in one go, Changelogs                                                             |
| `git-workflow.md`        | Git and hosting, Branching, the rulesets and required-check table                                                                            |
| `code-style.md`          | Formatting, C# coding style                                                                                                                  |

The `.meta` section and "Adding a new package" go to skills (§5.2), not to rules files.

`.agents/rules/MANIFEST.tsv` lists every heading carved out of the old `AGENTS.md`, one per line,
tab-separated from the file that now holds it. It is the record the heading check (§7) reads.

### 5.2 Four project skills

Each is `.agents/skills/<name>/SKILL.md`, copied byte for byte to `.claude/skills/<name>/SKILL.md`,
with a `description:` that starts "Use when…" so a tool loads it only for a matching task. Each
gets a row in `.agents/skills/THIRD_PARTY_SKILLS.md` with the source **Authored in-repo**.

| Skill                 | Loads when                                                                                                          | Holds                                                                                                                                                                                                                                                                                         |
| --------------------- | ------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `releasing-packages`  | preparing, running or repairing a release; editing a package `CHANGELOG.md`; a changelog check fails                | The ordered procedure: entries under `## [Unreleased]` → `Tools/release.bat`, which stops at the pull request URL → a human merges the release PR into `master`, which publishes → the `tag` job tags. The irreversible points. One line per changelog rule with its remedy and waiver label. |
| `adding-a-package`    | creating a package under `Packages/`                                                                                | The seven-step checklist from "Adding a new package", with the package-id and asmdef naming it depends on, and the README package-table row a new package needs.                                                                                                                              |
| `unity-asset-editing` | editing, adding, deleting or committing a `.unity`, `.prefab`, `.asset` or `.meta`, or any file in a package folder | The "Unity `.meta` files" section, the rule against hand-editing asset YAML with its Editor-mediated fallback, and `npm pack --dry-run` for new root-level files.                                                                                                                             |
| `feature-worktree`    | starting feature, fix or docs work; finishing a branch                                                              | A worktree cut from `origin/dev`; the branch prefixes in use (`feat/`, `fix/`, `docs/`, `chore/`, `ci/`); conventional-commit messages; named stashes only, because the stash stack is shared across worktrees; finishing with a pushed branch and `gh pr create`, which targets `dev`.       |

A skill holds a procedure; the matching rules file holds the reference; each links to the other,
and neither repeats the other's text.

### 5.3 Pointers into the old `AGENTS.md`

Every reference to an `AGENTS.md` section is retargeted to the file that now holds it:

- Markdown links, which the link and anchor checks see: `QWEN.md` (`AGENTS.md#branching`),
  `.github/rulesets/README.md` (`AGENTS.md#branching`), and four links in the specs to
  `../../.agents/AGENTS.md` — `2026-08-23-upm-package-registry-github-design.md` line 123,
  `2026-09-05-basic-rename-and-moq-design.md` line 41, `2026-09-13-auto-formatting-design.md`
  lines 11 and 144.
- Comments and prose no checker sees: `.editorconfig` line 35, `.gitignore` line 84,
  `.github/workflows/changelog.yml` line 3, `.github/workflows/format.yml` lines 5 and 24,
  `Tools/upm-release.mjs` line 39, and `README.md` line 74, which describes what `AGENTS.md`
  covers.

`QWEN.md` and `CLAUDE.md` describe `AGENTS.md` as the entry point that routes to the rules, not as
the single home of all guidance.

Mentions of `.agents/AGENTS.md` inside code spans in the dated plans and specs stay as written:
they quote the path as it stood when the document was written, and no checker reads code spans.

### 5.4 The one new rule

"An agent opens the pull request and stops; it never merges" is not written in the repo today. It
extends the existing principle that merging a release PR stays a human click to every pull
request.

## 6. `docs/INDEX.md` and the Status line

Every spec and plan carries a `**Status:**` line directly after `**Date:**`, with one of five
values:

| Status                  | Meaning                                                       |
| ----------------------- | ------------------------------------------------------------- |
| `Designed`              | Spec written; no plan yet.                                    |
| `Planned`               | Plan written; the work has not landed on `dev`.               |
| `Implemented`           | The work has landed on `dev`.                                 |
| `Superseded — <reason>` | Replaced by another design, which the reason names and links. |
| `Abandoned — <reason>`  | Dropped, with why.                                            |

The Status line is the truth. A plan's checkboxes are working state and are not ticked after the
fact.

The ten existing documents take these values:

| Document                                                                                   | Status                                                          | Evidence                                              |
| ------------------------------------------------------------------------------------------ | --------------------------------------------------------------- | ----------------------------------------------------- |
| spec `2026-08-22-upm-package-registry-design`                                              | `Superseded — replaced by the GitHub + OpenUPM design` (linked) | the 08-23 spec                                        |
| spec `2026-08-23-upm-package-registry-github-design`                                       | `Implemented`                                                   | release tags; `release.yml`; OpenUPM rollout complete |
| spec and plan `2026-08-30-pr-test-ci`                                                      | `Implemented`                                                   | #14                                                   |
| spec and plan `2026-09-02-release-promotion`                                               | `Implemented`                                                   | #20, #24, #29                                         |
| spec `2026-09-05-basic-rename-and-moq-design`, plan `2026-09-05-drop-basic-prefix-and-moq` | `Implemented`                                                   | #31                                                   |
| spec and plan `2026-09-13-auto-formatting`                                                 | `Implemented`                                                   | #38                                                   |

This spec takes `Designed` now, its plan `Planned`, and both `Implemented` in the commit that
lands the work.

`docs/INDEX.md` opens with the Status vocabulary and the rule that new work adds its row in the
same commit as its spec. Then one row per feature: feature, status, spec link, plan link, and the
main files the feature touches, so an agent can go from a file to the document that explains it.

## 7. The documentation check

`Tools/docs-check.mjs` is dependency-free Node, like the rest of `Tools/`. It exits `0` when
clean and `1` on any finding, prints one line per finding with file and line, and takes `--json`.
It runs five checks over the repo's tracked Markdown:

1. **Links.** Every relative link resolves to an existing file or folder. Fenced and inline code
   is blanked first, so a quoted path is not read as a link. External URLs are not fetched.
2. **Anchors.** Every `#fragment`, in-file or cross-file, matches a heading in its target, slugged
   the way GitHub slugs: lowercase; drop every character that is not a letter, digit, underscore,
   space or hyphen; spaces become hyphens; a linked heading contributes its link text. Headings
   inside fenced code do not count. Skill folders whose `THIRD_PARTY_SKILLS.md` row names an
   outside source are skipped by this check: an anchor there is upstream's to fix, and a local
   repair is lost at the next sync.
3. **Skill mirror.** `.agents/skills/` and `.claude/skills/` hold the same set of files, byte for
   byte.
4. **Carved headings.** Every heading in `.agents/rules/MANIFEST.tsv` exists in the file its row
   names. Moving a section without updating the manifest fails.
5. **Core budget.** `AGENTS.md`, with line endings normalised to LF, is between 6,000 and 12,000
   bytes. Nothing else notices the core growing back.

`Tools/docs-check.test.mjs` runs under `node --test`. Each check has a passing and a failing
fixture, built in a temporary directory the way `changelog-check.test.mjs` builds its own, plus
the slug rules and one case that runs the check against the repo itself.

Wiring:

- `package.json` gains `"check:docs": "node Tools/docs-check.mjs"`.
- `.github/workflows/tests.yml`, job `tooling-tests`, gains two steps, both `if: always()`:
  `Test the docs check` (`node --test Tools/docs-check.test.mjs`) and `Check the docs`
  (`node Tools/docs-check.mjs`). `tooling-tests` is already required on `dev` and `master`, so
  no ruleset changes.
- `documentation-voice.md` closes with "Keeping the routing honest": run `npm run check:docs`
  after editing anything under `.agents/` or `docs/`; update `MANIFEST.tsv` in the same commit as
  a heading move; and prose that names a real file which no longer holds what it promises ("see
  the section below", "per AGENTS.md") is the gap no check closes.

## 8. Verification

- `npm run check:docs` exits 0, and `node --test Tools/docs-check.test.mjs` passes.
- `npm run format:check` exits 0.
- Every section heading of the old `AGENTS.md` appears in `MANIFEST.tsv` or in the new core, and a
  side-by-side read of the old file against its new homes finds no dropped paragraph.
- The tooling tests the repo already has still pass.
- A fresh session in Claude Code and in OpenCode, asked nothing about the rules, has the
  non-negotiables table in context, and a release question makes it open the
  `releasing-packages` skill.
- The pull request's CI is green.

## 9. Risks

- **An area file is not opened when it should be.** The rules that are expensive to break stay in
  the core, where they load every session; the routing table covers everything else, and each
  skill's `description:` names the task that triggers it.
- **Content lost in the carve.** Sections move whole; `MANIFEST.tsv` and the heading check pin
  where each one went; the side-by-side read in §8 covers what the check cannot.
- **Prose pointers the checker cannot see.** §5.3 lists every one found by searching for
  `AGENTS.md`; the pull request body lists anything left for a reviewer to judge.
- **A vendored skill update breaks the mirror.** The mirror check fails until both copies are
  updated together, which is the rule `THIRD_PARTY_SKILLS.md` already states.
