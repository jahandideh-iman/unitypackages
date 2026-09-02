# Release Promotion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `master` reachable only from `dev`, turn each package's accumulated `## [Unreleased]` entries into a semver version with one command, and stop empty `## [Unreleased]` headings from existing at all.

**Architecture:** Three additions to tooling that already exists, all dependency-free Node run identically by a developer and by CI. A new `Tools/promotion-check.mjs` fails a pull request into `master` whose head is not `dev`, backed by a GitHub ruleset that requires it. A new `prepare` subcommand in `Tools/upm-release.mjs` reads each package's `## [Unreleased]` sub-headings, derives a 0.x-aware bump, renames the heading to `## [X.Y.Z] - YYYY-MM-DD`, and rewrites `package.json`. A third rule in `Tools/changelog-check.mjs`, `empty-unreleased`, makes the heading's existence conditional on it having entries.

**Tech Stack:** Node 22 (`node:test`, no dependencies), GitHub Actions, GitHub rulesets via `gh api`, Markdown (Keep a Changelog), semver.

**Spec:** [`../specs/2026-09-02-release-promotion-design.md`](../specs/2026-09-02-release-promotion-design.md)

## Global Constraints

- **Branch from `dev`, not `master`.** `dev` is the repo default; `master` is release-only. Every branch in this plan cuts from `dev` and PRs back into `dev`, except the release PR in Task 7.
- **The repo is on GitHub.** Use `gh`, never `glab`.
- **⚠️ Merging a PR into `master` publishes.** The `tag` job runs on that push and the resulting name/version is permanent. Nothing in Tasks 1–6 may merge into `master`.
- **Do not modify the `tag` job in `.github/workflows/release.yml`**, in particular its `if: github.event_name == 'push' && github.ref == 'refs/heads/master'` condition.
- **Never gate a workflow job with `if:` on branch name.** A job skipped by an `if:` reports `skipped`, and a skipped required check blocks the merge. Branch logic lives inside the scripts.
- **Job names must be unique across workflows.** `changelog.yml` already owns `check` and `test`; the new jobs are `promotion-guard` and `release-script-tests`.
- **All tooling is dependency-free Node 22.** No `npm install`, no `package.json` at the repo root, no third-party imports. `node:*` builtins only.
- **Every subcommand supports `--json`.** Exit codes: `0` success, `1` a rule tripped, `2` the check could not run.
- **Three package folders contain spaces** — `Asset Providing`, `Scene Management`, `UI Management`. Always quote them, and never build a shell string by interpolation; pass them as separate argv elements.
- **Package CHANGELOGs are CRLF.** Preserve each file's existing line ending when rewriting it — detect with `text.includes("\r\n")` and join with the same terminator.
- **C# / PowerShell brace style: Allman.** JavaScript in `Tools/` uses 4-space indent and same-line braces, matching the existing scripts.
- **Do not reference other repositories by name** in any committed file or commit message.
- Commit messages end with:
  ```
  Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01A1sHxtmP44SQqLNoKikkGE
  ```
