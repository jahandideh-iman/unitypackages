// Checks this repo's own Markdown, so the routing from AGENTS.md to the files
// that hold each rule cannot rot unnoticed. Six checks:
//
//   link     every relative Markdown link resolves to a file or folder
//   anchor   every #fragment matches a heading in its target
//   mirror   .agents/skills/ and .claude/skills/ hold the same files, byte for byte
//   agents   .claude/agents/ and .opencode/agents/ pair up, same description and body
//   heading every heading in .agents/rules/MANIFEST.tsv exists in the file it names
//   budget   AGENTS.md is between 6,000 and 12,000 bytes, LF-normalised
//
//     node Tools/docs-check.mjs            # human-readable, exit 0 clean / 1 findings
//     node Tools/docs-check.mjs --json     # the same report as JSON
//     node Tools/docs-check.mjs --root <dir>
//
// Dependency-free, like the rest of Tools/. Scans tracked files only
// (`git ls-files`), so build output and untracked scratch never count.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const SCRIPT = fileURLToPath(import.meta.url);
const DEFAULT_ROOT = path.dirname(path.dirname(SCRIPT));

const CORE = "AGENTS.md";
export const CORE_MIN_BYTES = 6000;
export const CORE_MAX_BYTES = 12000;
const MANIFEST = ".agents/rules/MANIFEST.tsv";
const SKILLS = ".agents/skills";
const SKILLS_MIRROR = ".claude/skills";
const SKILL_SOURCES = ".agents/skills/THIRD_PARTY_SKILLS.md";
const AGENTS = ".claude/agents";
const AGENTS_MIRROR = ".opencode/agents";
const AUTHORED_IN_REPO = "Authored in-repo";

// A link target with a URL scheme (https:, mailto:, ...) is not a repo path.
const SCHEME = /^[a-z][a-z0-9+.-]*:/i;
// [text](target) and [text](<target with spaces>).
const LINK = /\]\(\s*(?:<([^>]+)>|([^)\s]+))/g;
// [label]: target and [label]: <target with spaces> "optional title", one per line.
const REFLINK = /^ {0,3}\[[^\]]+\]:\s*(?:<([^>]*)>|(\S+))/;
const HEADING = /^ {0,3}#{1,6}\s/;

function git(root, ...args) {
    const result = spawnSync("git", ["-C", root, ...args], { encoding: "utf8" });
    if (result.error) {
        throw new Error(`git failed: ${result.error.message}`);
    }
    if (result.status !== 0) {
        throw new Error(`git ${args.join(" ")} failed: ${result.stderr || result.stdout}`);
    }
    return result.stdout;
}

function tracked(root, pathspec) {
    return git(root, "ls-files", "-z", "--", pathspec)
        .split("\0")
        .filter((file) => file !== "");
}

function read(root, file) {
    return fs.readFileSync(path.join(root, file), "utf8");
}

/**
 * Blanks every fenced code block, keeping the line count so line numbers still
 * point at the right line. A fence closes on a run of the same character at
 * least as long as the one that opened it (CommonMark), so a 4-backtick fence
 * quoting a 3-backtick one is not closed early. An unclosed fence runs to the
 * end of the file.
 */
export function blankFences(text) {
    const lines = text.split("\n");
    let fence = null;
    for (let i = 0; i < lines.length; i++) {
        const opener = /^ {0,3}(`{3,}|~{3,})/.exec(lines[i]);
        if (fence === null) {
            if (opener !== null) {
                fence = opener[1];
                lines[i] = "";
            }
            continue;
        }
        const closer = /^ {0,3}(`{3,}|~{3,})\s*$/.exec(lines[i]);
        if (closer !== null && closer[1][0] === fence[0] && closer[1].length >= fence.length) {
            fence = null;
        }
        lines[i] = "";
    }
    return lines.join("\n");
}

/**
 * Blanks fences and inline code spans. Code quotes a path — a file as it once
 * was, an example, a heading cited literally — and is not a link the document
 * makes.
 */
