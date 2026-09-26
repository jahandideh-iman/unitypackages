# Git workflow

Hosting, remotes, the `dev`/`master` split, rulesets and required checks.

## Git and hosting

**This repo lives on GitHub** (`github.com/jahandideh-iman/unitypackages`, public), because OpenUPM only accepts GitHub-hosted packages. Use `gh` for PRs and releases.

`origin` is GitHub. The remote named `gitlab` is a read-only archive. Use `gh` here, not `glab`.

Never prefix a git command with `cd` (e.g. `cd <dir> && git ...`); use `git -C <path> ...` instead.

### Branching

Two long-lived branches, split by purpose:

| Branch   | Role                                                                                                                    |
| -------- | ----------------------------------------------------------------------------------------------------------------------- |
| `dev`    | **Development.** The repo default on GitHub. Every feature, fix, and docs branch cuts from here and PRs back into here. |
| `master` | **Release only.** Moves solely via a release PR from `dev`. Every release tag is cut from this branch.                  |

So the day-to-day loop is: branch from `dev` → PR into `dev` → merge. `gh pr create` targets `dev` by default; you only pass `--base master` for a release PR.

Releasing is a promotion, not a separate build. Bump the `version` fields on `dev` and merge them normally, then open one release PR `dev` → `master`. Once it merges, tag from `master` — see [Distribution and releases](./releases.md#distribution-and-releases). Nothing is cherry-picked and `master` is never committed to directly, so `master` is always a commit that also exists on `dev`.

This split is enforced in two places, and both are deliberate belt-and-braces: `Tools/upm-release.mjs` refuses to tag off `master` (`RELEASE_BRANCH`, overridable with `--allow-branch`), and the `tag` job in `release.yml` is conditioned on `github.ref == 'refs/heads/master'`. The workflow condition is the one that matters, because the job runs on `push`: it is the only thing stopping a routine push to `dev` from publishing every package. Keep it.

The _source_ of a release PR is enforced separately, by `promotion-guard` in `release.yml` (`Tools/promotion-check.mjs`): a pull request into `master` from anything other than `dev` fails. A GitHub ruleset cannot express this — rulesets target a destination ref and say nothing about a pull request's source — so the ruleset's job is to make `promotion-guard` a **required** check. Run it by hand with `node Tools/promotion-check.mjs --event pull_request --base master --head my-branch`.

Both branches carry a ruleset, checked in under [`.github/rulesets/`](../../.github/rulesets/): `master.json` and `dev.json`. Each requires a pull request and blocks force pushes and branch deletion. **Neither has bypass actors, repository owner included.** Merging into `master` publishes permanently; a bypass is the door this flow exists to close.

| Required check      | `dev`  | `master` |
| ------------------- | :----: | :------: |
| `check` (changelog) |  yes   |   yes    |
| `promotion-guard`   |  yes   |   yes    |
| `validate`          |  yes   |   yes    |
| `pack`              |  yes   |   yes    |
| `tooling-tests`     |  yes   |   yes    |
| `format`            |  yes   |   yes    |
| `unity-tests`       | **no** |   yes    |

`unity-tests` is required on `master` but not on `dev`, and that asymmetry is load-bearing. A skipped required check blocks the merge, and `unity-tests` is deliberately skipped on fork pull requests — requiring it on `dev`, which is where fork pull requests land, would block every outside contributor permanently. A release pull request comes from this repo's `dev`, where the job always runs, so requiring it on `master` costs nothing. Net effect: a red Unity suite can reach `dev`, but can never publish. `report` and `tag` are required on neither, for the same skip reason.

`master` restricts the merge method to a true merge. Squashing a release pull request would create a commit on `master` that is not on `dev`, which is exactly the invariant `promotion-guard` exists to protect.

Each entry pins `integration_id: 15368` (GitHub Actions), so only a check run from Actions can satisfy it — a bare context name would be satisfiable by any app or token that can post a commit status with a matching name.

The GitHub web UI is not the source of truth here, and is a poor way to edit these: its required-checks picker suggests only check names it has recently observed, so a renamed job or a `pull_request`-only check like `check` may not appear at all. The field accepts free text, but prefer applying the JSON.
