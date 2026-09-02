#!/usr/bin/env node
// upm-release.mjs — validate / pack / tag the embedded UPM packages.
//
// Dependency-free. Run identically from a dev machine or from CI:
//
//   node Tools/upm-release.mjs validate
//   node Tools/upm-release.mjs pack [--out PackageExports]
//   node Tools/upm-release.mjs tag [--push] [--dry-run] [--only <package>]
//
// Under the OpenUPM model a git tag <package-name>/<version> IS the release —
// there is no upload step and no registry secret. See
// docs/specs/2026-08-23-upm-package-registry-github-design.md.
//
// Exit codes: 0 = success, 1 = failure, 2 = bad usage.

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const PACKAGES_DIR = path.join(ROOT, "Packages");

// The branch a release may be tagged from. `master` is release-only: development
// lands on `dev`, and `master` moves solely via a release PR from it. (It is
// `master` here, not `main`.)
const RELEASE_BRANCH = "master";

const NPM = process.platform === "win32" ? "npm.cmd" : "npm";

// Package ids are one flat namespace: com.arman.<kebab-case-name>. Normalised
// 2026-08-23; see .agents/AGENTS.md § Naming. A published id is permanent.
const NAME_PATTERN = /^com\.arman\.[a-z0-9]+(?:-[a-z0-9]+)*$/;

const SEMVER_PATTERN =
    /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$/;

const UNITY_PATTERN = /^\d{4}\.\d+$/;

// Stock text carried by PackageTemplate. Shipping any of it is a validate failure.
const PLACEHOLDER_DESCRIPTIONS = [
    "description",
    "replace this string with your own description",
    "replace this string with your own package description",
];

// Unity does not generate .meta files for these, so neither do we require them.
const META_EXEMPT = new Set(["node_modules", "obj", "bin", "Library", "Temp"]);

function isMetaExempt(name) {
    return name.startsWith(".") || name.endsWith("~") || META_EXEMPT.has(name);
}

function run(cmd, args, opts = {}) {
    return spawnSync(cmd, args, { encoding: "utf8", ...opts });
}

