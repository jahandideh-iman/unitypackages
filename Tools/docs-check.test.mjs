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

test("the repository itself passes", () => {
    const { status, report } = run(path.dirname(HERE));
    assert.equal(status, 0, JSON.stringify(report.findings, null, 2));
});
