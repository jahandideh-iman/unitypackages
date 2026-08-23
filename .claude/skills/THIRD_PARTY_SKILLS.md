# Third-Party Skills

Skills below were vendored from external repos (not authored in this project), copied
across from the sibling `PopBalloon` repo on 2026-08-23. Re-sync manually by re-cloning
the source and copying over the relevant `skills/<name>/` folder.

Each skill lives twice on purpose: `.agents/Skills/<name>/` is the tool-agnostic canonical
copy, and `.claude/skills/<name>/` is the mirror Claude Code actually discovers. **Keep the
two in sync** — edit one, copy to the other.

| Skill(s) | Source | License |
|---|---|---|
| `unity-cli`, `unity-package-management` | [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills) | Unity Companion License |
| `lifeblood-mcp` | [user-hash/Lifeblood](https://github.com/user-hash/Lifeblood/tree/main/skills/lifeblood-mcp) | AGPL-3.0 |

## Notes for this repo

- This repo is a **host for embedded UPM packages**, not a game: 149 `.cs` files, 34 asmdefs,
  and a single throwaway `Assets/Scenes/SampleScene.unity`. PopBalloon's ~77 Unity-Editor MCP
  tool skills (`gameobject-*`, `scene-*`, `assets-*`, `screenshot-*`, `ui*`) and its
  storefront skills (`implement-in-app-purchases`, `levelplay-unity-integration`,
  `build-live-game`, `mobile-app-design`) were **deliberately not copied** — there is no live
  scene/prefab/UI work here for them to drive. Pull one across from `../PopBalloon` if that
  ever changes.
- `unity-package-management` is the directly on-topic one: it covers UPM package add/remove/
  upgrade through `UnityEditor.PackageManager.Client` rather than hand-editing
  `Packages/manifest.json`. Note that this repo's packages are **embedded** (checked in under
  `Packages/<Dir>/`), not registry-resolved, so that skill applies to this project's *external*
  dependencies, not to the 18 packages it hosts.
- `lifeblood-mcp` documents tool routing for the `lifeblood` MCP server — see
  [`AGENTS.md`](../AGENTS.md)'s MCP Tool Usage section. Its upstream `agents/openai.yaml`
  (tooling for a non-Claude runtime) was dropped. The source is AGPL-3.0, unlike the
  Unity-Companion-licensed entries above — this vendored copy is instructional prompt text read
  by agents, not code linked into any shipped package, but keep that distinction in mind before
  reusing content from it elsewhere.
