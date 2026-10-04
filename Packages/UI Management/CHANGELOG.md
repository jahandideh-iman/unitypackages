# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `Transition`, an abstract component whose `Play(CancellationToken)` returns a `UniTask`, for animating a popup in or out.
- `PopupWindow.InTransition` and `PopupWindow.OutTransition`, optional transitions that `UIManager` plays when the popup opens and before it destroys the popup on close.
- `UIManager.IsTransitioning`, and a transparent input blocker above all popups that swallows pointer input and the back button while a transition plays.

### Changed

- `UIManager.Close` to allow closing windows that are not focused
- `Window` is abstract. **Breaking** — replace a bare `Window` component with `MainWindow` or a subclass of your own.
- `UIManager.OpenPopUp` and `UIManager.Close` take a `PopupWindow` instead of any `Window`. **Breaking** — pass a `PopupWindow`; the main window is set only through `SetMainWindow`.
- `UIManager.Close` on a popup that is not on the stack destroys it and leaves the focused window alone, without calling its `OnFocused` again.
- The package depends on `com.cysharp.unitask` 2.5.11.

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

## [0.1.0] - 2026-08-30

First release of _UI Management_.

### Added

- `UIElement`, the base UI component with an `InternalOnDestroy` hook.
- `Window` and its `MainWindow`, `PopupWindow` and `Panel` variants, including sorting order and layer control and `OnBackButtonPressed`.
- `UIManager`, a `Canvas`-level `MonoBehaviour` providing `Init`, `SetMainWindow`, `MainWindow`, `OpenPopUp<T>`, `Close`, `SetMainCamera` and `BackgroundPanel`, and closing the focused window on the back/Escape key.
