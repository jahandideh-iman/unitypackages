# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-09-05

### Added

- `ChannelStateChangedEvent` on `IUpdateManager`. `BasicUpdateManager` already raised it, but the interface did not declare it and `UnityUpdateManager` did not forward it, so a consumer holding an `IUpdateManager` -- which is all a Service Locator hands out -- could not subscribe.

### Changed

- Updated `com.arman.package-basics` to `0.3.0`.

### Removed

- `BasicUpdateManager`, renamed to `UpdateManager`. It was the only plain implementation of `IUpdateManager`, so the `Basic` prefix distinguished it from nothing. The `UnityUpdateManager` MonoBehaviour adapter is unchanged. **Breaking** — update references to the new name.

### Fixed

- An `IUpdatable` whose `UpdateTime` throws no longer aborts the tick. The exception propagated out of `AdvanceTime`, abandoning the rest of that channel and every channel after it, and it repeated on every later frame. It is now caught and passed to `Debug.LogException`, so the remaining updatables carry on. The offender stays registered and is ticked again on each later frame, so it keeps throwing until the consumer unregisters it.

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened `Runtime/Scripts/Core` and `Runtime/Scripts/Unity` and consolidated all runtime types into the `Arman.UpdateManagement` namespace (previously `Arman.UpdateManagement.Foundation` / `Arman.UpdateManagement.Foundation.Unity`).
- Updated `com.arman.package-basics` to `0.2.0`.

## [0.1.0] - 2026-08-30

First release of *Update Management*.

### Added

- `IUpdatable`, with a single `UpdateTime(float dt)` callback.
- `IUpdateManager` and `BasicUpdateManager` — register updatables against an `IChannel`, nest channels with `RegisterChannelToParent`, and pause, resume or time-scale a channel and everything below it.
- `AdvanceTime(float)`, which ticks every registered updatable using its channel’s effective time scale, and the `ChannelStateChangedEvent` raised on pause and resume.
- `UnityUpdateManager`, a `MonoBehaviour` that drives a `BasicUpdateManager` from Unity’s `Update` loop using `Time.deltaTime`.
