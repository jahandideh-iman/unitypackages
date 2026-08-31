# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-08-30

First release of *Development Console*.

### Added

- `DevelopmentConsolePanel` — the console root, which discovers annotated members by reflection (`InitCommands`, `InitShortCuts`) and toggles the on-screen panel.
- `DevelopmentGroup` and `DevelopmentCommand` — the group and command views instantiated from prefabs.
- `DevOptionAttribute(group, commandName)` for marking a method as a console command, and `ShortCutAttribute(params KeyCode[])` for binding it to a key combination.
- `CommandInfo` and `CommandInputPrompt`, which invoke a command with or without typed arguments.
- `FPSProfiler` and `ButtonDragabler` Unity helpers.