- PR descriptions end with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`, a blank line, then the session URL.

---

## File Structure

| File | Responsibility |
|--|--|
| `Tools/promotion-check.mjs` | **New.** Decides whether a pull request may merge into `master`. Pure decision function plus a thin CLI reading the event from the environment. |
| `Tools/promotion-check.test.mjs` | **New.** Tests for the decision table and the CLI's exit codes. No git repo needed — the script reads only environment variables. |
| `Tools/upm-release.mjs` | **Modified.** Gains the `prepare` subcommand: changelog parsing, bump derivation, version arithmetic, the two file rewrites, and the guards. |
| `Tools/upm-release.test.mjs` | **New.** First tests this script has had. Covers the pure helpers (`unreleasedRange`, `bumpLevel`, `nextVersion`, `releaseChangelog`, `replaceManifestVersion`) directly, with no filesystem. |
| `Tools/upm-release.prepare.test.mjs` | **New.** End-to-end tests for the `prepare` subcommand, each against a throwaway git repo with CRLF fixtures. Split from the file above so the pure-helper suite stays fast and filesystem-free. |
| `Tools/changelog-check.mjs` | **Modified.** Gains the repo-wide `empty-unreleased` rule and a reworded `missing-section` message. |
| `Tools/changelog-check.test.mjs` | **Modified.** Six new tests for `empty-unreleased`. |
| `.github/workflows/release.yml` | **Modified.** Gains the `promotion-guard` and `release-script-tests` jobs. The `tag` job is untouched. |
| `.github/rulesets/master.json` | **New.** The checked-in ruleset applied to `master` with `gh api`. The repo is the record of what protection is configured. |
| `Packages/*/CHANGELOG.md` | **Modified.** Every empty `## [Unreleased]` heading is deleted. |
| `.agents/AGENTS.md` | **Modified.** Documents `prepare`, `promotion-guard`, the ruleset, and reverses the "every CHANGELOG carries an empty `## [Unreleased]` heading" rule. |

The split follows the existing shape of `Tools/`: one script per concern, each with a sibling `*.test.mjs`, each runnable by hand. `promotion-check.mjs` is a separate script rather than a fourth subcommand of `upm-release.mjs` because it answers a question about a pull request, not about a package — it never reads `Packages/` at all.

---

### Task 1: Delete every empty `## [Unreleased]` heading

**Files:**
- Modify: `Packages/*/CHANGELOG.md` — every package whose `## [Unreleased]` section has no entries
- Modify: `.agents/AGENTS.md:302` (the changelog section's opening sentence) and `.agents/AGENTS.md:379` (adding-a-new-package step 6)

**Interfaces:**
- Consumes: nothing.
- Produces: a repository where no CHANGELOG has an empty `## [Unreleased]` heading — the precondition Task 2's rule needs in order to be introduced without false blame.

This task must run **after** the folder-flattening refactor has merged into `dev`. That refactor adds `### Changed` entries to InGameMessageLogging, PackageBasics, UnityUtilities, and UpdateManagement; run before it, this task would delete the very headings those entries belong under. As of writing, 17 of 18 packages have an empty heading (only PersistentDataManagement has entries). After the flattening lands the count is 13, which is the number the spec quotes. The step below computes the set rather than hard-coding it, so it is correct either way — but check the count it prints against the expectation before committing.

`PackageTemplate` is included even though it is private and both existing rules skip it. A scaffolded package that starts life with an empty heading would begin by violating the new policy.

This is a Markdown-only change: `missing-entry` never fires (every `*.md` is exempt) and no tagged version section is touched, so `frozen-section` stays quiet.

- [ ] **Step 1: Confirm the flattening has landed**

```bash
git log --oneline dev -15
git switch -c chore/drop-empty-unreleased dev
```

Expect the flattening merge in the log. If it is not there, stop — this task is blocked on it.

- [ ] **Step 2: List which packages have an empty heading**

Write `Tools/tmp-empty-unreleased.mjs` (a throwaway; it is deleted in Step 5):

```js
import fs from "node:fs";
import path from "node:path";

const H2 = /^##\s/;
const H2_VERSION = /^##\s+\[([^\]]+)\]/;
const UNRELEASED = /^unreleased$/i;
const SUB_HEADING = /^#{3,}\s/;

for (const folder of fs.readdirSync("Packages").sort()) {
    const file = path.join("Packages", folder, "CHANGELOG.md");
    if (!fs.existsSync(file)) continue;
    const text = fs.readFileSync(file, "utf8");
    const eol = text.includes("\r\n") ? "\r\n" : "\n";
    const lines = text.split(/\r?\n/);

    const start = lines.findIndex((l) => H2_VERSION.test(l) && UNRELEASED.test(l.match(H2_VERSION)[1]));
    if (start === -1) continue;
    const rest = lines.slice(start + 1);
    const offset = rest.findIndex((l) => H2.test(l));
    const end = offset === -1 ? lines.length : start + 1 + offset;

    const entries = lines
        .slice(start + 1, end)
        .map((l) => l.trim())
        .filter((l) => l !== "" && !SUB_HEADING.test(l));
    if (entries.length > 0) {
        console.log(`keep    ${folder} — ${entries.length} entr${entries.length === 1 ? "y" : "ies"}`);
        continue;
    }
    console.log(`delete  ${folder}`);
    if (process.argv.includes("--write")) {
        lines.splice(start, end - start);
        fs.writeFileSync(file, lines.join(eol));
    }
}
```

Run: `node Tools/tmp-empty-unreleased.mjs`
Expected: `keep` for the five packages with entries (InGameMessageLogging, PackageBasics, PersistentDataManagement, UnityUtilities, UpdateManagement), `delete` for the other 13.

- [ ] **Step 3: Delete them**

```bash
node Tools/tmp-empty-unreleased.mjs --write
git diff --stat
```

Expected: 13 CHANGELOG.md files changed, each `-3` lines or so, no `+` lines.

- [ ] **Step 4: Verify the result by eye and by the existing checks**

```bash
git diff -- "Packages/ComponentSystem/CHANGELOG.md"
node Tools/upm-release.mjs validate
node --test Tools/changelog-check.test.mjs
```

Expected: the diff removes only `## [Unreleased]` and the blank line under it, leaving one blank line between the preamble and `## [0.1.0]`; 17/17 packages valid; 31 tests passing. Confirm no file's line endings flipped: `git diff --stat` should show no whole-file rewrite.

- [ ] **Step 5: Remove the throwaway script**

```bash
rm Tools/tmp-empty-unreleased.mjs
```

- [ ] **Step 6: Reverse the seeding rule in AGENTS.md**

In `.agents/AGENTS.md`, replace the opening sentence of the changelog section:

> Every package CHANGELOG carries an empty `## [Unreleased]` heading above its newest version, seeded across all 18 on 2026-08-30.

with:

> A package CHANGELOG carries a `## [Unreleased]` heading **only while it has entries under it**. The contributor with something to record creates the heading; `upm-release.mjs prepare` renames it to a version heading and leaves nothing in its place. The headings seeded across all 18 packages on 2026-08-30 were deleted on 2026-09-02 — an empty heading is now a CI failure, see `empty-unreleased` below.

And rewrite step 6 of *Adding a new package*:

> 6. Write a `README.md` and a `CHANGELOG.md` with **no `## [Unreleased]` heading** — add one when you have an entry to put under it. See [the changelog rules](#changelogs--two-rules-enforced-in-ci).

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "$(cat <<'MSG'
docs(changelog): delete every empty [Unreleased] heading

The heading exists only while it has entries. Thirteen packages were
carrying an empty one, seeded on 2026-08-30; the five with unreleased
work keep theirs. PackageTemplate is included so a scaffolded package
does not start out violating the rule.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01A1sHxtmP44SQqLNoKikkGE
MSG
)"
```

---

### Task 2: The `empty-unreleased` rule

**Files:**
- Modify: `Tools/changelog-check.mjs` — new scan function, new rule in the report, reworded `missing-section` explanation, `usage()` text, header comment
- Modify: `Tools/changelog-check.test.mjs` — six new tests
- Modify: `.agents/AGENTS.md` — the rules table and the details list

**Interfaces:**
- Consumes: the existing `entriesOf(lines)`, `unreleasedSection(text)`, `changelogAt(ref, folder)`, `manifestAt(ref, folder)` from `changelog-check.mjs`.
- Produces: rule id `"empty-unreleased"`, reported as `{ rule: "empty-unreleased" }` inside a package's `problems` array, with **no waiver label**. Report entries created by this rule have `files: []`.

Unlike the other two rules, this one is **repo-wide, not diff-scoped**: it walks every publishable package's CHANGELOG at the head commit, whether or not the pull request touched it. That is only safe because Task 1 cleaned all of them first. There is deliberately no waiver label — "I need an empty heading" is not a claim anyone should be able to make.

"No entries" reuses `entriesOf`, which is the same rule `prepare` uses in Task 5. A bare `### Added` with no bullet under it is not an entry, in both places. If one changes, both change.

- [ ] **Step 1: Write the failing tests**

Append to `Tools/changelog-check.test.mjs`. The helpers `makeRepo`, `check`, `alphaBase`, `changelog`, and `changelogWithoutUnreleased` already exist at the top of that file — reuse them, do not redefine them.

```js
// ---------------------------------------------------------- empty-unreleased

/** The rule ids reported against a folder, across every package in the report. */
function problemsFor(report, folder) {
    const pkg = report.packages.find((p) => p.folder === folder);
    return pkg === undefined ? [] : pkg.problems.map((p) => p.rule);
}

test("empty-unreleased: an empty heading fails, even in an untouched package", (t) => {
    const repo = makeRepo(
        t,
        {
            ...alphaBase(),
            "Packages/Beta/package.json": JSON.stringify({ name: "com.arman.beta", version: "0.1.0" }, null, 2),
            "Packages/Beta/CHANGELOG.md": changelog(),
        },
        { "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n" },
    );
    const { status, report } = check(repo);
    assert.equal(status, 1);
    assert.deepEqual(problemsFor(report, "Beta"), ["empty-unreleased"]);
});

test("empty-unreleased: a heading with only a bare ### sub-heading fails", (t) => {
    const repo = makeRepo(t, alphaBase({ "Packages/Alpha/CHANGELOG.md": changelog("### Added\n\n") }), {
        "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n",
    });
    const { status, report } = check(repo);
    assert.equal(status, 1);
    assert.deepEqual(problemsFor(report, "Alpha"), ["empty-unreleased"]);
});

test("empty-unreleased: a heading with entries passes", (t) => {
    const repo = makeRepo(t, alphaBase({ "Packages/Alpha/CHANGELOG.md": changelog("### Added\n\n- A thing.\n\n") }), {
        "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n",
    });
    const { status, report } = check(repo);
    assert.equal(status, 0);
    assert.deepEqual(problemsFor(report, "Alpha"), []);
});

test("empty-unreleased: no heading at all passes", (t) => {
    const repo = makeRepo(t, alphaBase({ "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased() }), {
        "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n",
    });
    const { status, report } = check(repo);
    assert.equal(status, 0);
    assert.deepEqual(problemsFor(report, "Alpha"), []);
});

test("empty-unreleased: a private package is skipped", (t) => {
    const repo = makeRepo(
        t,
        {
            ...alphaBase({ "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased() }),
            "Packages/Template/package.json": JSON.stringify(
                { name: "com.arman.template", version: "0.1.0", private: true },
                null,
                2,
            ),
            "Packages/Template/CHANGELOG.md": changelog(),
        },
        { "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n" },
    );
    const { status, report } = check(repo);
    assert.equal(status, 0);
    assert.deepEqual(problemsFor(report, "Template"), []);
});

test("empty-unreleased: deleting an empty heading is what fixes it", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased(),
    });
    const { status, report } = check(repo);
    assert.equal(status, 0);
    assert.deepEqual(problemsFor(report, "Alpha"), []);
});
```

- [ ] **Step 2: Run them to verify they fail**

Run: `node --test Tools/changelog-check.test.mjs`
Expected: 31 pass, the new tests fail — the first with an assertion that `Beta` is not in the report at all (`[]` vs `["empty-unreleased"]`), the second and sixth similarly.

- [ ] **Step 3: Add the scan to `changelog-check.mjs`**

After `frozenProblem` (around line 238), add:

```js
/**
 * Rule 3. A `## [Unreleased]` heading exists only while it has entries under
 * it. Unlike the other two rules this one is repo-wide rather than diff-scoped:
 * it walks every publishable package at the head commit. An empty heading is
 * noise wherever it sits, and every package was cleaned before the rule landed,
 * so nobody is blamed for someone else's leftover.
 */
function emptyUnreleasedFolders(head) {
    const listing = gitOrNull("ls-tree", "--name-only", "-z", `${head}:${PACKAGES_DIR}`);
    if (listing === null) return [];

    const folders = [];
    for (const folder of listing.split("\0").filter(Boolean)) {
        const manifest = manifestAt(head, folder);
        if (manifest === null || manifest.private === true) continue;
        const text = changelogAt(head, folder);
        if (text === null) continue;
        const section = unreleasedSection(text);
        if (section === null) continue; // No heading is the healthy state.
        if (entriesOf(section).length === 0) folders.push({ folder, name: manifest.name ?? null });
    }
    return folders;
}
```

- [ ] **Step 4: Fold the scan's findings into the report**

Replace the main loop near the bottom of the file (currently lines 383–387) with:

```js
const byFolder = new Map();
for (const [folder, touched] of [...packagesTouched(files)].sort((a, b) => a[0].localeCompare(b[0]))) {
    const result = inspect(folder, touched, from, flags.head, tags, report.waived);
    if (result !== null) byFolder.set(folder, result);
}

// Repo-wide, so a package the pull request never touched still reports.
for (const { folder, name } of emptyUnreleasedFolders(flags.head)) {
    const existing = byFolder.get(folder);
    if (existing === undefined) {
        byFolder.set(folder, { folder, name, files: [], problems: [{ rule: "empty-unreleased" }] });
    } else if (existing.skipped === undefined) {
        existing.problems.push({ rule: "empty-unreleased" });
    }
}

report.packages = [...byFolder.values()].sort((a, b) => a.folder.localeCompare(b.folder));
report.ok = !report.packages.some((pkg) => pkg.problems.length > 0);
```

Note the `existing.skipped === undefined` guard: a package that is private or new in the pull request keeps its `skipped` status and gains no problem. `emptyUnreleasedFolders` already filters private packages; the guard covers `skipped: "new"`, where a brand-new package's initial-release CHANGELOG is nobody's regression yet.

- [ ] **Step 5: Add the explanation and reword `missing-section`**

In the `EXPLANATIONS` map:

```js
    "missing-section": () =>
        "has no `## [Unreleased]` heading. Create one above the newest version together with the entry describing this change — the heading exists only while it has entries.",
    "empty-unreleased": () =>
        "has a `## [Unreleased]` heading with nothing under it. Delete the heading, or put an entry under it. (A bare `### Added` is not an entry.)",
```

In `render`, the waiver footer must not print `undefined` for a rule that has no waiver. Replace the footer block with:

```js
    const failing = report.packages.filter((pkg) => pkg.problems.length > 0);
    if (failing.length > 0) {
        const rules = new Set(failing.flatMap((pkg) => pkg.problems.map((p) => p.rule)));
        const waivable = [...rules].filter((rule) => WAIVER_LABELS[rule] !== undefined);
        lines.push("");
        lines.push(`${failing.length} package(s) need attention.`);
        if (waivable.length > 0) {
            lines.push("Waiver labels for these rules:");
            for (const rule of waivable) lines.push(`  ${rule} → ${WAIVER_LABELS[rule]}`);
        }
    }
```

- [ ] **Step 6: Update the script's header comment and `usage()`**

In the header comment block, after the `frozen-section` paragraph, add:

```js
//   empty-unreleased A `## [Unreleased]` heading with no entries under it is
//                    noise. Delete the heading, or put something under it.
//                    Checked repo-wide at the head commit, not just on the
//                    packages this pull request touched. No waiver.
```

In `usage()`, after the waiver-label list, add:

```js
            "",
            "`empty-unreleased` has no waiver: delete the heading or fill it in.",
```

- [ ] **Step 7: Run the tests**

Run: `node --test Tools/changelog-check.test.mjs`
Expected: 37 tests, 0 failures.

- [ ] **Step 8: Run the check against the real repo**

```bash
node Tools/changelog-check.mjs --base dev --head HEAD
```

Expected: exit 0. If a package reports `empty-unreleased`, Task 1 missed it — fix the CHANGELOG, do not weaken the rule.

- [ ] **Step 9: Document the rule in AGENTS.md**

Add a row to the rules table:

```markdown
| `empty-unreleased` | A `## [Unreleased]` heading must have at least one entry under it. Checked **repo-wide** at the head commit, not just on the packages the PR touched. | *none* |
```

And add to the details list:

```markdown
* `empty-unreleased` has **no waiver label**, deliberately: "I need an empty heading" is not a claim worth being able to make. Delete the heading or fill it in.
* It is the one rule that is not diff-scoped. Every publishable package's CHANGELOG is read at the head commit, which is only fair because all 18 were cleaned before the rule landed (2026-09-02).
```

Also update the test count in the code block above the table: `node --test Tools/changelog-check.test.mjs    # the check's own tests, 37 of them`.

- [ ] **Step 10: Commit**

```bash
git add Tools/changelog-check.mjs Tools/changelog-check.test.mjs .agents/AGENTS.md
git commit -m "$(cat <<'MSG'
feat(changelog-check): add the empty-unreleased rule

An `## [Unreleased]` heading with nothing under it is noise, so the
heading now exists only while it has entries. The rule is repo-wide
rather than diff-scoped and has no waiver label. `missing-section`'s
message is reworded for the same world.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01A1sHxtmP44SQqLNoKikkGE
MSG
)"
```

---

### Task 3: `promotion-check.mjs` and the `promotion-guard` job

**Files:**
- Create: `Tools/promotion-check.mjs`
- Create: `Tools/promotion-check.test.mjs`
- Modify: `.github/workflows/release.yml` — add the `promotion-guard` job
- Modify: `.agents/AGENTS.md` — the Branching section

**Interfaces:**
- Consumes: nothing.
- Produces: `Tools/promotion-check.mjs`, whose CLI reads `GITHUB_EVENT_NAME`, `GITHUB_BASE_REF`, and `GITHUB_HEAD_REF` from the environment (overridable with `--event`, `--base`, `--head`) and prints `{ ok, event, base, head, reason }` under `--json`. Exit `0` allowed, `1` refused, `2` bad usage.

The decision table:

| Event | Base | Head | Result |
|--|--|--|--|
| not `pull_request` | — | — | pass — "not a release pull request" |
| `pull_request` | not `master` | — | pass — "not a release pull request" |
| `pull_request` | `master` | `dev` | pass — "release pull request from `dev`" |
| `pull_request` | `master` | anything else | **fail**, naming the branch |

The branch logic lives in the script, never in a workflow `if:`. The job runs on every event `release.yml` fires on and reports a genuine success in the cases that are not release pull requests — a job skipped by an `if:` would report `skipped`, and this job is about to become a required check, where `skipped` blocks the merge.

- [ ] **Step 1: Write the failing tests**

Create `Tools/promotion-check.test.mjs`:

```js
// Tests for Tools/promotion-check.mjs.
//
// The script reads the pull request's event, base, and head from the
// environment, so a test is just an environment plus an expected exit code —
// no repository required.
//
//     node --test Tools/promotion-check.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const SCRIPT = path.join(HERE, "promotion-check.mjs");

