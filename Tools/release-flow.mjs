#!/usr/bin/env node
// release-flow.mjs — the whole release flow, in one go, with no arguments.
//
//   Tools/release.bat            (Windows; a two-line wrapper over this file)
//   node Tools/release-flow.mjs  (anywhere)
//
// Dependency-free, same as the rest of Tools/. Steps, in order, stopping at the
// first failure:
//
//   1. preflight — git and gh present, on `dev`, clean tree, not behind origin
//   2. node Tools/upm-release.mjs validate
//   3. node Tools/upm-release.mjs prepare   (rewrites CHANGELOGs and versions)
//   4. git commit the result
//   5. git push origin dev
//   6. gh pr create --base master --head dev
//
// It deliberately STOPS at the open pull request. Merging that PR is what
// publishes: the `tag` job in release.yml runs on the push to `master`, tags
// every package whose version moved, and an OpenUPM tag is permanent. There is
// no undo, so that last step stays a human click on a green PR.
//
// Takes no arguments — deliberately. The flow is the whole point; for a single
// step, or for `pack`, `--only`, `--dry-run` or `--bump`, call the underlying
// tool directly: node Tools/upm-release.mjs <command>.
//
// Exit codes: 0 = success, or nothing to release. 1 = failure. 2 = bad usage.

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.dirname(HERE);
const PACKAGES_DIR = path.join(ROOT, "Packages");
const RELEASE_TOOL = path.join(HERE, "upm-release.mjs");

// Development lands on `dev`; `master` is release-only and moves solely via the
// pull request this script opens. Mirrors upm-release.mjs and release.yml.
const DEV_BRANCH = "dev";
const RELEASE_BRANCH = "master";
const REMOTE = "origin";

