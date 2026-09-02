// Tests for Tools/changelog-check.mjs.
//
// Each test builds a throwaway git repo with a realistic Packages/ layout,
// commits a base and a head, and runs the real script inside it as a
// subprocess — the same way CI invokes it. Nothing is mocked.
//
//     node --test Tools/changelog-check.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const SCRIPT = path.join(HERE, "changelog-check.mjs");

const PREAMBLE = `# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

`;

const RELEASED = `## [0.1.0] - 2026-08-29

Initial release.
`;

/**
 * A placeholder [Unreleased] entry, used wherever a fixture needs *some*
 * entry under the heading but the test itself isn't about the entry's
 * content. Since the `empty-unreleased` rule is repo-wide, an untouched
 * package's CHANGELOG must not be left with a genuinely empty heading, or
 * every diff-scoped test would trip it as a side effect.
 */
const STABLE_ENTRY = "### Fixed\n\n- Unrelated cleanup.\n\n";

/** A CHANGELOG with an empty [Unreleased] section — the seeded shape. */
function changelog(unreleasedBody = "", released = RELEASED) {
    return `${PREAMBLE}## [Unreleased]\n\n${unreleasedBody}${released}`;
}

/** A CHANGELOG with no [Unreleased] heading at all — the pre-seed shape. */
function changelogWithoutUnreleased() {
    return PREAMBLE + RELEASED;
}

function git(cwd, ...args) {
    const result = spawnSync("git", args, { cwd, encoding: "utf8" });
    if (result.status !== 0) {
        throw new Error(`git ${args.join(" ")} failed: ${result.stderr || result.stdout}`);
    }
    return result.stdout.trim();
}

function write(repo, relative, contents) {
    const target = path.join(repo, relative);
    fs.mkdirSync(path.dirname(target), { recursive: true });
    fs.writeFileSync(target, contents);
}

/**
 * Creates a temp repo containing the script under test, applies `base`,
 * commits it on the base branch, tags that commit with `tags`, then applies
 * `head` on a branch off it.
 *
 * `base` and `head` are objects of { relativePath: contents | null }, where
 * null deletes the file.
 */
function makeRepo(t, base, head, { tags = [] } = {}) {
    const repo = fs.mkdtempSync(path.join(os.tmpdir(), "changelog-check-"));
    t.after(() => fs.rmSync(repo, { recursive: true, force: true }));

    git(repo, "init", "--initial-branch", "main");
    git(repo, "config", "user.email", "test@example.com");
    git(repo, "config", "user.name", "Test");
    git(repo, "config", "commit.gpgsign", "false");

    fs.mkdirSync(path.join(repo, "Tools"), { recursive: true });
    fs.copyFileSync(SCRIPT, path.join(repo, "Tools", "changelog-check.mjs"));

    applyAndCommit(repo, base, "base");
    for (const tag of tags) git(repo, "tag", tag);
    git(repo, "checkout", "-b", "feature");
    applyAndCommit(repo, head, "head");

    return repo;
}

function applyAndCommit(repo, changes, message) {
    for (const [relative, contents] of Object.entries(changes)) {
        if (contents === null) {
            fs.rmSync(path.join(repo, relative), { force: true });
        } else {
            write(repo, relative, contents);
        }
    }
    git(repo, "add", "-A");
    git(repo, "commit", "-m", message);
}

function check(repo, { base = "main", head = "feature", baseBranch = null, env = {} } = {}) {
    const args = ["Tools/changelog-check.mjs", "--base", base, "--head", head, "--json"];
    if (baseBranch !== null) args.push("--base-branch", baseBranch);
    const result = spawnSync(process.execPath, args, {
        cwd: repo,
        encoding: "utf8",
        env: { ...process.env, ...env },
    });
    let report;
    try {
        report = JSON.parse(result.stdout);
    } catch {
        throw new Error(`non-JSON output (exit ${result.status}):\n${result.stdout}\n${result.stderr}`);
    }
    return { status: result.status, report, stderr: result.stderr };
}

