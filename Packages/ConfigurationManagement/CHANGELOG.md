# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened the internal folder layout: runtime scripts now live directly under `Runtime/Scripts` (the `Foundation/Core` and `Foundation/Unity` split is gone).
- Simplified the public namespace: every runtime type moved to `Arman.ConfigurationManagement` — the former `Arman.Foundation.Core.ConfigurationManagement` and `Arman.Foundation.Unity.Configuration` namespaces are removed. Update `using` directives to match.

## [0.1.0] - 2026-08-30

First release of *Configuration Management*.

### Added

- `IConfigurationManager`, `IConfigurer` and `IConfigurer<T>` — the core contracts.
- `BasicConfigurationManager`, with `Register<T>`, `Configure<T>`, `Contains<T>`, `FindConfigurer<T>` and `RemoveConfigurer<T>`.
- `CompositeConfigurer<T>`, which applies several configurers to one target, and `DynamicConfigurer<T>`, which applies a list of `Action<T>` at configure time.
- Unity support: `ScriptableConfiguration`, `UnityConfigurationMaster`, `UnityConfigurationManager` and the `AutoFillAssetArrayAttribute` inspector helper.