// `gh` is a .cmd shim on Windows, and since Node 18.20/20.12 spawning one
// without a shell fails with EINVAL — the same wrinkle upm-release.mjs hits
// with npm. Every argument stays an array element and is quoted here rather
// than interpolated, because package folder names contain spaces.
function run(cmd, args, opts = {}) {
    if (process.platform === "win32" && !path.isAbsolute(cmd) && !cmd.endsWith(".exe")) {
        const quoted = args.map((a) => (/[\s"]/.test(a) ? `"${a.replace(/"/g, '\\"')}"` : a));
        return spawnSync(cmd, quoted, { encoding: "utf8", shell: true, ...opts });
    }
    return spawnSync(cmd, args, { encoding: "utf8", ...opts });
}

function git(...args) {
    const r = run("git", ["-C", ROOT, ...args]);
    if (r.error) throw r.error;
    return { code: r.status, out: (r.stdout || "").trim(), err: (r.stderr || "").trim() };
}

function gh(...args) {
    const r = run("gh", args, { cwd: ROOT });
    if (r.error) throw r.error;
    return { code: r.status, out: (r.stdout || "").trim(), err: (r.stderr || "").trim() };
}

// Streams straight to this process's stdio so `validate` and `prepare` report
// as they normally do — their output is the useful part of a release run.
function node(scriptArgs) {
    const r = spawnSync(process.execPath, scriptArgs, { cwd: ROOT, stdio: "inherit" });
    if (r.error) throw r.error;
    return r.status;
}

let stepNumber = 0;
function step(label) {
    stepNumber += 1;
    console.log(`\n[${stepNumber}/6] ${label}`);
}

function fail(message) {
    console.error(`\nerror: ${message}`);
    return 1;
}

// ------------------------------------------------------------------ helpers

// Reads every publishable package's current version, so the commit message and
// the pull request body can name exactly what moved. Private packages
// (PackageTemplate) are never released and are skipped, matching upm-release.
export function readVersions(packagesDir = PACKAGES_DIR) {
    const versions = new Map();
    if (!fs.existsSync(packagesDir)) return versions;
    for (const entry of fs.readdirSync(packagesDir, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name))) {
        if (!entry.isDirectory()) continue;
        const manifestPath = path.join(packagesDir, entry.name, "package.json");
        if (!fs.existsSync(manifestPath)) continue;
        try {
            const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
            if (manifest.private === true) continue;
            if (manifest.name && manifest.version) versions.set(manifest.name, manifest.version);
        } catch {
            // A malformed manifest is validate's problem to report, not this
            // script's — it runs before anything here is used.
        }
    }
    return versions;
}

// The set of packages whose version changed across `prepare`, as
// `name@old -> new`. Order follows the map, i.e. package-folder order.
export function versionChanges(before, after) {
    const changes = [];
    for (const [name, version] of after) {
        const previous = before.get(name);
        if (previous !== version) changes.push({ name, from: previous ?? "(new)", to: version });
    }
    return changes;
}

export function commitMessage(changes) {
    const subject =
        changes.length === 1
            ? `chore(release): ${changes[0].name}@${changes[0].to}`
            : `chore(release): promote ${changes.length} packages`;
    const body = changes.map((c) => `- ${c.name} ${c.from} -> ${c.to}`).join("\n");
    return `${subject}\n\n${body}\n`;
}

export function pullRequestBody(changes) {
    const lines = [
        "Release promotion assembled by `Tools/release-flow.mjs`.",
        "",
        `## Packages (${changes.length})`,
        "",
        "| Package | From | To |",
        "| --- | --- | --- |",
        ...changes.map((c) => `| \`${c.name}\` | ${c.from} | ${c.to} |`),
        "",
        "---",
        "",
        "> [!WARNING]",
        "> Merging this pull request **publishes**. The `tag` job in `release.yml`",
        "> runs on the push to `master` and tags every package whose version moved.",
        "> OpenUPM picks the tags up within 15-30 minutes and the resulting",
        "> name/version is permanent. Merge only when the checks are green.",
    ];
    return lines.join("\n");
}

// ------------------------------------------------------------------- the flow

function preflight() {
    step("Preflight");

    for (const [tool, args] of [
        ["git", ["--version"]],
        ["gh", ["--version"]],
    ]) {
        const r = run(tool, args, { cwd: ROOT });
        if (r.error || r.status !== 0) {
            return fail(`\`${tool}\` is not available on PATH. It is required to open the release pull request.`);
        }
    }

    const auth = gh("auth", "status");
    if (auth.code !== 0) {
        return fail("`gh` is not authenticated. Run `gh auth login`, then re-run.");
    }

    const branch = git("rev-parse", "--abbrev-ref", "HEAD").out;
    if (branch !== DEV_BRANCH) {
        return fail(
            `on branch \`${branch}\`, expected \`${DEV_BRANCH}\`. A release is a promotion of ` +
                `\`${DEV_BRANCH}\` to \`${RELEASE_BRANCH}\`; check out \`${DEV_BRANCH}\` and re-run.`,
        );
    }

    if (git("status", "--porcelain").out) {
        return fail("working tree is dirty. Commit or stash first, so the release diff is exactly what this script writes.");
    }

    const fetched = git("fetch", REMOTE, DEV_BRANCH);
    if (fetched.code !== 0) return fail(`\`git fetch ${REMOTE} ${DEV_BRANCH}\` failed: ${fetched.err || fetched.out}`);

    // Behind is fatal — releasing a stale `dev` would silently drop whatever
    // landed on the remote. Ahead is only a warning: those commits are about to
    // be pushed as part of the release, which is usually what was intended, but
    // it is worth saying out loud before anything is published.
    const counts = git("rev-list", "--left-right", "--count", `${REMOTE}/${DEV_BRANCH}...HEAD`).out;
    const [behind, ahead] = counts.split(/\s+/).map(Number);
    if (behind > 0) {
        return fail(
            `local \`${DEV_BRANCH}\` is ${behind} commit(s) behind \`${REMOTE}/${DEV_BRANCH}\`. ` +
                `Run \`git pull --ff-only\` and re-run.`,
        );
    }
    if (ahead > 0) {
        console.log(`  note: ${ahead} unpushed commit(s) on \`${DEV_BRANCH}\` will be included in this release.`);
    }

    console.log(`  on \`${branch}\`, clean, up to date with \`${REMOTE}/${DEV_BRANCH}\`.`);
    return 0;
}

function main() {
    if (process.argv.slice(2).length > 0) {
        // ASCII only in printed output: cmd.exe's default codepage mangles a
        // dash that this file's comments are free to use.
        console.error("error: release-flow takes no arguments - it runs the whole release flow.");
        console.error("For a single step, or for --dry-run / --only / --bump, use:");
        console.error("  node Tools/upm-release.mjs <validate|pack|tag|prepare> [options]");
        return 2;
    }

    console.log(`Release flow: promote \`${DEV_BRANCH}\` to \`${RELEASE_BRANCH}\`.`);

    const failed = preflight();
    if (failed) return failed;

    step("Validating packages");
    if (node([RELEASE_TOOL, "validate"]) !== 0) return fail("validate failed. Nothing has been changed.");

    const before = readVersions();

    step("Preparing the release (CHANGELOGs and versions)");
    if (node([RELEASE_TOOL, "prepare"]) !== 0) return fail("prepare failed. Check the working tree before re-running.");

    const changes = versionChanges(before, readVersions());
    if (!git("status", "--porcelain").out) {
        console.log("\nNothing to release: no package has an `## [Unreleased]` section with entries.");
        return 0;
    }
    if (changes.length === 0) {
        return fail("prepare changed files but moved no package version. Inspect `git diff` before continuing.");
    }

    step(`Committing ${changes.length} version bump(s)`);
    const added = git("add", "-A");
    if (added.code !== 0) return fail(`\`git add\` failed: ${added.err}`);
    const committed = git("commit", "-m", commitMessage(changes));
    if (committed.code !== 0) return fail(`\`git commit\` failed: ${committed.err || committed.out}`);
    console.log(`  ${git("log", "--oneline", "-1").out}`);

    step(`Pushing to ${REMOTE}/${DEV_BRANCH}`);
    const pushed = git("push", REMOTE, `HEAD:${DEV_BRANCH}`);
    if (pushed.code !== 0) {
        return fail(`\`git push\` failed: ${pushed.err || pushed.out}\nThe release commit is made locally; push it and open the pull request by hand.`);
    }

    step("Opening the release pull request");
    // An open dev -> master pull request already exists on a re-run (the push
    // above updated it), so adopt it rather than failing.
    const existing = gh(
        "pr", "list", "--base", RELEASE_BRANCH, "--head", DEV_BRANCH,
        "--state", "open", "--json", "url", "--jq", ".[0].url",
    );
    let url = existing.code === 0 ? existing.out : "";
    if (url) {
        console.log("  updated the pull request that was already open.");
    } else {
        const title =
            changes.length === 1
                ? `Release: ${changes[0].name}@${changes[0].to}`
                : `Release: ${changes.length} packages`;
        const created = gh(
            "pr", "create", "--base", RELEASE_BRANCH, "--head", DEV_BRANCH,
            "--title", title, "--body", pullRequestBody(changes),
        );
        if (created.code !== 0) {
            return fail(
                `\`gh pr create\` failed: ${created.err || created.out}\n` +
                    `The release commit is pushed. Open the pull request by hand:\n` +
                    `  gh pr create --base ${RELEASE_BRANCH} --head ${DEV_BRANCH}`,
            );
        }
        url = created.out.split(/\s+/).filter(Boolean).pop() || "";
    }

    console.log(`\nRelease pull request ready: ${url}`);
    console.log("\nNothing has been published yet. Merging that pull request is the publish:");
    console.log(`  the \`tag\` job tags every package whose version moved, and an OpenUPM tag is permanent.`);
    console.log("  Wait for green checks, then merge.");
    return 0;
}

const invokedDirectly = process.argv[1] && path.resolve(process.argv[1]) === path.resolve(fileURLToPath(import.meta.url));
if (invokedDirectly) process.exit(main());
