using Arman.AssetProviding.Foundation;
using UnityEngine;

namespace Arman.AssetProviding.Data
{
    public abstract class AssetProviderConfig : ScriptableObject
    {
        public abstract SyncUnityAssetProvider CreateSyncProvider();
        public abstract AsyncUnityAssetProvider CreateAsyncProvider();
    }
}