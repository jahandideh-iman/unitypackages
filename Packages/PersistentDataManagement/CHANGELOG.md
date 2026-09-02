# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `Delete(IChannel channel)` on `IPersistentDataManager` and `IPersistentDataIOStreamFactory`, implemented in `BasicPersistentDataManager`, the memory-based and file-based stream factories, to remove a channel's saved data. Deleting an unregistered channel does not throw.
- `PersistentDataManagerFactory.Create()`, building a `BasicPersistentDataManager` wired to JSON files under `Application.persistentDataPath`.
- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened the internal folder layout: runtime scripts now live directly under `Runtime/Scripts` (the `Foundation/Core` and `Foundation/Unity` split is gone), unit tests are flat in `Tests/Editor/UnitTests` and mocks in `Tests/Editor/Mocks`.
- Simplified the public namespaces: every runtime type moved to `Arman.PersistentDataManagement`, and unit tests and their mocks now live in `Arman.PersistentDataManagement.Tests`. The `Arman.Foundation.Core...` and `Arman.Foundation.Unity...` namespaces are removed — update `using` directives to match.

## [0.1.0] - 2026-08-30

First release of *Persistent Data Management*.

### Added

- `IPersistentDataManager` and `BasicPersistentDataManager`, which register `IPersistentDataSerializer`s against an `IChannel` and drive `SaveAll` / `Save(channel)` and `LoadAll` / `Load(channel)`, with a settable save version.
- `IPersistentDataWrapper`, split into `IReadablePersistentDataWrapper` and `IWritablePersistentDataWrapper`, covering `int`, `float`, `string` and `bool` plus nested read/write blocks.
- Wrapper implementations: `JSONPersistentDataWrapper`, `PlayerPrefsPersistentDataWrapper` and `EmptyPersistentDataWrapper`.
- `IPersistentDataIOStreamFactory` with file-based, memory-based and empty implementations.
- `PersistentDataChannelNotFoundException` and `PersistentDataSerializerAlreadyRegisterException`.
