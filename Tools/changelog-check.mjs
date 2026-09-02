#!/usr/bin/env node
// Guards each package's CHANGELOG on a pull request, with two rules:
//
//   missing-entry    A change to a package's shipped code must be recorded
//                    under that package's `## [Unreleased]` heading.
//   frozen-section   A version section whose `<package-name>/<version>` tag
//                    already exists must not be edited — it is a published
//                    record of what shipped.
//   empty-unreleased A `## [Unreleased]` heading with no entries under it is
//                    noise. Delete the heading, or put something under it.
//                    Checked repo-wide at the head commit, not just on the
//                    packages this pull request touched. No waiver.
//
//     node Tools/changelog-check.mjs --base <ref> --head <ref> [--json]
//
// Shipped code means anything under `Runtime/` or `Editor/`, plus the
// `package.json`. Tests, samples, documentation, Markdown, and `.meta` files
// are exempt — none of them reach a consumer of the published tarball. The
// frozen rule looks at the CHANGELOG regardless, since it is Markdown and so
// would otherwise never be inspected at all.
//
// Escape hatches, passed in as the JSON array `PR_LABELS`: `no-changelog`
// waives the entry rule, `changelog-rewrite` waives the frozen rule. They are
// deliberately separate — "this change needs no entry" is not the same claim
// as "I may rewrite what 0.1.0 says it shipped".
//
// Exit 0 = nothing to report, 1 = at least one rule tripped, 2 = the check
// itself could not run.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const PACKAGES_DIR = "Packages";
const CHANGELOG = "CHANGELOG.md";
const MANIFEST = "package.json";

const WAIVER_LABELS = { "missing-entry": "no-changelog", "frozen-section": "changelog-rewrite" };
const CODE_DIRS = new Set(["Runtime", "Editor"]);

const H2 = /^##\s/;
const H2_VERSION = /^##\s+\[([^\]]+)\]/;
const UNRELEASED = /^unreleased$/i;
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

/**
 * Groups the diff by package. A package is in scope if its shipped code moved
 * (the entry rule) or its CHANGELOG moved (the frozen rule).
 */
function packagesTouched(files) {
    const byFolder = new Map();
    for (const file of files) {
        const folder = packageFolderOf(file);
        if (folder === null) continue;
        const withinPackage = file.slice(`${PACKAGES_DIR}/${folder}/`.length);

        const code = isShippedCode(withinPackage);
        const changelog = withinPackage === CHANGELOG;
        if (!code && !changelog) continue;

        if (!byFolder.has(folder)) byFolder.set(folder, { files: [], code: false, changelog: false });
        const touched = byFolder.get(folder);
        touched.files.push(withinPackage);
        touched.code ||= code;
        touched.changelog ||= changelog;
    }
    return byFolder;
}

// ------------------------------------------------------------ changelog parsing

