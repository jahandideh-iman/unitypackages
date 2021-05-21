using Arman.AssetProviding.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding.Foundation
{
    public class TableBasedAssetProvider : SyncUnityAssetProvider, AsyncUnityAssetProvider
    {
        Dictionary<string, Object> objectTable;

        public TableBasedAssetProvider(Dictionary<string, Object> objectTable)
        {
            this.objectTable = objectTable;
        }

        public T LoadAssetById<T>(string id) where T : Object
        {
            return UnityAssetUtilities.CastToAsset<T>(objectTable[id]);
        }

        public T LoadAssetByType<T>() where T : Object
        {
            foreach (var obj in objectTable.Values)
                if (UnityAssetUtilities.IsOfAssetType<T>(obj, out var tObj))
                    return tObj;

            return default;
        }

        public Task<T> LoadAssetByIdAsync<T>(string id) where T : Object
        {
            return Task.FromResult(LoadAssetById<T>(id));
        }


        public Task<T> LoadAssetByTypeAsync<T>() where T : Object
        {
            return Task.FromResult(LoadAssetByType<T>());
        }
    }
}