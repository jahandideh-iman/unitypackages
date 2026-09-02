// Tests for Tools/upm-release.mjs.
//
// The pure helpers are imported and called directly; `prepare` itself is
// exercised end-to-end in Tools/upm-release.prepare.test.mjs.
//
//     node --test Tools/upm-release.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";

import {
    unreleasedRange,
    unreleasedEntries,
    populatedSubsections,
    bumpLevel,
    nextVersion,
    explicitBump,
    releaseChangelog,
    replaceManifestVersion,
    replaceManifestDependency,
    internalDependencies,
    addChangedEntries,
    cascadeDependents,
} from "./upm-release.mjs";

const CHANGELOG = [
    "# Changelog",
    "",
    "## [Unreleased]",
    "",
    "### Added",
    "",
    "- A new thing.",
    "",
    "### Fixed",
    "",
    "- An old thing.",
    "",
    "## [0.1.0] - 2026-08-29",
    "",
    "Initial release.",
    "",
].join("\n");

test("unreleasedRange finds the heading and the end of its body", () => {
    const lines = CHANGELOG.split("\n");
    assert.deepEqual(unreleasedRange(lines), { start: 2, end: 12 });
});

test("unreleasedRange returns null when there is no heading", () => {
    assert.equal(unreleasedRange(["# Changelog", "", "## [0.1.0] - 2026-08-29"]), null);
});

test("unreleasedRange runs to the end of file when nothing follows", () => {
    const lines = ["# Changelog", "", "## [Unreleased]", "", "### Added", "", "- A thing."];
    assert.deepEqual(unreleasedRange(lines), { start: 2, end: 7 });
});

test("unreleasedEntries ignores blanks and sub-headings", () => {
    const lines = CHANGELOG.split("\n");
    const { start, end } = unreleasedRange(lines);
    assert.deepEqual(unreleasedEntries(lines.slice(start + 1, end)), ["- A new thing.", "- An old thing."]);
});

test("unreleasedEntries treats a bare sub-heading as no entry", () => {
    assert.deepEqual(unreleasedEntries(["", "### Added", "", "### Fixed", ""]), []);
});

test("populatedSubsections lists only sub-headings with something under them", () => {
    assert.deepEqual(populatedSubsections(["### Added", "", "- A thing.", "", "### Fixed", ""]), ["Added"]);
});

test("bumpLevel takes the highest level present", () => {
    assert.equal(bumpLevel(["Fixed"]), "fix");
    assert.equal(bumpLevel(["Fixed", "Added"]), "feature");
    assert.equal(bumpLevel(["Fixed", "Removed"]), "breaking");
    assert.equal(bumpLevel(["Changed", "Deprecated"]), "feature");
    assert.equal(bumpLevel(["Security"]), "fix");
});

test("bumpLevel is case-insensitive and ignores unknown headings", () => {
    assert.equal(bumpLevel(["added"]), "feature");
    assert.equal(bumpLevel(["Notes"]), null);
    assert.equal(bumpLevel(["Notes", "Fixed"]), "fix");
    assert.equal(bumpLevel([]), null);
});

test("nextVersion keeps a breaking change on the minor while major is 0", () => {
    assert.equal(nextVersion("0.1.0", "breaking"), "0.2.0");
    assert.equal(nextVersion("0.1.0", "feature"), "0.2.0");
    assert.equal(nextVersion("0.1.3", "fix"), "0.1.4");
    assert.equal(nextVersion("0.4.2", "feature"), "0.5.0");
});

test("nextVersion uses ordinary semver from 1.0.0 on", () => {
    assert.equal(nextVersion("1.2.3", "breaking"), "2.0.0");
    assert.equal(nextVersion("1.2.3", "feature"), "1.3.0");
    assert.equal(nextVersion("1.2.3", "fix"), "1.2.4");
});

test("explicitBump bumps the part it is told to, with no 0.x remapping", () => {
    assert.equal(explicitBump("0.1.0", "major"), "1.0.0");
    assert.equal(explicitBump("0.1.0", "minor"), "0.2.0");
    assert.equal(explicitBump("0.1.0", "patch"), "0.1.1");
});