function run(env = {}, args = ["--json"]) {
    const result = spawnSync(process.execPath, [SCRIPT, ...args], {
        encoding: "utf8",
        env: { ...process.env, GITHUB_EVENT_NAME: "", GITHUB_BASE_REF: "", GITHUB_HEAD_REF: "", ...env },
    });
    return { status: result.status, stdout: result.stdout, stderr: result.stderr };
}

function report(env) {
    const { status, stdout } = run(env);
    return { status, report: JSON.parse(stdout) };
}

test("a pull request from dev into master is allowed", () => {
    const { status, report: r } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "master",
        GITHUB_HEAD_REF: "dev",
    });
    assert.equal(status, 0);
    assert.equal(r.ok, true);
});

test("a pull request from a feature branch into master is refused", () => {
    const { status, report: r } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "master",
        GITHUB_HEAD_REF: "feat/shiny",
    });
    assert.equal(status, 1);
    assert.equal(r.ok, false);
    assert.match(r.reason, /feat\/shiny/);
});

test("the refusal names master and dev so the fix is obvious", () => {
    const { status, stdout } = run(
        { GITHUB_EVENT_NAME: "pull_request", GITHUB_BASE_REF: "master", GITHUB_HEAD_REF: "hotfix" },
        [],
    );
    assert.equal(status, 1);
    assert.match(stdout, /master/);
    assert.match(stdout, /dev/);
});

test("a pull request into dev is not a release pull request", () => {
    const { status, report: r } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "dev",
        GITHUB_HEAD_REF: "feat/shiny",
    });
    assert.equal(status, 0);
    assert.equal(r.ok, true);
});

test("a push is not a release pull request", () => {
    const { status, report: r } = report({ GITHUB_EVENT_NAME: "push", GITHUB_BASE_REF: "", GITHUB_HEAD_REF: "" });
    assert.equal(status, 0);
    assert.equal(r.ok, true);
});

test("refs/heads/ prefixes are tolerated", () => {
    const { status } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "refs/heads/master",
        GITHUB_HEAD_REF: "refs/heads/dev",
    });
    assert.equal(status, 0);
});

test("flags override the environment", () => {
    const { status } = report({
        GITHUB_EVENT_NAME: "push",
    });
    assert.equal(status, 0);

    const overridden = run({ GITHUB_EVENT_NAME: "push" }, [
        "--json",
        "--event",
        "pull_request",
        "--base",
        "master",
        "--head",
        "feat/x",
    ]);
    assert.equal(overridden.status, 1);
});

test("a flag without a value is a usage error", () => {
    const { status } = run({}, ["--base"]);
    assert.equal(status, 2);
});

test("an unknown flag is a usage error", () => {
    const { status } = run({}, ["--nope"]);
    assert.equal(status, 2);
});
```

- [ ] **Step 2: Run them to verify they fail**

Run: `node --test Tools/promotion-check.test.mjs`
Expected: every test fails — `Cannot find module .../promotion-check.mjs`.

- [ ] **Step 3: Write the script**

Create `Tools/promotion-check.mjs`:

```js
#!/usr/bin/env node
// Refuses a pull request into `master` whose head is not `dev`.
//
//     node Tools/promotion-check.mjs [--event <name>] [--base <ref>] [--head <ref>] [--json]
//
// `master` is release-only: merging into it publishes every package whose
// version is not tagged yet. It moves solely via a release pull request from
// `dev`, which is what keeps every commit on `master` also present on `dev`.
// A GitHub ruleset can require this check but cannot express the rule itself —
// rulesets target a destination ref and say nothing about a pull request's
// source branch.
//
// The branch logic is deliberately inside the script rather than an `if:` on
// the job. A job skipped by an `if:` reports `skipped`, and a skipped required
// check blocks the merge.
//
// Exit 0 = allowed, 1 = refused, 2 = the check itself could not run.

const RELEASE_BRANCH = "master";
const SOURCE_BRANCH = "dev";

/** `refs/heads/dev` and `dev` are the same branch; the event gives either. */
function shortRef(ref) {
    return (ref ?? "").trim().replace(/^refs\/heads\//, "");
}

export function decide(event, base, head) {
    if (event !== "pull_request") {
        return { ok: true, reason: `event is \`${event || "none"}\`, not a pull request — nothing to guard.` };
    }
    if (base !== RELEASE_BRANCH) {
        return { ok: true, reason: `pull request targets \`${base || "?"}\`, not \`${RELEASE_BRANCH}\`.` };
    }
    if (head === SOURCE_BRANCH) {
        return { ok: true, reason: `release pull request \`${SOURCE_BRANCH}\` → \`${RELEASE_BRANCH}\`.` };
    }
    return {
        ok: false,
        reason: `pull request into \`${RELEASE_BRANCH}\` comes from \`${head || "?"}\`, but \`${RELEASE_BRANCH}\` may only be reached from \`${SOURCE_BRANCH}\`. Merge \`${head || "?"}\` into \`${SOURCE_BRANCH}\` first, then open the release pull request from \`${SOURCE_BRANCH}\`.`,
    };
}

function usage() {
    console.error(`usage: node Tools/promotion-check.mjs [options]

options:
  --event <name>   event name (default: $GITHUB_EVENT_NAME)
  --base <ref>     branch the pull request merges into (default: $GITHUB_BASE_REF)
  --head <ref>     branch the pull request proposes (default: $GITHUB_HEAD_REF)
  --json           machine-readable output`);
    return 2;
}

function parseArgs(argv) {
    const flags = {};
    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];
        if (arg === "--json") {
            flags.json = true;
        } else if (arg === "--event" || arg === "--base" || arg === "--head") {
            const value = argv[++i];
            if (value === undefined) return null;
            flags[arg.slice(2)] = value;
        } else {
            return null;
        }
    }
    return flags;
}

const flags = parseArgs(process.argv.slice(2));
if (flags === null) process.exit(usage());

const event = (flags.event ?? process.env.GITHUB_EVENT_NAME ?? "").trim();
const base = shortRef(flags.base ?? process.env.GITHUB_BASE_REF);
const head = shortRef(flags.head ?? process.env.GITHUB_HEAD_REF);

