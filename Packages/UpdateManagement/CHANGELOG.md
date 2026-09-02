# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Flattened `Runtime/Scripts/Core` and `Runtime/Scripts/Unity` and consolidated all runtime types into the `Arman.UpdateManagement` namespace (previously `Arman.UpdateManagement.Foundation` / `Arman.UpdateManagement.Foundation.Unity`).

## [0.1.0] - 2026-08-30

First release of *Update Management*.

### Added

- `IUpdatable`, with a single `UpdateTime(float dt)` callback.
- `IUpdateManager` and `BasicUpdateManager` — register updatables against an `IChannel`, nest channels with `RegisterChannelToParent`, and pause, resume or time-scale a channel and everything below it.
- `AdvanceTime(float)`, which ticks every registered updatable using its channel’s effective time scale, and the `ChannelStateChangedEvent` raised on pause and resume.
- `UnityUpdateManager`, a `MonoBehaviour` that drives a `BasicUpdateManager` from Unity’s `Update` loop using `Time.deltaTime`.