/** The baseline repo: one package, seeded CHANGELOG, one runtime file. */
function alphaBase(extra = {}) {
    return {
        "Packages/Alpha/package.json": JSON.stringify({ name: "com.arman.alpha", version: "0.1.0" }, null, 2),
        "Packages/Alpha/CHANGELOG.md": changelog(STABLE_ENTRY),
        "Packages/Alpha/README.md": "# Alpha\n",
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { }\n",
        "Packages/Alpha/Runtime/Alpha.cs.meta": "guid: 1111\n",
        "Packages/Alpha/Tests/AlphaTests.cs": "public class AlphaTests { }\n",
        ...extra,
    };
}

const ALPHA_TAG = "com.arman.alpha/0.1.0";

function entryFor(folder, report) {
    return report.packages.find((p) => p.folder === folder);
}

/** The rule names a package tripped, e.g. ["missing-entry"]. */
function problemsOf(report, folder) {
    return (entryFor(folder, report)?.problems ?? []).map((p) => p.rule);
}

function skipOf(report, folder) {
    return entryFor(folder, report)?.skipped;
}

// ------------------------------------------------- the [Unreleased] entry rule

test("fails when runtime code changes without an [Unreleased] entry", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.equal(report.ok, false);
    assert.deepEqual(problemsOf(report, "Alpha"), ["missing-entry"]);
});

test("passes when runtime code changes alongside an [Unreleased] entry", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
        "Packages/Alpha/CHANGELOG.md": changelog("### Added\n\n- A `Value` field on `Alpha`.\n\n"),
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.equal(report.ok, true);
    assert.deepEqual(problemsOf(report, "Alpha"), []);
});

test("fails when the only [Unreleased] addition is a bare sub-heading", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
        "Packages/Alpha/CHANGELOG.md": changelog("### Added\n\n"),
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Alpha").sort(), ["empty-unreleased", "missing-entry"]);
});

test("fails when the package.json changes without an [Unreleased] entry", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/package.json": JSON.stringify(
            { name: "com.arman.alpha", version: "0.2.0" },
            null,
            2,
        ),
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Alpha"), ["missing-entry"]);
});

test("fails when editor code changes without an [Unreleased] entry", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Editor/AlphaEditor.cs": "public class AlphaEditor { }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Alpha"), ["missing-entry"]);
});

test("ignores a tests-only change", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Tests/AlphaTests.cs": "public class AlphaTests { public void T() { } }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.deepEqual(report.packages, []);
});

test("ignores a docs-only change", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/README.md": "# Alpha\n\nNow with prose.\n",
        "Packages/Alpha/Documentation/Alpha.md": "# Manual\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.deepEqual(report.packages, []);
});

test("ignores a .meta-only change under Runtime", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs.meta": "guid: 2222\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.deepEqual(report.packages, []);
});

test("ignores changes outside Packages/", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Assets/Scratch.cs": "public class Scratch { }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.deepEqual(report.packages, []);
});

