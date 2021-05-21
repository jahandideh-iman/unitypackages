using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using System.IO;
using Arman.AssetProviding.Utility;

namespace Arman.AssetProviding.Foundation
{
    public class ResourcesAssetProvider : SyncUnityAssetProvider, AsyncUnityAssetProvider
    {
        string pathPrefix = "";

        public ResourcesAssetProvider(string pathPrefix)
        {
            this.pathPrefix = pathPrefix;
        }

        public T LoadAssetById<T>(string id) where T : Object
        {
            return Resources.Load<T>(Path.Combine(pathPrefix, id));
        }

        public T LoadAssetByType<T>() where T : Object
        {
            var assets = Resources.LoadAll<T>(pathPrefix);

            return assets.Length > 0 ? assets[0] : default;
        }


        public async Task<T> LoadAssetByIdAsync<T>(string id) where T : Object
        {
            var request = Resources.LoadAsync<T>(Path.Combine(pathPrefix, id));
            await TaskUtilities.WaitUntil(() => request.isDone);
            return (T) request.asset;
        }


        public Task<T> LoadAssetByTypeAsync<T>() where T : Object
        {
            return Task.FromResult(LoadAssetByType<T>());
        }

    }
}