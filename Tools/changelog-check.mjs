#!/usr/bin/env node
// Fails a pull request that changes a package's shipped code without recording
// it under that package's `## [Unreleased]` CHANGELOG heading.
//
//     node Tools/changelog-check.mjs --base <ref> --head <ref> [--json]
//
// Only shipped code counts: anything under `Runtime/` or `Editor/`, plus the
// `package.json` itself. Tests, samples, documentation, Markdown, and `.meta`
// files are exempt — none of them reach a consumer of the published tarball.
//
// Escape hatch: a `no-changelog` label on the pull request, passed in as the
// JSON array `PR_LABELS`, skips the whole check.
//
// Exit 0 = nothing to report, 1 = at least one package is missing an entry,
// 2 = the check itself could not run.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const PACKAGES_DIR = "Packages";
const CHANGELOG = "CHANGELOG.md";
const MANIFEST = "package.json";

const SKIP_LABEL = "no-changelog";
const CODE_DIRS = new Set(["Runtime", "Editor"]);

const UNRELEASED_HEADING = /^##\s+\[unreleased\]/i;
const ANY_H2 = /^##\s/;
const SUB_HEADING = /^#{3,}\s/;

const DEFAULT_BASE = "dev";
const DEFAULT_HEAD = "HEAD";

function git(...args) {
    const result = spawnSync("git", args, { cwd: ROOT, encoding: "utf8" });
    if (result.status !== 0) {
        throw new Error(`git ${args.join(" ")} failed: ${(result.stderr || result.stdout || "").trim()}`);
    }
    return result.stdout;
}

/** `git` for lookups that are allowed to miss — a path absent at a ref, say. */
function gitOrNull(...args) {
    const result = spawnSync("git", args, { cwd: ROOT, encoding: "utf8" });
    return result.status === 0 ? result.stdout : null;
}

function fail(message) {
    console.error(`changelog-check: ${message}`);
    process.exit(2);
}

// ---------------------------------------------------------------- changed set

/**
 * Diffs against the merge base rather than the base branch tip, so commits
 * that landed on the base after this branch forked are not blamed on it.
 */
function mergeBase(base, head) {
    const found = gitOrNull("merge-base", base, head);
    return found ? found.trim() : base;
}

function changedFiles(from, to) {
    // -z keeps paths verbatim; three package folders have spaces in their names.
    return git("diff", "--name-only", "-z", from, to)
        .split("\0")
        .filter(Boolean);
}

/** The package folder a repo-relative path belongs to, or null. */
function packageFolderOf(file) {
    const parts = file.split("/");
    return parts.length > 2 && parts[0] === PACKAGES_DIR ? parts[1] : null;
}

/** Does this path ship to a consumer, such that a change to it is user-visible? */
function isShippedCode(withinPackage) {
    if (withinPackage.endsWith(".meta")) return false;
    if (withinPackage === MANIFEST) return true;
    const [first, ...rest] = withinPackage.split("/");
    return rest.length > 0 && CODE_DIRS.has(first);
}

/** Groups the diff by package, keeping only packages whose shipped code moved. */
function packagesTouched(files) {
    const byFolder = new Map();
    for (const file of files) {
        const folder = packageFolderOf(file);
        if (folder === null) continue;
        const withinPackage = file.slice(`${PACKAGES_DIR}/${folder}/`.length);
        if (!isShippedCode(withinPackage)) continue;
        if (!byFolder.has(folder)) byFolder.set(folder, []);
        byFolder.get(folder).push(withinPackage);
    }
    return byFolder;
}

// ------------------------------------------------------------------ changelog

/**
 * The lines under `## [Unreleased]`, up to the next `##`. Returns null when the
 * heading is absent, which is a different failure from an empty section.
 */
function unreleasedSection(text) {
    const lines = text.split(/\r?\n/);
    const start = lines.findIndex((line) => UNRELEASED_HEADING.test(line));
    if (start === -1) return null;
    const rest = lines.slice(start + 1);
    const end = rest.findIndex((line) => ANY_H2.test(line));
    return end === -1 ? rest : rest.slice(0, end);
}

/**
 * The entry lines of a section — blank lines and `### Added`-style sub-headings
 * are scaffolding, not a record of a change.
 */
function entriesOf(lines) {
    return lines.map((line) => line.trim()).filter((line) => line !== "" && !SUB_HEADING.test(line));
}

function unreleasedEntriesAt(ref, folder) {
    const text = gitOrNull("show", `${ref}:${PACKAGES_DIR}/${folder}/${CHANGELOG}`);
    if (text === null) return { present: false, entries: [] };
    const section = unreleasedSection(text);
    if (section === null) return { present: true, section: false, entries: [] };
    return { present: true, section: true, entries: entriesOf(section) };
}

function manifestAt(ref, folder) {
    const text = gitOrNull("show", `${ref}:${PACKAGES_DIR}/${folder}/${MANIFEST}`);
    if (text === null) return null;
    try {
        return JSON.parse(text);
    } catch {
        // A broken manifest is `upm-release.mjs validate`'s finding, not ours.
        return {};
    }
}

// --------------------------------------------------------------------- verdict