test("skips a private package", (t) => {
    const base = alphaBase({
        "Packages/Alpha/package.json": JSON.stringify(
            { name: "com.arman.alpha", version: "0.1.0", private: true },
            null,
            2,
        ),
    });
    const repo = makeRepo(t, base, {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.equal(skipOf(report, "Alpha"), "private");
});

test("skips a package that is new in the pull request", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Beta/package.json": JSON.stringify({ name: "com.arman.beta", version: "0.1.0" }, null, 2),
        "Packages/Beta/CHANGELOG.md": changelog(),
        "Packages/Beta/Runtime/Beta.cs": "public class Beta { }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.equal(skipOf(report, "Beta"), "new");
});

test("reports a missing [Unreleased] heading distinctly from a missing entry", (t) => {
    const base = alphaBase({ "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased() });
    const repo = makeRepo(t, base, {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Alpha"), ["missing-section"]);
});

test("reports a missing CHANGELOG distinctly", (t) => {
    const base = alphaBase();
    delete base["Packages/Alpha/CHANGELOG.md"];
    const repo = makeRepo(t, base, {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Alpha"), ["missing-changelog"]);
});

test("requires an entry only from the package that changed", (t) => {
    const base = {
        ...alphaBase(),
        "Packages/Beta/package.json": JSON.stringify({ name: "com.arman.beta", version: "0.1.0" }, null, 2),
        "Packages/Beta/CHANGELOG.md": changelog(),
        "Packages/Beta/Runtime/Beta.cs": "public class Beta { }\n",
    };
    const repo = makeRepo(t, base, {
        "Packages/Beta/Runtime/Beta.cs": "public class Beta { public int Value; }\n",
        "Packages/Beta/CHANGELOG.md": changelog("### Added\n\n- A `Value` field on `Beta`.\n\n"),
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.equal(report.packages.length, 1);
    assert.deepEqual(problemsOf(report, "Beta"), []);
});

test("handles a package folder whose name contains a space", (t) => {
    const base = {
        "Packages/Scene Management/package.json": JSON.stringify(
            { name: "com.arman.scene-management", version: "0.1.0" },
            null,
            2,
        ),
        "Packages/Scene Management/CHANGELOG.md": changelog(STABLE_ENTRY),
        "Packages/Scene Management/Runtime/Scenes.cs": "public class Scenes { }\n",
    };
    const repo = makeRepo(t, base, {
        "Packages/Scene Management/Runtime/Scenes.cs": "public class Scenes { public int N; }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Scene Management"), ["missing-entry"]);
});

test("ignores commits added to the base branch after the head branched", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/README.md": "# Alpha\n\nDocs only.\n",
    });

    // A different package changes on main after `feature` branched. Diffing
    // against the branch tip rather than the merge base would blame this PR.
    git(repo, "checkout", "main");
    applyAndCommit(repo, {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Unrelated; }\n",
    }, "unrelated work on main");
    git(repo, "checkout", "feature");

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.deepEqual(report.packages, []);
});

// ------------------------------------------------- the frozen released-section rule

test("fails when an already-tagged version section is edited", (t) => {
    const repo = makeRepo(
        t,
        alphaBase(),
        {
            "Packages/Alpha/CHANGELOG.md": changelog(STABLE_ENTRY, "## [0.1.0] - 2026-08-29\n\nRewritten history.\n"),
        },
        { tags: [ALPHA_TAG] },
    );

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.equal(report.ok, false);
    assert.deepEqual(problemsOf(report, "Alpha"), ["frozen-section"]);
    assert.deepEqual(entryFor("Alpha", report).problems[0].versions, ["0.1.0"]);
});

test("fails when an already-tagged version's heading date is edited", (t) => {
    const repo = makeRepo(
        t,
        alphaBase(),
        {
            "Packages/Alpha/CHANGELOG.md": changelog(STABLE_ENTRY, "## [0.1.0] - 2020-01-01\n\nInitial release.\n"),
        },
        { tags: [ALPHA_TAG] },
    );

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Alpha"), ["frozen-section"]);
});

test("fails when an already-tagged version section is deleted", (t) => {
    const repo = makeRepo(
        t,
        alphaBase(),
        { "Packages/Alpha/CHANGELOG.md": `${PREAMBLE}## [Unreleased]\n\n${STABLE_ENTRY}` },
        { tags: [ALPHA_TAG] },
    );

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Alpha"), ["frozen-section"]);
});

test("checks frozen sections even when no shipped code changed", (t) => {
    // The CHANGELOG is Markdown, which the entry rule exempts. The frozen rule
    // has to look at it anyway, or released history can be rewritten silently.
    const repo = makeRepo(
        t,
        alphaBase(),
        {
            "Packages/Alpha/CHANGELOG.md": changelog(STABLE_ENTRY, "## [0.1.0] - 2026-08-29\n\nRewritten history.\n"),
        },
        { tags: [ALPHA_TAG] },
    );

    const { report } = check(repo);

    assert.deepEqual(entryFor("Alpha", report).files, ["CHANGELOG.md"]);
    assert.deepEqual(problemsOf(report, "Alpha"), ["frozen-section"]);
});

test("allows editing a version section that has no tag yet", (t) => {
    const untagged = "## [0.2.0] - 2026-08-30\n\nNot yet released.\n\n" + RELEASED;
    const base = alphaBase({ "Packages/Alpha/CHANGELOG.md": changelog(STABLE_ENTRY, untagged) });
    const repo = makeRepo(
        t,
        base,
        {
            "Packages/Alpha/CHANGELOG.md": changelog(
                STABLE_ENTRY,
                "## [0.2.0] - 2026-08-30\n\nStill being written.\n\n" + RELEASED,
            ),
        },
        { tags: [ALPHA_TAG] },
    );

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.deepEqual(problemsOf(report, "Alpha"), []);
});

test("allows renaming [Unreleased] to a version heading at release time", (t) => {
    const base = alphaBase({
        "Packages/Alpha/CHANGELOG.md": changelog("### Added\n\n- A `Value` field.\n\n"),
    });
    const repo = makeRepo(
        t,
        base,
        {
            "Packages/Alpha/CHANGELOG.md": changelog(
                STABLE_ENTRY,
                "## [0.2.0] - 2026-08-30\n\n### Added\n\n- A `Value` field.\n\n" + RELEASED,
            ),
            "Packages/Alpha/package.json": JSON.stringify(
                { name: "com.arman.alpha", version: "0.2.0" },
                null,
                2,
            ),
        },
        { tags: [ALPHA_TAG] },
    );

    const { status, report } = check(repo);

    assert.equal(status, 0, JSON.stringify(report, null, 2));
    assert.deepEqual(problemsOf(report, "Alpha"), []);
});

test("ignores a trailing-whitespace-only change in a tagged section", (t) => {
    const repo = makeRepo(
        t,
        alphaBase(),
        {
            "Packages/Alpha/CHANGELOG.md": changelog(STABLE_ENTRY, "## [0.1.0] - 2026-08-29  \n\nInitial release.   \n"),
        },
        { tags: [ALPHA_TAG] },
    );

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.deepEqual(problemsOf(report, "Alpha"), []);
});

test("reports a missing entry and a frozen-section edit together", (t) => {
    // The classic mistake: change code, then "update the changelog" by writing
    // into the released section instead of [Unreleased].
    const repo = makeRepo(
        t,
        alphaBase(),
        {
            "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
            "Packages/Alpha/CHANGELOG.md": changelog(
                STABLE_ENTRY,
                "## [0.1.0] - 2026-08-29\n\nInitial release.\n\n- A `Value` field.\n",
            ),
        },
        { tags: [ALPHA_TAG] },
    );

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Alpha").sort(), ["frozen-section", "missing-entry"]);
});

test("ignores tags belonging to a different package", (t) => {
    const repo = makeRepo(
        t,
        alphaBase(),
        {
            "Packages/Alpha/CHANGELOG.md": changelog(STABLE_ENTRY, "## [0.1.0] - 2026-08-29\n\nRewritten history.\n"),
        },
        { tags: ["com.arman.beta/0.1.0"] },
    );

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.deepEqual(problemsOf(report, "Alpha"), []);
});

// ------------------------------------------------------------ escape hatches

test("the no-changelog label waives a missing entry", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo, { env: { PR_LABELS: '["no-changelog","chore"]' } });

    assert.equal(status, 0);
    assert.equal(report.ok, true);
    assert.deepEqual(report.waived, ["no-changelog"]);
});

test("the no-changelog label waives a missing [Unreleased] heading", (t) => {
    // What a release pull request looks like: `upm-release.mjs prepare` has
    // turned the heading into a version heading, and the version bump in
    // package.json still counts as shipped code changing.
    const base = alphaBase({ "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased() });
    const repo = makeRepo(t, base, {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo, { env: { PR_LABELS: '["no-changelog"]' } });

    assert.equal(status, 0);
    assert.equal(report.ok, true);
});

test("the no-changelog label waives a missing CHANGELOG", (t) => {
    const base = alphaBase();
    delete base["Packages/Alpha/CHANGELOG.md"];
    const repo = makeRepo(t, base, {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo, { env: { PR_LABELS: '["no-changelog"]' } });

    assert.equal(status, 0);
    assert.equal(report.ok, true);
});

test("the no-changelog label does not waive a frozen-section edit", (t) => {
    // Different concerns: "this needs no entry" is not "I may rewrite 0.1.0".
    const repo = makeRepo(
        t,
        alphaBase(),
        {
            "Packages/Alpha/CHANGELOG.md": changelog(STABLE_ENTRY, "## [0.1.0] - 2026-08-29\n\nRewritten history.\n"),
        },
        { tags: [ALPHA_TAG] },
    );

    const { status, report } = check(repo, { env: { PR_LABELS: '["no-changelog"]' } });

    assert.equal(status, 1);
    assert.deepEqual(problemsOf(report, "Alpha"), ["frozen-section"]);
});

test("the changelog-rewrite label waives a frozen-section edit", (t) => {
    const repo = makeRepo(
        t,
        alphaBase(),
        {
            "Packages/Alpha/CHANGELOG.md": changelog(STABLE_ENTRY, "## [0.1.0] - 2026-08-29\n\nFixed a broken link.\n"),
        },
        { tags: [ALPHA_TAG] },
    );

    const { status, report } = check(repo, { env: { PR_LABELS: '["changelog-rewrite"]' } });

    assert.equal(status, 0);
    assert.deepEqual(report.waived, ["changelog-rewrite"]);
});

test("ignores unrelated labels", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo, { env: { PR_LABELS: '["bug","chore"]' } });

    assert.equal(status, 1);
    assert.deepEqual(report.waived, []);
});

test("tolerates a malformed PR_LABELS value", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status } = check(repo, { env: { PR_LABELS: "not json" } });

    assert.equal(status, 1);
});

test("writes a summary when GITHUB_STEP_SUMMARY is set", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });
    const summary = path.join(repo, "summary.md");

    check(repo, { env: { GITHUB_STEP_SUMMARY: summary } });

    assert.match(fs.readFileSync(summary, "utf8"), /Alpha/);
});

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

