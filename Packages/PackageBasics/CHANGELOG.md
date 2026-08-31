# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-08-30

First release of *Package Basics*.

### Added

- `IContainer<T>` and `BasicContainer<T>`, with `Add`, `Contains`, `Find<U>`, `FindAll<U>` and `Items`.
- `IChannel`, with the `NamedChannel` (string identity) and `IDedChannel` (integer identity) implementations, both providing value equality and hashing so they can be used as dictionary keys.
- The bundled `NiceJson` JSON parser and serializer (third party, MIT — see `Third Party Notices.md`).
