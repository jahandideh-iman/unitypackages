# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `Delete(IChannel channel)` on `IPersistentDataManager` and `IPersistentDataIOStreamFactory`, implemented in `BasicPersistentDataManager`, the memory-based and file-based stream factories, to remove a channel's saved data. Deleting an unregistered channel does not throw.

## [0.1.0] - 2026-08-30

First release of *Persistent Data Management*.

### Added

- `IPersistentDataManager` and `BasicPersistentDataManager`, which register `IPersistentDataSerializer`s against an `IChannel` and drive `SaveAll` / `Save(channel)` and `LoadAll` / `Load(channel)`, with a settable save version.
- `IPersistentDataWrapper`, split into `IReadablePersistentDataWrapper` and `IWritablePersistentDataWrapper`, covering `int`, `float`, `string` and `bool` plus nested read/write blocks.
- Wrapper implementations: `JSONPersistentDataWrapper`, `PlayerPrefsPersistentDataWrapper` and `EmptyPersistentDataWrapper`.
- `IPersistentDataIOStreamFactory` with file-based, memory-based and empty implementations.
- `PersistentDataChannelNotFoundException` and `PersistentDataSerializerAlreadyRegisterException`.
