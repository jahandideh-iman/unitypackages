# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Removed

- The camelCase names of public and protected fields, renamed to PascalCase: `IDedChannel.id` → `IDedChannel.Id`. **Breaking** — update references to the new names.

### Added

- `InvariantJsonNumber`, a `NiceJson` number node that serializes with the invariant culture, so the JSON it writes parses back on every device locale. `float` and `double` use the round-trip format.

### Changed

- Renamed private fields that Unity does not serialize to `_camelCase`. No public API or serialized data changes.

### Fixed

- `NiceJson` writes `decimal` values with the invariant culture, so on a comma-decimal locale such as German or French `ToJsonString()` emits `1.5` instead of the invalid `1,5`. The bundled `NiceJson` is upstream 1.3.3.

## [0.3.0] - 2026-09-05

### Removed

- `BasicContainer<T>`, renamed to `Container<T>`. It was the only implementation of `IContainer<T>`, so the `Basic` prefix distinguished it from nothing. **Breaking** — update references to the new name.

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened `Runtime/Scripts/` (removed the `Utility/Core` subfolder) and consolidated all runtime types into the `Arman.PackageBasics` namespace (previously `Arman.Utility.Core`).

## [0.1.0] - 2026-08-30

First release of _Package Basics_.

### Added

- `IContainer<T>` and `BasicContainer<T>`, with `Add`, `Contains`, `Find<U>`, `FindAll<U>` and `Items`.
- `IChannel`, with the `NamedChannel` (string identity) and `IDedChannel` (integer identity) implementations, both providing value equality and hashing so they can be used as dictionary keys.
- The bundled `NiceJson` JSON parser and serializer (third party, MIT — see `Third Party Notices.md`).
