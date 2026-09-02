# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened `Runtime/Scripts` into a single folder and merged the runtime namespaces `Arman.ObjectPooling.Core` and `Arman.ObjectPooling.Unity` into `Arman.ObjectPooling`.

## [0.1.0] - 2026-08-30

First release of *Object Pooling*.

### Added

- `IPoolable` and `IObjectPool<T>`, with `Acquire`, `Release`, `Reserve` and `Size`.
- `BasicObjectPool<T>`, an abstract pool that defers `CreateObject`, `ActivateObject` and `DeactivateObject` to subclasses.
- `UnityComponentObjectPool<T>`, which instantiates a `Component` prefab under a pooling container and toggles objects instead of allocating.
- `MonobehaviorObjectPool<T>` and `ScriptableObjectPool<T>`, `MonoBehaviour` and `ScriptableObject` front-ends over the component pool.