const result = { ...decide(event, base, head), event, base, head };
console.log(flags.json ? JSON.stringify(result, null, 2) : `${result.ok ? "ok" : "FAIL"}  ${result.reason}`);
process.exit(result.ok ? 0 : 1);
```

- [ ] **Step 4: Run the tests**

Run: `node --test Tools/promotion-check.test.mjs`
Expected: 9 tests, 0 failures.

- [ ] **Step 5: Add the `promotion-guard` job**

In `.github/workflows/release.yml`, insert this job **above** `validate` (leave `validate`, `pack`, and `tag` exactly as they are):

```yaml
jobs:
  # `master` may only be reached from `dev`. A ruleset can require this check
  # but cannot express the rule — rulesets target a destination ref and say
  # nothing about a pull request's source branch. The branch logic lives in the
  # script, not in an `if:` on this job: a skipped job reports `skipped`, and a
  # skipped required check blocks the merge.
  promotion-guard:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-node@v7
        with:
          node-version: 22
      # From the event, through the environment — never interpolated into the
      # shell body, where a branch name would be an injection seam.
      - name: Check the pull request's source branch
        env:
          GITHUB_EVENT_NAME: ${{ github.event_name }}
          GITHUB_BASE_REF: ${{ github.base_ref }}
          GITHUB_HEAD_REF: ${{ github.head_ref }}
        run: node Tools/promotion-check.mjs
```

- [ ] **Step 6: Document it in AGENTS.md**

In the Branching section, after the "enforced in two places" paragraph, add:

```markdown
The *source* of a release PR is enforced separately, by `promotion-guard` in `release.yml` (`Tools/promotion-check.mjs`): a pull request into `master` from anything other than `dev` fails. A GitHub ruleset cannot express this — rulesets target a destination ref and say nothing about a pull request's source — so the ruleset's job is to make `promotion-guard` a **required** check. Run it by hand with `node Tools/promotion-check.mjs --event pull_request --base master --head my-branch`.
```

- [ ] **Step 7: Commit**

```bash
git add Tools/promotion-check.mjs Tools/promotion-check.test.mjs .github/workflows/release.yml .agents/AGENTS.md
git commit -m "$(cat <<'MSG'
feat(ci): refuse a pull request into master from anything but dev

`master` is release-only and merging into it publishes, so it may only
be reached by a release pull request from `dev`. The rule lives in
Tools/promotion-check.mjs and runs as the `promotion-guard` job; a
ruleset makes it required, since a ruleset cannot express a pull
request's source branch itself.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01A1sHxtmP44SQqLNoKikkGE
MSG
)"
```

---

### Task 4: The `master` ruleset

**Files:**
- Create: `.github/rulesets/master.json`
- Modify: `.agents/AGENTS.md` — the Branching section

**Interfaces:**
- Consumes: the check name `promotion-guard` from Task 3, plus the existing check names `validate` and `pack` from `release.yml`.
- Produces: a ruleset applied to `master` on GitHub, and its definition checked into the repo so the configuration is reviewable.

The ruleset requires a pull request before merging, requires the three status checks, blocks force pushes, and blocks branch deletion. **No bypass actors, repository owner included** — a bypass is exactly the door this whole design closes.

Apply it after Task 3 has merged into `dev`, so `promotion-guard` has reported at least once and GitHub knows the name.

- [ ] **Step 1: Write the ruleset**

Create `.github/rulesets/master.json`:

```json
{
  "name": "master",
  "target": "branch",
  "enforcement": "active",
  "bypass_actors": [],
  "conditions": {
    "ref_name": {
      "include": ["refs/heads/master"],
      "exclude": []
    }
  },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 0,
        "dismiss_stale_reviews_on_push": false,
        "require_code_owner_review": false,
        "require_last_push_approval": false,
        "required_review_thread_resolution": false,
        "allowed_merge_methods": ["merge", "squash", "rebase"]
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": false,
        "do_not_enforce_on_create": false,
        "required_status_checks": [
          { "context": "promotion-guard" },
          { "context": "validate" },
          { "context": "pack" }
        ]
      }
    }
  ]
}
```

`non_fast_forward` is GitHub's name for "block force pushes". `deletion` blocks deleting the branch. `bypass_actors` is empty on purpose.

- [ ] **Step 2: Add a README next to it**

Create `.github/rulesets/README.md`:

```markdown
# Rulesets

The branch protection applied to this repository, checked in so that the
configuration is reviewable and restorable rather than living only in the
GitHub UI.

Apply or update:

```bash
gh api repos/:owner/:repo/rulesets --input .github/rulesets/master.json      # create
gh api --method PUT repos/:owner/:repo/rulesets/<id> --input .github/rulesets/master.json   # update
gh api repos/:owner/:repo/rulesets                                            # list, to find <id>
```

`master.json` requires a pull request into `master`, requires the
`promotion-guard`, `validate`, and `pack` checks, and blocks force pushes and
branch deletion. **`bypass_actors` is empty, repository owner included** — that
is the point: merging into `master` publishes permanently, and a bypass is the
door the whole flow closes.
```

- [ ] **Step 3: Check the ruleset does not already exist**

```bash
gh api repos/:owner/:repo/rulesets
```

Expected: `[]`, or a list without a `master` entry. If one exists, note its `id` and use the `PUT` form in the next step instead.

- [ ] **Step 4: Apply it**

```bash
gh api repos/:owner/:repo/rulesets --input .github/rulesets/master.json
```

Expected: the created ruleset echoed back with an `id` and `"enforcement": "active"`.

- [ ] **Step 5: Verify it took**

```bash
gh api repos/:owner/:repo/rulesets --jq '.[] | "\(.id) \(.name) \(.enforcement)"'
gh api repos/:owner/:repo/rules/branches/master --jq '[.[].type] | sort'
```

Expected: the ruleset listed as `active`, and the branch rules including `deletion`, `non_fast_forward`, `pull_request`, and `required_status_checks`.

- [ ] **Step 6: Document it in AGENTS.md**

After the `promotion-guard` paragraph added in Task 3, add:

```markdown
`master` carries a ruleset — [`.github/rulesets/master.json`](../.github/rulesets/master.json), applied with `gh api repos/:owner/:repo/rulesets --input .github/rulesets/master.json` — that requires a pull request, requires `promotion-guard`, `validate`, and `pack` to pass, and blocks force pushes and branch deletion. **It has no bypass actors, repository owner included.** Merging into `master` publishes permanently; a bypass is the door this flow exists to close.
```

- [ ] **Step 7: Commit**

```bash
git add .github/rulesets
git commit -m "$(cat <<'MSG'
ci(release): check in the master ruleset

Requires a pull request, requires promotion-guard/validate/pack, blocks
force pushes and branch deletion, with no bypass actors. Checked in so
the protection is reviewable and restorable rather than living only in
the GitHub UI.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01A1sHxtmP44SQqLNoKikkGE
MSG
)"
```

---

### Task 5: `prepare`'s pure helpers

**Files:**
- Modify: `Tools/upm-release.mjs` — add a "prepare" section of pure functions, exported for tests
- Create: `Tools/upm-release.test.mjs`
- Modify: `.github/workflows/release.yml` — add the `release-script-tests` job

**Interfaces:**
- Consumes: the existing constants and helpers in `upm-release.mjs`.
- Produces, all exported from `Tools/upm-release.mjs`:
  - `unreleasedRange(lines: string[]) → { start: number, end: number } | null` — the index of the `## [Unreleased]` heading and the index one past its last body line.
  - `unreleasedEntries(lines: string[]) → string[]` — the body's entry lines, blank lines and `###` sub-headings removed. Same rule as `changelog-check.mjs`'s `entriesOf`.
  - `populatedSubsections(lines: string[]) → string[]` — the names of `###` sub-headings that have at least one non-blank line under them, in file order.
  - `bumpLevel(names: string[]) → "breaking" | "feature" | "fix" | null` — the highest level implied, `null` if no name is recognised.
  - `nextVersion(version: string, level: "breaking" | "feature" | "fix") → string` — 0.x-aware.
  - `explicitBump(version: string, part: "major" | "minor" | "patch") → string` — no 0.x remapping; the user asked for a part.
  - `releaseChangelog(text: string, version: string, date: string) → string` — renames the heading, preserves everything else including the file's line endings.
  - `replaceManifestVersion(text: string, version: string) → string` — targeted single-line replacement; throws if the `"version"` key does not appear exactly once.

`bumpLevel` returning `null` when a package has entries but no recognised `###` heading is deliberate: Task 6 turns that into an error naming the package, rather than silently skipping it or guessing a level.

`replaceManifestVersion` rewrites one line rather than parsing and re-serialising, so key order, indentation, and trailing newline survive untouched. Re-serialising a manifest would produce a diff nobody asked for.

- [ ] **Step 1: Write the failing tests**

Create `Tools/upm-release.test.mjs`:

