using UnityEngine;

namespace Arman.AssetProviding
{
    public abstract class AssetProviderConfig : ScriptableObject
    {
        public abstract ISyncUnityAssetProvider CreateSyncProvider();
        public abstract IAsyncUnityAssetProvider CreateAsyncProvider();
    }
}