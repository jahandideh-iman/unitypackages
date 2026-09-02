using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding
{
    [CreateAssetMenu(menuName = ContextMenuConsts.PROVIDER_CATEGORY + "Table-based Provider")]
    public class TableBasedAssetProviderConfig : AssetProviderConfig, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField] string id;
            [SerializeField] Object asset;

            public string ID { get => id; }
            public Object Asset { get => asset; }
        }

        [SerializeField] Entry[] assets;

        Dictionary<string, Object> assetsTable = new Dictionary<string, Object>();

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            assetsTable.Clear();

            foreach (var entry in assets)
                if(entry.Asset != null)
                    assetsTable.Add(entry.ID, entry.Asset);
        }


        public override IAsyncUnityAssetProvider CreateAsyncProvider()
        {
            return new TableBasedAssetProvider(assetsTable);
        }

        public override ISyncUnityAssetProvider CreateSyncProvider()
        {
            return new TableBasedAssetProvider(assetsTable);
        }

    }
}