```js
// Tests for Tools/upm-release.mjs.
//
// The pure helpers are imported and called directly; `prepare` itself is
// exercised end-to-end in Tools/upm-release.prepare.test.mjs.
//
//     node --test Tools/upm-release.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";

import {
    unreleasedRange,
    unreleasedEntries,
    populatedSubsections,
    bumpLevel,
    nextVersion,
    explicitBump,
    releaseChangelog,
    replaceManifestVersion,
} from "./upm-release.mjs";

const CHANGELOG = [
    "# Changelog",
    "",
    "## [Unreleased]",
    "",
    "### Added",
    "",
    "- A new thing.",
    "",
    "### Fixed",
    "",
    "- An old thing.",
    "",
    "## [0.1.0] - 2026-08-29",
    "",
    "Initial release.",
    "",
].join("\n");

test("unreleasedRange finds the heading and the end of its body", () => {
    const lines = CHANGELOG.split("\n");
    assert.deepEqual(unreleasedRange(lines), { start: 2, end: 12 });
});

test("unreleasedRange returns null when there is no heading", () => {
    assert.equal(unreleasedRange(["# Changelog", "", "## [0.1.0] - 2026-08-29"]), null);
});

test("unreleasedRange runs to the end of file when nothing follows", () => {
    const lines = ["# Changelog", "", "## [Unreleased]", "", "### Added", "", "- A thing."];
    assert.deepEqual(unreleasedRange(lines), { start: 2, end: 7 });
});

test("unreleasedEntries ignores blanks and sub-headings", () => {
    const lines = CHANGELOG.split("\n");
    const { start, end } = unreleasedRange(lines);
    assert.deepEqual(unreleasedEntries(lines.slice(start + 1, end)), ["- A new thing.", "- An old thing."]);
});

test("unreleasedEntries treats a bare sub-heading as no entry", () => {
    assert.deepEqual(unreleasedEntries(["", "### Added", "", "### Fixed", ""]), []);
});

test("populatedSubsections lists only sub-headings with something under them", () => {
    assert.deepEqual(populatedSubsections(["### Added", "", "- A thing.", "", "### Fixed", ""]), ["Added"]);
});

test("bumpLevel takes the highest level present", () => {
    assert.equal(bumpLevel(["Fixed"]), "fix");
    assert.equal(bumpLevel(["Fixed", "Added"]), "feature");
    assert.equal(bumpLevel(["Fixed", "Removed"]), "breaking");
    assert.equal(bumpLevel(["Changed", "Deprecated"]), "feature");
    assert.equal(bumpLevel(["Security"]), "fix");
});

test("bumpLevel is case-insensitive and ignores unknown headings", () => {
    assert.equal(bumpLevel(["added"]), "feature");
    assert.equal(bumpLevel(["Notes"]), null);
    assert.equal(bumpLevel(["Notes", "Fixed"]), "fix");
    assert.equal(bumpLevel([]), null);
});

test("nextVersion keeps a breaking change on the minor while major is 0", () => {
    assert.equal(nextVersion("0.1.0", "breaking"), "0.2.0");
    assert.equal(nextVersion("0.1.0", "feature"), "0.2.0");
    assert.equal(nextVersion("0.1.3", "fix"), "0.1.4");
    assert.equal(nextVersion("0.4.2", "feature"), "0.5.0");
});

test("nextVersion uses ordinary semver from 1.0.0 on", () => {
    assert.equal(nextVersion("1.2.3", "breaking"), "2.0.0");
    assert.equal(nextVersion("1.2.3", "feature"), "1.3.0");
    assert.equal(nextVersion("1.2.3", "fix"), "1.2.4");
});

test("explicitBump bumps the part it is told to, with no 0.x remapping", () => {
    assert.equal(explicitBump("0.1.0", "major"), "1.0.0");
    assert.equal(explicitBump("0.1.0", "minor"), "0.2.0");
    assert.equal(explicitBump("0.1.0", "patch"), "0.1.1");
});

test("nextVersion rejects a version it cannot parse", () => {
    assert.throws(() => nextVersion("0.1", "fix"), /0\.1/);
    assert.throws(() => nextVersion("1.0.0-preview", "fix"), /preview/);
});

test("releaseChangelog renames the heading and changes nothing else", () => {
    const released = releaseChangelog(CHANGELOG, "0.2.0", "2026-09-02");
    assert.equal(released, CHANGELOG.replace("## [Unreleased]", "## [0.2.0] - 2026-09-02"));
    assert.ok(!released.includes("[Unreleased]"));
});

test("releaseChangelog preserves CRLF line endings", () => {
    const crlf = CHANGELOG.split("\n").join("\r\n");
    const released = releaseChangelog(crlf, "0.2.0", "2026-09-02");
    assert.ok(released.includes("## [0.2.0] - 2026-09-02\r\n"));
    assert.ok(!released.includes("\n\n")); // No bare LF pair survived.
});

test("replaceManifestVersion rewrites one line and leaves the rest byte-identical", () => {
    const manifest = '{\r\n  "name": "com.arman.alpha",\r\n  "version": "0.1.0",\r\n  "unity": "6000.0"\r\n}\r\n';
    const rewritten = replaceManifestVersion(manifest, "0.2.0");
    assert.equal(rewritten, manifest.replace('"version": "0.1.0"', '"version": "0.2.0"'));
});

test("replaceManifestVersion refuses an ambiguous manifest", () => {
    const manifest = '{\n  "version": "0.1.0",\n  "dependencies": {\n    "version": "1.0.0"\n  }\n}\n';
    assert.throws(() => replaceManifestVersion(manifest, "0.2.0"), /exactly one/);
});

test("replaceManifestVersion refuses a manifest with no version key", () => {
    assert.throws(() => replaceManifestVersion('{\n  "name": "x"\n}\n', "0.2.0"), /exactly one/);
});
```

- [ ] **Step 2: Run them to verify they fail**

Run: `node --test Tools/upm-release.test.mjs`
Expected: the import fails — `The requested module './upm-release.mjs' does not provide an export named 'unreleasedRange'`.

Note: `upm-release.mjs` runs its dispatch at import time. Guard it before writing the helpers, or the test run will execute `usage()` and exit. Step 3 handles this.

- [ ] **Step 3: Make the script importable**

At the very end of `Tools/upm-release.mjs`, wrap the existing dispatch so it only runs when the file is the entry point. Replace:

```js
const { command, flags } = parseArgs(process.argv.slice(2));
if (!command) process.exit(usage());

const packages = discoverPackages();
switch (command) {
```

with:

```js
// Importable for tests: the dispatch runs only when this file is the entry
// point, not when Tools/upm-release.test.mjs imports the helpers above.
const invokedDirectly = process.argv[1] !== undefined
    && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);

if (invokedDirectly) main();

function main() {
    const { command, flags } = parseArgs(process.argv.slice(2));
    if (!command) process.exit(usage());

    const packages = discoverPackages();
    switch (command) {
```

and close `main()` after the `switch`'s `default` case. `function main()` is hoisted, so calling it above its declaration is fine.

Run: `node Tools/upm-release.mjs validate`
Expected: unchanged behaviour — 17/17 packages valid.

- [ ] **Step 4: Write the helpers**

Add a new section to `Tools/upm-release.mjs`, after the `validate` section and before `pack`:

```js
// ---------------------------------------------------------------- prepare

const CHANGELOG = "CHANGELOG.md";
const H2 = /^##\s/;
const H2_VERSION = /^##\s+\[([^\]]+)\]/;
const UNRELEASED = /^unreleased$/i;
const SUB_HEADING = /^#{3,}\s+(.+?)\s*$/;
const PLAIN_SEMVER = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/;

// Keep a Changelog's six sections, mapped to what each implies about the API.
// `Removed` is the only breaking one; while major is 0 it lands on the minor
// all the same, which is the whole point of the 0.x rule below.
const SECTION_LEVELS = {
    added: "feature",
    changed: "feature",
    deprecated: "feature",
    removed: "breaking",
    fixed: "fix",
    security: "fix",
};
const LEVEL_RANK = { fix: 1, feature: 2, breaking: 3 };

/** The `## [Unreleased]` heading's line index and the index past its body. */
export function unreleasedRange(lines) {
    const start = lines.findIndex((line) => H2_VERSION.test(line) && UNRELEASED.test(line.match(H2_VERSION)[1]));
    if (start === -1) return null;
    const rest = lines.slice(start + 1);
    const offset = rest.findIndex((line) => H2.test(line));
    return { start, end: offset === -1 ? lines.length : start + 1 + offset };
}

/**
 * The body's entry lines. Identical to `entriesOf` in changelog-check.mjs —
 * "does this section have entries" must mean one thing in both scripts, or a
 * pull request can pass the check and then be skipped by the release.
 */
export function unreleasedEntries(lines) {
    return lines.map((line) => line.trim()).filter((line) => line !== "" && !SUB_HEADING.test(line));
}

/** The `###` sub-headings with at least one line under them, in file order. */
export function populatedSubsections(lines) {
    const found = [];
    let current = null;
    for (const line of lines) {
        const heading = line.match(SUB_HEADING);
        if (heading) {
            current = { name: heading[1], entries: 0 };
            found.push(current);
            continue;
        }
        if (current !== null && line.trim() !== "") current.entries += 1;
    }
    return found.filter((section) => section.entries > 0).map((section) => section.name);
}

/** The highest level the sub-headings imply, or null if none is recognised. */
export function bumpLevel(names) {
    let best = null;
    for (const name of names) {
        const level = SECTION_LEVELS[name.trim().toLowerCase()];
        if (level === undefined) continue;
        if (best === null || LEVEL_RANK[level] > LEVEL_RANK[best]) best = level;
    }
    return best;
}

function parts(version) {
    const match = PLAIN_SEMVER.exec(version);
    if (match === null) throw new Error(`version \`${version}\` is not plain X.Y.Z semver`);
    return match.slice(1, 4).map(Number);
}

/**
 * 0.x-aware. Below 1.0.0 the major is reserved, so a breaking change bumps the
 * minor exactly as a feature does — the two are indistinguishable to a consumer
 * pinning `0.1.0`, which is precisely what 0.x means.
 */
export function nextVersion(version, level) {
    const [major, minor, patch] = parts(version);
    if (major === 0) return level === "fix" ? `0.${minor}.${patch + 1}` : `0.${minor + 1}.0`;
    if (level === "breaking") return `${major + 1}.0.0`;
    if (level === "feature") return `${major}.${minor + 1}.0`;
    return `${major}.${minor}.${patch + 1}`;
}

/** `--bump pkg=minor` asked for a part, so no 0.x remapping is applied. */
export function explicitBump(version, part) {
    const [major, minor, patch] = parts(version);
    if (part === "major") return `${major + 1}.0.0`;
    if (part === "minor") return `${major}.${minor + 1}.0`;
    if (part === "patch") return `${major}.${minor}.${patch + 1}`;
    throw new Error(`unknown bump part \`${part}\`, expected major, minor, or patch`);
}

