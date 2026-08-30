---
name: sharplens-mcp
description: "Use when an agent needs C# semantic code navigation, inspection, or refactoring in this package repo: finding definitions/references/implementations, mapping types and call graphs, reading diagnostics, checking a package's public API surface before a version bump, or applying Roslyn refactorings via the sharplens MCP server — instead of grep-style guessing. Also covers loading the solution (there is no auto-load here) and when to fall back to lifeblood for Unity-aware questions."
---

# SharpLens MCP

`sharplens` (`mcp__sharplens__*`) is this repo's default Roslyn server: 92 tools over the
real compiler model. Prefer it over `Grep`/`Glob`/`LS` for any question about C# *meaning*
rather than C# *text*.

It is a pure .NET server with **no Unity knowledge**. See "Hand off to lifeblood" below.

## Session Start — you must load the solution yourself

**This repo has no `.mcp.json`.** `sharplens` is registered at user level with no
`DOTNET_SOLUTION_PATH`, so nothing is auto-loaded for you.

1. `health_check` — see whether a solution is loaded, and *which* one. On a machine with
   several Unity projects open, a shared server may be pointed somewhere else entirely.
2. `load_solution` at `unitypackages.slnx` in the repo root (29 projects — every asmdef's
   generated `.csproj`). There is no `.sln`; `.slnx` is the only solution file.
3. `sync_documents` after edits made outside the server's view (a `Write`/`Edit`, a Unity
   reimport, a `git checkout`).

### ⚠️ Worktrees have no solution

`.gitignore` excludes `*.csproj` and `*.slnx`, so a freshly created worktree under
`.claude/worktrees/` contains **no project files and no solution**. `load_solution` there
will fail or load nothing. Either:

* regenerate them in the worktree — `unity command menu --path "Assets/Open C# Project"`
  (see [`unity-cli`](../unity-cli/SKILL.md)), or
* `load_solution` against the **main checkout's** `unitypackages.slnx` and accept that
  results describe `master`'s code, not your worktree edits. Say which you did.

The same applies to any clean clone. Do not report "symbol not found" until you have
confirmed a solution is actually loaded.

### New files are invisible until Unity regenerates

The `.csproj` files are Unity-generated. A `.cs` file you just created is not in the
compilation until Unity imports it and rewrites the descriptors. If a symbol is missing,
regenerate project files and `load_solution` again before concluding it doesn't exist.

## Tool Routing

**Find a symbol**
- Name known → `search_symbols` (glob), then `get_symbol_info`.
- File position in hand → `get_containing_member`, or `get_code_actions_at_position`
  for what Roslyn offers there.
- Definition → `go_to_definition`. Overloads → `get_method_overloads`.

**Understand a type before changing it**
- `get_type_overview` for the shape, `get_type_members` for the full surface,
  `get_base_types` / `get_derived_types` for the hierarchy, `get_attributes` for metadata.
- `get_file_overview` to learn a file's shape without reading all of it.
- `get_method_source` to read one method instead of the whole file.

**Trace impact before editing**
- `find_references` (all usages), `find_callers` (inbound), `get_outgoing_calls` (outbound).
- `get_call_graph` and `find_path_between` for multi-hop chains.
- `analyze_change_impact` for a first-pass blast radius — **but see the caveats below.**
- `find_implementations` / `get_derived_types` before changing an interface or virtual member.

**Validate an edit**
- `get_diagnostics` on the file — never guess at a compile error.
- `get_code_fixes` → `apply_code_fix`, or `fix_all` for a whole category.
- `validate_code` / `check_type_compatibility` for a proposed snippet.

**Refactor**
- `rename_symbol`, `extract_method`, `extract_variable`, `extract_interface`,
  `change_signature`, `encapsulate_field`, `inline_variable`, `move_type_to_file`,
  `split_type`, `implement_missing_members`, `add_missing_imports`, `organize_usings`.
- These write to disk directly. Read the source first; verify with `get_diagnostics` after.
- ⚠️ Renaming a public member of a published package is an **API break** for consumers
  outside this repo, which `find_references` cannot see. See the next section.

