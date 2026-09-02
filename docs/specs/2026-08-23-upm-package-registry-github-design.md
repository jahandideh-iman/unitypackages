# Hosting the Arman UPM packages — GitHub variant

**Date:** 2026-08-23
**Status:** **Accepted — current direction.** Rollout in progress; see §6.
**Assumption (now satisfied):** the `unitypackages` repo can move from GitLab to GitHub. The move
happened on 2026-08-23 — `origin` is `github.com/jahandideh-iman/unitypackages`, GitLab is kept as a
read-only archive. It remains a single multi-package repository.
**Supersedes:** [`2026-08-22-upm-package-registry-design.md`](./2026-08-22-upm-package-registry-design.md)

> **Amended 2026-08-30 — package-id normalisation.** Every package id below has been rewritten to
> the ids that actually exist. On 2026-08-23 the three namespaces (`com.arman.foundation.*`,
> `com.arman.presentation.*`, plain `com.arman.*`) were collapsed into one flat namespace and every
> `snake_case` id converted to `kebab-case`: **`com.arman.<kebab-case-name>`, no exceptions.**
>
> This matters most for **`gitTagPrefix`**, which OpenUPM matches as a *literal prefix, not a regex*:
> the prefix must be the current id verbatim, e.g. `com.arman.service-locating/`. A prefix carrying
> an old dotted or underscored id would silently match no tags and the package would never build.
> The `"scopes": ["com.arman"]` registry entry is unaffected — it still matches every package by
> dot-separated prefix. Nothing else about the design was changed.

## 1. What moving to GitHub changes

Exactly one thing, and it is the thing that dominated the first design:

> OpenUPM's requirement is *"The package must be open-source and hosted on GitHub."*

That single constraint disqualified the purpose-built, free, Unity-native registry and forced the
first proposal onto npmjs.com. Moving to GitHub removes it. **OpenUPM becomes available, and it is
a better fit than npmjs for this repo.**

Everything else from the first design — the problem statement, the packaging facts, the cleanup
list — carries over unchanged and is not repeated here.

## 2. Registry decision

| | **OpenUPM** | npmjs.com | GitHub Packages |
|---|---|---|---|
| Cost | Free | Free (public) | Free |
| Unity browse tab | Works | Works | **Broken** — no `/-/all` |
| Unscoped `com.arman.*` names | Yes | Yes | **No** — requires `@scope` |
| Upload step | **None — builds from git tags** | `npm publish` + token | `npm publish` + token |
| Secrets in CI | **None** | `NPM_TOKEN` | `GITHUB_TOKEN` |
| Discoverability | Package listing, search, badges, install CLI | Buried among ~3M npm packages | None |
| Latency | 15–30 min build | Instant | Instant |
| Gatekeeping | One-time moderator approval (~24 h) | None | None |

**Selected: OpenUPM.** GitHub Packages is disqualified on the same two technical grounds as before
(no `/-/all`, and it mandates `@scope/name`, which Unity cannot consume).

### Why OpenUPM over npmjs, now that both are available

1. **There is no publish step.** OpenUPM's build pipeline watches the repo's git tags and builds
   versions itself. You never upload anything. This deletes the entire `publish` stage, the
   `NPM_TOKEN` secret, token rotation, and the 2FA-automation-token subtlety from the first design.
2. **It is Unity-native.** A real package listing page, search, install instructions, a version
   history, and a `openupm add com.arman.service-locating` CLI path — none of which npm
   gives a Unity audience.
3. **The consuming game project already trusts it.** Its `Packages/manifest.json` *already* declares an
   OpenUPM scoped registry for `com.dbrizov.naughtyattributes`. Consuming these packages becomes a
   one-line change to an existing entry rather than a new registry.

### The real costs, stated plainly

- **You give up control of the registry.** Builds happen on OpenUPM's infrastructure on their
  schedule. If their pipeline is down or slow, your release is too.
- **One-time curation.** A first-time contributor's submission needs moderator approval, typically
  within 24 hours.
- **Public and open-source only**, permanently. Same practical constraint as the npmjs route.
- **17 separate submissions.** One metadata YAML per package, PR'd to the `openupm/openupm` repo.
  They can go in a single PR, but it is 17 files.

## 3. Monorepo mechanics

This is the part that differs most from a single-package repo, and OpenUPM supports it directly.

**Per-package metadata.** Each package gets one YAML file in `openupm/openupm:data/packages/`:

```yaml
name: com.arman.service-locating
displayName: Service Locating
repoUrl: https://github.com/<user>/unitypackages
licenseSpdxId: MIT
topics: [utility]
gitTagPrefix: "com.arman.service-locating/"
```