/** Renames `## [Unreleased]` to `## [X.Y.Z] - DATE`, leaving nothing behind. */
export function releaseChangelog(text, version, date) {
    const eol = text.includes("\r\n") ? "\r\n" : "\n";
    const lines = text.split(/\r?\n/);
    const range = unreleasedRange(lines);
    if (range === null) throw new Error("no `## [Unreleased]` heading");
    lines[range.start] = `## [${version}] - ${date}`;
    return lines.join(eol);
}

/**
 * A targeted single-line replacement, not parse-and-reserialise: key order,
 * indentation, and the trailing newline all survive, so the diff is one line.
 * More than one `"version"` key means the file is not shaped as expected —
 * refuse rather than rewrite the wrong one.
 */
export function replaceManifestVersion(text, version) {
    const pattern = /^([ \t]*"version"[ \t]*:[ \t]*)"[^"]*"/gm;
    const matches = text.match(pattern) ?? [];
    if (matches.length !== 1) {
        throw new Error(`package.json must contain exactly one \`"version"\` line, found ${matches.length}`);
    }
    return text.replace(pattern, `$1"${version}"`);
}
```

- [ ] **Step 5: Run the tests**

Run: `node --test Tools/upm-release.test.mjs`
Expected: 17 tests, 0 failures.

- [ ] **Step 6: Add the `release-script-tests` job**

In `.github/workflows/release.yml`, after `promotion-guard` and before `validate`:

```yaml
  # The release tooling's own tests. `prepare` rewrites CHANGELOGs and version
  # fields, and a release is permanent, so its arithmetic is not something to
  # find out about afterwards. Named for uniqueness: changelog.yml owns `test`.
  release-script-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-node@v7
        with:
          node-version: 22
      - run: node --test Tools/upm-release.test.mjs Tools/promotion-check.test.mjs
```

- [ ] **Step 7: Run both suites the way CI will**

Run: `node --test Tools/upm-release.test.mjs Tools/promotion-check.test.mjs`
Expected: 26 tests, 0 failures.

- [ ] **Step 8: Commit**

```bash
git add Tools/upm-release.mjs Tools/upm-release.test.mjs .github/workflows/release.yml
git commit -m "$(cat <<'MSG'
feat(upm-release): add prepare's changelog and version helpers

Parses the [Unreleased] section, derives a 0.x-aware bump level from
its ### sub-headings, and rewrites the heading and the manifest version
by targeted replacement so the diff stays one line each. Pure functions
with tests; the subcommand that calls them comes next.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01A1sHxtmP44SQqLNoKikkGE
MSG
)"
```

---

### Task 6: The `prepare` subcommand

**Files:**
- Modify: `Tools/upm-release.mjs` — `planPrepare`, `cmdPrepare`, `--bump` parsing, `usage()`, dispatch
- Create: `Tools/upm-release.prepare.test.mjs`
- Modify: `.github/workflows/release.yml` — add the new test file to `release-script-tests`
- Modify: `.agents/AGENTS.md` — the release tooling section

**Interfaces:**
- Consumes: `unreleasedRange`, `unreleasedEntries`, `populatedSubsections`, `bumpLevel`, `nextVersion`, `explicitBump`, `releaseChangelog`, `replaceManifestVersion` from Task 5; the existing `discoverPackages`, `publishable`, `applyOnly`, `cmdValidate`, `git`, `fail`.
- Produces:
  - `planPrepare(packages, { bumps, date }) → { plan, errors }` where a plan entry is `{ folder, name, from, to, level, reason }` and `reason` reads like `"minor: Added, Changed"`.
  - The CLI: `node Tools/upm-release.mjs prepare [--dry-run] [--json] [--only <pkg>] [--bump <pkg>=<part>] [--date YYYY-MM-DD] [--allow-dirty] [--allow-branch]`.

Usage, as it will read in the docs:

```powershell
node Tools/upm-release.mjs prepare --dry-run
node Tools/upm-release.mjs prepare
node Tools/upm-release.mjs prepare --bump com.arman.package-basics=patch
node Tools/upm-release.mjs prepare --only "UI Management"
```

`prepare` is a local command that edits files. **It does not commit, push, tag, or open a pull request.**

Guards, inverting `tag`'s: `prepare` refuses to run **on** `master` (`--allow-branch` overrides) and refuses to run on a dirty working tree (`--allow-dirty` overrides). A dirty tree matters because the whole point of the command is that its diff is reviewable.

It deliberately does **not** touch `com.arman.*` dependency ranges. A dependent pinning `0.1.0` stays valid because `validate` accepts a dependency at a version that is either current or already tagged, and `0.1.0` is tagged.

- [ ] **Step 1: Write the failing tests**

Create `Tools/upm-release.prepare.test.mjs`:

```js
// End-to-end tests for `node Tools/upm-release.mjs prepare`.
//
// Each test builds a throwaway repo with a Packages/ layout and runs the real
// script inside it as a subprocess, the same way a developer does.
//
//     node --test Tools/upm-release.prepare.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const SCRIPT = path.join(HERE, "upm-release.mjs");

const PREAMBLE = "# Changelog\r\n\r\n";
const RELEASED = "## [0.1.0] - 2026-08-29\r\n\r\nInitial release.\r\n";

function changelog(body) {
    return body === null ? PREAMBLE + RELEASED : `${PREAMBLE}## [Unreleased]\r\n\r\n${body}\r\n${RELEASED}`;
}

function manifest(name, extra = {}) {
    return JSON.stringify(
        {
            name,
            version: "0.1.0",
            displayName: name,
            description: "A real description.",
            unity: "6000.0",
            license: "MIT",
            ...extra,
        },
        null,
        2,
    ).replace(/\n/g, "\r\n");
}

function git(cwd, ...args) {
    const result = spawnSync("git", args, { cwd, encoding: "utf8" });
    if (result.status !== 0) throw new Error(`git ${args.join(" ")} failed: ${result.stderr || result.stdout}`);
    return result.stdout.trim();
}

/** A repo with the script in Tools/ and one package per entry in `packages`. */
function makeRepo(t, packages) {
    const repo = fs.mkdtempSync(path.join(os.tmpdir(), "upm-prepare-"));
    t.after(() => fs.rmSync(repo, { recursive: true, force: true }));

    fs.mkdirSync(path.join(repo, "Tools"), { recursive: true });
    fs.copyFileSync(SCRIPT, path.join(repo, "Tools", "upm-release.mjs"));

    for (const [folder, files] of Object.entries(packages)) {
        const dir = path.join(repo, "Packages", folder);
        fs.mkdirSync(dir, { recursive: true });
        for (const [name, contents] of Object.entries(files)) {
            fs.writeFileSync(path.join(dir, name), contents);
        }
    }

    git(repo, "init", "--initial-branch", "dev");
    git(repo, "config", "user.email", "test@example.com");
    git(repo, "config", "user.name", "Test");
    git(repo, "config", "commit.gpgsign", "false");
    git(repo, "add", "-A");
    git(repo, "commit", "-m", "base");
    return repo;
}

function prepare(repo, args = []) {
    const result = spawnSync(process.execPath, ["Tools/upm-release.mjs", "prepare", ...args], {
        cwd: repo,
        encoding: "utf8",
    });
    return { status: result.status, stdout: result.stdout, stderr: result.stderr };
}

function json(repo, args = []) {
    const result = prepare(repo, ["--json", "--date", "2026-09-02", ...args]);
    try {
        return { status: result.status, report: JSON.parse(result.stdout) };
    } catch {
        throw new Error(`non-JSON output (exit ${result.status}):\n${result.stdout}\n${result.stderr}`);
    }
}

function read(repo, folder, name) {
    return fs.readFileSync(path.join(repo, "Packages", folder, name), "utf8");
}

const ALPHA = {
    "package.json": manifest("com.arman.alpha"),
    "CHANGELOG.md": changelog("### Changed\r\n\r\n- Flattened the folders.\r\n"),
    "LICENSE.md": "MIT\r\n",
};

test("a Changed section takes 0.1.0 to 0.2.0", (t) => {
    const repo = makeRepo(t, { Alpha: ALPHA });
    const { status, report } = json(repo);
    assert.equal(status, 0);
    assert.deepEqual(
        report.plan.map((p) => [p.name, p.from, p.to, p.level]),
        [["com.arman.alpha", "0.1.0", "0.2.0", "feature"]],
    );
    assert.match(report.plan[0].reason, /Changed/);
});

test("the changelog heading is renamed and nothing is left in its place", (t) => {
    const repo = makeRepo(t, { Alpha: ALPHA });
    assert.equal(json(repo).status, 0);
    const text = read(repo, "Alpha", "CHANGELOG.md");
    assert.ok(text.includes("## [0.2.0] - 2026-09-02"));
    assert.ok(!text.includes("[Unreleased]"));
    assert.ok(text.includes("- Flattened the folders."));
    assert.ok(text.includes("\r\n"));
});

test("the manifest version is rewritten", (t) => {
    const repo = makeRepo(t, { Alpha: ALPHA });
    assert.equal(json(repo).status, 0);
    assert.match(read(repo, "Alpha", "package.json"), /"version": "0\.2\.0"/);
});

test("a Fixed-only section takes a patch", (t) => {
    const repo = makeRepo(t, {
        Alpha: { ...ALPHA, "CHANGELOG.md": changelog("### Fixed\r\n\r\n- A null check.\r\n") },
    });
    const { report } = json(repo);
    assert.equal(report.plan[0].to, "0.1.1");
});

