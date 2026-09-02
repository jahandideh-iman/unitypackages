// Tests for Tools/release-flow.mjs.
//
// The flow itself pushes and opens a pull request, so it is not something to
// exercise end to end. What is tested here is everything that decides *what*
// gets released and what the release says: the version diff across `prepare`,
// the commit message and pull request body built from it, and the argument
// guard that keeps the entry point parameterless.
//
//     node --test Tools/release-flow.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

import { readVersions, versionChanges, commitMessage, pullRequestBody } from "./release-flow.mjs";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const SCRIPT = path.join(HERE, "release-flow.mjs");
const BAT = path.join(HERE, "release.bat");

function packagesFixture(manifests) {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), "release-flow-"));
    for (const [folder, manifest] of Object.entries(manifests)) {
        const packageDir = path.join(dir, folder);
        fs.mkdirSync(packageDir, { recursive: true });
        fs.writeFileSync(path.join(packageDir, "package.json"), JSON.stringify(manifest, null, 2));
    }
    return dir;
}

// ------------------------------------------------------------ readVersions

test("readVersions reads publishable packages and skips private ones", () => {
    const dir = packagesFixture({
        ObjectPooling: { name: "com.arman.object-pooling", version: "0.1.0" },
        "UI Management": { name: "com.arman.ui-management", version: "0.2.1" },
        PackageTemplate: { name: "com.arman.package-template", version: "0.0.1", private: true },
    });
    const versions = readVersions(dir);
    assert.equal(versions.get("com.arman.object-pooling"), "0.1.0");
    // A folder name with a space is real in this repo (three of them).
    assert.equal(versions.get("com.arman.ui-management"), "0.2.1");
    assert.equal(versions.has("com.arman.package-template"), false);
});

test("readVersions ignores a malformed manifest rather than throwing", () => {
    const dir = packagesFixture({ Good: { name: "com.arman.good", version: "1.0.0" } });
    fs.mkdirSync(path.join(dir, "Broken"));
    fs.writeFileSync(path.join(dir, "Broken", "package.json"), "{ not json");
    const versions = readVersions(dir);
    assert.equal(versions.size, 1);
    assert.equal(versions.get("com.arman.good"), "1.0.0");
});

test("readVersions returns empty for a missing directory", () => {
    assert.equal(readVersions(path.join(os.tmpdir(), "does-not-exist-here")).size, 0);
});

// ---------------------------------------------------------- versionChanges

test("versionChanges reports only the packages whose version moved", () => {
    const before = new Map([["a", "0.1.0"], ["b", "0.1.0"]]);
    const after = new Map([["a", "0.2.0"], ["b", "0.1.0"]]);
    assert.deepEqual(versionChanges(before, after), [{ name: "a", from: "0.1.0", to: "0.2.0" }]);
});

test("versionChanges is empty when nothing moved", () => {
    const versions = new Map([["a", "0.1.0"]]);
    assert.deepEqual(versionChanges(versions, versions), []);
});

test("versionChanges marks a package that did not exist before as new", () => {
    const changes = versionChanges(new Map(), new Map([["a", "0.1.0"]]));
    assert.deepEqual(changes, [{ name: "a", from: "(new)", to: "0.1.0" }]);
});

// ----------------------------------------------------------- commit message

test("commitMessage names the package when exactly one moved", () => {
    const message = commitMessage([{ name: "com.arman.object-pooling", from: "0.1.0", to: "0.2.0" }]);
    assert.match(message, /^chore\(release\): com\.arman\.object-pooling@0\.2\.0\n/);
    assert.match(message, /- com\.arman\.object-pooling 0\.1\.0 -> 0\.2\.0/);
});

test("commitMessage counts the packages when several moved, and lists each", () => {
    const message = commitMessage([
        { name: "a", from: "0.1.0", to: "0.2.0" },
        { name: "b", from: "0.1.0", to: "0.1.1" },
    ]);
    assert.match(message, /^chore\(release\): promote 2 packages\n/);
    assert.match(message, /- a 0\.1\.0 -> 0\.2\.0/);
    assert.match(message, /- b 0\.1\.0 -> 0\.1\.1/);
});

// -------------------------------------------------------- pull request body

test("pullRequestBody tabulates every change and warns that merging publishes", () => {
    const body = pullRequestBody([{ name: "com.arman.object-pooling", from: "0.1.0", to: "0.2.0" }]);
    assert.match(body, /## Packages \(1\)/);
    assert.match(body, /\| `com\.arman\.object-pooling` \| 0\.1\.0 \| 0\.2\.0 \|/);
    assert.match(body, /Merging this pull request \*\*publishes\*\*/);
    assert.match(body, /permanent/);
});

// ------------------------------------------------------------ argument guard

test("release-flow rejects any argument with exit code 2", () => {
    for (const args of [["validate"], ["--dry-run"], ["tag", "--push"]]) {
        const result = spawnSync(process.execPath, [SCRIPT, ...args], { encoding: "utf8" });
        assert.equal(result.status, 2, `expected \`${args.join(" ")}\` to be rejected`);
        assert.match(result.stderr, /takes no arguments/);
        // The rejection must point at the tool that does take options.
        assert.match(result.stderr, /upm-release\.mjs/);
    }
});

// ------------------------------------------------------------------- the bat
//
// release.bat was twice committed with its `Tools\release.bat` usage comments
// mangled — once into `Tools<CR>elease.bat` (harmless, still one `rem` line),
// once into a bare `release.bat validate` on its own line, which cmd runs. That
// second form recurses forever when the working directory is Tools/. The file
// now contains no backslash at all, which is what this test pins.

test("release.bat contains no backslash, the character that mangled it before", () => {
    assert.equal(fs.readFileSync(BAT, "utf8").includes("\\"), false);
});

test("release.bat has no executable line other than the node invocation", () => {
    const lines = fs
        .readFileSync(BAT, "utf8")
        .split(/\r?\n/)
        .map((l) => l.trim())
        .filter(Boolean)
        .filter((l) => !l.startsWith("rem") && l !== "@echo off");
    assert.deepEqual(lines, ['node "%~dp0release-flow.mjs" %*']);
});

test("release.bat is CRLF, as a batch file should be", () => {
    const text = fs.readFileSync(BAT, "utf8");
    assert.equal(/[^\r]\n/.test(text), false, "found a bare LF line ending");
});
