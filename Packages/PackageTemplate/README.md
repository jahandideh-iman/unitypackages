# Package Template

A minimal Unity UPM package **scaffold**. It carries **no runtime code** and serves as the reference layout for creating a new embedded Arman package. Keep `private: true` in its `package.json` (and omit `license`) so it is not published to OpenUPM, and copy it to bootstrap a new package.

## Layout

- `package.json` — the UPM manifest (with `private: true`).
- `Runtime/` — runtime C# code and its `.asmdef`.
- `Editor/` — editor-only code and its `.asmdef` (references the runtime definition).
- `Tests/` — editor/runtime test assemblies.
- `Samples~/<Name>/` — example content (scenes, scripts, prefabs), declared in the `samples`
  array of `package.json`. The `~` keeps it out of a consumer's compilation until they import it.
