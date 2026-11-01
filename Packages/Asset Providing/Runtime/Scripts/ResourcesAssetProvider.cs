using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding
{
    public class ResourcesAssetProvider : ISyncUnityAssetProvider, IAsyncUnityAssetProvider
    {
        string _pathPrefix = "";

        public ResourcesAssetProvider(string pathPrefix)
        {
            this._pathPrefix = pathPrefix;
        }

        public T LoadAssetById<T>(string id)
            where T : Object
        {
            return Resources.Load<T>(Path.Combine(_pathPrefix, id));
        }

        public T LoadAssetByType<T>()
            where T : Object
        {
            var assets = Resources.LoadAll<T>(_pathPrefix);

            return assets.Length > 0 ? assets[0] : default;
        }

        public async Task<T> LoadAssetByIdAsync<T>(string id)
            where T : Object
        {
            var request = Resources.LoadAsync<T>(Path.Combine(_pathPrefix, id));
            await TaskUtilities.WaitUntil(() => request.isDone);
            return (T)request.asset;
        }

        public Task<T> LoadAssetByTypeAsync<T>()
            where T : Object
        {
            return Task.FromResult(LoadAssetByType<T>());
        }
    }
}