**`gitTagPrefix` is the key field.** It is matched as a **literal prefix, not a regex**. Setting it
to `com.arman.service-locating/` means the pipeline only considers tags like
`com.arman.service-locating/0.1.0` for that package, and ignores every other package's
tags in the same repo. This gives each package an independent version line — the alternative
strategy (lockstep-versioning all 17 together) is strictly worse here, since these packages change
at very different rates.

**Subfolder packages are supported** — the submission flow lets you point at a `package.json` that
is not at the repo root, which is exactly the `Packages/<Dir>/package.json` layout.

### The 17 ids to submit (post-normalisation, verified 2026-08-30)

`gitTagPrefix` is the id followed by a single `/`. Three folders contain spaces and must be
path-quoted. `PackageTemplate` (`com.arman.package-template`) is `"private": true` and is **not**
submitted.

| Folder | `name` / `gitTagPrefix` base | Version |
|--|--|--|
| `Asset Providing` | `com.arman.asset-providing` | 0.1.0 |
| `ComponentSystem` | `com.arman.component-system` | 0.1.0 |
| `ConfigurationManagement` | `com.arman.configuration-management` | 0.1.0 |
| `DevelopmentConsole` | `com.arman.development-console` | 0.1.0 |
| `EventManagement` | `com.arman.event-management` | 0.1.0 |
| `HttpConnection` | `com.arman.http-connection` | 0.1.0 |
| `InGameMessageLogging` | `com.arman.in-game-message-logging` | 0.1.0 |
| `InventorySystem` | `com.arman.inventory-system` | 0.1.0 |
| `ObjectPooling` | `com.arman.object-pooling` | 0.1.0 |
| `PackageBasics` | `com.arman.package-basics` | 0.1.0 |
| `PersistentDataManagement` | `com.arman.persistent-data-management` | 0.1.0 |
| `Scene Management` | `com.arman.scene-management` | 0.1.0 |
| `ServiceLocating` | `com.arman.service-locating` | 0.1.0 |
| `ShopManagement` | `com.arman.shop-management` | 0.1.0 |
| `UI Management` | `com.arman.ui-management` | 0.1.0 |
| `UnityUtilities` | `com.arman.unity-utilities` | 0.1.0 |
| `UpdateManagement` | `com.arman.update-management` | 0.1.0 |

This table duplicates [`.agents/AGENTS.md`](../../.agents/AGENTS.md) § *Package catalogue*, which is
the source of truth if the two ever disagree.

> **Amended 2026-08-31 — `configuration-management` reads `0.1.0`.** It was the one package listed at
> `1.0.0`; the release-readiness pass normalised it to `0.1.0` on 2026-08-30 so all 17 share one
> first version, and it published at `0.1.0`. The table had been left at the old value, disagreeing
> with the catalogue in `AGENTS.md` — corrected here, and in §5 below, which drew the same
> distinction.

Two other fields worth knowing:
- `gitTagIgnore` — excludes tags from the build pipeline.
- `minVersion` — makes the pipeline ignore versions before a threshold. This was proposed as the
  clean way to handle the `persistent-data-management` rename: tag the corrected name and set
  `minVersion` so the misspelled history is never built. **No longer needed** — the id normalisation
  of 2026-08-23 landed while `git tag` was still empty, so no old id was ever tagged and there is no
  misspelled history for the pipeline to skip. Submit every package without `minVersion`.

## 4. The GitHub Actions flow

Because there is no upload, the pipeline collapses to **validate → pack → tag**.

```yaml
name: release
on:
  pull_request:
  push:
    branches: [master]   # this repo's default branch is master, not main

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: 20 }
      - run: node Tools/upm-release.mjs validate

  pack:
    needs: validate
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: 20 }
      - run: node Tools/upm-release.mjs pack --out PackageExports
      - uses: actions/upload-artifact@v4
        with: { name: upm-tarballs, path: PackageExports/ }

  tag:
    needs: [validate, pack]
    if: github.event_name == 'push'
    runs-on: ubuntu-latest
    permissions:
      contents: write        # the only permission needed; no registry secret
    steps:
      - uses: actions/checkout@v4
        with: { fetch-depth: 0 }   # tags must be visible to diff against
      - uses: actions/setup-node@v4
        with: { node-version: 20 }
      - run: node Tools/upm-release.mjs tag --push
```

