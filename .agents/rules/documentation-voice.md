# Documentation voice

How every document, skill file and code comment in this repo is written.

## Writing documentation

**Documents describe the project as it is now, not how it got there.** This applies to every document an agent writes or edits — this guide, READMEs, package docs, specs, skill files, code comments. State the current rule, layout, name or behaviour; leave out what it used to be, when it changed, what was renamed or deleted, and the incident that prompted it. When something changes, rewrite the affected text to the new state instead of appending a note about the change.

History lives in git and in the places whose job it is to record it: package `CHANGELOG.md` files, commit messages, and pull request descriptions. A document that is explicitly about history is the exception: a changelog, a migration guide, a postmortem, and the dated design specs and implementation plans under `docs/specs/` and `docs/plans/`, which record a decision as it was made.

A reason is not history. "The sole implementation of an interface takes its name without the `I`" is the rule; "a qualifier should distinguish it from another implementation, and there is none" is a reason worth keeping; "the twelve `Basic`-prefixed types were renamed on 2026-09-05" is history and belongs in the commit.

## Keeping the routing honest

`AGENTS.md` is the only file every agent loads eagerly; everything else is read on demand, found through its routing tables. So a rule is only as reachable as its row.

- A new rule that is expensive to break gets a row in the non-negotiables table in [`AGENTS.md`](../../AGENTS.md); its detail goes in the rules file or skill the row points at.
- A new area gets a rules file here and a row in the "Where the rules live" table.
- Moving or renaming a heading means updating its row in [`MANIFEST.tsv`](./MANIFEST.tsv) (heading, a tab, the file that holds it) and every link to its anchor.
- A skill under `.agents/skills/` is copied byte for byte to `.claude/skills/` in the same commit, and one it authors in-repo gets an **Authored in-repo** row in `THIRD_PARTY_SKILLS.md`.
- A link from a skill to a rules file is written `../../../.agents/rules/<file>.md`, which resolves from both copies of the skill.
- A new spec or plan starts at `**Status:** Designed` or `**Status:** Planned` and adds its row to [`docs/INDEX.md`](../../docs/INDEX.md) in the same commit. The statuses are `Designed`, `Planned`, `Implemented`, `Superseded — <reason>` and `Abandoned — <reason>`; the landing commit sets `Implemented`.

`npm run check:docs` (`Tools/docs-check.mjs`) holds all of this to account: every relative link and heading anchor resolves, every manifest row is still a heading in its file, the two skill folders match, and `AGENTS.md` stays between 6,000 and 12,000 bytes. CI runs it in the `tooling-tests` job.
