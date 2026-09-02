# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-30

First release of *In Game Message Logging*.

### Added

- `IInGameMessageLogger`, a single-method `Log(string)` contract.
- `UnityInGameMessageLogger`, a `MonoBehaviour` that instantiates one `LogMessage` per call into a container, evicting the oldest once `capacity` is reached.
- `LogMessage`, a UnityEvent-driven message view (`setTextAction`, `fadeInAction`, `startTimeAction`, `fadeOutAction`) that clears itself after `logLifeTime`.
- A sample wiring an `InputField` to the logger.