**Audit / health**
- `get_project_health`, `find_god_objects`, `get_complexity_metrics`, `find_naming_violations`.
- `get_exception_flow`, `find_throw_sites`, `find_catch_blocks`, `find_async_issues`.
- `analyze_data_flow` / `analyze_control_flow` for a specific method.
- `find_circular_dependencies`, `dependency_graph`, `find_unused_dependencies`.
- `get_di_registrations` — directly relevant to the `ServiceLocating` package and its consumers.
- `resolve_stack_trace` to turn a Unity console stack trace into symbols.

## What this repo uses it for that a game repo doesn't

This is a **host for 18 publishable UPM packages**, not a game — 148 `.cs` files across
34 asmdefs, and a throwaway `Assets/` sandbox. That shifts which tools matter:

| Repo-specific need | Tool |
|---|---|
| **Did this change break a package's public API?** Published `<name>/<version>` tags are permanent, so a removed or re-signatured public member is a consumer-visible break that must drive the semver bump in `package.json`. | `diff_api_surface` — run it against the package's runtime project before bumping `version`. |
| Is a type actually public surface, or incidentally public? | `get_type_members` + `get_symbol_info` for accessibility. |
| Does package A really use package B's code? (must match the `dependencies` block in `package.json` **and** the asmdef `references`) | `dependency_graph`, `find_unused_dependencies` — then confirm the asmdef with `lifeblood_asmdef_check`. |
| Are there dependency cycles between packages? | `find_circular_dependencies` |
| Does the Service Locator wiring resolve? | `get_di_registrations` |

`diff_api_surface` is the one to reach for whenever you touch a `Runtime/` folder — it is
the cheapest check against shipping a breaking change under a patch bump.

## Hand off to lifeblood

SharpLens knows nothing about Unity. Route these to `lifeblood` instead — see
[`lifeblood-mcp`](../lifeblood-mcp/SKILL.md):

| Question | Tool |
|----------|------|
| Does this asmdef declare its real dependencies? | `lifeblood_asmdef_check` |
| Does this hold in player builds, not just `#if UNITY_EDITOR`? | `lifeblood_analyze` + `defineProfiles` |
| Is this MonoBehaviour / UnityEvent-wired code really dead? | `lifeblood_dead_code` |
| Blast radius / which tests to run? | `lifeblood_blast_radius`, `lifeblood_file_impact`, `lifeblood_test_impact` |

## Limits And Honesty

- **Unity-blind dead code.** `find_unused_code`, `find_dead_branches`, `find_untested_code`,
  and `remove_unused_code` see only C# call sites. They will flag MonoBehaviour message
  methods (`Awake`, `Start`, `OnEnable`, `Update`), `[SerializeField]` fields, and
  UnityEvent-wired handlers as unused, because nothing in source calls them.
  **Never delete Unity-facing code on a SharpLens result alone** — confirm with
  `lifeblood_dead_code`, which resolves reflection entry points and scene/prefab YAML.
- **Package-blind, too — this matters more here than in a game repo.** These packages are
  *libraries*. Their public API exists to be called by consumers **outside this repo**, so a
  public member with no in-repo caller is the normal case, not a finding. Treat unused-code
  and change-impact results on any `Runtime/` public surface as lower bounds: the real
  blast radius extends to every consumer of a published version, which no tool here can see.
- `check_architecture` is generic .NET layering validation. This repo has no
  `docs/invariants/*.md` for it or for `lifeblood_invariant_check` to read — `docs/` holds
  registry-hosting and release-flow specs only. Package-boundary rules live in
  [`AGENTS.md`](../../AGENTS.md)'s catalogue and dependency graph, and are enforced by
  `Tools/upm-release.mjs validate`, not by either MCP server.
- **Three package directories contain spaces** (`Asset Providing`, `Scene Management`,
  `UI Management`). Quote every path you hand to a tool.
- Results are only as fresh as the loaded workspace. After external edits or a Unity project
  regeneration, call `sync_documents` (or `load_solution`) before trusting output.
- These tools narrow the search and validate assumptions; they do not replace reading the
  source or running tests.
