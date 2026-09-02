# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened the internal folder layout: runtime scripts now live directly under `Runtime/Scripts` (the `Foundation/ComponentSystem/Core` split is gone).
- Simplified the public namespace: every runtime type moved to `Arman.ComponentSystem` — the former `Arman.Foundation.ComponentSystem.Core` namespace is removed. Update `using` directives to match.

## [0.1.0] - 2026-08-30

First release of *Component System*.

### Added

- `IComponent` and `IEntity`, with `BasicEntity` providing `AddComponent`, `AddComponents`, `GetComponent<T>`, `GetComponent<T>(index)`, `GetComponentFromEnd<T>` and `AllComponents`.
- `ISpecializedEntity<T>` and `BasicSpecializedEntity<T>` for entities restricted to a single component type.
- `ICache` and `CacheableBasicEntity<T>`, which refreshes a cache through the `OnComponentAdded` hook so repeated component lookups are avoided.
