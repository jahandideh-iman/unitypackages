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

/** A CHANGELOG with an empty [Unreleased] section — the seeded shape. */
function changelog(unreleasedBody = "") {
    return `${PREAMBLE}## [Unreleased]\n\n${unreleasedBody}${RELEASED}`;
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
 * commits it on the base branch, then applies `head` on a branch off it.
 *
 * `base` and `head` are objects of { relativePath: contents | null }, where
 * null deletes the file.
 */
function makeRepo(t, base, head) {
    const repo = fs.mkdtempSync(path.join(os.tmpdir(), "changelog-check-"));
    t.after(() => fs.rmSync(repo, { recursive: true, force: true }));

    git(repo, "init", "--initial-branch", "main");
    git(repo, "config", "user.email", "test@example.com");
    git(repo, "config", "user.name", "Test");
    git(repo, "config", "commit.gpgsign", "false");

    fs.mkdirSync(path.join(repo, "Tools"), { recursive: true });
    fs.copyFileSync(SCRIPT, path.join(repo, "Tools", "changelog-check.mjs"));

    applyAndCommit(repo, base, "base");
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

function check(repo, { base = "main", head = "feature", env = {} } = {}) {
    const result = spawnSync(
        process.execPath,
        ["Tools/changelog-check.mjs", "--base", base, "--head", head, "--json"],
        { cwd: repo, encoding: "utf8", env: { ...process.env, ...env } },
    );
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
        "Packages/Alpha/CHANGELOG.md": changelog(),
        "Packages/Alpha/README.md": "# Alpha\n",
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { }\n",
        "Packages/Alpha/Runtime/Alpha.cs.meta": "guid: 1111\n",
        "Packages/Alpha/Tests/AlphaTests.cs": "public class AlphaTests { }\n",
        ...extra,
    };
}

function statusOf(report, folder) {
    return report.packages.find((p) => p.folder === folder)?.status;
}

test("fails when runtime code changes without an [Unreleased] entry", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.equal(report.ok, false);
    assert.equal(statusOf(report, "Alpha"), "missing-entry");
});

test("passes when runtime code changes alongside an [Unreleased] entry", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
        "Packages/Alpha/CHANGELOG.md": changelog("### Added\n\n- A `Value` field on `Alpha`.\n\n"),
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.equal(report.ok, true);
    assert.equal(statusOf(report, "Alpha"), "ok");
});

test("fails when the only [Unreleased] addition is a bare sub-heading", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
        "Packages/Alpha/CHANGELOG.md": changelog("### Added\n\n"),
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.equal(statusOf(report, "Alpha"), "missing-entry");
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
    assert.equal(statusOf(report, "Alpha"), "missing-entry");
});

test("fails when editor code changes without an [Unreleased] entry", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Editor/AlphaEditor.cs": "public class AlphaEditor { }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.equal(statusOf(report, "Alpha"), "missing-entry");
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
    assert.equal(statusOf(report, "Alpha"), "skipped-private");
});

test("skips a package that is new in the pull request", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Beta/package.json": JSON.stringify({ name: "com.arman.beta", version: "0.1.0" }, null, 2),
        "Packages/Beta/CHANGELOG.md": changelog(),
        "Packages/Beta/Runtime/Beta.cs": "public class Beta { }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 0);
    assert.equal(statusOf(report, "Beta"), "skipped-new");
});

test("reports a missing [Unreleased] heading distinctly from a missing entry", (t) => {
    const base = alphaBase({ "Packages/Alpha/CHANGELOG.md": changelogWithoutUnreleased() });
    const repo = makeRepo(t, base, {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.equal(statusOf(report, "Alpha"), "missing-section");
});

test("reports a missing CHANGELOG distinctly", (t) => {
    const base = alphaBase();
    delete base["Packages/Alpha/CHANGELOG.md"];
    const repo = makeRepo(t, base, {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.equal(statusOf(report, "Alpha"), "missing-changelog");
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
    assert.equal(statusOf(report, "Beta"), "ok");
});

test("handles a package folder whose name contains a space", (t) => {
    const base = {
        "Packages/Scene Management/package.json": JSON.stringify(
            { name: "com.arman.scene-management", version: "0.1.0" },
            null,
            2,
        ),
        "Packages/Scene Management/CHANGELOG.md": changelog(),
        "Packages/Scene Management/Runtime/Scenes.cs": "public class Scenes { }\n",
    };
    const repo = makeRepo(t, base, {
        "Packages/Scene Management/Runtime/Scenes.cs": "public class Scenes { public int N; }\n",
    });

    const { status, report } = check(repo);

    assert.equal(status, 1);
    assert.equal(statusOf(report, "Scene Management"), "missing-entry");
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

test("passes when the no-changelog label is present", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status, report } = check(repo, { env: { PR_LABELS: '["no-changelog","chore"]' } });

    assert.equal(status, 0);
    assert.equal(report.ok, true);
    assert.equal(report.skipped, "no-changelog label");
});

test("ignores unrelated labels", (t) => {
    const repo = makeRepo(t, alphaBase(), {
        "Packages/Alpha/Runtime/Alpha.cs": "public class Alpha { public int Value; }\n",
    });

    const { status } = check(repo, { env: { PR_LABELS: '["bug","chore"]' } });

    assert.equal(status, 1);
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
