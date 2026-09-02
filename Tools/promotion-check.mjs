#!/usr/bin/env node
// Refuses a pull request into `master` whose head is not `dev`.
//
//     node Tools/promotion-check.mjs [--event <name>] [--base <ref>] [--head <ref>]
//         [--head-repo <owner/name>] [--repo <owner/name>] [--json]
//
// `master` is release-only: merging into it publishes every package whose
// version is not tagged yet. It moves solely via a release pull request from
// `dev`, which is what keeps every commit on `master` also present on `dev`.
// A GitHub ruleset can require this check but cannot express the rule itself —
// rulesets target a destination ref and say nothing about a pull request's
// source branch.
//
// The branch logic is deliberately inside the script rather than an `if:` on
// the job. A job skipped by an `if:` reports `skipped`, and a skipped required
// check blocks the merge.
//
// Exit 0 = allowed, 1 = refused, 2 = the check itself could not run.

const RELEASE_BRANCH = "master";
const SOURCE_BRANCH = "dev";

/**
 * `refs/heads/dev` and `dev` are the same branch; the event gives either.
 * Anything else (a `refs/pull/…` ref, a tag) falls through unchanged and will
 * not equal `SOURCE_BRANCH` below — that is the fail-closed direction, and
 * deliberate: broaden the strip only if you can show it can't let a
 * non-`dev` ref masquerade as one.
 */
function shortRef(ref) {
    return (ref ?? "").trim().replace(/^refs\/heads\//, "");
}

export function decide(event, base, head, headRepo, repo) {
    if (event !== "pull_request") {
        return { ok: true, reason: `event is \`${event || "none"}\`, not a pull request — nothing to guard.` };
    }
    if (base !== RELEASE_BRANCH) {
        return { ok: true, reason: `pull request targets \`${base || "?"}\`, not \`${RELEASE_BRANCH}\`.` };
    }
    if (head === SOURCE_BRANCH) {
        // A fork can name its branch `dev` too. `head` alone cannot tell a
        // same-repo release branch from a fork's impostor, so provenance has
        // to agree as well — and an unknown provenance is not a pass.
        if (!headRepo || !repo || headRepo !== repo) {
            return {
                ok: false,
                reason: `pull request into \`${RELEASE_BRANCH}\` claims to come from \`${SOURCE_BRANCH}\`, but its head repository (\`${headRepo || "?"}\`) is not this repository (\`${repo || "?"}\`). A release pull request must come from this repository's \`${SOURCE_BRANCH}\` branch, not a fork.`,
            };
        }
        return { ok: true, reason: `release pull request \`${SOURCE_BRANCH}\` → \`${RELEASE_BRANCH}\`.` };
    }
    return {
        ok: false,
        reason: `pull request into \`${RELEASE_BRANCH}\` comes from \`${head || "?"}\`, but \`${RELEASE_BRANCH}\` may only be reached from \`${SOURCE_BRANCH}\`. Merge \`${head || "?"}\` into \`${SOURCE_BRANCH}\` first, then open the release pull request from \`${SOURCE_BRANCH}\`.`,
    };
}

function usage() {
    console.error(`usage: node Tools/promotion-check.mjs [options]

options:
  --event <name>       event name (default: $GITHUB_EVENT_NAME)
  --base <ref>         branch the pull request merges into (default: $GITHUB_BASE_REF)
  --head <ref>         branch the pull request proposes (default: $GITHUB_HEAD_REF)
  --head-repo <full>   head repository, owner/name (default: $PR_HEAD_REPO)
  --repo <full>        this repository, owner/name (default: $GITHUB_REPOSITORY)
  --json               machine-readable output`);
    return 2;
}

function parseArgs(argv) {
    const flags = {};
    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];
        if (arg === "--json") {
            flags.json = true;
        } else if (arg === "--event" || arg === "--base" || arg === "--head" || arg === "--head-repo" || arg === "--repo") {
            const value = argv[++i];
            if (value === undefined) return null;
            flags[arg.slice(2)] = value;
        } else {
            return null;
        }
    }
    return flags;
}

const flags = parseArgs(process.argv.slice(2));
if (flags === null) process.exit(usage());

const event = (flags.event ?? process.env.GITHUB_EVENT_NAME ?? "").trim();
const base = shortRef(flags.base ?? process.env.GITHUB_BASE_REF);
const head = shortRef(flags.head ?? process.env.GITHUB_HEAD_REF);
const headRepo = (flags["head-repo"] ?? process.env.PR_HEAD_REPO ?? "").trim();
const repo = (flags.repo ?? process.env.GITHUB_REPOSITORY ?? "").trim();

const result = { ...decide(event, base, head, headRepo, repo), event, base, head };
console.log(flags.json ? JSON.stringify(result, null, 2) : `${result.ok ? "ok" : "FAIL"}  ${result.reason}`);
process.exit(result.ok ? 0 : 1);
