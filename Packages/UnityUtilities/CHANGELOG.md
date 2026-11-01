# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-11-01

### Removed

- The camelCase names of public and protected fields, renamed to PascalCase: `DelayHandler.id` → `DelayHandler.Id`, `DelayHandler.duration` → `DelayHandler.Duration`, `DelayHandler.autoStart` → `DelayHandler.AutoStart`, `DelayHandler.timeoutEvent` → `DelayHandler.TimeoutEvent`, `AnimationEvent.name` → `AnimationEvent.Name`, `AnimationEvent.action` → `AnimationEvent.Action`, `UnityAnimatorEventHandler.animationEvents` → `UnityAnimatorEventHandler.AnimationEvents`, `DelegationInfo.id` → `DelegationInfo.Id`, `DelegationInfo.delegates` → `DelegationInfo.Delegates`, `UnityEventDelegator.delegations` → `UnityEventDelegator.Delegations`. **Breaking** — update references to the new names. Fields Unity serializes carry `[FormerlySerializedAs]` with the old name, so values already saved in scenes, prefabs and assets keep loading.

### Changed

- Renamed private fields that Unity does not serialize to `_camelCase`. No public API or serialized data changes.

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened the internal folder layout: runtime scripts now live directly under `Runtime/Scripts` (the `Utility/` and `Utility/UnityEvents/` split is gone).
- Simplified the public namespaces: every runtime type moved to `Arman.UnityUtilities`. The former `Arman.Utilty.Unity` namespace (note the "Utilty" typo) is removed — update `using` directives to match.

## [0.1.0] - 2026-08-30

First release of _Unity Utilities_.

### Added

- `DelayHandler`, a coroutine-backed timer `MonoBehaviour` that raises a `UnityEvent` after a duration, with optional auto-start.
- `UnityAnimationPlayer`, a wrapper over the legacy `Animation` component for playing, adding and removing clips.
- `UnityAnimatorEventHandler`, which routes named animation events to `UnityEvent`s.
- `UnityEventDelegator`, which invokes groups of `UnityEvent`s by id.
- Serializable `UnityEvent<T>` subclasses so typed events survive Inspector serialization: `BooleanUnityEvent`, `FloatUnityEvent`, `IntUnityEvent` and `StringUnityEvent`.
