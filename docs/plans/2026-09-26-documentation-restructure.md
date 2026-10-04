# Documentation Restructure Implementation Plan

**Status:** Implemented

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the 57 KB `AGENTS.md` into a ~10 KB core that routes to `.agents/rules/`, four authored skills and `docs/INDEX.md`, with a CI check that keeps every link, anchor, mirror and heading honest.

**Architecture:** A dependency-free Node checker lands first, test-driven, so every later task is measured by it. A one-off carve script moves `AGENTS.md`'s sections verbatim into eight rules files and writes the heading manifest; the four skills, the new core and the pointer fixes follow; the last task wires the checker into `npm` and the `tooling-tests` job.

**Tech Stack:** Node 22 (`.nvmrc`), ES modules, `node:test`, Prettier 3.9.6, GitHub Actions, `gh`.

**Spec:** [`docs/specs/2026-09-26-documentation-restructure-design.md`](../specs/2026-09-26-documentation-restructure-design.md)

## Global Constraints

- Work in the worktree `.claude/worktrees/agent-docs-restructure` on branch `docs/agent-docs-restructure`, cut from `origin/dev`. `<wt>` below is that worktree's absolute path.
- Run `npm ci` in `<wt>` once before Task 1: a `Tools/upm-release.prepare.test.mjs` test runs the repo's own Prettier and fails without `node_modules/`.
- Run git as `git -C <wt> …`, never after a `cd`. Never push to `dev` or `master`, never force-push, never merge. The last step opens a pull request into `dev` with `gh` and stops.
- No `.unity`, `.prefab`, `.asset` or `.meta` file is touched.
- Carved sections move **verbatim**. The only changes on the way are the ones the carve script makes: relative links re-based, in-file anchors pointed at their new file, the asset-editing bullet swapped for a pointer, and five lines of the repo-layout tree brought up to date.
- `Tools/docs-check.mjs` reads only files git knows about (`git ls-files`, which includes staged files). Stage new files with `git -C <wt> add` before running it, or it passes over them silently.
- The vendored skills (`unity-cli`, `unity-package-management`, `lifeblood-mcp`) are not edited. No `opencode.json`, `.mcp.json` or `.claude/settings.json` is added.
- Every change under `.agents/skills/` is copied byte for byte to `.claude/skills/` in the same commit.
- A link from a skill to a rules file is written `../../../.agents/rules/<file>.md`, which resolves from both copies of the skill.
- `AGENTS.md` stays between 6,000 and 12,000 bytes, measured with LF line endings.
- Tool code is dependency-free Node, 4-space indent, `printWidth` 100 — Prettier enforces it.
- Before each commit, run `npx prettier --write` on the files the task touched, then `npx prettier --check .`. (`npm run format:check` also runs CSharpier; no C# changes here.)
- Commit messages are Conventional Commits (`feat(tools): …`, `docs: …`, `ci: …`) and end with the attribution trailers the session's instructions give.
- Every document describes the present, never the change — `.agents/rules/documentation-voice.md` once it exists; `AGENTS.md` § Writing documentation until then.

## Review Focus

Five inputs the spec implies but its happy path does not exercise. Task 1 pins each with a test.

1. **A link with a query string** (`./a.md?plain=1`, `./a.md?plain=1#part`) resolves to `./a.md`, not to a file named `a.md?plain=1`.
2. **A heading with closing hashes** (`## Notes ##`) offers the anchor `notes`, and a manifest row `Notes` matches it.
3. **A manifest checked out with CRLF line endings**, as `core.autocrlf=true` produces on Windows, reads exactly like an LF one.
4. **An anchor in a different case or percent-encoded** (`#Unity-meta-files`, `#%C3%BCber`) matches the heading GitHub would link to.
5. **`--root` at a directory that is not a git repository** exits 2 with a one-line `docs-check:` message, not 1 with a stack trace that reads like a finding.

---

## File map

| File                                                                                                                                                                                         | Change                                                                                    |
| -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `Tools/docs-check.mjs`, `Tools/docs-check.test.mjs`                                                                                                                                          | **Create** (Task 1). The five checks and their tests.                                     |
| `docs/INDEX.md`                                                                                                                                                                              | **Create** (Task 2). Every spec and plan, with its status.                                |
| `docs/specs/*.md`, `docs/plans/*.md`                                                                                                                                                         | **Modify** (Tasks 2, 5, 6). The `**Status:**` line; four broken links and one anchor.     |
| `.agents/rules/*.md`, `.agents/rules/MANIFEST.tsv`                                                                                                                                           | **Create** (Task 3). Eight rules files carved from `AGENTS.md`, and the heading manifest. |
| `.agents/skills/{releasing-packages,adding-a-package,unity-asset-editing,feature-worktree}/SKILL.md` and their `.claude/skills/` copies                                                      | **Create** (Task 4).                                                                      |
| `.agents/skills/THIRD_PARTY_SKILLS.md`, `.agents/skills/sharplens-mcp/SKILL.md` and their copies                                                                                             | **Modify** (Task 4). New rows; two pointers.                                              |
| `AGENTS.md`                                                                                                                                                                                  | **Replace** (Task 5). The core.                                                           |
| `CLAUDE.md`, `QWEN.md`, `README.md`, `.github/rulesets/README.md`, `.editorconfig`, `.gitignore`, `.github/workflows/changelog.yml`, `.github/workflows/format.yml`, `Tools/upm-release.mjs` | **Modify** (Task 5). Pointers into the new files.                                         |
| `package.json`, `.github/workflows/tests.yml`                                                                                                                                                | **Modify** (Task 6). `check:docs`, and two steps in `tooling-tests`.                      |

---

### Task 1: The docs checker

**Files:**

- Create: `Tools/docs-check.test.mjs`
- Create: `Tools/docs-check.mjs`

**Interfaces:**

- Consumes: nothing.
- Produces: `node Tools/docs-check.mjs [--root <dir>] [--json]` — exit `0` clean, `1` findings, `2` usage error or no git repository. JSON report `{ ok: boolean, findings: { check: "link"|"anchor"|"mirror"|"heading"|"budget", file: string, line?: number, message: string }[] }`. Exports `slug(heading: string): string` (Task 3's carve script imports it), `headings(text)`, `anchors(text): Set<string>`, `links(text): {target, line}[]`, `blankFences`, `blankCode`, `vendoredSkills(root): Set<string>`, `check(root)`, `CORE_MIN_BYTES = 6000`, `CORE_MAX_BYTES = 12000`.

- [ ] **Step 1: Write the failing tests**

Create `Tools/docs-check.test.mjs`:

`````js
// Tests for Tools/docs-check.mjs.
//
// The pure helpers (slug, anchors, links, headings) are imported and tested
// directly. The five checks are tested the way CI runs them: each test builds a
// throwaway git repo, stages a set of files, and runs the real script against
// it as a subprocess with --root. Nothing is mocked.
//
//     node --test Tools/docs-check.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";
import { anchors, headings, links, slug, vendoredSkills } from "./docs-check.mjs";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const SCRIPT = path.join(HERE, "docs-check.mjs");

const SKILL = "---\nname: example\ndescription: Use when testing.\n---\n\n# Example\n";

/** A repo that passes every check; each test overrides the files it is about. */
function baseline() {
    return {
        "AGENTS.md": `# Guide\n\n## Rules\n\n${"x".repeat(6500)}\n`,
        ".agents/rules/area.md": "# Area\n\n## Section One\n\nText.\n",
        ".agents/rules/MANIFEST.tsv":
            "# heading / destination\nSection One\t.agents/rules/area.md\n",
        ".agents/skills/example/SKILL.md": SKILL,
        ".claude/skills/example/SKILL.md": SKILL,
    };
}

/** Writes `files` (null deletes a baseline file) into a fresh git repo and stages them. */
function repo(overrides = {}) {
    const root = fs.mkdtempSync(path.join(os.tmpdir(), "docs-check-"));
    const files = { ...baseline(), ...overrides };
    for (const [file, content] of Object.entries(files)) {
        if (content === null) continue;
        fs.mkdirSync(path.dirname(path.join(root, file)), { recursive: true });
        fs.writeFileSync(path.join(root, file), content);
    }
    for (const args of [
        ["init", "-q"],
        ["add", "-A"],
    ]) {
        const result = spawnSync("git", ["-C", root, ...args], { encoding: "utf8" });
        assert.equal(result.status, 0, result.stderr);
    }
    return root;
}

function run(root, ...args) {
    const result = spawnSync(process.execPath, [SCRIPT, "--root", root, "--json", ...args], {
        encoding: "utf8",
    });
    return {
        status: result.status,
        report: result.status === 2 ? null : JSON.parse(result.stdout),
        result,
    };
}

function findings(overrides, check) {
    const { report } = run(repo(overrides));
    return report.findings.filter((finding) => finding.check === check);
}

// --- helpers ---------------------------------------------------------------

test("slug follows GitHub's heading ids", () => {
    assert.equal(
        slug("Changelogs — four rules, enforced in CI"),
        "changelogs--four-rules-enforced-in-ci",
    );
    assert.equal(slug("`unity command`: Editor State"), "unity-command-editor-state");
    assert.equal(slug("Unity `.meta` files"), "unity-meta-files");
    assert.equal(slug("C# coding style"), "c-coding-style");
    assert.equal(slug("Test doubles — Moq for interactions"), "test-doubles--moq-for-interactions");
    assert.equal(slug("[`docs/`](./docs) layout"), "docs-layout");
    assert.equal(slug("snake_case stays"), "snake_case-stays");
});