function changelogAt(ref, folder) {
    return gitOrNull("show", `${ref}:${PACKAGES_DIR}/${folder}/${CHANGELOG}`);
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

/**
 * The lines under `## [Unreleased]`, up to the next `##`. Returns null when the
 * heading is absent, which is a different failure from an empty section.
 */
function unreleasedSection(text) {
    const lines = text.split(/\r?\n/);
    const start = lines.findIndex((line) => H2_VERSION.test(line) && UNRELEASED.test(line.match(H2_VERSION)[1]));
    if (start === -1) return null;
    const rest = lines.slice(start + 1);
    const end = rest.findIndex((line) => H2.test(line));
    return end === -1 ? rest : rest.slice(0, end);
}

/**
 * The entry lines of a section — blank lines and `### Added`-style sub-headings
 * are scaffolding, not a record of a change.
 */
function entriesOf(lines) {
    return lines.map((line) => line.trim()).filter((line) => line !== "" && !SUB_HEADING.test(line));
}

/** Every `## [x.y.z]` section, keyed by version, each including its heading. */
function versionSections(text) {
    const sections = new Map();
    let current = null;
    for (const line of text.split(/\r?\n/)) {
        if (H2.test(line)) {
            const heading = line.match(H2_VERSION);
            const version = heading?.[1];
            if (version === undefined || UNRELEASED.test(version)) {
                current = null;
            } else {
                current = [line];
                sections.set(version, current);
            }
            continue;
        }
        current?.push(line);
    }
    return sections;
}

/** Compares sections ignoring trailing whitespace, which no reader can see. */
function normalize(lines) {
    const trimmed = lines.map((line) => line.trimEnd());
    while (trimmed.length > 0 && trimmed[trimmed.length - 1] === "") trimmed.pop();
    return trimmed.join("\n");
}

// ---------------------------------------------------------------------- rules

/**
 * Rule 1. A shipped-code change has to show up under `## [Unreleased]`.
 *
 * Opening a new version section counts too: that is the release pull request,
 * which moves the accumulated entries out of `[Unreleased]` under a version
 * heading and bumps `package.json` in the same breath.
 */
function entryProblem(folder, base, head) {
    const headText = changelogAt(head, folder);
    if (headText === null) return { rule: "missing-changelog" };

    const section = unreleasedSection(headText);
    if (section === null) return { rule: "missing-section" };

    const baseText = changelogAt(base, folder);
    const baseSection = baseText === null ? null : unreleasedSection(baseText);
    const before = new Set(baseSection === null ? [] : entriesOf(baseSection));
    if (entriesOf(section).some((entry) => !before.has(entry))) return null;

    const knownVersions = new Set(baseText === null ? [] : versionSections(baseText).keys());
    const released = [...versionSections(headText).keys()].filter((v) => !knownVersions.has(v));
    if (released.length > 0) return null;

    return { rule: "missing-entry" };
}

/**
 * Rule 2. A version section whose tag already exists is a published record.
 * Editing or deleting it makes the repo disagree with what OpenUPM built.
 */
function frozenProblem(folder, name, base, head, tags) {
    if (name === null) return null;
    const prefix = `${name}/`;
    const tagged = new Set(tags.filter((tag) => tag.startsWith(prefix)).map((tag) => tag.slice(prefix.length)));
    if (tagged.size === 0) return null;

    const baseText = changelogAt(base, folder);
    if (baseText === null) return null;
    const headText = changelogAt(head, folder);
    const after = headText === null ? new Map() : versionSections(headText);

    const versions = [];
    for (const [version, lines] of versionSections(baseText)) {
        if (!tagged.has(version)) continue;
        const now = after.get(version);
        if (now === undefined || normalize(now) !== normalize(lines)) versions.push(version);
    }
    return versions.length > 0 ? { rule: "frozen-section", versions } : null;
}

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

function inspect(folder, touched, base, head, tags, waived) {
    const headManifest = manifestAt(head, folder);
    if (headManifest === null) return null; // Not a package — a stray Packages/ path.

    const result = { folder, name: headManifest.name ?? null, files: touched.files.sort(), problems: [] };

    if (headManifest.private === true) return { ...result, skipped: "private" };
    if (manifestAt(base, folder) === null) return { ...result, skipped: "new" };

    const candidates = [
        touched.code ? entryProblem(folder, base, head) : null,
        touched.changelog ? frozenProblem(folder, result.name, base, head, tags) : null,
    ];
    result.problems = candidates.filter(
        (problem) => problem !== null && !waived.includes(WAIVER_LABELS[problem.rule]),
    );
    return result;
}

/** Which waiver labels this pull request actually carries. */
function waivedLabels() {
    const known = Object.values(WAIVER_LABELS);
    const raw = process.env.PR_LABELS;
    if (!raw) return [];
    let labels;
    try {
        labels = JSON.parse(raw);
    } catch {
        // A malformed label list must not become an accidental bypass.
        return [];
    }
    if (!Array.isArray(labels)) return [];
    return known.filter((label) => labels.includes(label));
}

// -------------------------------------------------------------------- reporting

const EXPLANATIONS = {
    "missing-changelog": () => `has no ${CHANGELOG}. Add one and record the change under \`## [Unreleased]\`.`,
    "missing-section": () =>
        "has no `## [Unreleased]` heading. Create one above the newest version together with the entry describing this change — the heading exists only while it has entries.",
    "missing-entry": () => "changed, but nothing was added under `## [Unreleased]`. Describe the change there.",
    "frozen-section": (problem) =>
        `edits ${problem.versions.map((v) => `\`${v}\``).join(", ")}, which ${problem.versions.length > 1 ? "have" : "has"} already been tagged and published. Released history must not change — put the note under \`## [Unreleased]\` instead.`,
    "empty-unreleased": () =>
        "has a `## [Unreleased]` heading with nothing under it. Delete the heading, or put an entry under it. (A bare `### Added` is not an entry.)",
};

function render(report) {
    const lines = [];
    if (report.waived.length > 0) {
        lines.push(`Waived by label: ${report.waived.join(", ")}.`);
    }
    if (report.packages.length === 0) {
        lines.push("No package shipped code or changelog changed. Nothing to check.");
    }
    for (const pkg of report.packages) {
        const label = pkg.name ? `${pkg.folder} (${pkg.name})` : pkg.folder;
        if (pkg.skipped === "private") {
            lines.push(`skipped  ${label} — private, never published`);
        } else if (pkg.skipped === "new") {
            lines.push(`skipped  ${label} — new in this pull request`);
        } else if (pkg.problems.length === 0) {
            lines.push(`ok       ${label}`);
        } else {
            for (const problem of pkg.problems) {
                lines.push(`FAIL     ${label} — ${EXPLANATIONS[problem.rule](problem)}`);
            }
            for (const file of pkg.files) lines.push(`           ${file}`);
        }
    }

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
    return lines;
}

function writeStepSummary(report) {
    const target = process.env.GITHUB_STEP_SUMMARY;
    if (!target) return;
    fs.appendFileSync(target, ["## Changelog check", "", "```", ...render(report), "```", ""].join("\n"));
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
            "Set PR_LABELS to a JSON array to honour the waiver labels:",
            ...Object.entries(WAIVER_LABELS).map(([rule, label]) => `  ${rule} → ${label}`),
            "",
            "`empty-unreleased` has no waiver: delete the heading or fill it in.",
        ].join("\n"),
    );
}

const flags = parseArgs(process.argv.slice(2));
if (flags.help) {
    usage();
    process.exit(0);
}

const report = { ok: true, base: flags.base, head: flags.head, waived: waivedLabels(), packages: [] };

let from;
try {
    from = mergeBase(flags.base, flags.head);
    report.mergeBase = from;
} catch (error) {
    fail(error.message);
}

let files;
let tags;
try {
    files = changedFiles(from, flags.head);
    tags = git("tag", "--list").split(/\r?\n/).filter(Boolean);
} catch (error) {
    fail(error.message);
}

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

writeStepSummary(report);
console.log(flags.json ? JSON.stringify(report, null, 2) : render(report).join("\n"));
process.exit(report.ok ? 0 : 1);
