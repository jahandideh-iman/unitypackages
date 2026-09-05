# Asset Providing

An abstraction for loading Unity assets (prefabs, audio clips, sprites, …) through pluggable *asset
providers*. A provider resolves an asset either **by id** or **by type**, synchronously or
asynchronously. Providers can be chained, so a lookup falls through to the next provider until one
returns a result.

## What it provides

Namespace `Arman.AssetProviding`:

| Type | Purpose |
|---|---|
| `ISyncUnityAssetProvider` | `LoadAssetById<T>(id)` and `LoadAssetByType<T>()`. |
| `IAsyncUnityAssetProvider` | `LoadAssetByIdAsync<T>(id)` and `LoadAssetByTypeAsync<T>()`, both returning `Task<T>`. |
| `ResourcesAssetProvider` | Resolves through `Resources`, under a constructor-supplied path prefix. |
| `TableBasedAssetProvider` | Resolves from an `id → Object` dictionary. |
| `ChainedSyncUnityAssetProvider` / `ChainedAsyncUnityAssetProvider` | Try each added provider in order, return the first hit. |
| `IAssetProviderService` / `ChainedAssetProviderService` | Holds one chained sync provider and one chained async provider. |
| `AssetProviderExtensions` | `InstantiateById<T>`, `InstantiateByType<T>` and their async variants. |

Namespace `Arman.AssetProviding` — `ScriptableObject` configuration assets so providers can be
authored in the Editor: `AssetProviderConfig` (abstract, with `CreateSyncProvider()` /
`CreateAsyncProvider()`), `ResourcesAssetProviderConfig`, `TableBasedAssetProviderConfig`, and
`AssetProviderServiceConfig` (`CreateAssetProviderService()`).

Namespace `Arman.AssetProviding` — `ChainedContainer<T>`, `UnityAssetUtilities`, `TaskUtilities`.

## Usage

Build a service by hand and load through the chain:

```csharp
using Arman.AssetProviding;
using UnityEngine;

var service = new ChainedAssetProviderService();
service.AddSyncProvider(new ResourcesAssetProvider("Prefabs/"));
service.AddAsyncProvider(new ResourcesAssetProvider("Prefabs/"));

// By id — resolves "Prefabs/Enemy" through the Resources provider.
GameObject enemy = service.ISyncUnityAssetProvider.LoadAssetById<GameObject>("Enemy");

// By type — the first asset of that type the chain can supply.
var config = await service.IAsyncUnityAssetProvider.LoadAssetByTypeAsync<GameSettings>();
```

Adding a second provider makes the first one a fast path and the second a fallback:

```csharp
service.AddSyncProvider(new TableBasedAssetProvider(overrides)); // consulted first
service.AddSyncProvider(new ResourcesAssetProvider("Prefabs/")); // fallback
```

Instantiating in one step, via the extension methods:

```csharp
Enemy spawned = service.ISyncUnityAssetProvider.InstantiateById<Enemy>("Enemy", parentTransform);
```

Or drive it from Editor-authored configuration:

```csharp
using Arman.AssetProviding;

// serviceConfig is an AssetProviderServiceConfig asset.
ChainedAssetProviderService service = serviceConfig.CreateAssetProviderService();
```

## Things to know

- **`ResourcesAssetProvider` prefixes every id.** `new ResourcesAssetProvider("Prefabs/")` turns
  `LoadAssetById<T>("Enemy")` into a `Resources.Load` of `Prefabs/Enemy`.
- **A chained provider returns the first non-null result**, in the order providers were added.
- **`IAssetProviderService` exposes providers, it does not proxy them.** Go through
  `service.ISyncUnityAssetProvider` / `service.IAsyncUnityAssetProvider` to load.
- **`TableBasedAssetProvider` takes its table at construction**; the `TableBasedAssetProviderConfig`
  asset builds that dictionary from a serialized id/asset list.
- **Flat namespace.** The runtime lives in `Arman.AssetProviding` (formerly `Arman.AssetProviding.Utility`, `Arman.AssetProviding.Foundation` [including its `.Service` and `.AssetProviders` subnamespaces], and `Arman.AssetProviding.Data`); the scripts are flat under `Runtime/Scripts`.
