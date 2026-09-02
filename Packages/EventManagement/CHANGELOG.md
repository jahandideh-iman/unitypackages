# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened the runtime source tree: all `.cs` files now live directly under `Runtime/Scripts` (the `Foundation/EventManagement` subfolder is gone).
- Simplified the namespace: every runtime type is now in `Arman.EventManagement` — the former `Arman.Foundation.EventManagement` namespace is gone. Update `using` directives to match.

## [0.1.0] - 2026-08-30

First release of *Event Management*.

### Added

- `IGameEvent`, `IEventListener` and `IEventManager` — the pub-sub contracts.
- `BasicEventManager`, with `Register`, `UnRegister`, `Has` and `Clear`.
- `Propagate(evt, sender)`, which dispatches over a snapshot of the listener list so handlers may register or unregister during dispatch.