test("a package with no Unreleased heading is skipped", (t) => {
    const repo = makeRepo(t, { Alpha: { ...ALPHA, "CHANGELOG.md": changelog(null) } });
    const { status, report } = json(repo);
    assert.equal(status, 0);
    assert.deepEqual(report.plan, []);
});

test("a package with an empty Unreleased heading is skipped, untouched", (t) => {
    const before = changelog("");
    const repo = makeRepo(t, { Alpha: { ...ALPHA, "CHANGELOG.md": before } });
    const { status, report } = json(repo);
    assert.equal(status, 0);
    assert.deepEqual(report.plan, []);
    assert.equal(read(repo, "Alpha", "CHANGELOG.md"), before);
});

test("entries under no recognised sub-heading are an error, not a guess", (t) => {
    const repo = makeRepo(t, {
        Alpha: { ...ALPHA, "CHANGELOG.md": changelog("- A loose bullet.\r\n") },
    });
    const { status, report } = json(repo);
    assert.equal(status, 1);
    assert.equal(report.plan.length, 0);
    assert.match(report.errors.join(" "), /com\.arman\.alpha/);
});

test("--dry-run reports the plan and writes nothing", (t) => {
    const before = changelog("### Added\r\n\r\n- A thing.\r\n");
    const repo = makeRepo(t, { Alpha: { ...ALPHA, "CHANGELOG.md": before } });
    const { status, report } = json(repo, ["--dry-run"]);
    assert.equal(status, 0);
    assert.equal(report.plan.length, 1);
    assert.equal(read(repo, "Alpha", "CHANGELOG.md"), before);
    assert.match(read(repo, "Alpha", "package.json"), /"version": "0\.1\.0"/);
});

test("--bump overrides the derived level", (t) => {
    const repo = makeRepo(t, { Alpha: ALPHA });
    const { report } = json(repo, ["--bump", "com.arman.alpha=patch"]);
    assert.equal(report.plan[0].to, "0.1.1");
    assert.match(report.plan[0].reason, /requested/);
});

test("--bump accepts a folder name and rejects an unknown one", (t) => {
    const repo = makeRepo(t, { Alpha: ALPHA });
    assert.equal(json(repo, ["--bump", "Alpha=major"]).report.plan[0].to, "1.0.0");
    assert.equal(prepare(repo, ["--bump", "Nope=major"]).status, 1);
    assert.equal(prepare(repo, ["--bump", "Alpha=sideways"]).status, 1);
});

test("--only limits the run", (t) => {
    const repo = makeRepo(t, {
        Alpha: ALPHA,
        Beta: {
            "package.json": manifest("com.arman.beta"),
            "CHANGELOG.md": changelog("### Added\r\n\r\n- A thing.\r\n"),
            "LICENSE.md": "MIT\r\n",
        },
    });
    const { report } = json(repo, ["--only", "Alpha"]);
    assert.deepEqual(report.plan.map((p) => p.folder), ["Alpha"]);
    assert.match(read(repo, "Beta", "package.json"), /"version": "0\.1\.0"/);
});

test("a private package is never prepared", (t) => {
    const repo = makeRepo(t, {
        Template: {
            "package.json": manifest("com.arman.template", { private: true }),
            "CHANGELOG.md": changelog("### Added\r\n\r\n- A thing.\r\n"),
            "LICENSE.md": "MIT\r\n",
        },
    });
    const { status, report } = json(repo);
    assert.equal(status, 0);
    assert.deepEqual(report.plan, []);
});

test("nothing to prepare exits 0", (t) => {
    const repo = makeRepo(t, { Alpha: { ...ALPHA, "CHANGELOG.md": changelog(null) } });
    const result = prepare(repo, ["--date", "2026-09-02"]);
    assert.equal(result.status, 0);
    assert.match(result.stdout, /nothing to prepare/i);
});

test("a dirty tree is refused unless --allow-dirty", (t) => {
    const repo = makeRepo(t, { Alpha: ALPHA });
    fs.writeFileSync(path.join(repo, "Packages", "Alpha", "README.md"), "stray\r\n");
    assert.equal(prepare(repo, ["--date", "2026-09-02"]).status, 1);
    assert.equal(prepare(repo, ["--date", "2026-09-02", "--allow-dirty", "--dry-run"]).status, 0);
});

test("running on master is refused unless --allow-branch", (t) => {
    const repo = makeRepo(t, { Alpha: ALPHA });
    git(repo, "checkout", "-b", "master");
    const refused = prepare(repo, ["--date", "2026-09-02"]);
    assert.equal(refused.status, 1);
    assert.match(refused.stderr, /master/);
    assert.equal(prepare(repo, ["--date", "2026-09-02", "--allow-branch", "--dry-run"]).status, 0);
});
```

- [ ] **Step 2: Run them to verify they fail**

Run: `node --test Tools/upm-release.prepare.test.mjs`
Expected: every test fails with `unknown command \`prepare\`` (exit 2, non-JSON output).

- [ ] **Step 3: Write `planPrepare`**

Add to the prepare section of `Tools/upm-release.mjs`, after `replaceManifestVersion`:

```js
/**
 * Works out what each package's next version is, without writing anything.
 * Returns the plan and the packages that could not be planned; a package with
 * entries under no recognised `###` heading is an error rather than a guess or
 * a silent skip, because the alternative is releasing the wrong number.
 */
export function planPrepare(packages, { bumps = new Map() } = {}) {
    const plan = [];
    const errors = [];

    for (const pkg of packages) {
        const file = path.join(pkg.dir, CHANGELOG);
        if (!fs.existsSync(file)) {
            errors.push(`${pkg.folder}: no ${CHANGELOG}`);
            continue;
        }
        const text = fs.readFileSync(file, "utf8");
        const lines = text.split(/\r?\n/);
        const range = unreleasedRange(lines);
        if (range === null) continue; // No heading: nothing is waiting to ship.

        const body = lines.slice(range.start + 1, range.end);
        if (unreleasedEntries(body).length === 0) continue; // Empty: nothing to ship.

        const requested = bumps.get(pkg.name) ?? bumps.get(pkg.folder);
        const sections = populatedSubsections(body);
        const level = requested ? null : bumpLevel(sections);

        if (!requested && level === null) {
            errors.push(
                `${pkg.name ?? pkg.folder}: has entries under \`## [Unreleased]\` but none under a recognised \`###\` heading (Added, Changed, Deprecated, Removed, Fixed, Security). File them, or pass --bump ${pkg.name ?? pkg.folder}=<major|minor|patch>.`,
            );
            continue;
        }

        let to;
        try {
            to = requested ? explicitBump(pkg.version, requested) : nextVersion(pkg.version, level);
        } catch (error) {
            errors.push(`${pkg.name ?? pkg.folder}: ${error.message}`);
            continue;
        }

        plan.push({
            folder: pkg.folder,
            name: pkg.name,
            from: pkg.version,
            to,
            level: requested ?? level,
            reason: requested ? `${requested}: requested with --bump` : `${level}: ${sections.join(", ")}`,
        });
    }

    return { plan, errors };
}
```

- [ ] **Step 4: Write `cmdPrepare`**

```js
function cmdPrepare(packages, flags) {
    const branch = git("rev-parse", "--abbrev-ref", "HEAD").out;
    if (branch === RELEASE_BRANCH && !flags["allow-branch"]) {
        return fail(`on branch \`${branch}\`. Prepare a release on a branch off \`dev\`, not on \`${RELEASE_BRANCH}\`. Pass --allow-branch to override.`);
    }
    if (git("status", "--porcelain").out && !flags["allow-dirty"] && !flags["dry-run"]) {
        return fail("working tree is dirty. Prepare a clean tree so the release diff is reviewable, or pass --allow-dirty.");
    }

    let bumps;
    try {
        bumps = parseBumps(flags.bump, packages);
    } catch (error) {
        return fail(error.message);
    }

    const date = flags.date ?? new Date().toISOString().slice(0, 10);
    if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) return fail(`--date \`${date}\` is not YYYY-MM-DD`);

    const selected = applyOnly(publishable(packages), flags.only);
    const { plan, errors } = planPrepare(selected, { bumps });

    const report = { command: "prepare", date, dryRun: flags["dry-run"] === true, plan, errors };

    if (errors.length === 0 && plan.length > 0 && !flags["dry-run"]) {
        for (const entry of plan) {
            const dir = path.join(PACKAGES_DIR, entry.folder);
            const changelogPath = path.join(dir, CHANGELOG);
            const manifestPath = path.join(dir, "package.json");
            fs.writeFileSync(
                changelogPath,
                releaseChangelog(fs.readFileSync(changelogPath, "utf8"), entry.to, date),
            );
            fs.writeFileSync(
                manifestPath,
                replaceManifestVersion(fs.readFileSync(manifestPath, "utf8"), entry.to),
            );
        }
    }

    if (flags.json) {
        console.log(JSON.stringify(report, null, 2));
    } else {
        for (const error of errors) console.error(`error: ${error}`);
        if (plan.length === 0) {
            console.log("nothing to prepare — no package has entries under `## [Unreleased]`.");
        } else {
            for (const entry of plan) {
                console.log(`  ${entry.name ?? entry.folder}  ${entry.from} → ${entry.to}   (${entry.reason})`);
            }
            console.log(
                `\n${plan.length} package(s) ${flags["dry-run"] ? "would be prepared" : "prepared"} for ${date}.`,
            );
            if (!flags["dry-run"]) {
                console.log("Review the diff, commit it, and open a pull request into `dev`.");
            }
        }
    }

    if (errors.length > 0) return 1;

    // The written state has to survive the same checks CI runs, before it is
    // ever committed. Re-discover: the manifests on disk have just changed.
    if (plan.length > 0 && !flags["dry-run"]) {
        const only = plan.map((entry) => entry.folder);
        if (cmdValidate(discoverPackages(), { only }) !== 0) {
            return fail("the prepared packages do not validate. Inspect the diff before committing.");
        }
    }
    return 0;
}
```

Note `PACKAGES_DIR` is already an absolute path built from `ROOT`, so `path.join(PACKAGES_DIR, entry.folder)` is absolute — folder names with spaces need no special handling here, only on the command line.

- [ ] **Step 5: Parse `--bump`**

Add next to `applyOnly`:

```js
/** `--bump <package>=<major|minor|patch>`, repeatable, id or folder name. */
export function parseBumps(raw, packages) {
    const bumps = new Map();
    if (!raw) return bumps;
    for (const item of Array.isArray(raw) ? raw : [raw]) {
        const at = item.lastIndexOf("=");
        if (at === -1) throw new Error(`--bump \`${item}\` is not <package>=<major|minor|patch>`);
        const target = item.slice(0, at).trim();
        const part = item.slice(at + 1).trim().toLowerCase();
        if (!["major", "minor", "patch"].includes(part)) {
            throw new Error(`--bump \`${item}\`: \`${part}\` is not major, minor, or patch`);
        }
        if (!packages.some((p) => p.name === target || p.folder === target)) {
            throw new Error(`--bump matched no package: ${target}`);
        }
        bumps.set(target, part);
    }
    return bumps;
}
```

In `parseArgs`, make `--bump` and `--date` value-taking, `--bump` repeatable:

```js
        if (key === "out" || key === "date") flags[key] = argv[++i];
        else if (key === "only") (flags.only ??= []).push(argv[++i]);
        else if (key === "bump") (flags.bump ??= []).push(argv[++i]);
        else flags[key] = true;
