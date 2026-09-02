# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

## [0.1.0] - 2026-08-30

First release of *UI Management*.

### Added

- `UIElement`, the base UI component with an `InternalOnDestroy` hook.
- `Window` and its `MainWindow`, `PopupWindow` and `Panel` variants, including sorting order and layer control and `OnBackButtonPressed`.
- `UIManager`, a `Canvas`-level `MonoBehaviour` providing `Init`, `SetMainWindow`, `MainWindow`, `OpenPopUp<T>`, `Close`, `SetMainCamera` and `BackgroundPanel`, and closing the focused window on the back/Escape key.
