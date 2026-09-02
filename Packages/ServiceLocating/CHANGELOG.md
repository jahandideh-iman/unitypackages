# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened `Runtime/Scripts` into a single folder and renamed the runtime namespace from `Arman.Foundation.Core.ServiceLocating` to `Arman.ServiceLocating`.

## [0.1.0] - 2026-08-30

First release of *Service Locating*.

### Added

- `ServiceLocator`, a static locator with `Init`, `IsInited`, `Register<TInterface, TImplementation>`, `Find<T>`, `UnRegister<T>`, `Replace<TInterface, TImplementation>` and `Clear`.
