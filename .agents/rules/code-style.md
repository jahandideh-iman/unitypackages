# Code style and formatting

The formatters, what they cover, and the C# conventions they do not enforce.

## Formatting

Layout is owned by two formatters, run through one pair of npm scripts from the repo root:

```powershell
dotnet tool restore     # once: installs the pinned CSharpier from .config/dotnet-tools.json
npm ci                  # once: installs the pinned Prettier from package-lock.json
npm run format          # rewrite every in-scope file
npm run format:check    # what the required `format` check runs
```

| Tool            | Formats                                        | Config                                  |
| --------------- | ---------------------------------------------- | --------------------------------------- |
| CSharpier 1.3.0 | `*.cs`                                         | `.csharpierrc.json`, `.csharpierignore` |
| Prettier 3.9.6  | JSON, YAML, JS, Markdown — changelogs included | `.prettierrc.json`, `.prettierignore`   |

`.editorconfig` covers the rest: editor defaults, and the naming rules from [C# coding style](#c-coding-style) as IDE1006 warnings. Those show in Rider, Visual Studio and VS Code only; neither Unity nor CI reports them. `.editorconfig` cannot tell a serialized field from a plain one, so its `_camelCase` check covers `readonly` fields only. Code blocks inside Markdown are left as written (`embeddedLanguageFormatting: "off"`): docs quote exact file contents and fragments, and reformatting them would change what they show.

The root `package.json` exists only to pin Prettier. It is `private`, Unity ignores it (Unity reads `Packages/manifest.json`), and the release tooling globs `Packages/*/package.json`, which does not match it. The `Tools/` scripts stay dependency-free.

**Excluded, and why.** Each exclusion has a reason; don't remove one without replacing the reason:

- **Unity-written files** — `.meta`, `.asset`, `.prefab`, `.unity`, `.anim`, `.asmdef`, `ProjectSettings/`, `Packages/manifest.json`, `Packages/packages-lock.json`. Unity's next save would undo the formatting. `.asmdef` in particular is written with no final newline, and Prettier always adds one.
- **Vendored code** — `.agents/skills/`, `.claude/`, `.qwen/`, `Packages/PackageBasics/Runtime/ThirdParties/`. Reformatting makes it harder to compare with upstream. `.opencode/` is excluded with them: its subagents pair with `.claude/agents/`, and formatting one copy alone would split the pair.
- **`Tools/ci/Tests/fixtures/`** — read byte for byte by the tests.
- **Line endings and BOMs are left alone.** Both formatters use `endOfLine: auto`, and `.editorconfig` sets neither `end_of_line` nor `charset`. Git stores LF, and most C# files carry a BOM.

**Release tooling must emit formatted text.** A release PR is the output of `upm-release.mjs prepare`, and it has to pass `format` like any other PR. `Tools/upm-release.prepare.test.mjs` runs `prepare` on formatted input and asserts that `prettier --check` still passes. This is why `tooling-tests` runs `npm ci`. If you change how `prepare` writes a heading or a bullet, that test is the one that tells you.

**`git blame`.** The repo-wide reformat is listed in `.git-blame-ignore-revs`. GitHub honours it automatically; locally, run once:

```powershell
git config blame.ignoreRevsFile .git-blame-ignore-revs
```

**The first release PR after the reformat merges** — `dev` → `master`, carrying the reformat commit across — needs both waiver labels, `no-changelog` and `changelog-rewrite`. Prettier turned `*Name*` into `_Name_` inside every tagged section, so `Tools/changelog-check.mjs` reports `frozen-section` for every tagged package; it also reports `missing-section`/`missing-entry` for packages the release doesn't otherwise touch.

**Upgrading a formatter** is its own pull request: bump the pin, run `npm run format`, commit the result, and add that commit to `.git-blame-ignore-revs`. Merge it with a merge commit, not a squash, or the listed SHA will not exist on `dev`. The same reformat problem applies to a formatter upgrade whenever it changes changelog text: that pull request, and the next release PR after it, both need `no-changelog` and `changelog-rewrite`.

## C# coding style

Layout — indentation, wrapping, brace placement — is CSharpier's; see [Formatting](#formatting). The rules below are the ones a formatter cannot apply.

- **Curly braces:** Allman (brace on its own line).
- **PascalCase:** classes, interfaces, methods, properties, public/internal fields.
- **Interfaces are `I`-prefixed**, file names included. Anything without the prefix is a class or struct — don't add an interface that breaks this.
- **The sole implementation of an interface takes the interface's name without the `I`.** `IUpdateManager` is implemented by `UpdateManager`, `IShopCenter` by `ShopCenter`. Do **not** reach for a `Basic` prefix: it distinguishes the type from nothing. Introduce a qualifier only when a second implementation actually exists and the name has to say which one it is — the way `UnityUpdateManager` and `UnityConfigurationManager` (MonoBehaviour adapters over the plain types) already do.
- **camelCase:** locals and parameters.
- **`_camelCase`:** private/protected fields that are not serialized.
- **`[SerializeField]` on private fields** rather than making them public, and **PascalCase** whatever their accessibility: `[SerializeField] private float Speed;`. When other code reads the value, serialize an auto-property's backing field instead of pairing a field with a getter: `[field: SerializeField] public float Speed { get; private set; }`. Its serialized name is `<Speed>k__BackingField`, which is what `SerializedObject.FindProperty` and `JsonUtility` keys need.
- **A shipped serialized field keeps its name.** The field name is the key in every consumer's scenes, prefabs and assets, so renaming it silently drops their values. A rename that has to happen carries `[FormerlySerializedAs("oldName")]` and a changelog entry. The fields that predate the PascalCase rule are listed under [Known inconsistencies](./packages.md#known-inconsistencies).
- **Keep `UnityEngine` out of foundation packages** where it isn't needed. `PackageBasics` and `ServiceLocating` are pure C# and testable as plain libraries — preserve that.
- Avoid per-frame allocations; prefer event-driven designs over `Update()` polling.
