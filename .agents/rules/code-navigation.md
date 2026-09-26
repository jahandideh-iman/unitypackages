# Code navigation, MCP tools and the Unity CLI

Which tool answers which question: `sharplens`, `lifeblood`, the vendored skills, and `unity command`.

## MCP Tool Usage & Unity CLI

Two MCP servers are available, with a deliberate division of labour:

- **`sharplens`** (`mcp__sharplens__*`) — **the default for C# code navigation, inspection, and refactoring.** A pure .NET/Roslyn server exposing 92 tools. Use it for "where is this defined / what calls this / rename this / does this compile".
- **`lifeblood`** (`mcp__lifeblood__*`) — for the **Unity-aware and change-impact** questions SharpLens cannot answer. See "What belongs to `lifeblood`" below.

For the Editor — console, scenes, GameObjects, assets, eval, tests, builds — use the official `unity` CLI directly (see below); it isn't registered as an MCP server. Prefer all of these over generic file tools and over guessing.

Both servers are registered per user, not in this repo: in Claude Code with `claude mcp add`, in OpenCode under `mcp` in `~/.config/opencode/opencode.json` (`"type": "local"`, `"command": ["sharplens"]` / `["lifeblood-mcp", "--shared"]`). Tool names below use Claude Code's `mcp__<server>__<tool>` form; OpenCode names the same tool `<server>_<tool>` (`sharplens_find_references`).

### Skills

Vendored under `.agents/skills/` (canonical) and mirrored to `.claude/skills/` (what Claude Code discovers). **Edit one, copy to the other** — they must stay identical.

OpenCode reads both folders, so it finds each skill twice and logs a "duplicate skill name" warning; the copies are identical, so this is harmless, and `OPENCODE_DISABLE_CLAUDE_CODE_SKILLS=1` silences it. The folder name is lowercase `skills`, and each skill's `name:` is lowercase-hyphenated and equal to its folder name — OpenCode rejects any other name, and on Linux and macOS it does not find a capitalised folder.

| Skill                      | Use it for                                                                                                                                                                                                                       |
| -------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `lifeblood-mcp`            | Routing between the `lifeblood` MCP tools; read before a multi-step refactor.                                                                                                                                                    |
| `unity-package-management` | Add/remove/upgrade UPM packages via `UnityEditor.PackageManager.Client` instead of hand-editing `Packages/manifest.json`. Applies to this project's _external_ deps — the packages it hosts are embedded, not registry-resolved. |
| `releasing-packages`       | Preparing and running a release, and fixing a failed changelog check.                                                                                                                                                            |
| `adding-a-package`         | Creating a package from `PackageTemplate`.                                                                                                                                                                                       |
| `unity-asset-editing`      | Any change to a scene, prefab, `.asset` or `.meta` file.                                                                                                                                                                         |
| `feature-worktree`         | Starting a branch in a worktree from `origin/dev`, and finishing at the pull request.                                                                                                                                            |
| `unity-cli`                | Editor install, project creation, headless build/test.                                                                                                                                                                           |

### `sharplens`: Code Navigation — ALWAYS PREFER over Grep/Glob/LS

| Instead of                             | Use                                                                         |
| -------------------------------------- | --------------------------------------------------------------------------- |
| `Grep` for where a symbol is defined   | `mcp__sharplens__go_to_definition`, `mcp__sharplens__search_symbols`        |
| `Grep` for callers of a method         | `mcp__sharplens__find_references`, `mcp__sharplens__find_callers`           |
| `Grep` for interface implementers      | `mcp__sharplens__find_implementations`, `mcp__sharplens__get_derived_types` |
| `LS` / reading files to map a type     | `mcp__sharplens__get_type_overview`, `mcp__sharplens__get_type_members`     |
| Reading a whole file to see one method | `mcp__sharplens__get_method_source`                                         |
| Skimming a file to learn its shape     | `mcp__sharplens__get_file_overview`                                         |
| Guessing at a compile error            | `mcp__sharplens__get_diagnostics`, then `mcp__sharplens__get_code_fixes`    |
| Hand-editing a rename across files     | `mcp__sharplens__rename_symbol`                                             |
| Manually tracing a call chain          | `mcp__sharplens__get_call_graph`, `mcp__sharplens__find_path_between`       |

**Why?** These use Roslyn semantic analysis — far faster, far fewer tokens, and correct where text search is not (partial types, overloads, inheritance, `using` aliases). They also cover refactorings generic file tools can't do at all: `extract_method`, `extract_interface`, `change_signature`, `encapsulate_field`, `move_type_to_file`, `implement_missing_members`, `organize_usings`, `fix_all`.

