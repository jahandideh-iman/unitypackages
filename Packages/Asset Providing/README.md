# Asset Providing

A small abstraction for loading Unity assets (prefabs, audio, etc.) through pluggable *asset providers*. Concrete providers wrap `Resources` or a table of `AssetData` (a `ScriptableObject`), and sync/async/chained variants let you route a request past the first provider that can fulfil it.

## What it provides

- `ISyncUnityAssetProvider` / `IAsyncUnityAssetProvider` — the provider contracts.
- `IAssetProviderService` — an ordered chain of providers with a lookup table.
- `ResourcesAssetProvider` and `TableBasedAssetProvider` — the two concrete providers.
- `ChainedSyncUnityAssetProvider` / `ChainedAsyncUnityAssetProvider` — try providers in order.
- `ChainedAssetProviderService` — the concrete service.

## Usage

```csharp
using Arman.AssetProviding;

IAssetProviderService service = new ChainedAssetProviderService();
service.AddAssetProvider("Default", new ResourcesAssetProvider());

IAsyncUnityAssetProvider provider = service.GetAsyncProvider("Default");
provider.LoadAsset("Prefabs/MyPrefab", prefab => Debug.Log("Loaded " + prefab));
```
