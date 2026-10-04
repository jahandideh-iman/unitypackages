# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `PopupTransition`, an optional component that animates a popup in when `UIManager.OpenPopUp` opens it and out before `UIManager.Close` destroys it.
- A transparent input blocker that `UIManager` places above every popup while any transition runs, and `UIManager.IsInputBlocked`. Escape is ignored while input is blocked.

### Changed

- `UIManager.Close` to allow closing windows that are not focused

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

## [0.1.0] - 2026-08-30

First release of _UI Management_.

### Added

- `UIElement`, the base UI component with an `InternalOnDestroy` hook.
- `Window` and its `MainWindow`, `PopupWindow` and `Panel` variants, including sorting order and layer control and `OnBackButtonPressed`.
- `UIManager`, a `Canvas`-level `MonoBehaviour` providing `Init`, `SetMainWindow`, `MainWindow`, `OpenPopUp<T>`, `Close`, `SetMainCamera` and `BackgroundPanel`, and closing the focused window on the back/Escape key.
