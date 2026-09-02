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
            // validate requires a sibling .meta for every real file, exactly as
            // Unity would generate one; prepare re-validates what it touches.
            fs.writeFileSync(path.join(dir, `${name}.meta`), "fileFormatVersion: 2\r\nguid: 00000000000000000000000000000000\r\n");
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
