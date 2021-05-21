using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding.Foundation
{
    public static class AssetProviderExtensions
    {
        public static T InstantiateByType<T>(this SyncUnityAssetProvider assetProvider, Transform parent) where T: Object
        {
            return Object.Instantiate(assetProvider.LoadAssetByType<T>(), parent);
        }

        public static T InstantiateById<T>(this SyncUnityAssetProvider assetProvider, string id, Transform parent) where T : Object
        {
            return Object.Instantiate(assetProvider.LoadAssetById<T>(id), parent);
        }

        public static async Task<T> InstantiateByTypeAsync<T>(this AsyncUnityAssetProvider assetProvider, Transform parent) where T : Object
        {
            return Object.Instantiate( await assetProvider.LoadAssetByTypeAsync<T>(), parent);
        }

        public static async Task<T> InstantiateByIdAsync<T>(this AsyncUnityAssetProvider assetProvider, string id, Transform parent) where T : Object
        {
            return Object.Instantiate(await assetProvider.LoadAssetByIdAsync<T>(id), parent);
        }
    }
}