```

- [ ] **Step 6: Wire the dispatch and usage**

In `main()`'s `switch`, before `default`:

```js
        case "prepare":
            process.exit(cmdPrepare(packages, flags));
```

In `usage()`, add to the command list and options:

```
  prepare             turn each [Unreleased] section into a new version
```
```
  --bump <pkg>=<part> prepare: force major|minor|patch for one package; repeatable
  --date <YYYY-MM-DD> prepare: the date written into the version heading
```

and extend the existing `--dry-run`, `--allow-dirty`, and `--allow-branch` descriptions to mention `prepare` alongside `tag`.

- [ ] **Step 7: Run the tests**

Run: `node --test Tools/upm-release.prepare.test.mjs`
Expected: 15 tests, 0 failures.

- [ ] **Step 8: Add the file to CI and run every suite**

In `release-script-tests`, extend the run line:

```yaml
      - run: node --test Tools/upm-release.test.mjs Tools/upm-release.prepare.test.mjs Tools/promotion-check.test.mjs
```

Run: `node --test Tools/upm-release.test.mjs Tools/upm-release.prepare.test.mjs Tools/promotion-check.test.mjs Tools/changelog-check.test.mjs`
Expected: 78 tests, 0 failures.

- [ ] **Step 9: Dry-run against the real repo**

```bash
node Tools/upm-release.mjs prepare --dry-run
git status --porcelain
```

Expected: the five packages with entries listed as `0.1.0 → 0.2.0` with reasons, and an empty `git status` — a dry run writes nothing.

- [ ] **Step 10: Document `prepare` in AGENTS.md**

In the release tooling section, add to the command block:

```powershell
node Tools/upm-release.mjs prepare --dry-run    # what would each [Unreleased] section become?
node Tools/upm-release.mjs prepare              # rename the headings, bump the versions
```

and after the `--only` paragraph:

```markdown
`prepare` turns every package's `## [Unreleased]` section into a version. Per package: no heading, or a heading with no entries, means skip; otherwise the `###` sub-headings with bullets under them decide the level — `Removed` is breaking, `Added`/`Changed`/`Deprecated` are features, `Fixed`/`Security` are fixes, highest wins — and **while the major is `0`, breaking and feature both land on the minor**. The heading is renamed to `## [X.Y.Z] - YYYY-MM-DD` with nothing left in its place, `package.json`'s `version` line is rewritten in place, and `validate` re-runs over the packages it touched.

`--bump <package>=<major|minor|patch>` overrides the derived level for one package and is repeatable; entries filed under no recognised `###` heading are an error rather than a guess. `prepare` refuses to run **on** `master` and refuses a dirty tree (`--allow-branch`, `--allow-dirty`), the inverse of `tag`'s guards. **It edits files and stops there** — it does not commit, push, tag, or open a pull request. It also leaves `com.arman.*` dependency ranges alone: a dependent pinning `0.1.0` stays valid, because `validate` accepts a dependency at a version that is either current or already tagged.
```

- [ ] **Step 11: Commit**

```bash
git add Tools/upm-release.mjs Tools/upm-release.prepare.test.mjs .github/workflows/release.yml .agents/AGENTS.md
git commit -m "$(cat <<'MSG'
feat(upm-release): add the prepare subcommand

Turns each package's [Unreleased] section into a new version, deriving
the bump from the section's ### headings under a 0.x-aware rule, and
re-runs validate over what it touched. Refuses to run on master or on a
dirty tree; edits files and nothing else.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01A1sHxtmP44SQqLNoKikkGE
MSG
)"
```

---

### Task 7: The first release

**Files:**
- Modify: `Packages/*/CHANGELOG.md` and `Packages/*/package.json` for the five packages with unreleased work
- No source changes.

**Interfaces:**
- Consumes: `prepare` from Task 6, `promotion-guard` from Task 3, the ruleset from Task 4.
- Produces: five packages at `0.2.0` — `com.arman.in-game-message-logging`, `com.arman.package-basics`, `com.arman.persistent-data-management`, `com.arman.unity-utilities`, `com.arman.update-management`. The other twelve stay at `0.1.0` and tag nothing; `tag` is idempotent.

All five are minor bumps. Three of them describe breaking namespace and folder flattening, which still lands on the minor under the 0.x rule — that is what `0.x` means.

**⚠️ The merge of the release PR is the publish.** There is no dry run in front of it and no undo behind it. Stop at Step 6 and get the user's explicit confirmation before merging.

- [ ] **Step 1: Confirm every prior task has merged into `dev`**

```bash
git switch dev && git pull
git log --oneline -12
node --test Tools/upm-release.test.mjs Tools/upm-release.prepare.test.mjs Tools/promotion-check.test.mjs Tools/changelog-check.test.mjs
node Tools/upm-release.mjs validate
```

Expected: Tasks 1–6 in the log, all tests passing, 17/17 valid.

- [ ] **Step 2: Prepare**

```bash
git switch -c release/2026-09-02 dev
node Tools/upm-release.mjs prepare --dry-run
node Tools/upm-release.mjs prepare
```

Expected: five packages `0.1.0 → 0.2.0`, then the same five written and validated.

- [ ] **Step 3: Read the diff before trusting it**

```bash
git diff --stat
git diff -- "Packages/PackageBasics/CHANGELOG.md" "Packages/PackageBasics/package.json"
```

Expected: exactly two files changed per package — the CHANGELOG's `## [Unreleased]` line becoming `## [0.2.0] - 2026-09-02`, and one `"version"` line. No entry text moved. No line-ending churn.

- [ ] **Step 4: Commit and open the pull request into `dev`**

```bash
git add -A
git commit -m "$(cat <<'MSG'
chore(release): prepare 0.2.0 for five packages

Renames each [Unreleased] section to 0.2.0 and bumps the matching
version fields. All five are minor: while the major is 0, a breaking
change lands on the minor too.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01A1sHxtmP44SQqLNoKikkGE
MSG
)"
git push -u origin release/2026-09-02
gh pr create --base dev --title "chore(release): prepare 0.2.0 for five packages" --body "..."
```

The version bumps land on `dev` normally, like any other change. Wait for `validate`, `pack`, `check`, `test`, `release-script-tests`, and `promotion-guard` to go green, then merge.

- [ ] **Step 5: Open the release pull request**

```bash
git switch dev && git pull
gh pr create --base master --head dev --title "release: 0.2.0 for five packages" --body "..."
```

Expected: `promotion-guard` passes (head is `dev`), along with `validate` and `pack`. Confirm the ruleset lists all three as required on the PR page.

- [ ] **Step 6: STOP. Ask the user before merging.**

Report the PR URL, the five package/version pairs, and this sentence: merging publishes five permanent versions to OpenUPM and cannot be undone. Do not merge without an explicit yes.

- [ ] **Step 7: After the user's confirmation, merge and watch the tags**

```bash
gh pr merge --merge
gh run watch
git fetch --tags && git tag --list "com.arman.*/0.2.0"
```

Expected: the `tag` job creates and pushes five `<name>/0.2.0` tags. OpenUPM builds them within 15–30 minutes.

- [ ] **Step 8: Verify the guard actually guards**

With the release merged, confirm the new rule refuses the thing it exists to refuse:

```bash
node Tools/promotion-check.mjs --event pull_request --base master --head feat/anything
```

Expected: exit 1, with a message naming `feat/anything`, `master`, and `dev`.

---

## Notes for the executor

- **Task order matters.** Task 1 must follow the flattening merge and precede Task 2 (the rule would otherwise blame thirteen innocent packages). Task 4 must follow Task 3's merge (GitHub only knows a check's name once it has reported). Task 7 must follow everything.
- **Each of Tasks 1–6 is its own branch off `dev` and its own PR into `dev`.** None of them goes near `master`.
- Run `node --test Tools/*.test.mjs` before every commit. All four suites are fast and none of them needs a network.
- If a test in this plan disagrees with the implementation, the test is the specification — fix the implementation, and if the test itself is wrong, say so explicitly rather than quietly relaxing it.