test("nextVersion rejects a version it cannot parse", () => {
    assert.throws(() => nextVersion("0.1", "fix"), /0\.1/);
    assert.throws(() => nextVersion("1.0.0-preview", "fix"), /preview/);
});

test("releaseChangelog renames the heading and changes nothing else", () => {
    const released = releaseChangelog(CHANGELOG, "0.2.0", "2026-09-02");
    assert.equal(released, CHANGELOG.replace("## [Unreleased]", "## [0.2.0] - 2026-09-02"));
    assert.ok(!released.includes("[Unreleased]"));
});

test("releaseChangelog preserves CRLF line endings", () => {
    const crlf = CHANGELOG.split("\n").join("\r\n");
    const released = releaseChangelog(crlf, "0.2.0", "2026-09-02");
    assert.ok(released.includes("## [0.2.0] - 2026-09-02\r\n"));
    assert.ok(!released.includes("\n\n")); // No bare LF pair survived.
});

test("replaceManifestVersion rewrites one line and leaves the rest byte-identical", () => {
    const manifest = '{\r\n  "name": "com.arman.alpha",\r\n  "version": "0.1.0",\r\n  "unity": "6000.0"\r\n}\r\n';
    const rewritten = replaceManifestVersion(manifest, "0.2.0");
    assert.equal(rewritten, manifest.replace('"version": "0.1.0"', '"version": "0.2.0"'));
});

test("replaceManifestVersion refuses an ambiguous manifest", () => {
    const manifest = '{\n  "version": "0.1.0",\n  "dependencies": {\n    "version": "1.0.0"\n  }\n}\n';
    assert.throws(() => replaceManifestVersion(manifest, "0.2.0"), /exactly one/);
});

test("replaceManifestVersion refuses a manifest with no version key", () => {
    assert.throws(() => replaceManifestVersion('{\n  "name": "x"\n}\n', "0.2.0"), /exactly one/);
});

test("replaceManifestDependency rewrites the range and nothing else", () => {
    const manifest = '{\r\n  "name": "com.arman.beta",\r\n  "version": "0.1.0",\r\n  "dependencies": {\r\n    "com.arman.alpha": "0.1.0"\r\n  }\r\n}\r\n';
    const rewritten = replaceManifestDependency(manifest, "com.arman.alpha", "0.2.0");
    assert.equal(rewritten, manifest.replace('"com.arman.alpha": "0.1.0"', '"com.arman.alpha": "0.2.0"'));
});

test("replaceManifestDependency cannot be fooled by the package's own name value", () => {
    const manifest = '{\n  "name": "com.arman.alpha",\n  "version": "0.1.0"\n}\n';
    assert.throws(() => replaceManifestDependency(manifest, "com.arman.alpha", "0.2.0"), /exactly one/);
});

test("internalDependencies keeps only this repo's packages", () => {
    const manifest = { dependencies: { "com.arman.alpha": "0.1.0", "com.unity.test-framework": "1.4.5" } };
    assert.deepEqual(internalDependencies(manifest), [["com.arman.alpha", "0.1.0"]]);
    assert.deepEqual(internalDependencies({}), []);
    assert.deepEqual(internalDependencies(null), []);
});

const BULLET = "- Updated `com.arman.alpha` to `0.2.0`.";

test("addChangedEntries creates both headings when the changelog has neither", () => {
    const text = "# Changelog\n\n## [0.1.0] - 2026-08-29\n\nInitial release.\n";
    assert.equal(
        addChangedEntries(text, [BULLET]),
        `# Changelog\n\n## [Unreleased]\n\n### Changed\n\n${BULLET}\n\n## [0.1.0] - 2026-08-29\n\nInitial release.\n`,
    );
});

test("addChangedEntries appends to an existing Changed section", () => {
    const text = "# Changelog\n\n## [Unreleased]\n\n### Changed\n\n- Flattened the folders.\n\n## [0.1.0] - 2026-08-29\n";
    assert.equal(
        addChangedEntries(text, [BULLET]),
        `# Changelog\n\n## [Unreleased]\n\n### Changed\n\n- Flattened the folders.\n${BULLET}\n\n## [0.1.0] - 2026-08-29\n`,
    );
});

