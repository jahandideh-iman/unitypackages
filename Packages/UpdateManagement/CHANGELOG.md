# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `ChannelStateChangedEvent` on `IUpdateManager`. `BasicUpdateManager` already raised it, but the interface did not declare it and `UnityUpdateManager` did not forward it, so a consumer holding an `IUpdateManager` -- which is all a Service Locator hands out -- could not subscribe.

### Removed

- `BasicUpdateManager`, renamed to `UpdateManager`. It was the only plain implementation of `IUpdateManager`, so the `Basic` prefix distinguished it from nothing. The `UnityUpdateManager` MonoBehaviour adapter is unchanged. **Breaking** — update references to the new name.

### Fixed

- An `IUpdatable` whose `UpdateTime` throws no longer aborts the rest of its channel's tick. It stayed registered, so the exception repeated every frame and the channel never advanced again; it is now unregistered and the remaining updatables carry on.

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
