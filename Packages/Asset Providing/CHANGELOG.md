# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Flattened `Runtime/Scripts` into a single folder and merged the runtime namespaces `Arman.AssetProviding.Utility`, `Arman.AssetProviding.Foundation` (including its `.Service` and `.AssetProviders` subnamespaces), and `Arman.AssetProviding.Data` into `Arman.AssetProviding`.

## [0.1.0] - 2026-08-30

First release of *Asset Providing*.

### Added

- `ISyncUnityAssetProvider` and `IAsyncUnityAssetProvider` — the provider contracts, with `LoadAssetById<T>` / `LoadAssetByType<T>` and their `…Async` counterparts.
- `ResourcesAssetProvider`, which resolves assets under a path prefix via `Resources`, and `TableBasedAssetProvider`, which resolves them from an id/asset dictionary.
- `ChainedSyncUnityAssetProvider` and `ChainedAsyncUnityAssetProvider`, which try each registered provider in order and return the first hit.
- `IAssetProviderService` and `ChainedAssetProviderService`, exposing one chained sync provider and one chained async provider.
- `ScriptableObject` configuration assets — `AssetProviderConfig`, `ResourcesAssetProviderConfig`, `TableBasedAssetProviderConfig` and `BasicAssetProviderServiceConfig` — so providers can be authored in the Editor.
- `AssetProviderExtensions` instantiation helpers (`InstantiateById`, `InstantiateByType` and async variants), plus `UnityAssetUtilities` and `TaskUtilities`.