test("a repeated heading gets -1, -2", () => {
    assert.deepEqual(
        [...anchors("## Notes\n\n## Notes\n\n## Notes\n")],
        ["notes", "notes-1", "notes-2"],
    );
});

test("closing hashes are not part of a heading", () => {
    assert.deepEqual(headings("## Notes ##\n### Tools #\n"), ["Notes", "Tools"]);
    assert.deepEqual([...anchors("## Notes ##\n")], ["notes"]);
});

test("a # line inside a fence is not a heading", () => {
    assert.deepEqual(headings("# Real\n\n```md\n# Sample\n```\n\n## Also real\n"), [
        "Real",
        "Also real",
    ]);
});

test("a four-backtick fence is not closed by a three-backtick one inside it", () => {
    const text = "````md\n```\n[x](./gone.md)\n```\n````\n[y](./here.md)\n";
    assert.deepEqual(
        links(text).map((link) => link.target),
        ["./here.md"],
    );
});

test("links report their line and skip code", () => {
    const text = "one\n[a](./a.md)\n`[b](./b.md)`\n```\n[c](./c.md)\n```\n[d](<./with space.md>)\n";
    assert.deepEqual(links(text), [
        { target: "./a.md", line: 2 },
        { target: "./with space.md", line: 7 },
    ]);
});

test("vendored skills are read from the Source column", () => {
    const root = repo({
        ".agents/skills/THIRD_PARTY_SKILLS.md":
            "| Skill(s) | Source | License |\n|---|---|---|\n" +
            "| `one`, `two` | [upstream](https://example.com) | MIT |\n" +
            "| `mine` | **Authored in-repo** | — |\n",
    });
    assert.deepEqual([...vendoredSkills(root)].sort(), ["one", "two"]);
});

// --- the whole script ------------------------------------------------------

test("a clean repo passes all five checks", () => {
    const root = repo();
    const result = spawnSync(process.execPath, [SCRIPT, "--root", root], { encoding: "utf8" });
    assert.equal(result.status, 0, result.stdout);
    assert.match(result.stdout, /all five checks pass/);
});

test("a broken link is reported with its file and line", () => {
    const { status, report } = run(repo({ "docs/a.md": "# A\n\nSee [b](./b.md).\n" }));
    assert.equal(status, 1);
    assert.deepEqual(report.findings, [
        { check: "link", file: "docs/a.md", line: 3, message: "./b.md does not exist" },
    ]);
});

test("links resolve through %20, <...>, folders and a repo-root slash", () => {
    const found = findings(
        {
            "Packages/UI Management/README.md": "# UI\n",
            "docs/a.md":
                "[1](../Packages/UI%20Management/README.md) [2](<../Packages/UI Management/README.md>)\n" +
                "[3](../Packages/UI%20Management) [4](/AGENTS.md) [5](https://example.com/x.md) [6](mailto:a@b.c)\n",
        },
        "link",
    );
    assert.deepEqual(found, []);
});

test("a query string is not part of the path", () => {
    const found = findings(
        {
            "docs/a.md": "# A\n\n## Part\n",
            "docs/b.md": "[1](./a.md?plain=1) [2](./a.md?plain=1#part)\n",
        },
        "link",
    );
    assert.deepEqual(found, []);
});

test("an untracked Markdown file is not scanned", () => {
    const root = repo({ "docs/a.md": "# A\n" });
    fs.writeFileSync(path.join(root, "docs/c.md"), "[x](./nowhere.md)\n");
    assert.equal(run(root).report.findings.length, 0);
});

test("anchors are checked in-file and across files", () => {
    const found = findings(
        {
            "docs/a.md": "# A\n\n## Twice\n\n## Twice\n\n[ok](#twice-1) [bad](#missing)\n",
            "docs/b.md":
                "[ok](./a.md#twice) [bad](./a.md#nope) [ok](../.agents/rules/area.md#section-one)\n",
        },
        "anchor",
    );
    assert.deepEqual(
        found.map((finding) => `${finding.file}:${finding.line}`),
        ["docs/a.md:7", "docs/b.md:1"],
    );
});

test("a fragment matches regardless of case and percent-encoding", () => {
    const found = findings(
        {
            "docs/a.md": "# A\n\n## Unity `.meta` files\n\n## Über\n",
            "docs/b.md":
                "[1](./a.md#Unity-meta-files) [2](./a.md#%C3%BCber) [3](./a.md?plain=1#nope)\n",
        },
        "anchor",
    );
    assert.deepEqual(
        found.map((finding) => finding.message),
        ["./a.md?plain=1#nope: no heading #nope in docs/a.md"],
    );
});

test("an anchor into a missing or non-Markdown file is not an anchor finding", () => {
    const found = findings(
        { ".github/w.yml": "a: 1\n", "docs/a.md": "[x](./gone.md#y) [w](../.github/w.yml#L1)\n" },
        "anchor",
    );
    assert.deepEqual(found, []);
});

test("anchors inside a vendored skill are skipped; an authored skill's are checked", () => {
    const sources =
        "| Skill(s) | Source | License |\n|---|---|---|\n" +
        "| `vendor` | [upstream](https://example.com) | MIT |\n| `example` | **Authored in-repo** | — |\n";
    const vendorSkill = "# Vendor\n\n[x](#not-here)\n";
    const authoredSkill = `${SKILL}\n[x](#not-here)\n`;
    const found = findings(
        {
            ".agents/skills/THIRD_PARTY_SKILLS.md": sources,
            ".claude/skills/THIRD_PARTY_SKILLS.md": sources,
            ".agents/skills/vendor/SKILL.md": vendorSkill,
            ".claude/skills/vendor/SKILL.md": vendorSkill,
            ".agents/skills/example/SKILL.md": authoredSkill,
            ".claude/skills/example/SKILL.md": authoredSkill,
        },
        "anchor",
    );
    assert.deepEqual(
        found.map((finding) => finding.file),
        [".agents/skills/example/SKILL.md", ".claude/skills/example/SKILL.md"],
    );
});

test("the skill mirror must match file for file and byte for byte", () => {
    const found = findings(
        {
            ".claude/skills/example/SKILL.md": `${SKILL}drift\n`,
            ".agents/skills/only-source/SKILL.md": SKILL,
            ".claude/skills/only-mirror/SKILL.md": SKILL,
        },
        "mirror",
    );
    assert.deepEqual(found.map((finding) => `${finding.file}: ${finding.message}`).sort(), [
        ".agents/skills/example/SKILL.md: differs from .claude/skills/example/SKILL.md",
        ".agents/skills/only-source/SKILL.md: missing from .claude/skills/",
        ".claude/skills/only-mirror/SKILL.md: missing from .agents/skills/",
    ]);
});

test("a manifest with CRLF line endings reads the same", () => {
    const crlf = "# heading / destination\r\nSection One\t.agents/rules/area.md\r\n";
    assert.deepEqual(findings({ ".agents/rules/MANIFEST.tsv": crlf }, "heading"), []);
});

test("a missing manifest is reported", () => {
    assert.equal(findings({ ".agents/rules/MANIFEST.tsv": null }, "heading").length, 1);
});

test("a manifest heading must still be a heading in its file", () => {
    const found = findings(
        {
            ".agents/rules/area.md":
                "# Area\n\nSection One is mentioned only in prose.\n\n```md\n## Section One\n```\n\n## Dup A\n\n## Dup B\n",
            ".agents/rules/MANIFEST.tsv":
                "# heading / destination\nSection One\t.agents/rules/area.md\nDup\t.agents/rules/area.md\n" +
                "Gone\t.agents/rules/nowhere.md\nno tab here\n",
        },
        "heading",
    );
    assert.deepEqual(
        found.map((finding) => `${finding.line}: ${finding.message}`),
        [
            '2: "Section One" is not a heading in .agents/rules/area.md',
            '3: "Dup" matches 2 headings in .agents/rules/area.md',
            "4: .agents/rules/nowhere.md does not exist",
            "5: a row needs a heading, a tab, and a file",
        ],
    );
});

test("AGENTS.md must stay inside the byte budget", () => {
    assert.match(findings({ "AGENTS.md": "# Tiny\n" }, "budget")[0].message, /^7 bytes/);
    assert.equal(findings({ "AGENTS.md": "x".repeat(12001) }, "budget").length, 1);
    assert.equal(findings({ "AGENTS.md": "x".repeat(12000) }, "budget").length, 0);
});

test("the budget counts LF line endings, so a CRLF checkout measures the same", () => {
    const line = "y".repeat(39);
    assert.equal(findings({ "AGENTS.md": `${line}\r\n`.repeat(300) }, "budget").length, 0);
});

test("a root that is not a git repository is exit 2, not a finding", () => {
    const root = fs.mkdtempSync(path.join(os.tmpdir(), "docs-check-plain-"));
    const result = spawnSync(process.execPath, [SCRIPT, "--root", root], { encoding: "utf8" });
    assert.equal(result.status, 2, result.stdout);
    assert.match(result.stderr, /^docs-check: /);
});

