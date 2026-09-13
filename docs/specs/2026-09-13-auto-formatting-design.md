# Consistent auto-formatting: CSharpier, Prettier and `.editorconfig`

**Date:** 2026-09-13
**Status:** **Accepted.** Design approved 2026-09-13. One branch off `dev`, `chore/auto-formatting`,
landed as one pull request.

## 1. The problem

The repo has no formatter. There is no `.editorconfig`, no formatter config and no CI check, so
layout is whatever each contributor's editor produced. The only written style rules are the
naming and brace conventions in [C# coding style](../../.agents/AGENTS.md#c-coding-style), and
nothing checks those either.

The goal is a single command that formats the repo identically on every machine, and a required
check that keeps it that way.

## 2. Scope

### 2.1 In scope

* **C#:** every tracked `.cs` file, including `Samples~/` and `Assets/`, formatted by CSharpier.
* **JSON, YAML, JS and Markdown:** formatted by Prettier. That includes every package's
  `CHANGELOG.md` and `package.json`, the workflows, the `Tools/*.mjs` scripts, READMEs, `AGENTS.md`
  and `docs/`.
* **Editor settings:** a root `.editorconfig` that editors apply as you type, including the naming
  rules as IDE warnings.
* **Enforcement:** a `format` CI job, made a required check on `dev` and `master`.

### 2.2 Out of scope

* **Enforcing naming in CI.** That needs `dotnet format style` against a loadable project, which in a
  Unity repo means generating `.csproj` files in batchmode on every run. The naming rules are
  editor warnings only.
* **A pre-commit hook.** CI is the gate. Editors can format on save through their CSharpier and
  Prettier plugins.
* **Vendored code.** `Packages/PackageBasics/Runtime/ThirdParties/NiceJson.cs`, and the vendored
  agent skills under `.agents/Skills/` and `.claude/`. Reformatting third-party code makes it harder
  to compare with upstream and gains nothing.
* **Files Unity or other tools write.** `.meta`, `.asset`, `.prefab`, `.unity`, `.anim`, `.asmdef`,
  `ProjectSettings/`, `Packages/manifest.json`, `Packages/packages-lock.json` and
  `package-lock.json`. Their writer would overwrite any formatting on its next save.
  * `.asmdef` was in scope in the approved design and moved out here, under the fallback in §7.
    35 of the 36 are written the way Unity writes them, with no final newline. Prettier always
    adds one, so every Inspector save would fail the check.
* **Test fixtures.** `Tools/ci/Tests/fixtures/`, which the tests read byte for byte.
* **`csc.rsp`, `.bat`, `.ps1`, `.txt`.** Neither formatter handles these, and adding a third
  formatter for three PowerShell scripts is not worth it.
* **Releasing.** The reformat changes no behaviour, so it gets no version bumps and no changelog
  entries.

## 3. What the repo looks like today

These measurements drive the settings in §4. They were taken on 2026-09-13 at `dev` `a6352b7`.

| Property | Measured | Consequence |
|--|--|--|
| Stored line endings | LF for every text file; the Windows checkout converts to CRLF (`core.autocrlf=true`) | Both formatters use `endOfLine: auto`; `.editorconfig` sets no `end_of_line` |
| BOM | 136 of 149 `.cs` files start with a UTF-8 BOM; no other type has one | `.editorconfig` sets no `charset`; the reformat must preserve BOMs (§6.3) |
| Indentation | 4 spaces in C#, `.asmdef` and most JSON; 2 in `package.json` and YAML | Defaults of 4, with 2-space overrides |
| Changelog formatting | Prettier 3.9.6 changes 18 of the 19 package changelogs, all cosmetically (e.g. `*Name*` → `_Name_`) | The reformat PR needs the `changelog-rewrite` waiver (§6.4) |
| Open pull requests | None | Nothing to rebase over the reformat |

## 4. Tools and configuration

### 4.1 `.editorconfig`

A root file (`root = true`), the base layer every editor reads.

* **`[*]`:** `indent_style = space`, `indent_size = 4`, `insert_final_newline = true`,
  `trim_trailing_whitespace = true`. Deliberately **no `end_of_line` and no `charset`**: git
  already normalises line endings, and the repo mixes BOM and no-BOM C# files. Pinning either
  setting would flag or rewrite files that are correct.
* **`[*.{yml,yaml,md}]` and `[package.json]`:** `indent_size = 2`.
* **`[*.md]`:** `trim_trailing_whitespace = false`, because two trailing spaces are a hard line break.
* **`[*.asmdef]`:** `insert_final_newline = false`, matching Unity's writer.
* **`[*.cs]`:** the naming rules from AGENTS.md as `dotnet_naming_rule`s at `warning` severity:
  * interfaces are PascalCase with an `I` prefix;
  * types, methods, properties, events, and public or internal fields are PascalCase;
  * private and protected fields are `_camelCase`;
  * locals and parameters are camelCase.

  These show up in Rider, Visual Studio and VS Code only. They are IDE analyzers (IDE1006), so
  neither Unity's compiler nor CI reports them.

### 4.2 CSharpier

* **Install:** a local dotnet tool pinned in `.config/dotnet-tools.json`, restored with
  `dotnet tool restore`. Use the latest stable 1.x at implementation time.
* **`.csharpierrc.json`:** only `"endOfLine": "auto"`. Everything else is the default: 4 spaces, a
  100-column print width, and Allman braces, which already match the repo.
* **`.csharpierignore`:** `Library/`, `Temp/`, `Logs/`, `obj/`, `.agents/Skills/`, `.claude/`, and
  `Packages/PackageBasics/Runtime/ThirdParties/`.
* **Commands:** `dotnet csharpier format .` and `dotnet csharpier check .`. It needs no project or
  solution, which is what makes it usable in a Unity repo's CI.

### 4.3 Prettier

* **Install:** a new root `package.json` with `"private": true` and `prettier` as its only
  `devDependency` at an exact version, plus the `package-lock.json` npm generates. `node_modules/`
  is added to `.gitignore`.
  * The `Tools/` scripts stay dependency-free; this adds no runtime dependency to anything.
  * Unity reads only `Packages/manifest.json`, so a root `package.json` and `node_modules/` are
    invisible to it.
  * The release tooling globs `Packages/*/package.json`, which does not match the root file.
* **Scripts in the root `package.json`:**
  * `"format": "dotnet csharpier format . && prettier --write ."`
  * `"format:check": "dotnet csharpier check . && prettier --check ."`
* **`.prettierrc.json`:**
  * Global: `printWidth: 100` to match CSharpier, `tabWidth: 4`, `endOfLine: "auto"`.
  * Overrides for `package.json`, `*.yml`, `*.yaml` and `*.md`: `tabWidth: 2`. Prettier already
    uses `json-stringify` for any file named `package.json`, which matches the
    `JSON.stringify(…, null, 2)` layout the release tooling writes.
  * A further override for `*.md` alone: `embeddedLanguageFormatting: "off"`. Code blocks in docs
    quote exact file contents and fragments — reformatting the code inside a fence would change
    what it shows (for example, de-indenting a YAML fragment so it no longer sits under `jobs:`).
* **`.prettierignore`:** `.agents/Skills/`, `.claude/`, `.qwen/`, `ProjectSettings/`, `*.asmdef`
  (Prettier does not recognise the extension, but the entry makes the exclusion explicit),
  `Packages/manifest.json`, `Packages/packages-lock.json`, `Tools/ci/Tests/fixtures/`, and
  `package-lock.json`. Prettier 3 also honours `.gitignore`, so `Library/`, `Temp/` and the
  generated `.csproj` files are skipped automatically.

## 5. CI and enforcement

### 5.1 `.github/workflows/format.yml`

It is a separate workflow, like `changelog.yml`, not a job inside `tests.yml`. It has nothing to do
with the Unity suites, and `tests.yml`'s concurrency group cancels in-progress runs.

* **Triggers:** `pull_request`, and `push` to `dev` and `master`.
* **Permissions:** `contents: read`.
* **Job `format`, on `ubuntu-latest`:**
  1. `actions/checkout@v7`
  2. `actions/setup-node@v7` with `node-version-file: .nvmrc`
  3. `actions/setup-dotnet` with `dotnet-version: 10.0.x`
  4. `npm ci`
  5. `dotnet tool restore`
  6. `npm run format:check`; on failure it prints
     `::error::Formatting is out of date. Run \`npm run format\` locally and commit the result.`
* **It runs on fork pull requests.** It is read-only, uses no secrets and never touches the
  self-hosted runner, so the fork boundary that protects `unity-tests` does not apply. Every action
  is first-party (`actions/*`), so tag pins meet the repo's rule that only third-party actions need
  SHA pins.
* **The job name `format` must be unique** across all workflows, for the reason given in
  [CI](../../.agents/AGENTS.md#ci). The workflow must pass `actionlint`.

### 5.2 Required checks

Add `{ "context": "format", "integration_id": 15368 }` to `required_status_checks` in both
`.github/rulesets/dev.json` and `.github/rulesets/master.json`.

Unlike `unity-tests`, `format` is never skipped, so requiring it on `dev` blocks no fork contributor.

**The JSON is committed in the PR, but the live rulesets are updated only after the merge,** once
`format` has passed on `dev`. A required check that has never reported blocks every open pull
request. Updating the live rulesets is an outward-facing change, so the maintainer confirms it
before `gh api` runs.

### 5.3 Documentation

In `.agents/AGENTS.md`:

* A new **Formatting** section: the two commands, the one-time `dotnet tool restore` and `npm ci`,
  the exclusions and why each exists, the `prepare` compatibility rule (§6.2), and
  `git config blame.ignoreRevsFile .git-blame-ignore-revs`.
* A `format` row in the CI job table and in the required-checks table.
* A line in **C# coding style** saying layout is CSharpier's, and the bullets there cover what it
  does not.
* `format.yml` added to the **Repo layout** tree.

## 6. Rollout

### 6.1 Commit order

One pull request into `dev`. Each commit can be reviewed on its own:

1. **Tooling config.** `.editorconfig`, `.config/dotnet-tools.json`, `.csharpierrc.json`,
   `.csharpierignore`, the root `package.json` and `package-lock.json`, `.prettierrc.json`,
   `.prettierignore`, `.gitignore`. No file is reformatted.
2. **The `prepare` compatibility test** (§6.2), the `npm ci` step it needs in `tests.yml`, and a fix
   to the release tooling if it fails.
3. **The mechanical reformat:** the output of `npm run format`, with no hand edits.
4. **`.git-blame-ignore-revs`**, listing commit 3's SHA.
5. **`format.yml`** and the two ruleset JSON edits.
6. **The documentation** (§5.3).

### 6.2 Release tooling must produce formatted output

`Tools/upm-release.mjs prepare` rewrites `CHANGELOG.md` headings and `package.json` versions. If its
output is not already Prettier-formatted, every release PR fails `format`.

A test in `Tools/upm-release.prepare.test.mjs` runs `prepare` over a fixture changelog and
`package.json` that Prettier has already formatted, then runs `prettier --check` on the result and
expects it to pass. The test is written first. If it fails, `prepare` is fixed in the same commit.

That test needs Prettier, so `tooling-tests` in `tests.yml` gains an `npm ci` step. If
`node_modules/.bin/prettier` is missing, the test fails with a message naming `npm ci`; it does not
skip, because a silently skipped test is how this rule would stop being enforced.

### 6.3 Verifying the reformat

Before commit 3 is made:

* `git diff -w --ignore-blank-lines` shows only layout changes, spot-checked in at least one file per
  package.
* Exactly 136 `.cs` files still start with a BOM, and `git ls-files --eol` still reports `i/lf` for
  every text file.
* No `.meta` file, Unity-written file, `NiceJson.cs`, vendored skill or test fixture appears in
  `git status`.
* `npm run format:check` passes, and running `npm run format` a second time changes nothing.
* The EditMode suite passes in the Editor pinned by `ProjectVersion.txt`, run with `unity test` and
  no `-e`.
* All tooling tests pass: `Test-CiScripts.ps1`, the changelog check, and the release tooling.

### 6.4 The pull request

* **Labels:** `no-changelog`, because every package's shipped code changes without an entry, and
  `changelog-rewrite`, because tagged changelog sections change cosmetically. The two waivers are
  separate by design, and both claims are true here.
* **Merge with a merge commit, never a squash.** A squash gives commit 3 a new SHA, and
  `.git-blame-ignore-revs` would then point at a commit that is not on `dev`. The PR description
  says so, prominently.
* **The first `dev` → `master` release PR after this merges also needs both waiver labels**, for
  the same reason: it carries the reformat commit into `master`, so `changelog-check.mjs` reports
  `frozen-section` for every tagged package and `missing-section`/`missing-entry` for packages the
  release doesn't touch.

### 6.5 After the merge

1. Confirm `format` passed on the `dev` push.
2. With the maintainer's confirmation, apply `dev.json` and `master.json` to the live rulesets with
   `gh api`.
3. Read both rulesets back and confirm `format` is listed with `integration_id` 15368.

### 6.6 Rollback

Remove `format` from both live rulesets first, so no pull request waits on a check that has stopped
running. Then revert the merge commit.

## 7. Risks

| Risk | Mitigation |
|--|--|
| A CSharpier or Prettier upgrade reformats code | Both versions are pinned exactly. An upgrade is its own PR that includes the reformat. |
| Unity rewrites a file in a layout the formatter rejects | Unity-written files are out of scope. `.asmdef` was dropped for exactly this reason (§2.2). Drop any other type that turns out the same way, rather than fighting the Editor. |
| Line endings or BOMs flip on a Windows checkout | `endOfLine: auto`, no `charset` in `.editorconfig`, and the BOM and eol checks in §6.3. |
| Squash-merging the reformat breaks blame-ignore | The merge-method instruction in §6.4. |
| `prepare` output fails `format` on the next release | The test in §6.2. |
