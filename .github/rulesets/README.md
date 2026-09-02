# Rulesets

The branch protection applied to this repository, checked in so that the
configuration is reviewable and restorable rather than living only in the
GitHub UI.

Apply or update:

```bash
gh api repos/:owner/:repo/rulesets --input .github/rulesets/master.json      # create
gh api --method PUT repos/:owner/:repo/rulesets/<id> --input .github/rulesets/master.json   # update
gh api repos/:owner/:repo/rulesets                                            # list, to find <id>
```

`master.json` requires a pull request into `master`, requires the
`promotion-guard`, `validate`, and `pack` checks, and blocks force pushes and
branch deletion. **`bypass_actors` is empty, repository owner included** — that
is the point: merging into `master` publishes permanently, and a bypass is the
door the whole flow closes.