test("an unknown argument is a usage error", () => {
    assert.equal(run(repo(), "--nope").status, 2);
});
`````

- [ ] **Step 2: Run the tests to see them fail**

Run: `node --test Tools/docs-check.test.mjs`
Expected: FAIL — `Cannot find module …/Tools/docs-check.mjs`.

- [ ] **Step 3: Write the checker**

Create `Tools/docs-check.mjs`:

```js
// Checks this repo's own Markdown, so the routing from AGENTS.md to the files
// that hold each rule cannot rot unnoticed. Five checks:
//
//   link     every relative Markdown link resolves to a file or folder
//   anchor   every #fragment matches a heading in its target
//   mirror   .agents/skills/ and .claude/skills/ hold the same files, byte for byte
//   heading  every heading in .agents/rules/MANIFEST.tsv exists in the file it names
//   budget   AGENTS.md is between 6,000 and 12,000 bytes, LF-normalised
//
//     node Tools/docs-check.mjs            # human-readable, exit 0 clean / 1 findings
//     node Tools/docs-check.mjs --json     # the same report as JSON
//     node Tools/docs-check.mjs --root <dir>
//
// Dependency-free, like the rest of Tools/. Scans tracked files only
// (`git ls-files`), so build output and untracked scratch never count.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const SCRIPT = fileURLToPath(import.meta.url);
const DEFAULT_ROOT = path.dirname(path.dirname(SCRIPT));

const CORE = "AGENTS.md";
export const CORE_MIN_BYTES = 6000;
export const CORE_MAX_BYTES = 12000;
const MANIFEST = ".agents/rules/MANIFEST.tsv";
const SKILLS = ".agents/skills";
const SKILLS_MIRROR = ".claude/skills";
const SKILL_SOURCES = ".agents/skills/THIRD_PARTY_SKILLS.md";
const AUTHORED_IN_REPO = "Authored in-repo";

// A link target with a URL scheme (https:, mailto:, ...) is not a repo path.
const SCHEME = /^[a-z][a-z0-9+.-]*:/i;
// [text](target) and [text](<target with spaces>).
const LINK = /\]\(\s*(?:<([^>]+)>|([^)\s]+))/g;
const HEADING = /^ {0,3}#{1,6}\s/;

function git(root, ...args) {
    const result = spawnSync("git", ["-C", root, ...args], { encoding: "utf8" });
    if (result.status !== 0) {
        throw new Error(`git ${args.join(" ")} failed: ${result.stderr || result.stdout}`);
    }
    return result.stdout;
}

function tracked(root, pathspec) {
    return git(root, "ls-files", "-z", "--", pathspec)
        .split("\0")
        .filter((file) => file !== "");
}

function read(root, file) {
    return fs.readFileSync(path.join(root, file), "utf8");
}

/**
 * Blanks every fenced code block, keeping the line count so line numbers still
 * point at the right line. A fence closes on a run of the same character at
 * least as long as the one that opened it (CommonMark), so a 4-backtick fence
 * quoting a 3-backtick one is not closed early. An unclosed fence runs to the
 * end of the file.
 */
export function blankFences(text) {
    const lines = text.split("\n");
    let fence = null;
    for (let i = 0; i < lines.length; i++) {
        const opener = /^ {0,3}(`{3,}|~{3,})/.exec(lines[i]);
        if (fence === null) {
            if (opener !== null) {
                fence = opener[1];
                lines[i] = "";
            }
            continue;
        }
        const closer = /^ {0,3}(`{3,}|~{3,})\s*$/.exec(lines[i]);
        if (closer !== null && closer[1][0] === fence[0] && closer[1].length >= fence.length) {
            fence = null;
        }
        lines[i] = "";
    }
    return lines.join("\n");
}

/**
 * Blanks fences and inline code spans. Code quotes a path — a file as it once
 * was, an example, a heading cited literally — and is not a link the document
 * makes.
 */