const REASONS = {
    "missing-changelog": (folder) => `has no ${CHANGELOG}. Add one and record the change under \`## [Unreleased]\`.`,
    "missing-section": () => `has no \`## [Unreleased]\` heading. Add one above the newest version and record the change under it.`,
    "missing-entry": () => `changed, but nothing was added under \`## [Unreleased]\`. Describe the change there.`,
};

function inspect(folder, files, base, head) {
    const result = { folder, files: files.sort() };

    const headManifest = manifestAt(head, folder);
    if (headManifest === null) return null; // Not a package — a stray Packages/ path.
    result.name = headManifest.name ?? null;

    if (headManifest.private === true) return { ...result, status: "skipped-private" };
    if (manifestAt(base, folder) === null) return { ...result, status: "skipped-new" };

    const at = unreleasedEntriesAt(head, folder);
    if (!at.present) return { ...result, status: "missing-changelog" };
    if (!at.section) return { ...result, status: "missing-section" };

    const before = new Set(unreleasedEntriesAt(base, folder).entries);
    const added = at.entries.filter((entry) => !before.has(entry));
    return { ...result, status: added.length > 0 ? "ok" : "missing-entry", added };
}

function labelSkip() {
    const raw = process.env.PR_LABELS;
    if (!raw) return false;
    let labels;
    try {
        labels = JSON.parse(raw);
    } catch {
        // A malformed label list must not become an accidental bypass.
        return false;
    }
    return Array.isArray(labels) && labels.includes(SKIP_LABEL) ? `${SKIP_LABEL} label` : false;
}

// -------------------------------------------------------------------- reporting

function render(report) {
    const lines = [];
    if (report.skipped) {
        lines.push(`Skipped: the \`${SKIP_LABEL}\` label is set on this pull request.`);
        return lines;
    }
    const failures = report.packages.filter((p) => REASONS[p.status]);
    if (report.packages.length === 0) {
        lines.push("No package shipped code changed. Nothing to check.");
    }
    for (const pkg of report.packages) {
        const label = pkg.name ? `${pkg.folder} (${pkg.name})` : pkg.folder;
        if (pkg.status === "ok") {
            lines.push(`ok       ${label}`);
        } else if (pkg.status === "skipped-private") {
            lines.push(`skipped  ${label} — private, never published`);
        } else if (pkg.status === "skipped-new") {
            lines.push(`skipped  ${label} — new in this pull request`);
        } else {
            lines.push(`FAIL     ${label} — ${REASONS[pkg.status](pkg.folder)}`);
            for (const file of pkg.files) lines.push(`           ${file}`);
        }
    }
    if (failures.length > 0) {
        lines.push("");
        lines.push(
            `${failures.length} package(s) need a CHANGELOG entry. Add the \`${SKIP_LABEL}\` label if this change is genuinely invisible to users.`,
        );
    }
    return lines;
}

function writeStepSummary(report) {
    const target = process.env.GITHUB_STEP_SUMMARY;
    if (!target) return;
    const body = ["## Changelog check", "", "```", ...render(report), "```", ""].join("\n");
    fs.appendFileSync(target, body);
}

// ------------------------------------------------------------------------ main

function parseArgs(argv) {
    const flags = { base: DEFAULT_BASE, head: DEFAULT_HEAD, json: false };
    for (let i = 0; i < argv.length; i += 1) {
        const arg = argv[i];
        if (arg === "--json") flags.json = true;
        else if (arg === "--help" || arg === "-h") flags.help = true;
        else if (arg === "--base" || arg === "--head") {
            const value = argv[i + 1];
            if (value === undefined) fail(`${arg} needs a value`);
            flags[arg.slice(2)] = value;
            i += 1;
        } else fail(`unknown argument: ${arg}`);
    }
    return flags;
}

function usage() {
    console.log(
        [
            "Usage: node Tools/changelog-check.mjs [--base <ref>] [--head <ref>] [--json]",
            "",
            `  --base   Branch or commit the pull request merges into (default: ${DEFAULT_BASE}).`,
            `  --head   Branch or commit the pull request proposes (default: ${DEFAULT_HEAD}).`,
            "  --json   Emit the report as JSON instead of text.",
            "",
            `Set PR_LABELS to a JSON array to honour the \`${SKIP_LABEL}\` escape hatch.`,
        ].join("\n"),
    );
}

const flags = parseArgs(process.argv.slice(2));
if (flags.help) {
    usage();
    process.exit(0);
}

const skipped = labelSkip();
const report = { ok: true, skipped, base: flags.base, head: flags.head, packages: [] };

if (!skipped) {
    let from;
    try {
        from = mergeBase(flags.base, flags.head);
        report.mergeBase = from;
    } catch (error) {
        fail(error.message);
    }

    let files;
    try {
        files = changedFiles(from, flags.head);
    } catch (error) {
        fail(error.message);
    }

    for (const [folder, touched] of [...packagesTouched(files)].sort((a, b) => a[0].localeCompare(b[0]))) {
        const result = inspect(folder, touched, from, flags.head);
        if (result !== null) report.packages.push(result);
    }
    report.ok = !report.packages.some((pkg) => REASONS[pkg.status]);
}

writeStepSummary(report);
console.log(flags.json ? JSON.stringify(report, null, 2) : render(report).join("\n"));
process.exit(report.ok ? 0 : 1);