// On Windows `npm` is a .cmd shim, and since Node 18.20/20.12 spawning one
// without a shell fails with EINVAL. Shelling out means quoting any argument
// that could contain a space (the pack destination can).
function npm(args, opts = {}) {
    if (process.platform !== "win32") return run(NPM, args, opts);
    const quoted = args.map((a) => (/[\s"]/.test(a) ? `"${a.replace(/"/g, '\\"')}"` : a));
    return run(NPM, quoted, { shell: true, ...opts });
}

function describeFailure(result) {
    if (result.error) return result.error.message;
    return (result.stderr || result.stdout || "").trim() || `exited with code ${result.status}`;
}

function git(...args) {
    const r = run("git", ["-C", ROOT, ...args]);
    if (r.error) throw r.error;
    return { code: r.status, out: (r.stdout || "").trim(), err: (r.stderr || "").trim() };
}

// ---------------------------------------------------------------- discovery

// Globs Packages/*/package.json. Packages/manifest.json and packages-lock.json
// sit at the Packages/ root and are skipped by construction. Three directories
// contain spaces (Asset Providing, Scene Management, UI Management) — every path
// here stays an array element, never an interpolated shell string.
function discoverPackages() {
    if (!fs.existsSync(PACKAGES_DIR)) {
        fail(`No Packages/ directory at ${PACKAGES_DIR}`);
    }
    const packages = [];
    for (const entry of fs.readdirSync(PACKAGES_DIR, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name))) {
        if (!entry.isDirectory()) continue;
        const dir = path.join(PACKAGES_DIR, entry.name);
        const manifestPath = path.join(dir, "package.json");
        if (!fs.existsSync(manifestPath)) continue;

        const pkg = { folder: entry.name, dir, manifestPath, manifest: null, parseError: null };
        try {
            pkg.manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
        } catch (e) {
            pkg.parseError = e.message;
        }
        pkg.private = pkg.manifest?.private === true;
        pkg.name = pkg.manifest?.name;
        pkg.version = pkg.manifest?.version;
        packages.push(pkg);
    }
    return packages;
}

function publishable(packages) {
    return packages.filter((p) => !p.private && !p.parseError);
}

// `--only <name-or-folder>`, repeatable. Rollout step 4 is a single-package
// smoke test, so tagging exactly one package has to be a first-class operation.
function applyOnly(packages, only) {
    if (!only) return packages;
    const wanted = (Array.isArray(only) ? only : [only]).map((s) => s.trim());
    const selected = packages.filter((p) => wanted.includes(p.name) || wanted.includes(p.folder));
    const unmatched = wanted.filter(
        (w) => !packages.some((p) => p.name === w || p.folder === w)
    );
    if (unmatched.length) {
        fail(`--only matched no package: ${unmatched.join(", ")}`);
        process.exit(1);
    }
    return selected;
}

/** `--bump <package>=<major|minor|patch>`, repeatable, id or folder name. */
export function parseBumps(raw, packages) {
    const bumps = new Map();
    if (!raw) return bumps;
    for (const item of Array.isArray(raw) ? raw : [raw]) {
        const at = item.lastIndexOf("=");
        if (at === -1) throw new Error(`--bump \`${item}\` is not <package>=<major|minor|patch>`);
        const target = item.slice(0, at).trim();
        const part = item.slice(at + 1).trim().toLowerCase();
        if (!["major", "minor", "patch"].includes(part)) {
            throw new Error(`--bump \`${item}\`: \`${part}\` is not major, minor, or patch`);
        }
        if (!packages.some((p) => p.name === target || p.folder === target)) {
            throw new Error(`--bump matched no package: ${target}`);
        }
        bumps.set(target, part);
    }
    return bumps;
}

// ---------------------------------------------------------------- validate

function missingMetaFiles(dir) {
    const missing = [];
    const walk = (current) => {
        const entries = fs.readdirSync(current, { withFileTypes: true });
        const present = new Set(entries.map((e) => e.name));
        for (const entry of entries) {
            if (isMetaExempt(entry.name) || entry.name.endsWith(".meta")) continue;
            if (!present.has(`${entry.name}.meta`)) {
                missing.push(path.relative(dir, path.join(current, entry.name)));
            }
            if (entry.isDirectory()) walk(path.join(current, entry.name));
        }
    };
    walk(dir);
    return missing;
}

function validatePackage(pkg, byName, existingTags) {
    const errors = [];
    const add = (msg) => errors.push(msg);

    if (pkg.parseError) return [`package.json is not parseable JSON: ${pkg.parseError}`];
    const m = pkg.manifest;

    if (!m.name) add("missing `name`");
    else if (!NAME_PATTERN.test(m.name)) add(`name \`${m.name}\` is not com.arman.<kebab-case-name>`);

    if (!m.version) add("missing `version`");
    else if (!SEMVER_PATTERN.test(m.version)) add(`version \`${m.version}\` is not valid semver`);

    if (!m.displayName) add("missing `displayName`");

    const description = (m.description || "").trim();
    if (!description) add("missing `description`");
    else if (PLACEHOLDER_DESCRIPTIONS.includes(description.toLowerCase())) {
        add(`description is stock placeholder text: "${description}"`);
    }

    if (!m.unity) add("missing `unity` (minimum Editor version)");
    else if (!UNITY_PATTERN.test(m.unity)) add(`unity \`${m.unity}\` is not YYYY.M`);

    // Publishing an unlicensed public package is not acceptable.
    if (m.license !== "MIT") add(`license is \`${m.license ?? "absent"}\`, expected "MIT"`);
    if (!fs.existsSync(path.join(pkg.dir, "LICENSE.md"))) add("missing LICENSE.md");

    // Asmdef GUIDs live in .meta files and are referenced across packages —
    // a missing one silently breaks compilation for consumers.
    for (const missing of missingMetaFiles(pkg.dir)) add(`missing .meta for \`${missing}\``);

    // Internal deps must point at a package in this repo, at a version that is
    // either already tagged or is the one this run would tag.
    for (const [dep, range] of Object.entries(m.dependencies ?? {})) {
        if (!dep.startsWith("com.arman.")) continue;
        const target = byName.get(dep);
        if (!target) {
            add(`depends on \`${dep}\`, which is not a package in this repo`);
            continue;
        }
        if (target.private) {
            add(`depends on \`${dep}\`, which is private and will never publish`);
            continue;
        }
        const satisfied = range === target.version || existingTags.has(`${dep}/${range}`);
        if (!satisfied) {
            add(`depends on \`${dep}\`@${range}, but that package is at ${target.version} and no \`${dep}/${range}\` tag exists`);
        }
    }

    // The real packaging step. Catches anything the checks above miss.
    const packed = npm(["pack", "--dry-run"], { cwd: pkg.dir });
    if (packed.status !== 0) add(`npm pack --dry-run failed: ${describeFailure(packed)}`);

    return errors;
}

function cmdValidate(packages, flags) {
    // Dependency resolution always sees every package, even under --only.
    const byName = new Map(packages.filter((p) => p.name).map((p) => [p.name, p]));
    const existingTags = listTags();
    const results = [];
    let failed = 0;

    for (const pkg of applyOnly(packages, flags.only)) {
        if (pkg.private) {
            results.push({ folder: pkg.folder, name: pkg.name, status: "skipped", reason: "private" });
            continue;
        }
        const errors = validatePackage(pkg, byName, existingTags);
        if (errors.length) failed++;
        results.push({
            folder: pkg.folder,
            name: pkg.name,
            version: pkg.version,
            status: errors.length ? "fail" : "ok",
            errors,
        });
    }

    if (flags.json) {
        console.log(JSON.stringify({ command: "validate", results }, null, 2));
    } else {
        for (const r of results) {
            if (r.status === "skipped") {
                console.log(`  --  ${r.folder} (${r.name ?? "?"}) — skipped, ${r.reason}`);
            } else if (r.status === "ok") {
                console.log(`  ok  ${r.folder} — ${r.name}@${r.version}`);
            } else {
                console.log(`FAIL  ${r.folder} — ${r.name ?? "?"}`);
                for (const e of r.errors) console.log(`        ${e}`);
            }
        }
        const checked = results.filter((r) => r.status !== "skipped").length;
        console.log(`\n${checked - failed}/${checked} packages valid` + (failed ? `, ${failed} failed` : ""));
    }
    return failed === 0 ? 0 : 1;
}

// ---------------------------------------------------------------- prepare

const CHANGELOG = "CHANGELOG.md";
const H2 = /^##\s/;
const H2_VERSION = /^##\s+\[([^\]]+)\]/;
const UNRELEASED = /^unreleased$/i;
const SUB_HEADING = /^#{3,}\s+(.+?)\s*$/;
const PLAIN_SEMVER = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/;

// Keep a Changelog's six sections, mapped to what each implies about the API.
// `Removed` is the only breaking one; while major is 0 it lands on the minor
// all the same, which is the whole point of the 0.x rule below.
const SECTION_LEVELS = {
    added: "feature",
    changed: "feature",
    deprecated: "feature",
    removed: "breaking",
    fixed: "fix",
    security: "fix",
};
const LEVEL_RANK = { fix: 1, feature: 2, breaking: 3 };

/** The `## [Unreleased]` heading's line index and the index past its body. */
export function unreleasedRange(lines) {
    const start = lines.findIndex((line) => H2_VERSION.test(line) && UNRELEASED.test(line.match(H2_VERSION)[1]));
    if (start === -1) return null;
    const rest = lines.slice(start + 1);
    const offset = rest.findIndex((line) => H2.test(line));
    return { start, end: offset === -1 ? lines.length : start + 1 + offset };
}

/**
 * The body's entry lines. Identical to `entriesOf` in changelog-check.mjs —
 * "does this section have entries" must mean one thing in both scripts, or a
 * pull request can pass the check and then be skipped by the release.
 */
export function unreleasedEntries(lines) {
    return lines.map((line) => line.trim()).filter((line) => line !== "" && !SUB_HEADING.test(line));
}

/** The `###` sub-headings with at least one line under them, in file order. */
export function populatedSubsections(lines) {
    const found = [];
    let current = null;
    for (const line of lines) {
        const heading = line.match(SUB_HEADING);
        if (heading) {
            current = { name: heading[1], entries: 0 };
            found.push(current);
            continue;
        }
        if (current !== null && line.trim() !== "") current.entries += 1;
    }
    return found.filter((section) => section.entries > 0).map((section) => section.name);
}

/** The highest level the sub-headings imply, or null if none is recognised. */
export function bumpLevel(names) {
    let best = null;
    for (const name of names) {
        const level = SECTION_LEVELS[name.trim().toLowerCase()];
        if (level === undefined) continue;
        if (best === null || LEVEL_RANK[level] > LEVEL_RANK[best]) best = level;
    }
    return best;
}

function parts(version) {
    const match = PLAIN_SEMVER.exec(version);
    if (match === null) throw new Error(`version \`${version}\` is not plain X.Y.Z semver`);
    return match.slice(1, 4).map(Number);
}

/**
 * 0.x-aware. Below 1.0.0 the major is reserved, so a breaking change bumps the
 * minor exactly as a feature does — the two are indistinguishable to a consumer
 * pinning `0.1.0`, which is precisely what 0.x means.
 */
export function nextVersion(version, level) {
    const [major, minor, patch] = parts(version);
    if (major === 0) return level === "fix" ? `0.${minor}.${patch + 1}` : `0.${minor + 1}.0`;
    if (level === "breaking") return `${major + 1}.0.0`;
    if (level === "feature") return `${major}.${minor + 1}.0`;
    return `${major}.${minor}.${patch + 1}`;
}

/** `--bump pkg=minor` asked for a part, so no 0.x remapping is applied. */
export function explicitBump(version, part) {
    const [major, minor, patch] = parts(version);
    if (part === "major") return `${major + 1}.0.0`;
    if (part === "minor") return `${major}.${minor + 1}.0`;
    if (part === "patch") return `${major}.${minor}.${patch + 1}`;
    throw new Error(`unknown bump part \`${part}\`, expected major, minor, or patch`);
}

/** Renames `## [Unreleased]` to `## [X.Y.Z] - DATE`, leaving nothing behind. */
export function releaseChangelog(text, version, date) {
    const eol = text.includes("\r\n") ? "\r\n" : "\n";
    const lines = text.split(/\r?\n/);
    const range = unreleasedRange(lines);
    if (range === null) throw new Error("no `## [Unreleased]` heading");
    lines[range.start] = `## [${version}] - ${date}`;
    return lines.join(eol);
}

/**
 * A targeted single-line replacement, not parse-and-reserialise: key order,
 * indentation, and the trailing newline all survive, so the diff is one line.
 * More than one `"version"` key means the file is not shaped as expected —
 * refuse rather than rewrite the wrong one.
 */
export function replaceManifestVersion(text, version) {
    const pattern = /^([ \t]*"version"[ \t]*:[ \t]*)"[^"]*"/gm;
    const matches = text.match(pattern) ?? [];
    if (matches.length !== 1) {
        throw new Error(`package.json must contain exactly one \`"version"\` line, found ${matches.length}`);
    }
    return text.replace(pattern, `$1"${version}"`);
}

/**
 * Works out what each package's next version is, without writing anything.
 * Returns the plan and the packages that could not be planned; a package with
 * entries under no recognised `###` heading is an error rather than a guess or
 * a silent skip, because the alternative is releasing the wrong number.
 */
export function planPrepare(packages, { bumps = new Map() } = {}) {
    const plan = [];
    const errors = [];

    for (const pkg of packages) {
        const file = path.join(pkg.dir, CHANGELOG);
        if (!fs.existsSync(file)) {
            errors.push(`${pkg.folder}: no ${CHANGELOG}`);
            continue;
        }
        const text = fs.readFileSync(file, "utf8");
        const lines = text.split(/\r?\n/);
        const range = unreleasedRange(lines);
        if (range === null) continue; // No heading: nothing is waiting to ship.

        const body = lines.slice(range.start + 1, range.end);
        if (unreleasedEntries(body).length === 0) continue; // Empty: nothing to ship.

        const requested = bumps.get(pkg.name) ?? bumps.get(pkg.folder);
        const sections = populatedSubsections(body);
        const level = requested ? null : bumpLevel(sections);

        if (!requested && level === null) {
            errors.push(
                `${pkg.name ?? pkg.folder}: has entries under \`## [Unreleased]\` but none under a recognised \`###\` heading (Added, Changed, Deprecated, Removed, Fixed, Security). File them, or pass --bump ${pkg.name ?? pkg.folder}=<major|minor|patch>.`,
            );
            continue;
        }

        let to;
        try {
            to = requested ? explicitBump(pkg.version, requested) : nextVersion(pkg.version, level);
        } catch (error) {
            errors.push(`${pkg.name ?? pkg.folder}: ${error.message}`);
            continue;
        }

        plan.push({
            folder: pkg.folder,
            name: pkg.name,
            from: pkg.version,
            to,
            level: requested ?? level,
            reason: requested ? `${requested}: requested with --bump` : `${level}: ${sections.join(", ")}`,
        });
    }

    return { plan, errors };
}

// ---------------------------------------------------------------- pack

function cmdPack(packages, flags) {
    const outDir = path.resolve(ROOT, flags.out ?? "PackageExports");
    fs.mkdirSync(outDir, { recursive: true });

    const results = [];
    let failed = 0;
    for (const pkg of publishable(applyOnly(packages, flags.only))) {
        const packed = npm(["pack", "--pack-destination", outDir], { cwd: pkg.dir });
        const ok = packed.status === 0;
        if (!ok) failed++;
        const tarball = ok ? (packed.stdout || "").trim().split(/\r?\n/).pop() : null;
        results.push({ folder: pkg.folder, name: pkg.name, version: pkg.version, ok, tarball });
        if (!flags.json) {
            console.log(ok ? `  ok  ${tarball}` : `FAIL  ${pkg.folder}: ${describeFailure(packed)}`);
        }
    }

    if (flags.json) console.log(JSON.stringify({ command: "pack", outDir, results }, null, 2));
    else console.log(`\n${results.length - failed} tarball(s) in ${path.relative(ROOT, outDir)}/`);
    return failed === 0 ? 0 : 1;
}

// ---------------------------------------------------------------- tag

function listTags() {
    const r = git("tag", "--list");
    return new Set(r.out ? r.out.split(/\r?\n/).map((t) => t.trim()).filter(Boolean) : []);
}

function cmdTag(packages, flags) {
    const branch = git("rev-parse", "--abbrev-ref", "HEAD").out;
    if (branch !== RELEASE_BRANCH && !flags["allow-branch"]) {
        return fail(`on branch \`${branch}\`, expected \`${RELEASE_BRANCH}\`. Pass --allow-branch to override.`);
    }
    if (git("status", "--porcelain").out && !flags["allow-dirty"]) {
        return fail("working tree is dirty. Tag a clean tree, or pass --allow-dirty.");
    }

    const existing = listTags();
    const planned = [];
    const skipped = [];

    for (const pkg of publishable(applyOnly(packages, flags.only))) {
        if (!pkg.name || !pkg.version) continue;
        const tag = `${pkg.name}/${pkg.version}`;
        // Tags are independent, so no topological sort is needed here — unlike
        // the npm-publish design, nothing has to be released before anything else.
        (existing.has(tag) ? skipped : planned).push({ tag, folder: pkg.folder });
    }

    if (!flags.json) {
        for (const s of skipped) console.log(`  --  ${s.tag} — already tagged`);
        for (const p of planned) console.log(`${flags["dry-run"] ? " plan" : "  ok"}  ${p.tag}`);
    }

    if (flags["dry-run"]) {
        if (flags.json) console.log(JSON.stringify({ command: "tag", dryRun: true, planned, skipped }, null, 2));
        else console.log(`\n${planned.length} tag(s) would be created, ${skipped.length} already exist.`);
        return 0;
    }

    for (const p of planned) {
        const r = git("tag", "-a", p.tag, "-m", p.tag);
        if (r.code !== 0) return fail(`could not create tag ${p.tag}: ${r.err}`);
    }

    // Pushing a tag IS the publish. Never implicit.
    let pushed = false;
    if (flags.push && planned.length) {
        const r = git("push", "origin", ...planned.map((p) => p.tag));
        if (r.code !== 0) return fail(`could not push tags: ${r.err}`);
        pushed = true;
    }

    if (flags.json) console.log(JSON.stringify({ command: "tag", planned, skipped, pushed }, null, 2));
    else {
        console.log(`\n${planned.length} tag(s) created, ${skipped.length} already existed.`);
        if (planned.length && !pushed) console.log("Not pushed. Re-run with --push to publish, or `git push origin <tag>`.");
        if (pushed) console.log("Pushed. OpenUPM builds these within 15-30 minutes.");
    }
    return 0;
}

function cmdPrepare(packages, flags) {
    const branch = git("rev-parse", "--abbrev-ref", "HEAD").out;
    if (branch === RELEASE_BRANCH && !flags["allow-branch"]) {
        return fail(`on branch \`${branch}\`. Prepare a release on a branch off \`dev\`, not on \`${RELEASE_BRANCH}\`. Pass --allow-branch to override.`);
    }
    if (git("status", "--porcelain").out && !flags["allow-dirty"] && !flags["dry-run"]) {
        return fail("working tree is dirty. Prepare a clean tree so the release diff is reviewable, or pass --allow-dirty.");
    }

    // Validate --bump against the packages that will actually be prepared,
    // not the full discovery list — otherwise `--bump PackageTemplate=major`
    // is silently accepted and then does nothing.
    const selected = applyOnly(publishable(packages), flags.only);

    let bumps;
    try {
        bumps = parseBumps(flags.bump, selected);
    } catch (error) {
        return fail(error.message);
    }

    const date = flags.date ?? new Date().toISOString().slice(0, 10);
    if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || Number.isNaN(Date.parse(date))) {
        return fail(`--date \`${date}\` is not a valid YYYY-MM-DD date`);
    }

    const { plan, errors } = planPrepare(selected, { bumps });

    // Phase 1: compute every rewritten file up front, for every package in
    // the plan, before touching disk. A manifest that can't be rewritten
    // (e.g. not exactly one "version" line) becomes a reported error here,
    // not an uncaught throw mid-write that leaves the release half-done.
    const writes = [];
    for (const entry of plan) {
        const dir = path.join(PACKAGES_DIR, entry.folder);
        const changelogPath = path.join(dir, CHANGELOG);
        const manifestPath = path.join(dir, "package.json");
        try {
            const changelogText = releaseChangelog(fs.readFileSync(changelogPath, "utf8"), entry.to, date);
            const manifestText = replaceManifestVersion(fs.readFileSync(manifestPath, "utf8"), entry.to);
            writes.push({ path: changelogPath, contents: changelogText }, { path: manifestPath, contents: manifestText });
        } catch (error) {
            errors.push(`${entry.name ?? entry.folder}: ${error.message}`);
        }
    }

    // Phase 2: write only if every package in the plan computed cleanly — a
    // half-rewritten release is worse than none.
    if (errors.length === 0 && plan.length > 0 && !flags["dry-run"]) {
        for (const write of writes) fs.writeFileSync(write.path, write.contents);
    }

    // The written state has to survive the same checks CI runs, before it is
    // ever committed. Re-discover: the manifests on disk have just changed.
    // cmdValidate always prints its own report; capture it here so it can't
    // interleave with (and corrupt) this command's own --json output, and
    // fold any failure text into our own errors instead of discarding it.
    //
    // Gated on `!flags["dry-run"]` deliberately, not as an oversight: a dry
    // run writes nothing, so there is nothing on disk to validate. The
    // asymmetry is real — a `--dry-run` plan cannot surface a validation
    // failure the real run would hit — and accepted as the cost of "dry-run
    // touches nothing."
    if (errors.length === 0 && plan.length > 0 && !flags["dry-run"]) {
        const only = plan.map((entry) => entry.folder);
        const captured = [];
        const originalLog = console.log;
        let code;
        console.log = (...args) => captured.push(args.join(" "));
        try {
            code = cmdValidate(discoverPackages(), { only });
        } finally {
            console.log = originalLog;
        }
        if (code !== 0) {
            errors.push("the prepared packages do not validate. Inspect the diff before committing:");
            for (const line of captured) if (line.trim() !== "") errors.push(line);
        }
    }

    const report = { command: "prepare", date, dryRun: flags["dry-run"] === true, plan, errors };

    if (flags.json) {
        console.log(JSON.stringify(report, null, 2));
    } else {
        for (const error of errors) console.error(`error: ${error}`);
        if (plan.length === 0) {
            console.log("nothing to prepare — no package has entries under `## [Unreleased]`.");
        } else {
            for (const entry of plan) {
                console.log(`  ${entry.name ?? entry.folder}  ${entry.from} → ${entry.to}   (${entry.reason})`);
            }
            if (errors.length === 0) {
                console.log(
                    `\n${plan.length} package(s) ${flags["dry-run"] ? "would be prepared" : "prepared"} for ${date}.`,
                );
                if (!flags["dry-run"]) {
                    console.log("Review the diff, commit it, and open a pull request into `dev`.");
                }
            } else {
                // Phase 1 collected errors, so the write loop above never ran —
                // nothing on disk changed. Say so plainly; on a terminal stdout
                // and stderr interleave, and this is the line the user sees last.
                console.log(`\nnothing written — ${plan.length} package(s) would have been prepared for ${date}.`);
            }
        }
    }

    return errors.length > 0 ? 1 : 0;
}

// ---------------------------------------------------------------- entry

function fail(message) {
    console.error(`error: ${message}`);
    return 1;
}

function usage() {
    console.error(`usage: node Tools/upm-release.mjs <command> [options]

commands:
  validate            check every publishable package is releasable
  pack                write tarballs (verification aid, not a distribution channel)
  tag                 create <package-name>/<version> tags for new versions
  prepare             turn each [Unreleased] section into a new version

options:
  --json              machine-readable output
  --only <pkg>        limit to one package, by id or folder name; repeatable
  --out <dir>         pack destination (default: PackageExports)
  --push              tag: push the created tags to origin (this is the publish)
  --dry-run           tag/prepare: report what would happen, change nothing
  --allow-dirty       tag/prepare: permit a dirty working tree
  --allow-branch      tag/prepare: permit a branch other than ${RELEASE_BRANCH}
  --bump <pkg>=<part> prepare: force major|minor|patch for one package; repeatable
  --date <YYYY-MM-DD> prepare: the date written into the version heading`);
    return 2;
}

function parseArgs(argv) {
    const flags = {};
    const positional = [];
    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];
        if (!arg.startsWith("--")) {
            positional.push(arg);
            continue;
        }
        const key = arg.slice(2);
        if (key === "out" || key === "date") {
            const value = argv[++i];
            if (value === undefined || value.startsWith("--")) throw new Error(`--${key} requires a value`);
            flags[key] = value;
        } else if (key === "only") {
            const value = argv[++i];
            if (value === undefined || value.startsWith("--")) throw new Error("--only requires a value");
            (flags.only ??= []).push(value);
        } else if (key === "bump") {
            const value = argv[++i];
            if (value === undefined || value.startsWith("--")) throw new Error("--bump requires a value");
            (flags.bump ??= []).push(value);
        } else flags[key] = true;
    }
    return { command: positional[0], flags };
}

// Importable for tests: the dispatch runs only when this file is the entry
// point, not when Tools/upm-release.test.mjs imports the helpers above.
const invokedDirectly = process.argv[1] !== undefined
    && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);

if (invokedDirectly) main();

function main() {
    let command, flags;
    try {
        ({ command, flags } = parseArgs(process.argv.slice(2)));
    } catch (error) {
        console.error(`error: ${error.message}`);
        process.exit(2);
    }
    if (!command) process.exit(usage());

    const packages = discoverPackages();
    switch (command) {
        case "validate":
            process.exit(cmdValidate(packages, flags));
        case "pack":
            process.exit(cmdPack(packages, flags));
        case "tag":
            process.exit(cmdTag(packages, flags));
        case "prepare":
            process.exit(cmdPrepare(packages, flags));
        default:
            console.error(`error: unknown command \`${command}\``);
            process.exit(usage());
    }
}