// A package added in the pull request, seeded with the empty [Unreleased]
// heading every new package starts with, must not trip empty-unreleased —
// it is new, the same as inspect()'s own "skipped: new" packages, and the
// repo-wide sweep in emptyUnreleasedFolders() must agree rather than
// re-deriving "is this package new" a second way.
test("empty-unreleased: a package new in the pull request is not reported", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Beta/package.json": JSON.stringify({ name: "com.arman.beta", version: "0.1.0" }, null, 2),
        "Packages/Beta/CHANGELOG.md": changelog(),
        "Packages/Beta/README.md": "# Beta\n",
    });
    const { status, report } = check(repo);
    assert.equal(status, 0);
    assert.equal(skipOf(report, "Beta"), "new");
    assert.deepEqual(problemsFor(report, "Beta"), []);
});

// ------------------------------------------------------ unpromoted-unreleased

// The release gate. On a pull request into `master` every [Unreleased] section
// should already have been turned into a version heading by `upm-release.mjs
// prepare`; one that survives means the promotion step was skipped, and the
// work would land on the release branch still labelled unreleased. The rule is
// inert on every other base branch, where a populated [Unreleased] is the
// healthy state that `missing-entry` insists on.

test("unpromoted-unreleased: a populated heading fails a pull request into master", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n",
    });
    const { status, report } = check(repo, { baseBranch: "master" });
    assert.equal(status, 1);
    assert.equal(report.ok, false);
    assert.deepEqual(problemsFor(report, "Alpha"), ["unpromoted-unreleased"]);
});

