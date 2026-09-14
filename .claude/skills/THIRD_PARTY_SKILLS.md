# Skills

Most skills here are vendored from external repos rather than authored in this project.
Re-sync manually by re-cloning the source and copying over the relevant `skills/<name>/` folder.

Each skill lives twice on purpose: `.agents/Skills/<name>/` is the tool-agnostic canonical
copy, and `.claude/skills/<name>/` is the mirror Claude Code actually discovers. **Keep the
two in sync** — edit one, copy to the other.

| Skill(s) | Source | License |
|---|---|---|
| `unity-cli`, `unity-package-management` | [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills) | Unity Companion License |
| `lifeblood-mcp` | [user-hash/Lifeblood](https://github.com/user-hash/Lifeblood/tree/main/skills/lifeblood-mcp) | AGPL-3.0 |
| `sharplens-mcp` | **Authored in-repo** | — |

## Notes for this repo

- This repo is a **host for embedded UPM packages**, not a game, with a throwaway
  `Assets/Scenes/` sandbox. Roslyn code navigation and public-API discipline matter here;
  live scene/prefab/UI authoring does not. Skills covering the following are
  **deliberately not installed**:
  - **UI authoring** (uGUI / UI Toolkit / IMGUI routers) — there is no live Canvas or UITK
    work here for them to drive.
  - **Unity Gaming Services, IAP, ad mediation, mobile UX** — this repo ships libraries to a
    registry. It has no storefront, no player-facing build, and no players.
  - **Unity-Editor MCP tool skills** (`gameobject-*`, `scene-*`, `assets-*`, `screenshot-*`)
    — nothing here for them to act on.
  - **New-project scaffolding** — this project exists.
  - The **`unity-developer`** community skill — its body is generic filler ("Working on unity
    developer tasks or workflows"), so it would add discovery noise without adding guidance.

  Install any of these if that changes.
- `unity-package-management` is the directly on-topic one: it covers UPM package add/remove/
  upgrade through `UnityEditor.PackageManager.Client` rather than hand-editing
  `Packages/manifest.json`. Note that this repo's packages are **embedded** (checked in under
  `Packages/<Dir>/`), not registry-resolved, so that skill applies to this project's *external*
  dependencies, not to the packages it hosts.
- `sharplens-mcp` is **not vendored** — it is written for this repo and describes the
  `sharplens` MCP server, which is registered at *user* level, not in a project `.mcp.json`.
  Two things in it are specific to this repo and will be wrong if copied elsewhere: there is
  **no auto-load** (call `load_solution` at `unitypackages.slnx` yourself), and because
  `.gitignore` excludes `*.csproj`/`*.slnx`, **a fresh worktree or clone has no solution to
  load at all**. It also flags `diff_api_surface` as the tool that matters most here —
  published `<name>/<version>` tags are permanent, so a public-API break has to drive the
  semver bump.
- `lifeblood-mcp` documents tool routing for the `lifeblood` MCP server — see
  [`AGENTS.md`](../AGENTS.md)'s agent-tooling section. It is **not the primary Roslyn
  code-navigation tool**; `sharplens` is. `lifeblood` is used for what SharpLens cannot do:
  Unity asmdef checks, multi-define-profile analysis, Unity-reflection-aware dead code, and
  blast-radius/file-impact/test-impact analysis. **Locally modified:** its frontmatter
  `description` is narrowed and a scoping note sits at the top of `SKILL.md` so agents don't
  route ordinary navigation to it — re-apply both if you re-sync from upstream. Its upstream
  `agents/openai.yaml` (tooling for a non-Claude agent runtime) is not included. Two upstream
  capabilities have nothing to act on in this repo: `invariant_check` (no
  `docs/invariants/*.md`) and `evidence_drift` (no generated evidence baseline).
  Package-boundary rules are enforced by `Tools/upm-release.mjs validate` instead.
  The source is AGPL-3.0, unlike the Unity-Companion-licensed entries above — this vendored
  copy is instructional prompt text read by agents, not code linked into any shipped package,
  but keep that distinction in mind before reusing content from it elsewhere.
