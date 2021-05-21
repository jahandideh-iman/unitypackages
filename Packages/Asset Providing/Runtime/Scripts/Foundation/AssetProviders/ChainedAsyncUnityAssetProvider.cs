using Arman.AssetProviding.Utility;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding.Foundation
{
    public class ChainedAsyncUnityAssetProvider : ChainedContainer<AsyncUnityAssetProvider>, AsyncUnityAssetProvider
    {
        public async Task<T> LoadAssetByIdAsync<T>(string id) where T : Object
        {
            foreach (var obj in ChainedObjects())
            {
                var asset = await obj.LoadAssetByIdAsync<T>(id);
                if (asset != null)
                    return asset;
            }

            return default;
        }

        public async Task<T> LoadAssetByTypeAsync<T>() where T : Object
        {
            foreach (var obj in ChainedObjects())
            {
                var asset = await obj.LoadAssetByTypeAsync<T>();
                if (asset != null)
                    return asset;
            }

            return default;
        }
    }
}