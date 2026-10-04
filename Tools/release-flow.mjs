#!/usr/bin/env node
// release-flow.mjs — the release flow, driven by one command with no arguments.
//
//   Tools/release.bat            (Windows; a two-line wrapper over this file)
//   node Tools/release-flow.mjs  (anywhere)
//
// Dependency-free, same as the rest of Tools/. Both `dev` and `master` require
// a pull request and have no bypass actors, so a release takes two merges and
// this script opens the pull request for whichever one is next. It reads the
// state of `origin`, never the local checkout, and decides:
//
//   Phase 1 — prepare. `origin/dev` still has populated `## [Unreleased]`
//   sections. In a temporary worktree on a new `chore/prepare-release-<date>`
//   branch cut from `origin/dev`: upm-release.mjs validate, upm-release.mjs
//   prepare, commit, push the branch, gh pr create --base dev. A human merges
//   that pull request, then runs this script again.
//
//   Phase 2 — promote. Nothing is left to prepare and some package version on
//   `origin/dev` differs from `origin/master`: gh pr create --base master
//   --head dev, or report the one already open.
//
// While a prepare pull request is still open, a run reports it and stops.
//
// It deliberately STOPS at an open pull request. Merging the `dev` -> `master`
// one is what publishes: the `tag` job in release.yml runs on the push to
// `master`, tags every package whose version moved, and an OpenUPM tag is
// permanent. There is no undo, so that step stays a human click on a green PR.
//
// Takes no arguments — deliberately. For a single step, or for `pack`,
// `--only`, `--dry-run` or `--bump`, call the underlying tool directly:
// node Tools/upm-release.mjs <command>.
//
// Exit codes: 0 = success, or nothing to release. 1 = failure. 2 = bad usage.

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.dirname(HERE);
const PACKAGES_DIR = path.join(ROOT, "Packages");

// Development lands on `dev`; `master` is release-only and moves solely via the
// release pull request. Mirrors upm-release.mjs and release.yml.
const DEV_BRANCH = "dev";
const RELEASE_BRANCH = "master";
const REMOTE = "origin";
export const PREPARE_BRANCH_PREFIX = "chore/prepare-release";