Useful beyond navigation: `mcp__sharplens__get_project_health`, `find_god_objects`, `get_complexity_metrics`, `get_exception_flow`, `analyze_data_flow`, `find_async_issues`, `resolve_stack_trace`, `diff_api_surface`, `get_di_registrations` (relevant to this project's Service Locator), `find_circular_dependencies`.

**Still read the source before editing.** These tools narrow the search and validate assumptions; they don't replace judgement.

### `lifeblood` — Roslyn code navigation

`lifeblood` is a shared daemon exposing Roslyn semantic analysis over a C# solution. Call `lifeblood_analyze` with `projectPath` pointing at the repo root once per session before any other `lifeblood_*` tool; write-side tools (`find_references`, `rename`, `diagnose`, `compile_check`) additionally need a non-read-only analyze (`readOnly:false`). Prefer it over `Grep`/`Glob` whenever the question is semantic:

| Instead of                              | Use                                                                        |
| --------------------------------------- | -------------------------------------------------------------------------- |
| `Grep` for a symbol                     | `lifeblood_find_definition`, `lifeblood_find_references`                   |
| `Grep` for implementers of an interface | `lifeblood_find_implementations`                                           |
| Guessing what a change breaks           | `lifeblood_blast_radius`, `lifeblood_file_impact`, `lifeblood_test_impact` |
| Eyeballing asmdef wiring                | `lifeblood_asmdef_check`, `lifeblood_cycles`                               |
| A cross-package symbol rename           | `lifeblood_rename`                                                         |

⚠️ **`lifeblood` needs a solution** If there is no `.sln`/`.slnx` or `.csproj`, **generate the solution first, then analyze.** . Runs `unity command menu --path "Assets/Open C# Project"`) to generate.

### What belongs to `lifeblood`

SharpLens has no Unity knowledge whatsoever. Use `lifeblood` for:

| Question                                                       | Tool                                                                       |
| -------------------------------------------------------------- | -------------------------------------------------------------------------- |
| Does this asmdef actually declare its dependencies?            | `lifeblood_asmdef_check`                                                   |
| Does this hold in player builds as well as `#if UNITY_EDITOR`? | `lifeblood_analyze` with `defineProfiles:["Editor","Player","Standalone"]` |
| Is this MonoBehaviour / UnityEvent-wired code genuinely dead?  | `lifeblood_dead_code`                                                      |
| What's the blast radius, and which tests should I run?         | `lifeblood_blast_radius`, `lifeblood_file_impact`, `lifeblood_test_impact` |

> **Unity-blindness warning.** SharpLens ships near-equivalents — `analyze_change_impact`, `check_architecture`, `find_unused_code`, `find_untested_code`, `find_dead_branches` — that know nothing about Unity. They will report MonoBehaviour message methods (`Awake`, `Start`, `OnEnable`, `Update`), `[SerializeField]` targets, and UnityEvent-wired handlers as unused or unreachable, because nothing in C# source calls them. **Never delete Unity-facing code on a SharpLens unused/dead-code result alone** — confirm with `lifeblood_dead_code`, which resolves MonoBehaviour and Editor reflection entry points and UnityEvent persistent calls from scene/prefab YAML.

### `unity command`: Editor State — official Unity CLI, called directly (not an MCP server)

The Unity Editor is reachable through Unity's own `unity` CLI, backed by the `com.unity.pipeline` package. Call it straight from Bash — no MCP registration, no session reconnect: `unity command` (no args) lists every available command; `unity command <name> --arg value` runs one.

Common commands (see `unity command` for the full ~140-command surface — GameObjects, prefabs, scenes, assets, animator, timeline, navmesh, lighting, builds):

| Need                                                                              | Command                                         |
| --------------------------------------------------------------------------------- | ----------------------------------------------- |
| Read the console before fixing a compile error — do not guess the line/error code | `unity command console --tail 50 --level error` |
| Inspect the active scene hierarchy before changing it                             | `unity command get_scene_hierarchy`             |
| Quick C# check without a full recompile/domain reload                             | `unity command eval --code "<expression>"`      |
| Find GameObjects by name/tag/component                                            | `unity command find_gameobjects --name "..."`   |
| Run tests from inside a live Editor (vs. the batchmode `unity test`)              | `unity command run_tests --mode EditMode`       |

- Scenes, prefabs and `.asset` files change only through `unity command` or the Editor — never by hand-editing their YAML. The [`unity-asset-editing`](../skills/unity-asset-editing/SKILL.md) skill carries the rule and the fallback when a command is broken.

### Permissions

No `.claude/settings.json` is committed. If you want the usual allow-list (`git`, `gh`, and the `lifeblood` tools, with `lifeblood_execute` denied — it runs arbitrary code), add one yourself; `.claude/settings.local.json` is git-ignored for per-machine additions. In OpenCode the same deny is `"permission": { "lifeblood_lifeblood_execute": "deny" }` in your own `opencode.json`.