test("addChangedEntries files a new Changed section in Keep a Changelog order", () => {
    const text = "# Changelog\n\n## [Unreleased]\n\n### Added\n\n- A thing.\n\n### Fixed\n\n- A leak.\n\n## [0.1.0] - 2026-08-29\n";
    const result = addChangedEntries(text, [BULLET]);
    assert.ok(result.indexOf("### Added") < result.indexOf("### Changed"), result);
    assert.ok(result.indexOf("### Changed") < result.indexOf("### Fixed"), result);
});

test("addChangedEntries preserves CRLF", () => {
    const text = "# Changelog\r\n\r\n## [0.1.0] - 2026-08-29\r\n";
    const result = addChangedEntries(text, [BULLET]);
    assert.ok(result.includes("### Changed\r\n"));
    assert.ok(!/[^\r]\n/.test(result));
});

test("addChangedEntries with no bullets is a no-op", () => {
    const text = "# Changelog\n\n## [0.1.0] - 2026-08-29\n";
    assert.equal(addChangedEntries(text, []), text);
});

// The shape discoverPackages produces, reduced to what the cascade reads.
function pkg(folder, name, version, dependencies) {
    return { folder, name, version, manifest: dependencies ? { dependencies } : {} };
}

test("cascadeDependents walks the graph transitively", () => {
    const packages = [
        pkg("Alpha", "com.arman.alpha", "0.1.0"),
        pkg("Beta", "com.arman.beta", "0.1.0", { "com.arman.alpha": "0.1.0" }),
        pkg("Gamma", "com.arman.gamma", "0.1.0", { "com.arman.beta": "0.1.0" }),
        pkg("Delta", "com.arman.delta", "0.1.0"),
    ];
    const direct = [{ folder: "Alpha", name: "com.arman.alpha", from: "0.1.0", to: "0.2.0", level: "feature", reason: "feature: Changed" }];
    const { plan, errors } = cascadeDependents(packages, direct);

    assert.deepEqual(errors, []);
    assert.deepEqual(plan.map((entry) => [entry.name, entry.to]), [
        ["com.arman.alpha", "0.2.0"],
        ["com.arman.beta", "0.1.1"],
        ["com.arman.gamma", "0.1.1"],
    ]);
    assert.deepEqual(plan[1].dependencyUpdates, [{ name: "com.arman.alpha", from: "0.1.0", to: "0.2.0" }]);
});

test("cascadeDependents survives a dependency cycle", () => {
    const packages = [
        pkg("Alpha", "com.arman.alpha", "0.1.0", { "com.arman.beta": "0.1.0" }),
        pkg("Beta", "com.arman.beta", "0.1.0", { "com.arman.alpha": "0.1.0" }),
    ];
    const direct = [{ folder: "Alpha", name: "com.arman.alpha", from: "0.1.0", to: "0.2.0", level: "feature", reason: "feature: Changed" }];
    const { plan } = cascadeDependents(packages, direct);
    assert.deepEqual(plan.map((entry) => [entry.name, entry.to]), [["com.arman.alpha", "0.2.0"], ["com.arman.beta", "0.1.1"]]);
    // Alpha's own range on Beta is updated too, so neither side ships stale.
    assert.deepEqual(plan[0].dependencyUpdates, [{ name: "com.arman.beta", from: "0.1.0", to: "0.1.1" }]);
});

test("cascadeDependents reports a dependent it cannot version", () => {
    const packages = [
        pkg("Alpha", "com.arman.alpha", "0.1.0"),
        pkg("Beta", "com.arman.beta", "0.1.0-preview", { "com.arman.alpha": "0.1.0" }),
    ];
    const direct = [{ folder: "Alpha", name: "com.arman.alpha", from: "0.1.0", to: "0.2.0", level: "feature", reason: "feature: Changed" }];
    const { plan, errors } = cascadeDependents(packages, direct);
    assert.equal(plan.length, 1);
    assert.match(errors[0], /com\.arman\.beta: version `0\.1\.0-preview` is not plain/);
});