// `gh` is a .cmd shim on Windows, and since Node 18.20/20.12 spawning one
// without a shell fails with EINVAL — the same wrinkle upm-release.mjs hits
// with npm. Every argument stays an array element and is quoted here rather
// than interpolated, because package folder names contain spaces. cmd.exe ends
// a command at a newline, so multi-line text travels in a file (`git commit
// -F`, `gh pr create --body-file`), never as an argument.
function run(cmd, args, opts = {}) {
    if (process.platform === "win32" && !path.isAbsolute(cmd) && !cmd.endsWith(".exe")) {
        const quoted = args.map((a) => (/[\s"]/.test(a) ? `"${a.replace(/"/g, '\\"')}"` : a));
        return spawnSync(cmd, quoted, { encoding: "utf8", shell: true, ...opts });
    }
    return spawnSync(cmd, args, { encoding: "utf8", ...opts });
}

function gitIn(dir, ...args) {
    const r = run("git", ["-C", dir, ...args]);
    if (r.error) throw r.error;
    return { code: r.status, out: (r.stdout || "").trim(), err: (r.stderr || "").trim() };
}

const git = (...args) => gitIn(ROOT, ...args);

function gh(...args) {
    const r = run("gh", args, { cwd: ROOT });
    if (r.error) throw r.error;
    return { code: r.status, out: (r.stdout || "").trim(), err: (r.stderr || "").trim() };
}

// Streams straight to this process's stdio so `validate` and `prepare` report
// as they normally do — their output is the useful part of a release run.
function node(scriptArgs, cwd) {
    const r = spawnSync(process.execPath, scriptArgs, { cwd, stdio: "inherit" });
    if (r.error) throw r.error;
    return r.status;
}

let stepNumber = 0;
function step(label) {
    stepNumber += 1;
    console.log(`\n[${stepNumber}] ${label}`);
}

function fail(message) {
    console.error(`\nerror: ${message}`);
    return 1;
}

// ------------------------------------------------------------------ helpers

// Every publishable package's version, from `[folder, package.json text]`
// pairs. Private packages (PackageTemplate) are never released and are
// skipped, matching upm-release. A malformed manifest is validate's problem to
// report, not this script's.
export function versionsFromManifests(manifests) {
    const versions = new Map();
    for (const [, text] of [...manifests].sort((a, b) => a[0].localeCompare(b[0]))) {
        try {
            const manifest = JSON.parse(text);
            if (manifest.private === true) continue;
            if (manifest.name && manifest.version) versions.set(manifest.name, manifest.version);
        } catch {
            // See above.
        }
    }
    return versions;
}

// Versions as they are on disk under `packagesDir`.
export function readVersions(packagesDir = PACKAGES_DIR) {
    if (!fs.existsSync(packagesDir)) return new Map();
    const manifests = [];
    for (const entry of fs.readdirSync(packagesDir, { withFileTypes: true })) {
        if (!entry.isDirectory()) continue;
        const manifestPath = path.join(packagesDir, entry.name, "package.json");
        if (fs.existsSync(manifestPath))
            manifests.push([entry.name, fs.readFileSync(manifestPath, "utf8")]);
    }
    return versionsFromManifests(manifests);
}

// Versions as they are at a git ref, read without checking it out.
export function readVersionsAt(ref) {
    const listed = git("ls-tree", "--name-only", `${ref}:Packages`);
    if (listed.code !== 0) return new Map();
    const manifests = [];
    for (const folder of listed.out.split(/\r?\n/).filter(Boolean)) {
        const shown = git("show", `${ref}:Packages/${folder}/package.json`);
        if (shown.code === 0) manifests.push([folder, shown.out]);
    }
    return versionsFromManifests(manifests);
}

// The set of packages whose version differs between two snapshots, as
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

function changesTable(changes) {
    return [
        `## Packages (${changes.length})`,
        "",
        "| Package | From | To |",
        "| --- | --- | --- |",
        ...changes.map((c) => `| \`${c.name}\` | ${c.from} | ${c.to} |`),
    ];
}

// The body of the phase 1 pull request into `dev`.
export function preparePullRequestBody(changes) {
    return [
        "Release preparation assembled by `Tools/release-flow.mjs`: `upm-release.mjs prepare`",
        "promoted each `## [Unreleased]` section to a version heading and bumped `package.json`.",
        "",
        ...changesTable(changes),
        "",
        "---",
        "",
        "Merging this into `dev` publishes nothing. Once it is merged, run `Tools/release.bat`",
        "again to open the release pull request into `master`.",
    ].join("\n");
}

// The body of the phase 2 release pull request into `master`.
export function pullRequestBody(changes) {
    return [
        "Release promotion assembled by `Tools/release-flow.mjs`.",
        "",
        ...changesTable(changes),
        "",
        "---",
        "",
        "> [!WARNING]",
        "> Merging this pull request **publishes**. The `tag` job in `release.yml`",
        "> runs on the push to `master` and tags every package whose version moved.",
        "> OpenUPM picks the tags up within 15-30 minutes and the resulting",
        "> name/version is permanent. Merge only when the checks are green.",
    ].join("\n");
}

// `chore/prepare-release-YYYY-MM-DD`, with `-2`, `-3`, ... appended when a
// branch of that name already exists on the remote.
export function prepareBranchName(date, taken = new Set()) {
    const base = `${PREPARE_BRANCH_PREFIX}-${date.toISOString().slice(0, 10)}`;
    let name = base;
    for (let n = 2; taken.has(name); n += 1) name = `${base}-${n}`;
    return name;
}

// The open pull request into `dev` from a prepare branch, from the output of
// `gh pr list --json url,headRefName`.
export function findPreparePullRequest(pullRequests) {
    const found = pullRequests.find((pr) =>
        (pr.headRefName || "").startsWith(`${PREPARE_BRANCH_PREFIX}-`),
    );
    return found ? found.url : "";
}

function openPullRequests(base, head) {
    const args = ["pr", "list", "--base", base, "--state", "open", "--json", "url,headRefName"];
    if (head) args.push("--head", head);
    const listed = gh(...args);
    if (listed.code !== 0) return null;
    try {
        return JSON.parse(listed.out || "[]");
    } catch {
        return null;
    }
}

// `gh pr create`, with the body passed through a file. Returns the URL, or
// null after printing why it failed.
function createPullRequest(base, head, title, body) {
    const bodyFile = path.join(fs.mkdtempSync(path.join(os.tmpdir(), "release-flow-")), "body.md");
    fs.writeFileSync(bodyFile, body);
    const created = gh(
        "pr",
        "create",
        "--base",
        base,
        "--head",
        head,
        "--title",
        title,
        "--body-file",
        bodyFile,
    );
    fs.rmSync(path.dirname(bodyFile), { recursive: true, force: true });
    if (created.code !== 0) {
        console.error(`\`gh pr create\` failed: ${created.err || created.out}`);
        return null;
    }
    return created.out.split(/\s+/).filter(Boolean).pop() || "";
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
            return fail(
                `\`${tool}\` is not available on PATH. It is required to open the release pull requests.`,
            );
        }
    }

    const auth = gh("auth", "status");
    if (auth.code !== 0) {
        return fail("`gh` is not authenticated. Run `gh auth login`, then re-run.");
    }

    const fetched = git("fetch", REMOTE, DEV_BRANCH, RELEASE_BRANCH);
    if (fetched.code !== 0) {
        return fail(
            `\`git fetch ${REMOTE} ${DEV_BRANCH} ${RELEASE_BRANCH}\` failed: ${fetched.err || fetched.out}`,
        );
    }

    console.log(
        `  fetched \`${REMOTE}/${DEV_BRANCH}\` and \`${REMOTE}/${RELEASE_BRANCH}\`; ` +
            "the release is built from those, not from the local checkout.",
    );
    return 0;
}

// Phase 1. Returns { code } when the run ends here, or null when there was
// nothing to prepare and phase 2 should run.
function prepare() {
    step(`Checking for an open pull request from a \`${PREPARE_BRANCH_PREFIX}-*\` branch`);
    const intoDev = openPullRequests(DEV_BRANCH);
    if (intoDev === null) return { code: fail("`gh pr list` failed.") };
    const pending = findPreparePullRequest(intoDev);
    if (pending) {
        console.log(`\nA release preparation pull request is already open: ${pending}`);
        console.log(`Merge it into \`${DEV_BRANCH}\`, then run Tools/release.bat again.`);
        return { code: 0 };
    }
    console.log("  none open.");

    const remoteHeads = git("ls-remote", "--heads", REMOTE, `${PREPARE_BRANCH_PREFIX}-*`).out;
    const taken = new Set(
        remoteHeads
            .split(/\r?\n/)
            .filter(Boolean)
            .map((line) => line.split(/\s+/)[1].replace(/^refs\/heads\//, "")),
    );
    const branch = prepareBranchName(new Date(), taken);
    const worktree = path.join(fs.mkdtempSync(path.join(os.tmpdir(), "release-flow-")), "dev");

    step(`Cutting \`${branch}\` from \`${REMOTE}/${DEV_BRANCH}\` in a temporary worktree`);
    const added = git(
        "worktree",
        "add",
        "--no-track",
        "-b",
        branch,
        worktree,
        `${REMOTE}/${DEV_BRANCH}`,
    );
    if (added.code !== 0)
        return { code: fail(`\`git worktree add\` failed: ${added.err || added.out}`) };
    console.log(`  ${worktree}`);

    // On every path that does not leave a commit behind, the worktree and its
    // local branch go away again.
    const discard = () => {
        git("worktree", "remove", "--force", worktree);
        git("branch", "-D", branch);
        fs.rmSync(path.dirname(worktree), { recursive: true, force: true });
    };

    // The worktree's own copy of the tool, so the release is prepared by the
    // tooling that is on `dev`.
    const tool = path.join(worktree, "Tools", "upm-release.mjs");

    step("Validating packages");
    if (node([tool, "validate"], worktree) !== 0) {
        discard();
        return { code: fail("validate failed. Nothing has been changed.") };
    }

    const before = readVersions(path.join(worktree, "Packages"));

    step("Preparing the release (CHANGELOGs and versions)");
    if (node([tool, "prepare"], worktree) !== 0) {
        console.error(`The worktree is left in place for inspection: ${worktree}`);
        return { code: fail("prepare failed.") };
    }

    if (!gitIn(worktree, "status", "--porcelain").out) {
        console.log(`  nothing to prepare on \`${REMOTE}/${DEV_BRANCH}\`.`);
        discard();
        return null;
    }
    const changes = versionChanges(before, readVersions(path.join(worktree, "Packages")));
    if (changes.length === 0) {
        console.error(`The worktree is left in place for inspection: ${worktree}`);
        return {
            code: fail(
                "prepare changed files but moved no package version. Inspect `git diff` there.",
            ),
        };
    }

    step(`Committing ${changes.length} version bump(s) and pushing \`${branch}\``);
    const messageFile = path.join(path.dirname(worktree), "COMMIT_MSG");
    fs.writeFileSync(messageFile, commitMessage(changes));
    const staged = gitIn(worktree, "add", "-A");
    const committed = staged.code === 0 ? gitIn(worktree, "commit", "-F", messageFile) : staged;
    if (committed.code !== 0) {
        console.error(`The worktree is left in place for inspection: ${worktree}`);
        return { code: fail(`\`git commit\` failed: ${committed.err || committed.out}`) };
    }
    console.log(`  ${gitIn(worktree, "log", "--oneline", "-1").out}`);

    const pushed = gitIn(worktree, "push", "-u", REMOTE, branch);
    if (pushed.code !== 0) {
        return {
            code: fail(
                `\`git push\` failed: ${pushed.err || pushed.out}\n` +
                    `The commit is on \`${branch}\` in ${worktree}; push it and open the pull request into \`${DEV_BRANCH}\` by hand.`,
            ),
        };
    }

    step(`Opening the preparation pull request into \`${DEV_BRANCH}\``);
    const title = commitMessage(changes).split("\n")[0];
    const url = createPullRequest(DEV_BRANCH, branch, title, preparePullRequestBody(changes));
    // The branch is pushed, so nothing is lost by dropping the local copy.
    discard();
    if (url === null) {
        return {
            code: fail(
                `\`${branch}\` is pushed. Open its pull request by hand:\n` +
                    `  gh pr create --base ${DEV_BRANCH} --head ${branch}`,
            ),
        };
    }

    console.log(`\nRelease preparation pull request ready: ${url}`);
    console.log(`\nMerging it into \`${DEV_BRANCH}\` publishes nothing. Once it is merged, run`);
    console.log(
        `Tools/release.bat again to open the release pull request into \`${RELEASE_BRANCH}\`.`,
    );
    return { code: 0 };
}

// Phase 2.
function promote() {
    step(
        `Comparing package versions on \`${REMOTE}/${RELEASE_BRANCH}\` and \`${REMOTE}/${DEV_BRANCH}\``,
    );
    const changes = versionChanges(
        readVersionsAt(`${REMOTE}/${RELEASE_BRANCH}`),
        readVersionsAt(`${REMOTE}/${DEV_BRANCH}`),
    );
    if (changes.length === 0) {
        console.log(
            `\nNothing to release: no package has an \`## [Unreleased]\` section with entries, ` +
                `and every version on \`${DEV_BRANCH}\` matches \`${RELEASE_BRANCH}\`.`,
        );
        return 0;
    }
    for (const c of changes) console.log(`  ${c.name} ${c.from} -> ${c.to}`);

    step(`Opening the release pull request \`${DEV_BRANCH}\` -> \`${RELEASE_BRANCH}\``);
    const open = openPullRequests(RELEASE_BRANCH, DEV_BRANCH);
    if (open === null) return fail("`gh pr list` failed.");
    let url = open.length > 0 ? open[0].url : "";
    if (url) {
        console.log(
            "  one is already open; its head is `dev`, so it already carries these versions.",
        );
    } else {
        const title =
            changes.length === 1
                ? `Release: ${changes[0].name}@${changes[0].to}`
                : `Release: ${changes.length} packages`;
        url = createPullRequest(RELEASE_BRANCH, DEV_BRANCH, title, pullRequestBody(changes));
        if (url === null) {
            return fail(
                "Open the pull request by hand:\n" +
                    `  gh pr create --base ${RELEASE_BRANCH} --head ${DEV_BRANCH}`,
            );
        }
    }

    console.log(`\nRelease pull request ready: ${url}`);
    console.log("\nNothing has been published yet. Merging that pull request is the publish:");
    console.log(
        `  the \`tag\` job tags every package whose version moved, and an OpenUPM tag is permanent.`,
    );
    console.log("  Wait for green checks, then merge it with a true merge.");
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

    const prepared = prepare();
    if (prepared) return prepared.code;

    return promote();
}

const invokedDirectly =
    process.argv[1] &&
    path.resolve(process.argv[1]) === path.resolve(fileURLToPath(import.meta.url));
if (invokedDirectly) process.exit(main());
