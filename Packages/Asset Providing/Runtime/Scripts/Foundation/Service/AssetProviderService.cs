using Arman.Foundation.Core.ServiceLocating;

namespace Arman.AssetProviding.Foundation
{
    public interface AssetProviderService : Service
    {
        SyncUnityAssetProvider SyncUnityAssetProvider { get; }
        AsyncUnityAssetProvider AsyncUnityAssetProvider { get; }
    }
}