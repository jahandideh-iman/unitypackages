# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-30

First release of *Unity Utilities*.

### Added

- `DelayHandler`, a coroutine-backed timer `MonoBehaviour` that raises a `UnityEvent` after a duration, with optional auto-start.
- `UnityAnimationPlayer`, a wrapper over the legacy `Animation` component for playing, adding and removing clips.
- `UnityAnimatorEventHandler`, which routes named animation events to `UnityEvent`s.
- `UnityEventDelegator`, which invokes groups of `UnityEvent`s by id.
- Serializable `UnityEvent<T>` subclasses so typed events survive Inspector serialization: `BooleanUnityEvent`, `FloatUnityEvent`, `IntUnityEvent` and `StringUnityEvent`.
