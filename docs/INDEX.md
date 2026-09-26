# Documentation index

Every feature with a design spec in [`specs/`](./specs) or an implementation plan in [`plans/`](./plans), with its status. Read a feature's documents before changing what they cover. The rules for agents are elsewhere: [`AGENTS.md`](../AGENTS.md) and [`.agents/rules/`](../.agents/rules).

Each spec and plan carries a `**Status:**` line near its top, and the row here repeats it:

| Status                  | Meaning                                                         |
| ----------------------- | --------------------------------------------------------------- |
| `Designed`              | Spec written; no plan yet.                                      |
| `Planned`               | The implementation plan is written; the work has not landed.    |
| `Implemented`           | The work has landed on `dev`.                                   |
| `Superseded — <reason>` | Another design replaced this one; the reason names or links it. |
| `Abandoned — <reason>`  | The work was dropped; the reason says why.                      |

A new spec adds its row in the same commit, and the commit that lands the work sets `Implemented` in the row, the spec and the plan. A plan's checkboxes are working state while it runs; they are not ticked after the fact.

| Feature                                      | Status                                               | Spec                                                             | Plan                                                    | Main files                                                                                                                                            |
| -------------------------------------------- | ---------------------------------------------------- | ---------------------------------------------------------------- | ------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| Package registry: GitLab + npmjs.com variant | Superseded — replaced by the GitHub + OpenUPM design | [spec](./specs/2026-08-22-upm-package-registry-design.md)        | —                                                       | —                                                                                                                                                     |
| Package registry: GitHub + OpenUPM           | Implemented                                          | [spec](./specs/2026-08-23-upm-package-registry-github-design.md) | —                                                       | `Tools/upm-release.mjs`, `.github/workflows/release.yml`, `Packages/*/package.json`                                                                   |
| Pull-request test CI                         | Implemented                                          | [spec](./specs/2026-08-30-pr-test-ci-design.md)                  | [plan](./plans/2026-08-30-pr-test-ci.md)                | `.github/workflows/tests.yml`, `Tools/ci/`, `.github/actionlint.yaml`                                                                                 |
| Release promotion                            | Implemented                                          | [spec](./specs/2026-09-02-release-promotion-design.md)           | [plan](./plans/2026-09-02-release-promotion.md)         | `Tools/upm-release.mjs`, `Tools/release-flow.mjs`, `Tools/release.bat`, `Tools/promotion-check.mjs`, `Tools/changelog-check.mjs`, `.github/rulesets/` |
| Dropping the `Basic` prefix, and Moq         | Implemented                                          | [spec](./specs/2026-09-05-basic-rename-and-moq-design.md)        | [plan](./plans/2026-09-05-drop-basic-prefix-and-moq.md) | `Packages/*/Runtime/`, the test asmdefs, `Packages/manifest.json`                                                                                     |
| Auto-formatting                              | Implemented                                          | [spec](./specs/2026-09-13-auto-formatting-design.md)             | [plan](./plans/2026-09-13-auto-formatting.md)           | `.editorconfig`, `.csharpierrc.json`, `.prettierrc.json`, `package.json`, `.github/workflows/format.yml`, `.git-blame-ignore-revs`                    |
| Documentation restructure                    | Implemented                                          | [spec](./specs/2026-09-26-documentation-restructure-design.md)   | [plan](./plans/2026-09-26-documentation-restructure.md) | `AGENTS.md`, `.agents/rules/`, `.agents/skills/`, `docs/INDEX.md`, `Tools/docs-check.mjs`                                                             |
