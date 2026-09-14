# Rulesets

The branch protection applied to this repository, checked in so that the
configuration is reviewable and restorable rather than living only in the
GitHub UI.

| File          | Branch                        | Live ruleset |
| ------------- | ----------------------------- | ------------ |
| `master.json` | `refs/heads/master`           | `Master`     |
| `dev.json`    | `~DEFAULT_BRANCH`, i.e. `dev` | `default`    |

Both are **updates to rulesets that already exist**. Use `PUT` against the id —
`POST`ing the file creates a second, overlapping ruleset on the same branch:

```bash
gh api repos/:owner/:repo/rulesets                                    # list, to find the ids
gh api --method PUT repos/:owner/:repo/rulesets/<id> --input .github/rulesets/master.json
gh api --method PUT repos/:owner/:repo/rulesets/<id> --input .github/rulesets/dev.json
```

Read back what GitHub actually stored, which is the only way to catch drift:

```bash
gh api repos/:owner/:repo/rulesets/<id> -q '.rules[] | select(.type=="required_status_checks") | .parameters.required_status_checks[].context'
```

Edits made in the web UI do not come back here on their own, and nothing
records when the live ruleset stops matching these files. Read the live ruleset
back rather than trusting them.

## Which checks are required

See [the ruleset table in `AGENTS.md`](../../.agents/AGENTS.md#branching) for
the full list and the reasoning. The one rule to keep in mind when adding a
check: **a skipped required check blocks the merge.** A job that any legitimate
pull request can skip — `unity-tests` on a fork, `tag` on a pull request,
`report` behind either — must never be required, or that pull request can never
merge. This is also why branch logic lives inside `Tools/*.mjs` rather than in a
workflow `if:`.

Each entry pins `integration_id: 15368` (GitHub Actions). Without it, any app or
token able to post a commit status with a matching name would satisfy the check.

**`bypass_actors` is empty in both, repository owner included** — that is the
point: merging into `master` publishes permanently, and a bypass is the door the
whole flow closes.
