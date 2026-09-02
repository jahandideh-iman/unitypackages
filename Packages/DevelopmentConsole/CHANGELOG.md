# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened the runtime source tree: all `.cs` files now live directly under `Runtime/Scripts` (the `Development/Base/DevelopmentConsole` and `Development/Unity` subfolders are gone), and `Arman.DevelopmentConsole.asmdef` moved to the `Runtime/` root.
- Simplified the namespace: every runtime type is now in `Arman.DevelopmentConsole` — the former `Arman.Development.DevelopmentConsole.Base`, `Arman.Development.DevelopmentConsole.Unity` and `Arman.Presentation` namespaces are gone. Update `using` directives to match.

## [0.1.0] - 2026-08-30

First release of *Development Console*.

### Added

- `DevelopmentConsolePanel` — the console root, which discovers annotated members by reflection (`InitCommands`, `InitShortCuts`) and toggles the on-screen panel.
- `DevelopmentGroup` and `DevelopmentCommand` — the group and command views instantiated from prefabs.
- `DevOptionAttribute(group, commandName)` for marking a method as a console command, and `ShortCutAttribute(params KeyCode[])` for binding it to a key combination.
- `CommandInfo` and `CommandInputPrompt`, which invoke a command with or without typed arguments.
- `FPSProfiler` and `ButtonDragabler` Unity helpers.