test("unpromoted-unreleased: an untouched package is reported too", (t) => {
    const repo = makeRepo(
        t,
        {
            ...alphaBase({ "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased() }),
            "Packages/Beta/package.json": JSON.stringify({ name: "com.arman.beta", version: "0.1.0" }, null, 2),
            "Packages/Beta/CHANGELOG.md": changelog("### Added\n\n- A thing.\n\n"),
        },
        { "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n" },
    );
    const { status, report } = check(repo, { baseBranch: "master" });
    assert.equal(status, 1);
    assert.deepEqual(problemsFor(report, "Beta"), ["unpromoted-unreleased"]);
});

test("unpromoted-unreleased: no heading at all is what a release pull request looks like", (t) => {
    const repo = makeRepo(t, alphaBase({ "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased() }), {
        "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n",
    });
    const { status, report } = check(repo, { baseBranch: "master" });
    assert.equal(status, 0);
    assert.equal(report.ok, true);
    assert.deepEqual(problemsFor(report, "Alpha"), []);
});

// An empty heading is unpromoted too, and reporting both rules against it would
// give the reader two diagnostics with one remedy between them.
test("unpromoted-unreleased: an empty heading reports this rule alone, not empty-unreleased", (t) => {
    const repo = makeRepo(t, alphaBase({ "Packages/Alpha/CHANGELOG.md": changelog() }), {
        "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n",
    });
    const { status, report } = check(repo, { baseBranch: "master" });
    assert.equal(status, 1);
    assert.deepEqual(problemsFor(report, "Alpha"), ["unpromoted-unreleased"]);
});

test("unpromoted-unreleased: inert on a pull request into dev", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n",
    });
    const { status, report } = check(repo, { baseBranch: "dev" });
    assert.equal(status, 0);
    assert.deepEqual(problemsFor(report, "Alpha"), []);
});

