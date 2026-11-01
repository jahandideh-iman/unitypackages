using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding
{
    [CreateAssetMenu(menuName = ContextMenuConsts.PROVIDER_CATEGORY + "Table-based Provider")]
    public class TableBasedAssetProviderConfig : AssetProviderConfig, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct Entry
        {
            [field: SerializeField]
            [field: FormerlySerializedAs("id")]
            public string ID { get; private set; }

            [field: SerializeField]
            [field: FormerlySerializedAs("asset")]
            public Object Asset { get; private set; }
        }

        [FormerlySerializedAs("assets")]
        [SerializeField]
        Entry[] Assets;

        Dictionary<string, Object> _assetsTable = new Dictionary<string, Object>();

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            _assetsTable.Clear();

            foreach (var entry in Assets)
                if (entry.Asset != null)
                    _assetsTable.Add(entry.ID, entry.Asset);
        }

        public override IAsyncUnityAssetProvider CreateAsyncProvider()
        {
            return new TableBasedAssetProvider(_assetsTable);
        }

        public override ISyncUnityAssetProvider CreateSyncProvider()
        {
            return new TableBasedAssetProvider(_assetsTable);
        }
    }
}
