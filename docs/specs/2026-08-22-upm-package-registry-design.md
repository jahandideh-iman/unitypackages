# Hosting the Arman UPM packages on a real registry

**Date:** 2026-08-22
**Status:** **Superseded** by [`2026-08-23-upm-package-registry-github-design.md`](./2026-08-23-upm-package-registry-github-design.md) (GitHub + OpenUPM). Retained for its problem statement, packaging facts, and pre-publish cleanup list, which carry over unchanged.
**Repos affected:** `unitypackages` (now GitHub: `jahandideh-iman/unitypackages`; the GitLab remote `jahandideh-iman-indie/public/unitypackages` is a read-only archive). A separate, private game project consumes them.

> **Amended 2026-08-30 — package-id normalisation.** Every package id in this document has been
> rewritten to the ids that actually exist. On 2026-08-23 the three namespaces
> (`com.arman.foundation.*`, `com.arman.presentation.*`, plain `com.arman.*`) were collapsed into one
> flat namespace and every `snake_case` id converted to `kebab-case`: **`com.arman.<kebab-case-name>`,
> no exceptions.** So `com.arman.foundation.service_locating` is now `com.arman.service-locating`.
> The rename was safe because nothing had ever been published; from the first tag onward, ids are
> permanent. The scoped-registry entry `"scopes": ["com.arman"]` is unaffected — it still matches
> every package by dot-separated prefix. Stale dependency counts corrected in the same pass are
> marked inline. Nothing else about the design was changed.

## 1. Problem

`../unitypackages` is a Unity sandbox project holding **18 embedded packages** under `Packages/`
(17 of them publishable; `PackageTemplate` is a scaffold). There is no distribution mechanism. Consumers get the packages one of two bad ways:

1. **Dead absolute paths.** The consuming game project's `Packages/manifest.json` declares five dependencies as
   `file:D:/Projects/Games/PackagesProject/Packages/...`. That drive does not exist on the current
   machine. The entries are inert.
2. **Vendored copies.** Six package folders are physically duplicated into that project's `Packages/`
   and are git-tracked. `packages-lock.json` confirms `"source": "embedded"`. This is what actually
   makes the project compile today.

The result: no versioning, no upgrade path, no way to consume a package from a second project, and
source drift between the canonical package and each vendored copy.

`PackageExports/*.tgz` shows a manual `npm pack` step already happens by hand. This design automates
it and gives the tarballs somewhere to go.

## 2. Research: free registry options

Unity's Package Manager speaks the **npm protocol**. Any npm-compatible registry is a candidate,
subject to two Unity-specific constraints:

- **Unity does not support `@scope/name` notation.** Package names must be unscoped reverse-DNS.
- Unity's docs say a registry should implement `/-/v1/search` or `/-/all`. Verified what actually
  breaks when they are missing: **only the "My Registries" browse tab in the Package Manager
  window.** Resolution from `manifest.json` still works. This is a UX degradation, not a blocker —
  a distinction that changes the ranking below.

| Option | Free | Unity-compatible | Verdict |
|---|---|---|---|
| **npmjs.com (public)** | Yes, unlimited public packages | **Full** — `/-/v1/search` returns HTTP 200 | **Selected** |
| GitLab Pages static registry | Yes | Yes, if metadata is generated | Viable; needs custom generator |
| GitLab Package Registry | Yes; anonymous read works on public projects | **Degraded** | Fallback only |
| OpenUPM | Yes | Full | **Disqualified — GitHub-only** |
| Verdaccio self-hosted | Not in practice | Full | Free PaaS tiers with persistent disk have dried up |
| Cloudsmith Core | 500 MB / 1 GB delivery | Yes | Metered ceiling, no upside here |

### Why OpenUPM — the obvious answer — does not apply

OpenUPM's docs are explicit: *"The package must be open-source and hosted on GitHub."* This repo is
on GitLab. This single constraint is what makes the problem non-trivial; OpenUPM would otherwise be
the default recommendation.

### Why GitLab's own Package Registry is weak here

Probed live against project `15949052`:

- The project is `"visibility": "public"` and anonymous reads are permitted (no 401).
- **`/-/all` and `/-/v1/search` both return `302` redirecting to `registry.npmjs.org`.** Unity's
  browse tab would therefore search *npm's* catalogue rather than this project's packages.
- A packument request for a package not present locally also 302s to npmjs, which silently masks
  typo'd package names instead of returning a clean 404.
