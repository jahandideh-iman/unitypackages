using Arman.AssetProviding.Utility;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding.Foundation
{
    public class ChainedSyncUnityAssetProvider : ChainedContainer<SyncUnityAssetProvider>, SyncUnityAssetProvider
    {
        public T LoadAssetById<T>(string id) where T : Object
        {
            foreach(var obj in ChainedObjects())
            {
                var asset = obj.LoadAssetById<T>(id);
                if (asset != null)
                    return asset;
            }

            return default;
        }

        public T LoadAssetByType<T>() where T : Object
        {
            foreach (var obj in ChainedObjects())
            {
                var asset = obj.LoadAssetByType<T>();
                if (asset != null)
                    return asset;
            }

            return default;
        }
    }
}