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

options:
  --json              machine-readable output
  --only <pkg>        limit to one package, by id or folder name; repeatable
  --out <dir>         pack destination (default: PackageExports)
  --push              tag: push the created tags to origin (this is the publish)
  --dry-run           tag: report what would happen, change nothing
  --allow-dirty       tag: permit a dirty working tree
  --allow-branch      tag: permit a branch other than ${RELEASE_BRANCH}`);
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
        if (key === "out") flags[key] = argv[++i];
        else if (key === "only") (flags.only ??= []).push(argv[++i]);
        else flags[key] = true;
    }
    return { command: positional[0], flags };
}

const { command, flags } = parseArgs(process.argv.slice(2));
if (!command) process.exit(usage());

const packages = discoverPackages();
switch (command) {
    case "validate":
        process.exit(cmdValidate(packages, flags));
    case "pack":
        process.exit(cmdPack(packages, flags));
    case "tag":
        process.exit(cmdTag(packages, flags));
    default:
        console.error(`error: unknown command \`${command}\``);
        process.exit(usage());
}