> **Amended 2026-08-30 — the `tag` job ships gated.** As written above, the first push to `master`
> after this workflow lands would create all 17 tags at once, which pre-empts the single-package
> smoke test that step 4 of §6 exists to run. The implemented `.github/workflows/release.yml`
> therefore replaces `if: github.event_name == 'push'` with `if: github.event_name ==
> 'workflow_dispatch'`, plus a boolean `publish` input defaulting to `false` — a manual run does
> `tag --dry-run` unless `publish` is ticked, in which case it does `tag --push`. `validate` and
> `pack` are unchanged and still run on every PR and push.
>
> **This gate is temporary.** Once step 4 has proven the OpenUPM path end to end, delete the
> `workflow_dispatch` block and restore `if: github.event_name == 'push'`. The steady-state design
> is the one in the YAML above: bump `version`, open a PR, merge, and the tag happens by itself.

> **Amended 2026-08-31 — the gate is gone.** Step 4 proved the path and all 17 packages published, so
> the `workflow_dispatch` block and its `publish`/`only` inputs were deleted and the `tag` job is back
> to `if: github.event_name == 'push' && github.ref == 'refs/heads/master'`. The YAML above is now
> the implementation, not an aspiration: **merging a release PR into `master` publishes.** The ref
> condition is load-bearing — without it the same job would publish on every push to `dev`.

`Tools/upm-release.mjs` is the same dependency-free Node script from the first design, with
`validate` and `pack` unchanged. Only the release subcommand differs:

| | GitLab design | **GitHub design** |
|---|---|---|
| Compares version against | the npm registry (network) | **existing git tags (local)** |
| Then | `npm publish`, then tag | **creates the tag; that is the whole release** |
| Needs network/secrets | Yes | **No** |

So `tag` reads each `Packages/*/package.json`, and for any package whose
`<name>/<version>` tag does not yet exist, creates and pushes it. Pushing the tag *is* publishing —
OpenUPM picks it up within 15–30 minutes.

This is fully offline, idempotent, and needs no topological sort: tags are independent, so ordering
across packages is irrelevant. (The first design needed sorting only because `npm publish` made
dependencies visible in a particular order.)

**Developer workflow is unchanged and still one action:** bump `version` in a `package.json`, open
a PR, merge.

## 5. ⚠️ Package visibility — verify before committing to this

Unity's docs state that **experimental packages "either use `0` as the major part of their version
or the `-exp.#` suffix"**, and that experimental and pre-release packages do not appear in the
Package Manager's install list by default. `Enable Pre-release Packages` reveals pre-release ones.

That is a problem for this repo on its face: **all 17 packages are `0.x`.** (When this was written
`configuration-management` was `1.0.0` and the other 16 were `0.x`; it was normalised to `0.1.0` on
2026-08-30 — see the amendment in §3.)

> **Amended 2026-08-30 — `-preview` dropped.** Seven packages (six publishable, plus
> `PackageTemplate`) carried a `-preview` suffix. All seven were bumped to plain `0.1.0`, so **no
> package carries a pre-release suffix any more** and `Enable Pre-release Packages` is no longer
> relevant. This removes the `-preview` half of the risk below; the `0.x` half is untouched and
> still has to be settled by the smoke test.

> **Amended 2026-08-31 — the registry half is settled.** `com.arman.service-locating@0.1.0` built and
> listed on OpenUPM, and on 2026-08-31 the other 16 followed; all 17 resolve at `0.1.0` from
> `package.openupm.com`, dependencies included. So a `0.x` package from a scoped registry is *served*
> without complaint. What step 4 asked for and this does **not** answer is the second half — whether
> a `0.x` package appears unprompted in the Package Manager **install list**, which still wants
> checking in a scratch project. If it turns out not to, option (a) below (add by name) applies;
> nothing published so far forecloses option (b).

**However** — the same Unity documentation adds that these lifecycle states *"only apply to packages
that Unity develops internally,"* and in practice third-party packages from a scoped registry do
generally list. The two statements are in tension, and I could not resolve it from documentation
alone.

**Therefore this design does not assert an outcome.** The first concrete step below is a
single-package smoke test whose entire purpose is to answer this empirically before 17 submissions
are made. If `0.x` packages turn out to be hidden, the options are (a) add packages by name rather
than by browsing, or (b) revisit the decision to stay below `1.0.0`. *(A third option — enabling
*Project Settings → Package Manager → Enable Pre-release Packages* in consuming projects — no longer
applies now that no package carries a `-preview` suffix.)*

## 6. Migration and rollout

Ordered so the risky, irreversible steps come after the cheap verification.

1. ✅ **Mirror the repo.** `git push --mirror` to a new public GitHub repo preserves full history and
   all refs. Keep GitLab as a read-only archive initially — nothing is lost if this is reversed.
   *(Done 2026-08-23.)*
