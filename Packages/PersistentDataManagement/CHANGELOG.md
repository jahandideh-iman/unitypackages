# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Removed

- `SetSaveVersion`, `SetPersistentDataWrapper` and `SetPersistentDataIOStreamFactory` on `IPersistentDataManager` and `BasicPersistentDataManager`, along with the parameterless `BasicPersistentDataManager()` constructor. A manager's collaborators are now supplied once through `BasicPersistentDataManager(IPersistentDataIOStreamFactory, IPersistentDataWrapper, int saveVersion)`, so it can no longer be left half-wired or have its wrapper swapped out between a save and the matching load. **Breaking** — construct the manager with its collaborators instead of setting them afterwards.
- `BasicPersistentDataManager`, renamed to `PersistentDataManager`. It was the only implementation of `IPersistentDataManager`, so the `Basic` prefix distinguished it from nothing. **Breaking** — update references to the new name.

### Changed

- Renamed `PersistentDataManagerFactory.Create()` to `CreateDefault(int saveVersion = 0)`, which now also takes the save version. **Breaking** — rename the call.

### Fixed

- `JSONPersistentDataWrapper.ReadFrom` no longer throws on empty or whitespace-only content. `JsonNode.ParseJsonString` answers it with `null`, and every later read then dereferenced that null root -- so one save interrupted mid-write (which leaves a zero-byte file) broke every subsequent load of that channel. Empty content now loads as "nothing saved yet".
- `JSONPersistentDataWrapper.BeginReadingBlock` no longer throws on a key that is not present. It yields an empty block instead, so reads inside it return their defaults and the matching `EndReadingBlock` still balances. `BasicPersistentDataManager.Load` opens the `"Data"` block unconditionally, so this was reachable from any store that reports a channel as readable before anything has been written to it -- `MemoryBasedPersistetDataIOStreamFactory` always does.

## [0.2.0] - 2026-09-02

### Added

- `Delete(IChannel channel)` on `IPersistentDataManager` and `IPersistentDataIOStreamFactory`, implemented in `BasicPersistentDataManager`, the memory-based and file-based stream factories, to remove a channel's saved data. Deleting an unregistered channel does not throw.
- `PersistentDataManagerFactory.Create()`, building a `BasicPersistentDataManager` wired to JSON files under `Application.persistentDataPath`.
- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened the internal folder layout: runtime scripts now live directly under `Runtime/Scripts` (the `Foundation/Core` and `Foundation/Unity` split is gone), unit tests are flat in `Tests/Editor/UnitTests` and mocks in `Tests/Editor/Mocks`.
- Simplified the public namespaces: every runtime type moved to `Arman.PersistentDataManagement`, and unit tests and their mocks now live in `Arman.PersistentDataManagement.Tests`. The `Arman.Foundation.Core...` and `Arman.Foundation.Unity...` namespaces are removed — update `using` directives to match.
- Updated `com.arman.package-basics` to `0.2.0`.

## [0.1.0] - 2026-08-30

First release of *Persistent Data Management*.

### Added

- `IPersistentDataManager` and `BasicPersistentDataManager`, which register `IPersistentDataSerializer`s against an `IChannel` and drive `SaveAll` / `Save(channel)` and `LoadAll` / `Load(channel)`, with a settable save version.
- `IPersistentDataWrapper`, split into `IReadablePersistentDataWrapper` and `IWritablePersistentDataWrapper`, covering `int`, `float`, `string` and `bool` plus nested read/write blocks.
- Wrapper implementations: `JSONPersistentDataWrapper`, `PlayerPrefsPersistentDataWrapper` and `EmptyPersistentDataWrapper`.
- `IPersistentDataIOStreamFactory` with file-based, memory-based and empty implementations.
- `PersistentDataChannelNotFoundException` and `PersistentDataSerializerAlreadyRegisterException`.
