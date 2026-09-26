---
name: unity-asset-editing
description: Use when a change touches a Unity scene, prefab, ScriptableObject .asset or any .meta file in this repo — creating, moving, renaming or deleting an asset, or when editing Unity YAML by hand looks like the quick fix.
---

# Unity asset editing

The packages in this repo ship their assets and GUIDs to every consumer, so two rules hold without exception.

## Never hand-edit asset YAML

Only modify Unity assets (`.unity` scenes, `.prefab` files, `.asset` ScriptableObjects, etc.) through `unity command` or the Unity Editor itself — never by hand-editing their YAML with a text tool. If the `unity command` call you need is broken, report the bug and find another Editor-mediated path — another command, `unity command eval` against the `UnityEditor` API, or a person making the change in the Editor — rather than falling back to a raw file edit. `unity command` with no arguments lists what is available; [`code-navigation.md`](../../../.agents/rules/code-navigation.md) covers the CLI.

## Unity `.meta` files

⚠️ **Never delete, ignore, or hand-create a `.meta` file carelessly.** In this repo the rule is stricter than in a game project, because these files ship to consumers:

- Every file _and folder_ in a package has a `.meta` carrying a GUID.
- Asmdef GUIDs are referenced by other asmdefs (`"references": ["GUID:..."]`). Losing one silently breaks compilation in dependent packages.
- Always commit an asset and its `.meta` together.
- `npm pack` includes `.meta` files automatically — verify with `npm pack --dry-run` when adding root-level files.

`node Tools/upm-release.mjs validate` fails any package with a file or folder that has no `.meta`.