test("unpromoted-unreleased: inert when no base branch is supplied", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n",
    });
    const { status, report } = check(repo);
    assert.equal(status, 0);
    assert.deepEqual(problemsFor(report, "Alpha"), []);
});

test("unpromoted-unreleased: a private package is exempt", (t) => {
    const repo = makeRepo(
        t,
        {
            ...alphaBase({ "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased() }),
            "Packages/Template/package.json": JSON.stringify(
                { name: "com.arman.template", version: "0.1.0", private: true },
                null,
                2,
            ),
            "Packages/Template/CHANGELOG.md": changelog("### Added\n\n- A thing.\n\n"),
        },
        { "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n" },
    );
    const { status, report } = check(repo, { baseBranch: "master" });
    assert.equal(status, 0);
    assert.deepEqual(problemsFor(report, "Template"), []);
});

// Unlike empty-unreleased, a package new in the pull request gets no exemption
// here. `empty-unreleased` spares it because a brand-new package legitimately
// carries the empty scaffold heading while it is being written; a release pull
// request is the opposite situation — a package crossing into `master` for the
// first time still has to name the version it is publishing as.
test("unpromoted-unreleased: a package new in the pull request is not exempt", (t) => {
    const repo = makeRepo(t, alphaBase({ "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased() }), {
        "Packages/Beta/package.json": JSON.stringify({ name: "com.arman.beta", version: "0.1.0" }, null, 2),
        "Packages/Beta/CHANGELOG.md": changelog("### Added\n\n- A thing.\n\n"),
        "Packages/Beta/README.md": "# Beta\n",
    });
    const { status, report } = check(repo, { baseBranch: "master" });
    assert.equal(status, 1);
    assert.deepEqual(problemsFor(report, "Beta"), ["unpromoted-unreleased"]);
});

test("unpromoted-unreleased: has no waiver label", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/README.md": "# Alpha\n\nA doc fix.\n",
    });
    const { status, report } = check(repo, {
        baseBranch: "master",
        env: { PR_LABELS: JSON.stringify(["no-changelog", "changelog-rewrite"]) },
    });
    assert.equal(status, 1);
    assert.deepEqual(problemsFor(report, "Alpha"), ["unpromoted-unreleased"]);
});
