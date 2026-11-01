# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- The error logged for an unsupported command parameter type reads "is not supported" instead of "is not supproted".

### Removed

- The camelCase names of public and protected fields, renamed to PascalCase: `ButtonDragabler.dragThreshold` → `ButtonDragabler.DragThreshold`, `CommandInfo.name` → `CommandInfo.Name`, `CommandInfo.methodInfo` → `CommandInfo.MethodInfo`, `CommandInfo.keyCodes` → `CommandInfo.KeyCodes`, `CommandInputPrompt.inputFieldsContainer` → `CommandInputPrompt.InputFieldsContainer`, `CommandInputPrompt.inputFieldPrefab` → `CommandInputPrompt.InputFieldPrefab`, `DevelopmentCommand.commandNameText` → `DevelopmentCommand.CommandNameText`, `DevelopmentCommand.shortcutText` → `DevelopmentCommand.ShortcutText`, `DevelopmentConsolePanel.groupPrefab` → `DevelopmentConsolePanel.GroupPrefab`, `DevelopmentConsolePanel.commandInputPrompt` → `DevelopmentConsolePanel.CommandInputPrompt`, `DevelopmentConsolePanel.toolsPanel` → `DevelopmentConsolePanel.ToolsPanel`, `DevelopmentConsolePanel.devButton` → `DevelopmentConsolePanel.DevButton`, `DevelopmentConsolePanel.devButtonText` → `DevelopmentConsolePanel.DevButtonText`, `DevelopmentConsolePanel.groupsContainer` → `DevelopmentConsolePanel.GroupsContainer`, `DevelopmentConsolePanel.onErrorDetected` → `DevelopmentConsolePanel.OnErrorDetected`, `DevelopmentConsolePanel.onToolsPanelOpened` → `DevelopmentConsolePanel.OnToolsPanelOpened`, `DevelopmentConsolePanel.onToolsPanelClosed` → `DevelopmentConsolePanel.OnToolsPanelClosed`, `DevelopmentGroup.commandPrefab` → `DevelopmentGroup.CommandPrefab`, `DevelopmentGroup.gourpNameText` → `DevelopmentGroup.GroupNameText` (also fixing its spelling), `DevOptionAttribute.group` → `DevOptionAttribute.Group`, `DevOptionAttribute.commandName` → `DevOptionAttribute.CommandName`, `FPSProfiler.text` → `FPSProfiler.Text`, `FPSProfiler.intervalDuration` → `FPSProfiler.IntervalDuration`, `ShortCutAttribute.keyCodes` → `ShortCutAttribute.KeyCodes`, `ShortCutInfo.keyCodes` → `ShortCutInfo.KeyCodes`, `ShortCutInfo.action` → `ShortCutInfo.Action`. **Breaking** — update references to the new names. Fields Unity serializes carry `[FormerlySerializedAs]` with the old name, so values already saved in scenes, prefabs and assets keep loading.

### Changed

- Renamed private fields that Unity does not serialize to `_camelCase`. No public API or serialized data changes.

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened the runtime source tree: all `.cs` files now live directly under `Runtime/Scripts` (the `Development/Base/DevelopmentConsole` and `Development/Unity` subfolders are gone), and `Arman.DevelopmentConsole.asmdef` moved to the `Runtime/` root.
- Simplified the namespace: every runtime type is now in `Arman.DevelopmentConsole` — the former `Arman.Development.DevelopmentConsole.Base`, `Arman.Development.DevelopmentConsole.Unity` and `Arman.Presentation` namespaces are gone. Update `using` directives to match.

## [0.1.0] - 2026-08-30

First release of _Development Console_.

### Added

- `DevelopmentConsolePanel` — the console root, which discovers annotated members by reflection (`InitCommands`, `InitShortCuts`) and toggles the on-screen panel.
- `DevelopmentGroup` and `DevelopmentCommand` — the group and command views instantiated from prefabs.
- `DevOptionAttribute(group, commandName)` for marking a method as a console command, and `ShortCutAttribute(params KeyCode[])` for binding it to a key combination.
- `CommandInfo` and `CommandInputPrompt`, which invoke a command with or without typed arguments.
- `FPSProfiler` and `ButtonDragabler` Unity helpers.
