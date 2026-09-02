// Tests for Tools/promotion-check.mjs.
//
// The script reads the pull request's event, base, and head from the
// environment, so a test is just an environment plus an expected exit code —
// no repository required.
//
//     node --test Tools/promotion-check.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const SCRIPT = path.join(HERE, "promotion-check.mjs");

function run(env = {}, args = ["--json"]) {
    const result = spawnSync(process.execPath, [SCRIPT, ...args], {
        encoding: "utf8",
        env: {
            ...process.env,
            GITHUB_EVENT_NAME: "",
            GITHUB_BASE_REF: "",
            GITHUB_HEAD_REF: "",
            PR_HEAD_REPO: "",
            GITHUB_REPOSITORY: "",
            ...env,
        },
    });
    return { status: result.status, stdout: result.stdout, stderr: result.stderr };
}

function report(env) {
    const { status, stdout } = run(env);
    return { status, report: JSON.parse(stdout) };
}

test("a pull request from dev into master is allowed", () => {
    const { status, report: r } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "master",
        GITHUB_HEAD_REF: "dev",
        PR_HEAD_REPO: "jahandideh-iman/unitypackages",
        GITHUB_REPOSITORY: "jahandideh-iman/unitypackages",
    });
    assert.equal(status, 0);
    assert.equal(r.ok, true);
});

test("a fork's branch named dev into master is refused", () => {
    const { status, report: r } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "master",
        GITHUB_HEAD_REF: "dev",
        PR_HEAD_REPO: "someone-else/unitypackages",
        GITHUB_REPOSITORY: "jahandideh-iman/unitypackages",
    });
    assert.equal(status, 1);
    assert.equal(r.ok, false);
    assert.match(r.reason, /fork/);
});

test("a missing head repository into master is refused, not waved through", () => {
    const { status, report: r } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "master",
        GITHUB_HEAD_REF: "dev",
        PR_HEAD_REPO: "",
        GITHUB_REPOSITORY: "jahandideh-iman/unitypackages",
    });
    assert.equal(status, 1);
    assert.equal(r.ok, false);
});

test("a pull request from a feature branch into master is refused", () => {
    const { status, report: r } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "master",
        GITHUB_HEAD_REF: "feat/shiny",
    });
    assert.equal(status, 1);
    assert.equal(r.ok, false);
    assert.match(r.reason, /feat\/shiny/);
});

test("the refusal names master and dev so the fix is obvious", () => {
    const { status, stdout } = run(
        { GITHUB_EVENT_NAME: "pull_request", GITHUB_BASE_REF: "master", GITHUB_HEAD_REF: "hotfix" },
        [],
    );
    assert.equal(status, 1);
    assert.match(stdout, /master/);
    assert.match(stdout, /dev/);
});

test("a pull request into dev is not a release pull request", () => {
    const { status, report: r } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "dev",
        GITHUB_HEAD_REF: "feat/shiny",
    });
    assert.equal(status, 0);
    assert.equal(r.ok, true);
});

test("a push is not a release pull request", () => {
    const { status, report: r } = report({ GITHUB_EVENT_NAME: "push", GITHUB_BASE_REF: "", GITHUB_HEAD_REF: "" });
    assert.equal(status, 0);
    assert.equal(r.ok, true);
});

test("refs/heads/ prefixes are tolerated", () => {
    const { status } = report({
        GITHUB_EVENT_NAME: "pull_request",
        GITHUB_BASE_REF: "refs/heads/master",
        GITHUB_HEAD_REF: "refs/heads/dev",
        PR_HEAD_REPO: "jahandideh-iman/unitypackages",
        GITHUB_REPOSITORY: "jahandideh-iman/unitypackages",
    });
    assert.equal(status, 0);
});

test("flags override the environment", () => {
    const { status } = report({
        GITHUB_EVENT_NAME: "push",
    });
    assert.equal(status, 0);

    const overridden = run({ GITHUB_EVENT_NAME: "push" }, [
        "--json",
        "--event",
        "pull_request",
        "--base",
        "master",
        "--head",
        "feat/x",
    ]);
    assert.equal(overridden.status, 1);
});

test("a flag without a value is a usage error", () => {
    const { status } = run({}, ["--base"]);
    assert.equal(status, 2);
});

test("an unknown flag is a usage error", () => {
    const { status } = run({}, ["--nope"]);
    assert.equal(status, 2);
});