export function blankCode(text) {
    return blankFences(text).replace(/(`+)[^\n]*?[^`\n]\1(?!`)/g, (span) =>
        " ".repeat(span.length),
    );
}

function headingText(line) {
    return line.replace(/^ {0,3}#{1,6}\s+/, "").replace(/\s+#*\s*$/, "");
}

/**
 * A document's real headings, as text. Only fences are blanked: backticks are
 * ordinary heading text, and a manifest row matches them character for
 * character.
 */
export function headings(text) {
    return blankFences(text.replace(/\r\n/g, "\n"))
        .split("\n")
        .filter((line) => HEADING.test(line))
        .map(headingText);
}

/**
 * GitHub's heading id: lowercase; a link contributes its text, not its URL;
 * drop every character that is not a letter, mark, digit, underscore, space or
 * hyphen; spaces become hyphens. A spaced em dash therefore becomes "--".
 */
export function slug(heading) {
    return heading
        .toLowerCase()
        .replace(/!?\[([^\]]*)\]\([^)]*\)/g, "$1")
        .replace(/[^\p{L}\p{M}\p{N}_ -]/gu, "")
        .replace(/ /g, "-");
}

/** Every anchor a document offers; a repeated heading gets -1, -2, ... */
export function anchors(text) {
    const seen = new Map();
    const result = new Set();
    for (const heading of headings(text)) {
        const base = slug(heading);
        const count = seen.get(base);
        if (count === undefined) {
            seen.set(base, 0);
            result.add(base);
        } else {
            seen.set(base, count + 1);
            result.add(`${base}-${count + 1}`);
        }
    }
    return result;
}

/**
 * Every link in a document, with its 1-based line number: an inline
 * `[text](target)` and a reference-style definition `[label]: target` alike.
 */
export function links(text) {
    const blanked = blankCode(text.replace(/\r\n/g, "\n"));
    const result = [];
    for (const match of blanked.matchAll(LINK)) {
        const target = match[1] ?? match[2];
        const line = blanked.slice(0, match.index).split("\n").length;
        result.push({ target, line });
    }
    blanked.split("\n").forEach((lineText, index) => {
        const match = REFLINK.exec(lineText);
        if (match !== null) result.push({ target: match[1] ?? match[2], line: index + 1 });
    });
    return result;
}

function decode(part) {
    try {
        return decodeURIComponent(part);
    } catch {
        return part;
    }
}

/** A link's path without its query string: `./a.md?plain=1` is `./a.md`. */
function pathOf(linkPart) {
    return linkPart.split("?")[0];
}

/** Where a link's path part points, relative to the repo root. */
function resolveTarget(file, pathPart) {
    const decoded = decode(pathPart);
    return decoded.startsWith("/")
        ? path.posix.normalize(decoded.slice(1))
        : path.posix.normalize(path.posix.join(path.posix.dirname(file), decoded));
}

/**
 * Skill packs vendored from an outside source, read from the Source column of
 * THIRD_PARTY_SKILLS.md: every skill named in a row whose source is not
 * "Authored in-repo". An anchor inside one is upstream's to fix.
 */
export function vendoredSkills(root) {
    const file = path.join(root, SKILL_SOURCES);
    if (!fs.existsSync(file)) return new Set();
    const vendored = new Set();
    for (const row of fs.readFileSync(file, "utf8").split(/\r?\n/)) {
        const cells = row.split("|").map((cell) => cell.trim());
        if (cells.length < 4 || /^-+$/.test(cells[1]) || cells[2].includes(AUTHORED_IN_REPO)) {
            continue;
        }
        for (const name of cells[1].matchAll(/`([^`]+)`/g)) vendored.add(name[1]);
    }
    return vendored;
}

function isVendored(file, vendored) {
    for (const name of vendored) {
        if (file.startsWith(`${SKILLS}/${name}/`) || file.startsWith(`${SKILLS_MIRROR}/${name}/`)) {
            return true;
        }
    }
    return false;
}

function checkLinks(root, markdown) {
    const findings = [];
    for (const file of markdown) {
        for (const { target, line } of links(read(root, file))) {
            if (SCHEME.test(target) || target.startsWith("#")) continue;
            const pathPart = pathOf(target.split("#")[0]);
            if (pathPart === "") continue;
            if (!fs.existsSync(path.join(root, resolveTarget(file, pathPart)))) {
                findings.push({ check: "link", file, line, message: `${target} does not exist` });
            }
        }
    }
    return findings;
}

function checkAnchors(root, markdown, vendored) {
    const findings = [];
    const cache = new Map();
    for (const file of markdown) {
        if (isVendored(file, vendored)) continue;
        for (const { target, line } of links(read(root, file))) {
            if (SCHEME.test(target) || !target.includes("#")) continue;
            const [linkPart, fragment] = target.split(/#(.*)/s);
            if (fragment === "") continue;
            const pathPart = pathOf(linkPart);
            const destination = pathPart === "" ? file : resolveTarget(file, pathPart);
            // A missing target is already a link finding; a non-Markdown
            // target (a line anchor into a workflow) has no headings to check.
            if (!destination.endsWith(".md") || !fs.existsSync(path.join(root, destination))) {
                continue;
            }
            if (!cache.has(destination)) cache.set(destination, anchors(read(root, destination)));
            if (!cache.get(destination).has(decode(fragment).toLowerCase())) {
                findings.push({
                    check: "anchor",
                    file,
                    line,
                    message: `${target}: no heading #${fragment} in ${destination}`,
                });
            }
        }
    }
    return findings;
}

function checkMirror(root) {
    const findings = [];
    const source = new Set(tracked(root, SKILLS).map((file) => file.slice(SKILLS.length + 1)));
    const mirror = new Set(
        tracked(root, SKILLS_MIRROR).map((file) => file.slice(SKILLS_MIRROR.length + 1)),
    );
    for (const file of source) {
        if (!mirror.has(file)) {
            findings.push({
                check: "mirror",
                file: `${SKILLS}/${file}`,
                message: `missing from ${SKILLS_MIRROR}/`,
            });
        } else if (
            !fs
                .readFileSync(path.join(root, SKILLS, file))
                .equals(fs.readFileSync(path.join(root, SKILLS_MIRROR, file)))
        ) {
            findings.push({
                check: "mirror",
                file: `${SKILLS}/${file}`,
                message: `differs from ${SKILLS_MIRROR}/${file}`,
            });
        }
    }
    for (const file of mirror) {
        if (!source.has(file)) {
            findings.push({
                check: "mirror",
                file: `${SKILLS_MIRROR}/${file}`,
                message: `missing from ${SKILLS}/`,
            });
        }
    }
    return findings;
}

/** Splits a Markdown file into its `key: value` frontmatter and the body after it. */
function frontmatter(text) {
    const match = /^---\n([\s\S]*?)\n---\n/.exec(text.replace(/\r\n/g, "\n"));
    if (!match) return { fields: {}, body: text.replace(/\r\n/g, "\n") };
    const fields = {};
    for (const line of match[1].split("\n")) {
        const colon = line.indexOf(":");
        if (colon > 0) fields[line.slice(0, colon).trim()] = line.slice(colon + 1).trim();
    }
    return { fields, body: text.replace(/\r\n/g, "\n").slice(match[0].length) };
}

// The two copies of a subagent differ only in frontmatter: Claude Code keys it by
// `name:`, OpenCode by file name and needs `mode: subagent`.
function checkAgents(root) {
    const findings = [];
    const names = (folder) =>
        new Set(tracked(root, `${folder}/*.md`).map((file) => file.slice(folder.length + 1)));
    const claude = names(AGENTS);
    const opencode = names(AGENTS_MIRROR);
    for (const file of claude) {
        if (!opencode.has(file)) {
            findings.push({
                check: "agents",
                file: `${AGENTS}/${file}`,
                message: `missing from ${AGENTS_MIRROR}/`,
            });
            continue;
        }
        const ours = frontmatter(read(root, `${AGENTS}/${file}`));
        const theirs = frontmatter(read(root, `${AGENTS_MIRROR}/${file}`));
        if (ours.fields.description !== theirs.fields.description) {
            findings.push({
                check: "agents",
                file: `${AGENTS}/${file}`,
                message: `description differs from ${AGENTS_MIRROR}/${file}`,
            });
        }
        if (ours.body !== theirs.body) {
            findings.push({
                check: "agents",
                file: `${AGENTS}/${file}`,
                message: `body differs from ${AGENTS_MIRROR}/${file}`,
            });
        }
    }
    for (const file of opencode) {
        if (!claude.has(file)) {
            findings.push({
                check: "agents",
                file: `${AGENTS_MIRROR}/${file}`,
                message: `missing from ${AGENTS}/`,
            });
        }
        if (frontmatter(read(root, `${AGENTS_MIRROR}/${file}`)).fields.mode !== "subagent") {
            findings.push({
                check: "agents",
                file: `${AGENTS_MIRROR}/${file}`,
                message: "needs mode: subagent",
            });
        }
    }
    return findings;
}

function checkManifest(root) {
    const manifest = path.join(root, MANIFEST);
    if (!fs.existsSync(manifest)) {
        return [{ check: "heading", file: MANIFEST, message: "the manifest does not exist" }];
    }
    const findings = [];
    const rows = fs.readFileSync(manifest, "utf8").split(/\r?\n/);
    rows.forEach((row, index) => {
        const line = index + 1;
        // A comment is a '#' line with no tab; every heading row has one.
        if (row.trim() === "" || (row.trimStart().startsWith("#") && !row.includes("\t"))) return;
        const [heading, destination] = row.split("\t").map((cell) => (cell ?? "").trim());
        if (!heading || !destination) {
            findings.push({
                check: "heading",
                file: MANIFEST,
                line,
                message: "a row needs a heading, a tab, and a file",
            });
            return;
        }
        if (!fs.existsSync(path.join(root, destination))) {
            findings.push({
                check: "heading",
                file: MANIFEST,
                line,
                message: `${destination} does not exist`,
            });
            return;
        }
        // Still a heading, not merely a phrase in a sentence: a deleted section
        // whose name survives in prose is exactly the loss this check catches.
        const hits = headings(read(root, destination)).filter((text) => text === heading);
        if (hits.length === 0) {
            findings.push({
                check: "heading",
                file: MANIFEST,
                line,
                message: `"${heading}" is not a heading in ${destination}`,
            });
        } else if (hits.length > 1) {
            findings.push({
                check: "heading",
                file: MANIFEST,
                line,
                message: `"${heading}" matches ${hits.length} headings in ${destination}`,
            });
        }
    });
    return findings;
}

function checkBudget(root) {
    const core = path.join(root, CORE);
    if (!fs.existsSync(core)) return [{ check: "budget", file: CORE, message: "does not exist" }];
    const bytes = Buffer.byteLength(fs.readFileSync(core, "utf8").replace(/\r\n/g, "\n"), "utf8");
    if (bytes < CORE_MIN_BYTES || bytes > CORE_MAX_BYTES) {
        return [
            {
                check: "budget",
                file: CORE,
                message: `${bytes} bytes; the budget is ${CORE_MIN_BYTES}-${CORE_MAX_BYTES}`,
            },
        ];
    }
    return [];
}

export function check(root) {
    const markdown = tracked(root, "*.md");
    const vendored = vendoredSkills(root);
    const findings = [
        ...checkLinks(root, markdown),
        ...checkAnchors(root, markdown, vendored),
        ...checkMirror(root),
        ...checkAgents(root),
        ...checkManifest(root),
        ...checkBudget(root),
    ];
    return { ok: findings.length === 0, findings };
}

function render(report) {
    const lines = report.findings.map(
        ({ check, file, line, message }) =>
            `${check.padEnd(8)}${line === undefined ? file : `${file}:${line}`}  ${message}`,
    );
    lines.push(
        report.ok
            ? "docs-check: all six checks pass."
            : `docs-check: ${report.findings.length} problem(s). Fix them, or see .agents/rules/documentation-voice.md.`,
    );
    return lines;
}

function parseArgs(argv) {
    const flags = { root: DEFAULT_ROOT, json: false };
    for (let i = 0; i < argv.length; i++) {
        if (argv[i] === "--json") flags.json = true;
        else if (argv[i] === "--root" && i + 1 < argv.length) flags.root = path.resolve(argv[++i]);
        else {
            console.error(
                `docs-check: unknown argument ${argv[i]}. Usage: node Tools/docs-check.mjs [--root <dir>] [--json]`,
            );
            process.exit(2);
        }
    }
    return flags;
}

if (process.argv[1] !== undefined && path.resolve(process.argv[1]) === SCRIPT) {
    const flags = parseArgs(process.argv.slice(2));
    let report;
    try {
        report = check(flags.root);
    } catch (error) {
        // Not a git repository, or git missing: the check could not run at all.
        console.error(`docs-check: ${error.message.trim()}`);
        process.exit(2);
    }
    console.log(flags.json ? JSON.stringify(report, null, 2) : render(report).join("\n"));
    process.exit(report.ok ? 0 : 1);
}