- GitLab issues [#382760](https://gitlab.com/gitlab-org/gitlab/-/issues/382760) and
  [#354813](https://gitlab.com/gitlab-org/gitlab/-/issues/354813) (UPM support) are both still open.

### Why npmjs.com wins

- Zero operational burden — no server, no persistent disk, no uptime concern.
- Full Unity compatibility, including the browse tab.
- Correct transitive dependency resolution. This mattered at the time of writing: **nine packages
  depended on `com.arman.service-locating`.** ⚠️ Those nine declarations were stale and were removed
  on 2026-08-23 (commit `c578a71`). The internal graph is now three edges —
  `in-game-message-logging → unity-utilities`, `persistent-data-management → package-basics`,
  `update-management → package-basics` — and **nothing depends on `service-locating`**.
- Unscoped reverse-DNS names are supported, which is exactly what Unity requires.
- **All 18 names were verified available** on 2026-08-22 (HTTP 404 on `registry.npmjs.org`).

### Considered and rejected: Git URL dependencies

`https://gitlab.com/....git?path=Packages/ServiceLocating#v1.2.0` works natively in Unity and needs
no infrastructure. Rejected because Unity does not resolve the *dependencies* of a git-sourced
package — every consumer would have to manually add each transitive edge by hand (nine
`service-locating` edges at the time of writing; three edges total today, see above).
Acceptable as a stopgap, unworkable as the long-term answer.

### Note on visibility

Publishing to npmjs makes these packages **public and effectively permanent** (npm restricts
unpublish after 72 hours). This was explicitly confirmed as acceptable. If the requirement ever
flips to private, npmjs is disqualified twice over: private npm packages **must** be scoped
`@scope/name` (which Unity cannot consume) **and** require a paid plan. The fallback in that case
is the GitLab Package Registry with `.upmconfig.toml` token auth, accepting the broken browse tab.

## 3. Design

### 3.1 Naming and versioning

Names stay exactly as they are today. Versions remain the source of truth in each
`Packages/<Dir>/package.json`.

~~**Pre-release suffixes are retained** (seven packages carry a `-preview` suffix; six of those are
publishable) per explicit decision.~~ **Reversed 2026-08-30 — all seven `-preview` suffixes were
dropped to plain `0.1.0`.** The consequence this paragraph warned about therefore no longer applies:
Unity hides pre-release versions from the Package Manager UI unless *Project Settings → Package
Manager → Enable Pre-release Packages* is enabled, but with no `-preview` suffix left anywhere,
The consuming game project does not need that toggle for `scene-management` or `ui-management`.

### 3.2 Release tooling — `Tools/upm-release.mjs`

A single dependency-free Node script, used identically by a developer locally and by CI.

Package discovery globs `Packages/*/package.json`, skipping any manifest with `"private": true`.
That glob naturally excludes `Packages/manifest.json` and `Packages/packages-lock.json`, which sit
at the `Packages/` root rather than in a subdirectory. Three directories contain spaces
(`Asset Providing`, `Scene Management`, `UI Management`) and must be path-quoted throughout.

| Subcommand | Behaviour |
|---|---|
| `validate` | Per package: parseable JSON; name matches npm rules; valid semver; `license` field present; description is not stock placeholder text; `npm pack --dry-run` succeeds; every `com.arman.*` dependency resolves either to a version already on npm or to a package being published in this same run. |
| `pack` | Writes tarballs to `PackageExports/`, preserving the existing artifact convention. |
| `publish` | For each package, compares the local `version` against `npm view <name> versions`. Publishes only genuinely-new versions. Emits `published.json` listing what it did. |
| `tag` | Reads `published.json` and creates a `<package-name>/<version>` tag and GitLab Release per entry. |

**Publish order is topologically sorted** over the internal dependency graph, so a single MR that
bumps both `package-basics` and `persistent-data-management` publishes the dependency first.

**Verified:** `npm pack` on `ServiceLocating` produces a correct tarball with all `.meta` files
intact (12 files, 1.5 kB). The repo `.gitignore` does not strip `.meta`, so npm's
gitignore-fallback behaviour is not a hazard here. No `.npmignore` is required.

**CI needs no Unity licence.** `npm pack` is the entire build step, so a `node:20-alpine` image is
the whole toolchain. This keeps the pipeline free and fast.

### 3.3 Pipeline

```
validate → pack → publish → release
```

- `validate` and `pack` run on **every merge request** and on `master`. MR pipelines expose the
  tarballs as downloadable artifacts, so a package can be tested in a real project before merge.
- `publish` and `release` run on **`master` only**.

```yaml
stages: [validate, pack, publish, release]

default:
  image: node:20-alpine

variables:
  UPM_REGISTRY: "https://registry.npmjs.org"

.on_mr_or_master: &on_mr_or_master
  rules:
    - if: $CI_PIPELINE_SOURCE == "merge_request_event"
    - if: $CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH

validate:
  stage: validate
  <<: *on_mr_or_master
  script:
    - node Tools/upm-release.mjs validate

pack:
  stage: pack
  <<: *on_mr_or_master
  script:
    - node Tools/upm-release.mjs pack --out PackageExports
  artifacts:
    paths: [PackageExports/]
    expire_in: 30 days

publish:
  stage: publish
  rules:
    - if: $CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH
  script:
    - echo "//registry.npmjs.org/:_authToken=${NPM_TOKEN}" > ~/.npmrc
    - node Tools/upm-release.mjs publish
  artifacts:
    paths: [published.json]

release:
  stage: release
  image: registry.gitlab.com/gitlab-org/release-cli:latest
  rules:
    - if: $CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH
  script:
    - node Tools/upm-release.mjs tag
```

The `release` job uses `release-cli` with the built-in `CI_JOB_TOKEN`, which can create both the tag
and the Release via API. This deliberately avoids needing a write-scoped push token in CI.

### 3.4 Authentication

An npm **Automation** token stored as a masked + protected GitLab CI/CD variable `NPM_TOKEN`.
Automation tokens bypass npm's mandatory 2FA-on-publish. A granular token would also work but needs
"All packages" scope, because packages that do not yet exist cannot be enumerated in its allowlist.

### 3.5 Why this shape — idempotency

Publishing is driven by *comparing against the registry*, not by tracked state. Three consequences:

- Re-running a `master` pipeline publishes nothing new and is always safe.
- A five-package release that fails after three leaves those three published; a re-run completes the
  remaining two. No partial-release cleanup, no rollback logic.
- No release manifest or changelog file to keep in sync and no merge conflicts on it.

**The developer workflow reduces to:** edit `version` in a `package.json` → open MR → merge.

## 4. Pre-publish cleanup

Ordered by dependency. Items 1–3 must land before the first publish; item 4 is the payoff.

1. **MIT `LICENSE` file + `"license": "MIT"` field** in all 17 publishable packages. Currently *no*
   package has either. Publishing unlicensed public packages is not acceptable.
2. **`PackageTemplate` → `"private": true`.** Its name is the placeholder
   `com.arman.package-template` and it must never publish. Add a `validate`-stage guard so
   stock *"Replace this string with your own description"* text (present in four packages) cannot
   ship.
3. **Rename `com.arman.foundation.persistent_data_managemement`** → `com.arman.persistent-data-management`.
   "managemement" is misspelled, and a published name is permanent. No other package declares a
   dependency on it, so this touches only its own `package.json` and the consuming game project's manifest.
   *(Done 2026-08-23, subsumed by the repo-wide id normalisation — see the amendment note above.)*
4. **Repoint the consuming game project** — see below.

## 5. Consumer migration (the consuming game project)

Add the scoped registry alongside the existing OpenUPM entry:

```json
"scopedRegistries": [
  { "name": "OpenUpm", "url": "https://package.openupm.com",
    "scopes": ["com.dbrizov.naughtyattributes"] },
  { "name": "Arman",   "url": "https://registry.npmjs.org",
    "scopes": ["com.arman"] }
]
```

The single scope `com.arman` covers all packages by dot-separated prefix match.

Then replace the five dead `file:D:/...` entries with version constraints and delete the six
vendored folders from that project's `Packages/`.

> **Risk — must be handled before deletion.** The six vendored folders are git-tracked and may
> contain local edits that were never pushed upstream to `unitypackages`. Each must be diffed
> against its canonical source *before* removal. Divergence is a decision point, not something to
> silently overwrite.

## 6. Out of scope

- **Unity tests in CI.** Requires a licensed GameCI runner — a real cost and a separate decision.
  The current design's ability to run licence-free is a deliberate property worth preserving.
- **The `Runtime/Scritps` folder typo.** Cosmetic, and renaming it churns `.meta` GUIDs.
- **Backfilling READMEs and CHANGELOGs.** Only six of eighteen packages have a README; five have a
  CHANGELOG. Worth doing, but it does not block distribution.

## 7. Open risks

- Publishing is irreversible after 72 hours. Cleanup items 1–3 are gates, not suggestions.
- npm's global namespace is first-come. All 18 names are free *as of 2026-08-22*; the longer this
  sits, the more that can change.
- The consuming game project and `unitypackages` are separate checkouts with separate `master` branches. Changes
  to `unitypackages` need explicit sign-off.