2. ✅ **Land the cleanup** from the first design, unchanged: MIT `LICENSE` + `license` field on all 17
   publishable packages, `"private": true` on `PackageTemplate`, and the
   `com.arman.foundation.persistent_data_managemement` → `com.arman.persistent-data-management`
   rename. *(Done 2026-08-23, as part of normalising all 18 ids to `com.arman.<kebab-case-name>`.
   `minVersion` turned out to be unnecessary — see §3.)*
3. ✅ **Add `Tools/upm-release.mjs` and the workflow.** Merge a no-op version bump and confirm the
   `tag` job creates exactly the tags expected.
   *(Script and `.github/workflows/release.yml` written 2026-08-30 and exercised locally: `validate`
   passes 17/17 and fails correctly on seeded defects, `pack` produces 17 tarballs with `.meta` files
   intact, `tag --dry-run` plans the expected tags and the dirty-tree guard fires. `validate` and
   `pack` have since run green in CI on every PR; the `tag` job's own `push` path first runs with the
   gate removal in step 5.)*
4. ✅ **Smoke test — one package.** Tag `com.arman.service-locating` only, then submit that one
   package to OpenUPM. Confirm: the build succeeds, the version appears on the listing page, and —
   per §5 — that it is actually visible in the Unity Package Manager window of a scratch project.
   *(Done 2026-08-30: tagged by hand, submitted, and live at `0.1.0` on `package.openupm.com` by
   12:11 UTC. The Package Manager **install-list** half of the check is still owed — see the
   amendment in §5.)*
5. ✅ **Submit the remaining 16** once step 4 is proven, and **drop the `workflow_dispatch` gate** so
   merges tag automatically from then on.
   *(Done 2026-08-31: 16 tags pushed from `master`, submitted as one PR to `openupm/openupm`, and all
   17 packages now resolve at `0.1.0` with their `com.arman.*` dependencies intact. The gate is
   removed — see the amendment in §4.)*
6. **Repoint the consuming game project.** Add `"com.arman"` to the *existing* OpenUPM scoped registry entry:

   ```json
   { "name": "OpenUpm", "url": "https://package.openupm.com",
     "scopes": ["com.dbrizov.naughtyattributes", "com.arman"] }
   ```

   Then replace the five dead `file:D:/...` entries with version constraints and delete the six
   vendored folders — **after diffing each against its canonical source**, per the first design's
   standing warning. Those copies are git-tracked and may hold unpushed local edits.

## 7. Comparison to the GitLab proposal

| | GitLab + npmjs | **GitHub + OpenUPM** |
|---|---|---|
| Pipeline stages | 4 (`validate/pack/publish/release`) | 3 (`validate/pack/tag`) |
| Registry secrets | `NPM_TOKEN`, masked + protected | **None** |
| Release is | `npm publish` | **a git tag** |
| Topological sort | Required | Not needed |
| Unity discoverability | npm search only | OpenUPM listing + badges + CLI |
| Time to publish | Seconds | 15–30 min |
| External dependency | npm registry | OpenUPM build pipeline **and** GitHub |
| One-time cost | None | Repo migration + 17 YAML submissions + ~24 h approval |

**Recommendation: if the move to GitHub is genuinely on the table, take this design.** It is
strictly simpler to operate — no secrets, no publish step, no ordering constraints — and it puts the
packages somewhere Unity developers actually look. The one-time migration cost is real but bounded;
the operational saving is permanent.

## 8. Risks

- **Two external dependencies instead of one.** A release now needs both GitHub and OpenUPM's
  pipeline healthy. Mitigation: the tarballs are still built and retained as CI artifacts, so a
  manual npmjs publish remains available as a fallback path without redesigning anything.
- **The visibility question in §5 is unresolved.** Step 4 exists specifically to settle it, and no
  bulk submission should happen before it does.
- **Migration is one-way in practice.** Once OpenUPM points at a GitHub `repoUrl` and users install
  from it, moving back to GitLab means abandoning the listing. Keep the GitLab archive.
- **`Samples`/`Documentation` ship as imported assets** — ten folders across eight packages lack the
  `~` suffix, so consumers import the docs images and sample scenes unconditionally, and no package
  declares a `samples` array. Deliberately deferred, not forgotten: tracked in
  [issue #8](https://github.com/jahandideh-iman/unitypackages/issues/8), which records the two fixes
  (`.npmignore` exclusion vs. `trackingMode: githubRelease`) and why the repo keeps the folders
  tilde-free. Does not block step 4 — `com.arman.service-locating` has neither folder — but should be
  settled before the bulk submission in step 5.
- **Two thin packages.** `ServiceLocating` and `Scene Management` have two C# files each. All 17
  publishable packages contain real code, so OpenUPM's "functional and useful" bar should be met,
  but the smallest ones are the most likely to draw reviewer questions.