export function blankCode(text) {
    return blankFences(text).replace(/(`+)[^\n]*?[^`\n]\1(?!`)/g, (span) =>
        " ".repeat(span.length),
    );
}

function headingText(line) {
    return line.replace(/^ {0,3}#{1,6}\s+/, "").replace(/\s+#*\s*$/, "");
}

/**
 * A document's real headings, as text. Only fences are blanked: backticks are
 * ordinary heading text, and a manifest row matches them character for
 * character.
 */
export function headings(text) {
    return blankFences(text.replace(/\r\n/g, "\n"))
        .split("\n")
        .filter((line) => HEADING.test(line))
        .map(headingText);
}

/**
 * GitHub's heading id: lowercase; a link contributes its text, not its URL;
 * drop every character that is not a letter, mark, digit, underscore, space or
 * hyphen; spaces become hyphens. A spaced em dash therefore becomes "--".
 */
export function slug(heading) {
    return heading
        .toLowerCase()
        .replace(/!?\[([^\]]*)\]\([^)]*\)/g, "$1")
        .replace(/[^\p{L}\p{M}\p{N}_ -]/gu, "")
        .replace(/ /g, "-");
}

/** Every anchor a document offers; a repeated heading gets -1, -2, ... */
export function anchors(text) {
    const seen = new Map();
    const result = new Set();
    for (const heading of headings(text)) {
        const base = slug(heading);
        const count = seen.get(base);
        if (count === undefined) {
            seen.set(base, 0);
            result.add(base);
        } else {
            seen.set(base, count + 1);
            result.add(`${base}-${count + 1}`);
        }
    }
    return result;
}

/** Every link in a document, with its 1-based line number. */
export function links(text) {
    const blanked = blankCode(text.replace(/\r\n/g, "\n"));
    const result = [];
    for (const match of blanked.matchAll(LINK)) {
        const target = match[1] ?? match[2];
        const line = blanked.slice(0, match.index).split("\n").length;
        result.push({ target, line });
    }
    return result;
}

function decode(part) {
    try {
        return decodeURIComponent(part);
    } catch {
        return part;
    }
}

/** A link's path without its query string: `./a.md?plain=1` is `./a.md`. */
function pathOf(linkPart) {
    return linkPart.split("?")[0];
}

/** Where a link's path part points, relative to the repo root. */
function resolveTarget(file, pathPart) {
    const decoded = decode(pathPart);
    return decoded.startsWith("/")
        ? path.posix.normalize(decoded.slice(1))
        : path.posix.normalize(path.posix.join(path.posix.dirname(file), decoded));
}

/**
 * Skill packs vendored from an outside source, read from the Source column of
 * THIRD_PARTY_SKILLS.md: every skill named in a row whose source is not
 * "Authored in-repo". An anchor inside one is upstream's to fix.
 */
export function vendoredSkills(root) {
    const file = path.join(root, SKILL_SOURCES);
    if (!fs.existsSync(file)) return new Set();
    const vendored = new Set();
    for (const row of fs.readFileSync(file, "utf8").split(/\r?\n/)) {
        const cells = row.split("|").map((cell) => cell.trim());
        if (cells.length < 4 || /^-+$/.test(cells[1]) || cells[2].includes(AUTHORED_IN_REPO)) {
            continue;
        }
        for (const name of cells[1].matchAll(/`([^`]+)`/g)) vendored.add(name[1]);
    }
    return vendored;
}

function isVendored(file, vendored) {
    for (const name of vendored) {
        if (file.startsWith(`${SKILLS}/${name}/`) || file.startsWith(`${SKILLS_MIRROR}/${name}/`)) {
            return true;
        }
    }
    return false;
}

function checkLinks(root, markdown) {
    const findings = [];
    for (const file of markdown) {
        for (const { target, line } of links(read(root, file))) {
            if (SCHEME.test(target) || target.startsWith("#")) continue;
            const pathPart = pathOf(target.split("#")[0]);
            if (pathPart === "") continue;
            if (!fs.existsSync(path.join(root, resolveTarget(file, pathPart)))) {
                findings.push({ check: "link", file, line, message: `${target} does not exist` });
            }
        }
    }
    return findings;
}

function checkAnchors(root, markdown, vendored) {
    const findings = [];
    const cache = new Map();
    for (const file of markdown) {
        if (isVendored(file, vendored)) continue;
        for (const { target, line } of links(read(root, file))) {
            if (SCHEME.test(target) || !target.includes("#")) continue;
            const [linkPart, fragment] = target.split(/#(.*)/s);
            if (fragment === "") continue;
            const pathPart = pathOf(linkPart);
            const destination = pathPart === "" ? file : resolveTarget(file, pathPart);
            // A missing target is already a link finding; a non-Markdown
            // target (a line anchor into a workflow) has no headings to check.
            if (!destination.endsWith(".md") || !fs.existsSync(path.join(root, destination))) {
                continue;
            }
            if (!cache.has(destination)) cache.set(destination, anchors(read(root, destination)));
            if (!cache.get(destination).has(decode(fragment).toLowerCase())) {
                findings.push({
                    check: "anchor",
                    file,
                    line,
                    message: `${target}: no heading #${fragment} in ${destination}`,
                });
            }
        }
    }
    return findings;
}

function checkMirror(root) {
    const findings = [];
    const source = new Set(tracked(root, SKILLS).map((file) => file.slice(SKILLS.length + 1)));
    const mirror = new Set(
        tracked(root, SKILLS_MIRROR).map((file) => file.slice(SKILLS_MIRROR.length + 1)),
    );
    for (const file of source) {
        if (!mirror.has(file)) {
            findings.push({
                check: "mirror",
                file: `${SKILLS}/${file}`,
                message: `missing from ${SKILLS_MIRROR}/`,
            });
        } else if (
            !fs
                .readFileSync(path.join(root, SKILLS, file))
                .equals(fs.readFileSync(path.join(root, SKILLS_MIRROR, file)))
        ) {
            findings.push({
                check: "mirror",
                file: `${SKILLS}/${file}`,
                message: `differs from ${SKILLS_MIRROR}/${file}`,
            });
        }
    }
    for (const file of mirror) {
        if (!source.has(file)) {
            findings.push({
                check: "mirror",
                file: `${SKILLS_MIRROR}/${file}`,
                message: `missing from ${SKILLS}/`,
            });
        }
    }
    return findings;
}

function checkManifest(root) {
    const manifest = path.join(root, MANIFEST);
    if (!fs.existsSync(manifest)) {
        return [{ check: "heading", file: MANIFEST, message: "the manifest does not exist" }];
    }
    const findings = [];
    const rows = fs.readFileSync(manifest, "utf8").split(/\r?\n/);
    rows.forEach((row, index) => {
        const line = index + 1;
        // A comment is a '#' line with no tab; every heading row has one.
        if (row.trim() === "" || (row.trimStart().startsWith("#") && !row.includes("\t"))) return;
        const [heading, destination] = row.split("\t").map((cell) => (cell ?? "").trim());
        if (!heading || !destination) {
            findings.push({
                check: "heading",
                file: MANIFEST,
                line,
                message: "a row needs a heading, a tab, and a file",
            });
            return;
        }
        if (!fs.existsSync(path.join(root, destination))) {
            findings.push({
                check: "heading",
                file: MANIFEST,
                line,
                message: `${destination} does not exist`,
            });
            return;
        }
        // Still a heading, not merely a phrase in a sentence: a deleted section
        // whose name survives in prose is exactly the loss this check catches.
        const hits = headings(read(root, destination)).filter((text) => text.includes(heading));
        if (hits.length === 0) {
            findings.push({
                check: "heading",
                file: MANIFEST,
                line,
                message: `"${heading}" is not a heading in ${destination}`,
            });
        } else if (hits.length > 1) {
            findings.push({
                check: "heading",
                file: MANIFEST,
                line,
                message: `"${heading}" matches ${hits.length} headings in ${destination}`,
            });
        }
    });
    return findings;
}

function checkBudget(root) {
    const core = path.join(root, CORE);
    if (!fs.existsSync(core)) return [{ check: "budget", file: CORE, message: "does not exist" }];
    const bytes = Buffer.byteLength(fs.readFileSync(core, "utf8").replace(/\r\n/g, "\n"), "utf8");
    if (bytes < CORE_MIN_BYTES || bytes > CORE_MAX_BYTES) {
        return [
            {
                check: "budget",
                file: CORE,
                message: `${bytes} bytes; the budget is ${CORE_MIN_BYTES}-${CORE_MAX_BYTES}`,
            },
        ];
    }
    return [];
}

export function check(root) {
    const markdown = tracked(root, "*.md");
    const vendored = vendoredSkills(root);
    const findings = [
        ...checkLinks(root, markdown),
        ...checkAnchors(root, markdown, vendored),
        ...checkMirror(root),
        ...checkManifest(root),
        ...checkBudget(root),
    ];
    return { ok: findings.length === 0, findings };
}

function render(report) {
    const lines = report.findings.map(
        ({ check, file, line, message }) =>
            `${check.padEnd(8)}${line === undefined ? file : `${file}:${line}`}  ${message}`,
    );
    lines.push(
        report.ok
            ? "docs-check: all five checks pass."
            : `docs-check: ${report.findings.length} problem(s). Fix them, or see .agents/rules/documentation-voice.md.`,
    );
    return lines;
}

function parseArgs(argv) {
    const flags = { root: DEFAULT_ROOT, json: false };
    for (let i = 0; i < argv.length; i++) {
        if (argv[i] === "--json") flags.json = true;
        else if (argv[i] === "--root" && i + 1 < argv.length) flags.root = path.resolve(argv[++i]);
        else {
            console.error(
                `docs-check: unknown argument ${argv[i]}. Usage: node Tools/docs-check.mjs [--root <dir>] [--json]`,
            );
            process.exit(2);
        }
    }
    return flags;
}

if (process.argv[1] !== undefined && path.resolve(process.argv[1]) === SCRIPT) {
    const flags = parseArgs(process.argv.slice(2));
    let report;
    try {
        report = check(flags.root);
    } catch (error) {
        // Not a git repository, or git missing: the check could not run at all.
        console.error(`docs-check: ${error.message.trim()}`);
        process.exit(2);
    }
    console.log(flags.json ? JSON.stringify(report, null, 2) : render(report).join("\n"));
    process.exit(report.ok ? 0 : 1);
}
```

- [ ] **Step 4: Run the tests to see them pass**

Run: `node --test Tools/docs-check.test.mjs`
Expected: PASS, 24 tests.

- [ ] **Step 5: Run it against the repo**

Run: `node Tools/docs-check.mjs`
Expected: exit 1 with exactly these seven findings, all fixed by later tasks:

```text
link    docs/specs/2026-08-23-upm-package-registry-github-design.md:123  ../../.agents/AGENTS.md does not exist
link    docs/specs/2026-09-05-basic-rename-and-moq-design.md:41  ../../.agents/AGENTS.md#distribution-and-releases does not exist
link    docs/specs/2026-09-13-auto-formatting-design.md:11  ../../.agents/AGENTS.md#c-coding-style does not exist
link    docs/specs/2026-09-13-auto-formatting-design.md:144  ../../.agents/AGENTS.md#ci does not exist
anchor  docs/plans/2026-09-02-release-promotion.md:165  #changelogs--two-rules-enforced-in-ci: no heading #changelogs--two-rules-enforced-in-ci in docs/plans/2026-09-02-release-promotion.md
heading .agents/rules/MANIFEST.tsv  the manifest does not exist
budget  AGENTS.md  57794 bytes; the budget is 6000-12000
docs-check: 7 problem(s). Fix them, or see .agents/rules/documentation-voice.md.
```

The vendored skills produce no finding: their anchors are skipped, and their relative links resolve.

- [ ] **Step 6: Format and commit**

```bash
npx prettier --write Tools/docs-check.mjs Tools/docs-check.test.mjs
npx prettier --check .
git -C <wt> add Tools/docs-check.mjs Tools/docs-check.test.mjs
git -C <wt> commit -m "feat(tools): add docs-check for links, anchors, the skill mirror, the heading manifest and the AGENTS.md budget"
```

---

### Task 2: Status lines and `docs/INDEX.md`

**Files:**

- Create: `docs/INDEX.md`
- Modify: the six dated specs before this one, line 4; the four plans, after line 1

**Interfaces:**

- Consumes: `node Tools/docs-check.mjs` (Task 1).
- Produces: `docs/INDEX.md`, which `AGENTS.md` (Task 5) and `.agents/rules/documentation-voice.md` (Task 3) link to. The status vocabulary `Designed` / `Planned` / `Implemented` / `Superseded — <reason>` / `Abandoned — <reason>`.

- [ ] **Step 1: Replace each spec's status paragraph**

Each spec has `**Date:**` on line 3 and `**Status:**` on line 4. Replace the whole status paragraph with a single line; the lines after it (`**Repos affected:**`, `**Assumption (now satisfied):**`, …) stay.

`docs/specs/2026-08-22-upm-package-registry-design.md` — replace

```text
**Status:** **Superseded** by [`2026-08-23-upm-package-registry-github-design.md`](./2026-08-23-upm-package-registry-github-design.md) (GitHub + OpenUPM). Retained
```

with

```text
**Status:** Superseded — replaced by the GitHub + OpenUPM design, [`2026-08-23-upm-package-registry-github-design.md`](./2026-08-23-upm-package-registry-github-design.md). Retained
```

`docs/specs/2026-08-23-upm-package-registry-github-design.md` — replace

```text
**Status:** **Accepted — current direction.** Rollout in progress; see §6.
```

with `**Status:** Implemented`.

`docs/specs/2026-08-30-pr-test-ci-design.md` — replace

```text
**Status:** **Accepted.** Blocked on two things before it can go green — a self-hosted runner must be
registered (§3), and the Smart App Control block in §8 must be resolved. The workflow is correct and
committable before either happens; it will simply queue, then fail loudly and truthfully.
```

with `**Status:** Implemented`.

`docs/specs/2026-09-02-release-promotion-design.md` — replace

```text
**Status:** **Accepted.** Nothing here is blocked. The three code changes (§4, §5, §6) are
independent and can land in any order; §7 is a one-off cleanup that §6 depends on, and §8 is the
first release run under the new flow.
```

with `**Status:** Implemented`.

`docs/specs/2026-09-05-basic-rename-and-moq-design.md` — replace

```text
**Status:** **Accepted.** Design approved 2026-09-05. Two independent parts, executed in order as two
branches off `dev`.
```

with `**Status:** Implemented`.

`docs/specs/2026-09-13-auto-formatting-design.md` — replace

```text
**Status:** **Accepted.** Design approved 2026-09-13. One branch off `dev`, `chore/auto-formatting`,
landed as one pull request.
```

with `**Status:** Implemented`.

- [ ] **Step 2: Give each plan a status line**

The plans have no `**Date:**` line. In each, insert `**Status:** Implemented` and a blank line between the title and the `> **For agentic workers:**` blockquote, so the file starts:

```text
# <title, unchanged>

**Status:** Implemented

> **For agentic workers:** …
```

Files: `docs/plans/2026-08-30-pr-test-ci.md`, `docs/plans/2026-09-02-release-promotion.md`, `docs/plans/2026-09-05-drop-basic-prefix-and-moq.md`, `docs/plans/2026-09-13-auto-formatting.md`.

- [ ] **Step 3: Write `docs/INDEX.md`**

```markdown
# Documentation index

Every feature with a design spec in [`specs/`](./specs) or an implementation plan in [`plans/`](./plans), with its status. Read a feature's documents before changing what they cover. The rules for agents are elsewhere: [`AGENTS.md`](../AGENTS.md) and [`.agents/rules/`](../.agents/rules).

Each spec and plan carries a `**Status:**` line near its top, and the row here repeats it:

| Status                  | Meaning                                                         |
| ----------------------- | --------------------------------------------------------------- |
| `Designed`              | The spec is approved; no plan yet.                              |
| `Planned`               | The implementation plan is written; the work has not landed.    |
| `Implemented`           | The work has landed on `dev`.                                   |
| `Superseded — <reason>` | Another design replaced this one; the reason names or links it. |
| `Abandoned — <reason>`  | The work was dropped; the reason says why.                      |

A new spec adds its row in the same commit, and the commit that lands the work sets `Implemented` in the row, the spec and the plan. A plan's checkboxes are working state while it runs; they are not ticked after the fact.

| Feature                                      | Status      | Spec                                                             | Plan                                                    | Main files                                                                                                                                            |
| -------------------------------------------- | ----------- | ---------------------------------------------------------------- | ------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| Package registry: GitLab + npmjs.com variant | Superseded  | [spec](./specs/2026-08-22-upm-package-registry-design.md)        | —                                                       | —                                                                                                                                                     |
| Package registry: GitHub + OpenUPM           | Implemented | [spec](./specs/2026-08-23-upm-package-registry-github-design.md) | —                                                       | `Tools/upm-release.mjs`, `.github/workflows/release.yml`, `Packages/*/package.json`                                                                   |
| Pull-request test CI                         | Implemented | [spec](./specs/2026-08-30-pr-test-ci-design.md)                  | [plan](./plans/2026-08-30-pr-test-ci.md)                | `.github/workflows/tests.yml`, `Tools/ci/`, `.github/actionlint.yaml`                                                                                 |
| Release promotion                            | Implemented | [spec](./specs/2026-09-02-release-promotion-design.md)           | [plan](./plans/2026-09-02-release-promotion.md)         | `Tools/upm-release.mjs`, `Tools/release-flow.mjs`, `Tools/release.bat`, `Tools/promotion-check.mjs`, `Tools/changelog-check.mjs`, `.github/rulesets/` |
| Dropping the `Basic` prefix, and Moq         | Implemented | [spec](./specs/2026-09-05-basic-rename-and-moq-design.md)        | [plan](./plans/2026-09-05-drop-basic-prefix-and-moq.md) | `Packages/*/Runtime/`, the test asmdefs, `Packages/manifest.json`                                                                                     |
| Auto-formatting                              | Implemented | [spec](./specs/2026-09-13-auto-formatting-design.md)             | [plan](./plans/2026-09-13-auto-formatting.md)           | `.editorconfig`, `.csharpierrc.json`, `.prettierrc.json`, `package.json`, `.github/workflows/format.yml`, `.git-blame-ignore-revs`                    |
| Documentation restructure                    | Planned     | [spec](./specs/2026-09-26-documentation-restructure-design.md)   | [plan](./plans/2026-09-26-documentation-restructure.md) | `AGENTS.md`, `.agents/rules/`, `.agents/skills/`, `docs/INDEX.md`, `Tools/docs-check.mjs`                                                             |
```

- [ ] **Step 4: Check**

Run: `npx prettier --write docs/INDEX.md "docs/specs/*.md" "docs/plans/*.md"`, `git -C <wt> add docs`, then `node Tools/docs-check.mjs`.
Expected: the same seven findings as Task 1 Step 5, and none in `docs/INDEX.md`. Three line numbers move up by one where a status paragraph shrank (`2026-09-05-…:40`, `2026-09-13-…:10` and `:143`), and the plan anchor moves down by two (`2026-09-02-release-promotion.md:167`).

Run: `git -C <wt> grep -n -m1 "^\*\*Status:\*\*" -- docs`
Expected: twelve lines, one per file — five `Implemented` specs and one `Superseded — …`, four `Implemented` plans, this spec at `Designed` and this plan at `Planned`.

- [ ] **Step 5: Commit**

```bash
npx prettier --check .
git -C <wt> add docs
git -C <wt> commit -m "docs: add docs/INDEX.md and a status line to every spec and plan"
```

---

### Task 3: Carve `AGENTS.md` into `.agents/rules/`

**Files:**

- Create: `.agents/rules/documentation-voice.md`, `packages.md`, `testing.md`, `ci.md`, `code-navigation.md`, `releases.md`, `git-workflow.md`, `code-style.md`
- Create: `.agents/rules/MANIFEST.tsv`
- Read, not modified: `AGENTS.md` (Task 5 replaces it)

**Interfaces:**

- Consumes: `slug` from `Tools/docs-check.mjs` (Task 1); `docs/INDEX.md` (Task 2) as a link target.
- Produces: the eight rules files and their headings, which the skills (Task 4), the core (Task 5) and the pointer fixes link to by these anchors: `packages.md#package-catalogue`, `packages.md#naming`, `packages.md#assembly-definitions`, `releases.md#distribution-and-releases`, `releases.md#changelogs--four-rules-enforced-in-ci`, `git-workflow.md#branching`, `code-style.md#c-coding-style`, `code-style.md#formatting`, `ci.md#ci`. `MANIFEST.tsv` rows: `<heading>\t<file>`.

Every level-2 section of `AGENTS.md` has one destination:

| `AGENTS.md` section                                                    | Destination                                                                       |
| ---------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| Writing documentation                                                  | `documentation-voice.md`, plus a new closing section "Keeping the routing honest" |
| Repo layout, Package anatomy, Package catalogue, Known inconsistencies | `packages.md`                                                                     |
| Test Commands                                                          | `testing.md`                                                                      |
| CI                                                                     | `ci.md`                                                                           |
| MCP Tool Usage & Unity CLI                                             | `code-navigation.md`                                                              |
| Distribution and releases                                              | `releases.md`                                                                     |
| Git and hosting                                                        | `git-workflow.md`                                                                 |
| Formatting, C# coding style                                            | `code-style.md`                                                                   |
| What this repo is                                                      | rewritten in the core (Task 5)                                                    |
| Unity `.meta` files                                                    | the `unity-asset-editing` skill (Task 4)                                          |
| Adding a new package                                                   | the `adding-a-package` skill (Task 4)                                             |

The `###` sections travel with their `##` section.

- [ ] **Step 1: Save the carve script outside the repo**

Save this as `carve-agents.mjs` in a scratch directory outside `<wt>` — it is run once and never committed:

```js
// One-off: splits the long AGENTS.md into .agents/rules/*.md and writes
// .agents/rules/MANIFEST.tsv. Run once from outside the repo, then delete it.
//
//     node carve-agents.mjs <repo-root>
//
// Sections move verbatim. Two things are rewritten on the way: relative links
// gain the two extra levels a file under .agents/rules/ sits at, and an in-file
// anchor becomes a link to whichever rules file now holds its heading.

import fs from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";

const root = path.resolve(process.argv[2] ?? ".");
const { slug } = await import(pathToFileURL(path.join(root, "Tools/docs-check.mjs")).href);

const FILES = [
    {
        name: "documentation-voice",
        title: "Documentation voice",
        intro: "How every document, skill file and code comment in this repo is written.",
        sections: ["Writing documentation"],
    },
    {
        name: "packages",
        title: "Packages: layout, anatomy and naming",
        intro: "What lives where under `Packages/`, what every package contains, and the names that must not change.",
        sections: ["Repo layout", "Package anatomy", "Package catalogue", "Known inconsistencies"],
    },
    {
        name: "testing",
        title: "Testing",
        intro: "Running the Unity test suites, and how tests in this repo are written.",
        sections: ["Test Commands"],
    },
    {
        name: "ci",
        title: "Continuous integration",
        intro: "The GitHub Actions workflows, what each job checks, and the self-hosted runner.",
        sections: ["CI"],
    },
    {
        name: "code-navigation",
        title: "Code navigation, MCP tools and the Unity CLI",
        intro: "Which tool answers which question: `sharplens`, `lifeblood`, the vendored skills, and `unity command`.",
        sections: ["MCP Tool Usage & Unity CLI"],
    },
    {
        name: "releases",
        title: "Releases",
        intro: "How a package version reaches OpenUPM, the release tooling, and the changelog rules CI enforces. The step-by-step procedure is the [`releasing-packages`](../skills/releasing-packages/SKILL.md) skill.",
        sections: ["Distribution and releases"],
    },
    {
        name: "git-workflow",
        title: "Git workflow",
        intro: "Hosting, remotes, the `dev`/`master` split, rulesets and required checks.",
        sections: ["Git and hosting"],
    },
    {
        name: "code-style",
        title: "Code style and formatting",
        intro: "The formatters, what they cover, and the C# conventions they do not enforce.",
        sections: ["Formatting", "C# coding style"],
    },
];

// Sections that do not become rules: the new core rewrites the first, and the
// other two become skills.
const NOT_CARVED = ["What this repo is", "Unity `.meta` files", "Adding a new package"];

const ASSET_RULE = "- **Only modify Unity assets";
const ASSET_POINTER =
    "- Scenes, prefabs and `.asset` files change only through `unity command` or the Editor — never by hand-editing their YAML. The [`unity-asset-editing`](../skills/unity-asset-editing/SKILL.md) skill carries the rule and the fallback when a command is broken.";

// The repo-layout tree, once it lives in packages.md: "this file" no longer means
// AGENTS.md, and the tree gains the files this restructure adds. Each key is a whole
// line of the tree; its value is the line or lines that replace it.
const TREE = new Map([
    [
        "│   └── skills/              # vendored agent skills (canonical, tool-agnostic copy; OpenCode reads it)",
        "│   ├── rules/               # rules by area, routed from AGENTS.md; MANIFEST.tsv maps headings to files\n" +
            "│   └── skills/              # agent skills, vendored and authored (canonical copy; OpenCode reads it)",
    ],
    [
        "│   ├── promotion-check.mjs  # release PRs into master come from dev only",
        "│   ├── promotion-check.mjs  # release PRs into master come from dev only\n" +
            "│   ├── docs-check.mjs       # the docs check — links, anchors, skill mirror, manifest, budget",
    ],
    ["├── docs/", "├── docs/\n│   ├── INDEX.md             # every spec and plan, with its status"],
    [
        "├── AGENTS.md                # this file",
        "├── AGENTS.md                # agent entry point — the non-negotiables and the routing tables",
    ],
    [
        "├── CLAUDE.md, QWEN.md       # per-agent pointers to this file",
        "├── CLAUDE.md, QWEN.md       # per-agent pointers to AGENTS.md",
    ],
]);

const ROUTING = `## Keeping the routing honest

\`AGENTS.md\` is the only file every agent loads eagerly; everything else is read on demand, found through its routing tables. So a rule is only as reachable as its row.

- A new rule that is expensive to break gets a row in the non-negotiables table in [\`AGENTS.md\`](../../AGENTS.md); its detail goes in the rules file or skill the row points at.
- A new area gets a rules file here and a row in the "Where the rules live" table.
- Moving or renaming a heading means updating its row in [\`MANIFEST.tsv\`](./MANIFEST.tsv) (heading, a tab, the file that holds it) and every link to its anchor.
- A skill under \`.agents/skills/\` is copied byte for byte to \`.claude/skills/\` in the same commit, and one it authors in-repo gets an **Authored in-repo** row in \`THIRD_PARTY_SKILLS.md\`.
- A link from a skill to a rules file is written \`../../../.agents/rules/<file>.md\`, which resolves from both copies of the skill.
- A new spec or plan starts at \`**Status:** Designed\` or \`**Status:** Planned\` and adds its row to [\`docs/INDEX.md\`](../../docs/INDEX.md) in the same commit. The statuses are \`Designed\`, \`Planned\`, \`Implemented\`, \`Superseded — <reason>\` and \`Abandoned — <reason>\`; the landing commit sets \`Implemented\`.

\`npm run check:docs\` (\`Tools/docs-check.mjs\`) holds all of this to account: every relative link and heading anchor resolves, every manifest row is still a heading in its file, the two skill folders match, and \`AGENTS.md\` stays between 6,000 and 12,000 bytes. CI runs it in the \`tooling-tests\` job.
`;

const source = fs.readFileSync(path.join(root, "AGENTS.md"), "utf8").replace(/\r\n/g, "\n");

// Level-2 sections, in order. Nothing in AGENTS.md puts a heading inside a fence.
const sections = new Map();
let current = null;
for (const line of source.split("\n")) {
    const heading = /^## (.+)$/.exec(line);
    if (heading) {
        current = heading[1];
        sections.set(current, []);
    }
    if (current !== null) sections.get(current).push(line);
}

const planned = [...FILES.flatMap((file) => file.sections), ...NOT_CARVED];
for (const name of sections.keys()) {
    if (!planned.includes(name)) throw new Error(`AGENTS.md section "${name}" has no destination`);
}
for (const name of planned) {
    if (!sections.has(name)) throw new Error(`AGENTS.md has no section "${name}"`);
}

// Every carved heading, its anchor, and the file that now holds it.
const owner = new Map();
const manifest = ["What this repo is\tAGENTS.md"];
for (const file of FILES) {
    for (const name of file.sections) {
        for (const line of sections.get(name)) {
            const heading = /^#{2,3} (.+)$/.exec(line);
            if (!heading) continue;
            owner.set(slug(heading[1]), file.name);
            manifest.push(`${heading[1]}\t.agents/rules/${file.name}.md`);
        }
    }
}

function rewrite(text, fileName) {
    return text
        .replace(/\]\(\.\//g, "](../../")
        .replace(/\]\(#([^)]+)\)/g, (match, anchor) => {
            const destination = owner.get(anchor);
            if (destination === undefined) throw new Error(`#${anchor} has no carved heading`);
            return destination === fileName ? match : `](./${destination}.md#${anchor})`;
        });
}

fs.mkdirSync(path.join(root, ".agents/rules"), { recursive: true });
for (const file of FILES) {
    let body = file.sections
        .map((name) => sections.get(name).join("\n").trimEnd())
        .join("\n\n");
    body = rewrite(body, file.name);
    if (file.name === "code-navigation") {
        const lines = body.split("\n");
        const at = lines.findIndex((line) => line.startsWith(ASSET_RULE));
        if (at === -1) throw new Error("the asset-editing rule was not found");
        lines[at] = ASSET_POINTER;
        body = lines.join("\n");
    }
    if (file.name === "packages") {
        const lines = body.split("\n");
        for (const [line, replacement] of TREE) {
            const at = lines.indexOf(line);
            if (at === -1) throw new Error(`the layout tree has no line "${line}"`);
            lines[at] = replacement;
        }
        body = lines.join("\n");
    }
    if (file.name === "documentation-voice") body +=`\n\n${ROUTING.trimEnd()}`;
    const text = `# ${file.title}\n\n${file.intro}\n\n${body}\n`;
    fs.writeFileSync(path.join(root, ".agents/rules", `${file.name}.md`), text);
}

fs.writeFileSync(
    path.join(root, ".agents/rules/MANIFEST.tsv"),
    "# Every heading carved out of AGENTS.md and the file that holds it: heading, a tab, the file.\n" +
        `${manifest.join("\n")}\n`,
);
console.log(`carved ${FILES.length} files, ${manifest.length} manifest rows`);
```

- [ ] **Step 2: Run it**

Run: `node <scratch>/carve-agents.mjs <wt>`
Expected: `carved 8 files, 26 manifest rows`. It throws instead if `AGENTS.md` has a section missing from its table, or an in-file anchor with no carved heading.

- [ ] **Step 3: Check that the sections moved verbatim**

Read `releases.md` against `AGENTS.md` § Distribution and releases.
Expected: the body is identical apart from its links — `](./docs/…)` has become `](../../docs/…)`, and anchors to headings carved elsewhere have become `](./git-workflow.md#branching)` and `](./packages.md#package-catalogue)`.

Run: `git -C <wt> grep -n -e "Scenes, prefabs and" -e "rules/  " -e "docs-check.mjs  " -e "INDEX.md  " -e "agent entry point" -e "pointers to AGENTS.md" -- .agents/rules`
Expected: six lines — the asset-editing pointer in `code-navigation.md`, which replaced the bullet that began `**Only modify Unity assets`, and five lines of the layout tree in `packages.md`. (The files are untracked until Step 4, so run this after staging them, or use `grep -rn` with the same patterns.)

- [ ] **Step 4: Format and check**

Run: `npx prettier --write ".agents/rules/*.md"`, `git -C <wt> add .agents/rules`, then `node Tools/docs-check.mjs`.
Expected: exit 1 with these findings only — the two skill links resolve in Task 4, the rest in Task 5:

```text
link    .agents/rules/code-navigation.md:89  ../skills/unity-asset-editing/SKILL.md does not exist
link    .agents/rules/releases.md:3  ../skills/releasing-packages/SKILL.md does not exist
link    docs/specs/2026-08-23-upm-package-registry-github-design.md:123  ../../.agents/AGENTS.md does not exist
link    docs/specs/2026-09-05-basic-rename-and-moq-design.md:40  ../../.agents/AGENTS.md#distribution-and-releases does not exist
link    docs/specs/2026-09-13-auto-formatting-design.md:10  ../../.agents/AGENTS.md#c-coding-style does not exist
link    docs/specs/2026-09-13-auto-formatting-design.md:143  ../../.agents/AGENTS.md#ci does not exist
anchor  docs/plans/2026-09-02-release-promotion.md:167  #changelogs--two-rules-enforced-in-ci: no heading #changelogs--two-rules-enforced-in-ci in docs/plans/2026-09-02-release-promotion.md
budget  AGENTS.md  57794 bytes; the budget is 6000-12000
docs-check: 8 problem(s). Fix them, or see .agents/rules/documentation-voice.md.
```

No `heading` finding: every manifest row names a heading in its file.

- [ ] **Step 5: Commit**

```bash
npx prettier --check .
git -C <wt> add .agents/rules
git -C <wt> commit -m "docs: carve AGENTS.md into .agents/rules/ with a heading manifest"
```

---

### Task 4: Four authored skills

**Files:**

- Create: `.agents/skills/releasing-packages/SKILL.md`, `.agents/skills/adding-a-package/SKILL.md`, `.agents/skills/unity-asset-editing/SKILL.md`, `.agents/skills/feature-worktree/SKILL.md`, and the same four under `.claude/skills/`
- Modify: `.agents/skills/THIRD_PARTY_SKILLS.md`, `.agents/skills/sharplens-mcp/SKILL.md`, and their `.claude/skills/` copies
- Modify: `.agents/rules/code-navigation.md` (§ Skills table), `.agents/rules/MANIFEST.tsv`

**Interfaces:**

- Consumes: the rules files and anchors from Task 3.
- Produces: the skill paths `.agents/skills/<name>/SKILL.md` that the core's tables (Task 5) link to, with the headings `## Unity `.meta` files` and `## Adding a new package` that the manifest names.

`.prettierignore` excludes both skill folders, so these files are committed exactly as written.

- [ ] **Step 1: Write `releasing-packages`**

Create `.agents/skills/releasing-packages/SKILL.md`:

````markdown
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
````

- [ ] **Step 2: Write `adding-a-package`**

Create `.agents/skills/adding-a-package/SKILL.md`. Its first list is `AGENTS.md` § Adding a new package, verbatim apart from the link:

```markdown
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
```

- [ ] **Step 3: Write `unity-asset-editing`**

Create `.agents/skills/unity-asset-editing/SKILL.md`. Its `.meta` section is `AGENTS.md` § Unity `.meta` files, verbatim:

```markdown
---
name: unity-asset-editing
description: Use when a change touches a Unity scene, prefab, ScriptableObject .asset or any .meta file in this repo — creating, moving, renaming or deleting an asset, or when editing Unity YAML by hand looks like the quick fix.
---

# Unity asset editing

The packages in this repo ship their assets and GUIDs to every consumer, so two rules hold without exception.

## Never hand-edit asset YAML

Only modify Unity assets (`.unity` scenes, `.prefab` files, `.asset` ScriptableObjects, etc.) through `unity command` or the Unity Editor itself — never by hand-editing their YAML with a text tool. If the `unity command` call you need is broken, report the bug and find another Editor-mediated path — another command, `unity command eval` against the `UnityEditor` API, or a person making the change in the Editor — rather than falling back to a raw file edit. `unity command` with no arguments lists what is available; [`code-navigation.md`](../../../.agents/rules/code-navigation.md) covers the CLI.

## Unity `.meta` files

⚠️ **Never delete, ignore, or hand-create a `.meta` file carelessly.** In this repo the rule is stricter than in a game project, because these files ship to consumers:

- Every file _and folder_ in a package has a `.meta` carrying a GUID.
- Asmdef GUIDs are referenced by other asmdefs (`"references": ["GUID:..."]`). Losing one silently breaks compilation in dependent packages.
- Always commit an asset and its `.meta` together.
- `npm pack` includes `.meta` files automatically — verify with `npm pack --dry-run` when adding root-level files.

`node Tools/upm-release.mjs validate` fails any package with a file or folder that has no `.meta`.
```

- [ ] **Step 4: Write `feature-worktree`**

Create `.agents/skills/feature-worktree/SKILL.md`:

````markdown
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
````

- [ ] **Step 5: Record them as authored in-repo**

In `.agents/skills/THIRD_PARTY_SKILLS.md`, add a row after the `sharplens-mcp` row:

```text
| `releasing-packages`, `adding-a-package`, `unity-asset-editing`, `feature-worktree` | **Authored in-repo** | — |
```

In the same file, replace

```text
[`AGENTS.md`](../../AGENTS.md)'s agent-tooling section
```

with

```text
[`code-navigation.md`](../../.agents/rules/code-navigation.md)
```

In `.agents/skills/sharplens-mcp/SKILL.md`, replace

```text
[`AGENTS.md`](../../../AGENTS.md)'s catalogue and dependency graph
```

with

```text
the [package catalogue](../../../.agents/rules/packages.md#package-catalogue) and its dependency graph
```

`lifeblood-mcp/SKILL.md` also links `AGENTS.md`; it is vendored, and the link still resolves, so it stays.

- [ ] **Step 6: List them in the Skills table and the manifest**

In `.agents/rules/code-navigation.md` § Skills, add four rows above the `unity-cli` row:

```text
| `releasing-packages` | Preparing and running a release, and fixing a failed changelog check. |
| `adding-a-package` | Creating a package from `PackageTemplate`. |
| `unity-asset-editing` | Any change to a scene, prefab, `.asset` or `.meta` file. |
| `feature-worktree` | Starting a branch in a worktree from `origin/dev`, and finishing at the pull request. |
```

Append two rows to `.agents/rules/MANIFEST.tsv` (a tab between the columns):

```text
Unity `.meta` files	.agents/skills/unity-asset-editing/SKILL.md
Adding a new package	.agents/skills/adding-a-package/SKILL.md
```

- [ ] **Step 7: Mirror to `.claude/skills/`**

```bash
for s in releasing-packages adding-a-package unity-asset-editing feature-worktree sharplens-mcp; do
  mkdir -p <wt>/.claude/skills/$s
  cp <wt>/.agents/skills/$s/SKILL.md <wt>/.claude/skills/$s/SKILL.md
done
cp <wt>/.agents/skills/THIRD_PARTY_SKILLS.md <wt>/.claude/skills/THIRD_PARTY_SKILLS.md
```

- [ ] **Step 8: Check**

Run: `npx prettier --write .agents/rules/code-navigation.md`, `git -C <wt> add .agents .claude/skills`, then `node Tools/docs-check.mjs`.
Expected: exit 1 with only the four spec links, the release-promotion anchor and the budget — no `link` or `anchor` finding under `.agents/`, and no `mirror` or `heading` finding.

Run: `node -e "const t=require('fs').readFileSync('.agents/rules/MANIFEST.tsv','utf8'); if(/ {2,}\S/.test(t.split('\n').slice(1).join('\n'))) throw 'spaces, not a tab'"`
Expected: no output — the new rows use a tab.

- [ ] **Step 9: Commit**

```bash
npx prettier --check .
git -C <wt> add .agents .claude/skills
git -C <wt> commit -m "docs: add the releasing-packages, adding-a-package, unity-asset-editing and feature-worktree skills"
```

---

### Task 5: The new core, and every pointer into the old one

**Files:**

- Replace: `AGENTS.md`
- Modify: `CLAUDE.md`, `QWEN.md`, `README.md`, `.github/rulesets/README.md`, `.editorconfig`, `.gitignore`, `.github/workflows/changelog.yml`, `.github/workflows/format.yml`, `Tools/upm-release.mjs`
- Modify: `docs/specs/2026-08-23-upm-package-registry-github-design.md:123`, `docs/specs/2026-09-05-basic-rename-and-moq-design.md:41`, `docs/specs/2026-09-13-auto-formatting-design.md:11` and `:144`, `docs/plans/2026-09-02-release-promotion.md:165`

**Interfaces:**

- Consumes: every rules file, skill and anchor from Tasks 2–4.
- Produces: an `AGENTS.md` of 6,000–12,000 bytes with the heading `## What this repo is`, which `MANIFEST.tsv` names.

- [ ] **Step 1: Replace `AGENTS.md`**

Overwrite `AGENTS.md` with:

````markdown
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

| Touching                                                            | Read first                                                                       |
| ------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| Repo layout, package anatomy, asmdef names, the package catalogue   | [`.agents/rules/packages.md`](./.agents/rules/packages.md)                       |
| Unity tests, test doubles, Moq in a test asmdef                     | [`.agents/rules/testing.md`](./.agents/rules/testing.md)                         |
| `.github/workflows/`, `Tools/ci/`, the self-hosted runner           | [`.agents/rules/ci.md`](./.agents/rules/ci.md)                                   |
| C# navigation, refactoring, `sharplens`/`lifeblood`/`unity command` | [`.agents/rules/code-navigation.md`](./.agents/rules/code-navigation.md)         |
| `Tools/upm-release.mjs`, `Tools/release.bat`, changelogs, OpenUPM   | [`.agents/rules/releases.md`](./.agents/rules/releases.md)                       |
| Branches, rulesets, required checks, `gh`                           | [`.agents/rules/git-workflow.md`](./.agents/rules/git-workflow.md)               |
| C# naming and style, CSharpier, Prettier, `.editorconfig`           | [`.agents/rules/code-style.md`](./.agents/rules/code-style.md)                   |
| Any document, README, skill or code comment                         | [`.agents/rules/documentation-voice.md`](./.agents/rules/documentation-voice.md) |
| Preparing or running a release                                      | [`releasing-packages`](./.agents/skills/releasing-packages/SKILL.md)             |
| A new package under `Packages/`                                     | [`adding-a-package`](./.agents/skills/adding-a-package/SKILL.md)                 |
| A scene, prefab, `.asset` or `.meta`                                | [`unity-asset-editing`](./.agents/skills/unity-asset-editing/SKILL.md)           |
| Starting or finishing a branch                                      | [`feature-worktree`](./.agents/skills/feature-worktree/SKILL.md)                 |
| Any feature's design or plan                                        | [`docs/INDEX.md`](./docs/INDEX.md)                                               |

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
- **Documentation** — `npm run check:docs` verifies that every cross-file link and heading anchor resolves, that `.claude/skills/` matches `.agents/skills/`, and that this file stays inside its byte budget. Run it after editing anything under `.agents/` or `docs/`.

## Documentation

- [`docs/INDEX.md`](./docs/INDEX.md) lists every feature's design spec (`docs/specs/`) and implementation plan (`docs/plans/`) with its status. Read the relevant document before changing a feature it covers; new work adds its row in the same commit as its spec.
- [`.agents/rules/`](./.agents/rules) holds the detailed rules, one file per area, reachable through the routing table above.
- Every document describes the project as it is now, never the change that produced it; the dated specs and plans are the exception, because they record a decision as it was made. [`.agents/rules/documentation-voice.md`](./.agents/rules/documentation-voice.md) carries the rule in full, and it governs code comments too.
````

- [ ] **Step 2: Point the dated documents at the new files**

Each is a link repair; the surrounding text stays.

`docs/specs/2026-08-23-upm-package-registry-github-design.md` — replace

```text
This table duplicates [`.agents/AGENTS.md`](../../.agents/AGENTS.md) § _Package catalogue_, which is
```

with

```text
This table duplicates the [package catalogue](../../.agents/rules/packages.md#package-catalogue), which is
```

The other four:

| File                                                   | Replace                                               | With                                                                      |
| ------------------------------------------------------ | ----------------------------------------------------- | ------------------------------------------------------------------------- |
| `docs/specs/2026-09-05-basic-rename-and-moq-design.md` | `(../../.agents/AGENTS.md#distribution-and-releases)` | `(../../.agents/rules/releases.md#distribution-and-releases)`             |
| `docs/specs/2026-09-13-auto-formatting-design.md`      | `(../../.agents/AGENTS.md#c-coding-style)`            | `(../../.agents/rules/code-style.md#c-coding-style)`                      |
| `docs/specs/2026-09-13-auto-formatting-design.md`      | `(../../.agents/AGENTS.md#ci)`                        | `(../../.agents/rules/ci.md#ci)`                                          |
| `docs/plans/2026-09-02-release-promotion.md`           | `(#changelogs--two-rules-enforced-in-ci)`             | `(../../.agents/rules/releases.md#changelogs--four-rules-enforced-in-ci)` |

Mentions of `.agents/AGENTS.md` inside code spans in the dated documents are the path as it was when they were written, and stay.

- [ ] **Step 3: Point the entry files and comments at the new files**

`CLAUDE.md` — replace

```text
All developer and AI-agent guidance for this repo lives in [`AGENTS.md`](./AGENTS.md), imported below so Claude Code loads it at session start.
```

with

```text
Developer and AI-agent guidance starts at [`AGENTS.md`](./AGENTS.md), imported below so Claude Code loads it at session start. It routes to the file that holds each area in full.
```

`QWEN.md` — replace

```text
All developer and AI-agent guidance for this repo lives in [`AGENTS.md`](./AGENTS.md).

Read it before making changes. It covers the repo layout, package anatomy and asmdef conventions, the package catalogue and dependency graph, the release/versioning flow, C# coding style, `.meta` file rules, how to add a new package, and the known inconsistencies that should be left alone.
```

with

```text
Developer and AI-agent guidance starts at [`AGENTS.md`](./AGENTS.md): the rules that are expensive to break, and a routing table to the file under [`.agents/rules/`](./.agents/rules) or [`.agents/skills/`](./.agents/skills) that holds each area in full.

Read it before making changes.
```

and, in the same file, `See [Branching](./AGENTS.md#branching).` with `See [Branching](./.agents/rules/git-workflow.md#branching).`

`README.md` — replace

```text
Read [`AGENTS.md`](AGENTS.md) first. It covers the package anatomy, the asmdef and
`.meta` conventions, the release flow, and the deliberate inconsistencies that must be left alone.
```

with

```text
Read [`AGENTS.md`](AGENTS.md) first. It holds the rules that are expensive to break and routes to
[`.agents/rules/`](.agents/rules) for package anatomy, asmdef and `.meta` conventions, the release
flow, and the deliberate inconsistencies that must be left alone.
```

The rest are one-line comment or link edits:

| File                              | Replace                                                                       | With                                                                                        |
| --------------------------------- | ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| `.github/rulesets/README.md`      | ``[the ruleset table in `AGENTS.md`](../../AGENTS.md#branching)``             | ``[the ruleset table in `git-workflow.md`](../../.agents/rules/git-workflow.md#branching)`` |
| `.editorconfig`                   | `# Naming, from "C# coding style" in AGENTS.md.`                              | `# Naming, from "C# coding style" in .agents/rules/code-style.md.`                          |
| `.gitignore`                      | `(see AGENTS.md)`                                                             | `(see .agents/rules/releases.md)`                                                           |
| `.github/workflows/changelog.yml` | `— see AGENTS.md:`                                                            | `— see .agents/rules/releases.md:`                                                          |
| `.github/workflows/format.yml`    | `"Formatting" in` + newline + `# AGENTS.md.` (a comment split over two lines) | `"Formatting" in` + newline + `# .agents/rules/code-style.md.`                              |
| `.github/workflows/format.yml`    | `see "CI" in AGENTS.md for`                                                   | `see "CI" in .agents/rules/ci.md for`                                                       |
| `Tools/upm-release.mjs`           | `// AGENTS.md § Naming.`                                                      | `// .agents/rules/packages.md § Naming.`                                                    |

- [ ] **Step 4: Check**

Run: `npx prettier --write AGENTS.md CLAUDE.md QWEN.md README.md .github/rulesets/README.md "docs/**/*.md" ".github/workflows/*.yml" Tools/upm-release.mjs`, `git -C <wt> add -A`, then `node Tools/docs-check.mjs`.
Expected: exit 0, `docs-check: all five checks pass.`

Run: `node -e "console.log(Buffer.byteLength(require('fs').readFileSync('AGENTS.md','utf8').replace(/\r\n/g,'\n')))"`
Expected: about 10,500 — between 6,000 and 12,000.

Run: `git -C <wt> grep -n "AGENTS.md" -- ':!docs/specs' ':!docs/plans' ':!AGENTS.md'`
Expected: hits only in `CLAUDE.md`, `QWEN.md`, `README.md`, `docs/INDEX.md`, `.agents/rules/{MANIFEST.tsv,documentation-voice.md,packages.md}`, `Tools/docs-check{,.test}.mjs` and the two `lifeblood-mcp/SKILL.md` copies — each naming `AGENTS.md` as the entry point or the file the checker measures, none citing a section it no longer holds.

- [ ] **Step 5: Run the tooling tests**

Run: `node --test Tools/*.test.mjs`
Expected: PASS, 163 tests. `Tools/upm-release.mjs` changed only in a comment. (Without `npm ci`, the one test that runs the repo's Prettier fails.)

- [ ] **Step 6: Commit**

```bash
npx prettier --check .
git -C <wt> add -A
git -C <wt> status --short   # only the files listed in this task
git -C <wt> commit -m "docs: replace AGENTS.md with a routing core and repoint every link into it"
```

---

### Task 6: Wire the checker into npm and CI, and land

**Files:**

- Modify: `package.json`, `.github/workflows/tests.yml` (the `tooling-tests` job), `Tools/docs-check.test.mjs`
- Modify: `docs/specs/2026-09-26-documentation-restructure-design.md`, this plan, `docs/INDEX.md` (status)

**Interfaces:**

- Consumes: everything above.
- Produces: `npm run check:docs`; the `tooling-tests` steps "Test the docs check" and "Check the docs".

- [ ] **Step 1: Add the failing self-test**

Append to `Tools/docs-check.test.mjs`:

```js
test("the repository itself passes", () => {
    const { status, report } = run(path.dirname(HERE));
    assert.equal(status, 0, JSON.stringify(report.findings, null, 2));
});
```

Run: `node --test Tools/docs-check.test.mjs`
Expected: PASS, 25 tests. (It would have failed before Task 5; it pins the repo's own state from here on.)

- [ ] **Step 2: Add the npm script**

In `package.json`, after the `format:check` line, add `"check:docs": "node Tools/docs-check.mjs"` (with a comma on the line before):

```json
  "scripts": {
    "format": "dotnet csharpier format . && prettier --write .",
    "format:check": "dotnet csharpier check . && prettier --check .",
    "check:docs": "node Tools/docs-check.mjs"
  },
```

Run: `npm run check:docs`
Expected: `docs-check: all five checks pass.`

- [ ] **Step 3: Run it in `tooling-tests`**

In `.github/workflows/tests.yml`, after the "Test the release tooling" step, add:

```yaml
      - name: Test the docs check
        if: always()
        run: node --test Tools/docs-check.test.mjs

      - name: Check the docs
        if: always()
        run: node Tools/docs-check.mjs
```

`if: always()` matches the steps before it: one failing suite does not hide an independent failure after it. `tooling-tests` already checks out with `actions/checkout`, which gives the checker the git repository it needs.

- [ ] **Step 4: Mark this feature implemented**

- `docs/specs/2026-09-26-documentation-restructure-design.md`: `**Status:** Designed` → `**Status:** Implemented`.
- This plan: `**Status:** Planned` → `**Status:** Implemented`.
- `docs/INDEX.md`, the "Documentation restructure" row: `Planned` → `Implemented`.

- [ ] **Step 5: Verify everything**

```bash
npm run check:docs
node --test Tools/*.test.mjs
npx prettier --check .
```

Expected: `all five checks pass`; 164 Node tests pass; Prettier reports `All matched files use Prettier code style!`. If `actionlint` is installed, run it on `.github/workflows/tests.yml`.

Then read the old file against its new homes: `git -C <wt> show origin/dev:AGENTS.md`, one `##` section at a time, beside the file `MANIFEST.tsv` names for it. Expected: every paragraph, list item, table row and code block is present in its new home. The deliberate differences are the ones the carve script makes, "What this repo is" rewritten as one paragraph in the core, and the § Adding a new package changelog link retargeted in its skill.

- [ ] **Step 6: Commit and push**

```bash
git -C <wt> add package.json .github/workflows/tests.yml Tools/docs-check.test.mjs docs
git -C <wt> commit -m "ci: run docs-check in tooling-tests and mark the documentation restructure implemented"
git -C <wt> push origin docs/agent-docs-restructure
```

- [ ] **Step 7: Open the pull request and stop**

```bash
gh pr create --base dev --head docs/agent-docs-restructure \
  --title "docs: restructure the agent docs into a routing AGENTS.md, rules, skills and an index" \
  --body-file <scratch>/pr-body.md
```

`pr-body.md` summarises the change (the core's size, the eight rules files, the four skills, `docs/INDEX.md`, the checker and its CI step), lists the verification commands above with their results, and ends with the session's attribution lines. Its test plan also carries the two checks only a reviewer can make:

- A fresh session in Claude Code and in OpenCode, asked nothing about the rules, has the non-negotiables table in context.
- Asked to cut a release, the session opens the `releasing-packages` skill.

Run `gh pr checks <number> --watch` and report the result with the pull request URL; a red check is fixed with a new commit on the branch. **Do not merge.**
