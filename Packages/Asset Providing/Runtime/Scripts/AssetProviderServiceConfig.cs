using UnityEngine;
using UnityEngine.Serialization;

namespace Arman.AssetProviding
{
    [CreateAssetMenu(menuName = ContextMenuConsts.ROOT_CATEGORY + "Asset Provider Service Config")]
    public class AssetProviderServiceConfig : ScriptableObject
    {
        [FormerlySerializedAs("syncProviders")]
        [SerializeField]
        AssetProviderConfig[] SyncProviders;

        [FormerlySerializedAs("asyncProviders")]
        [SerializeField]
        AssetProviderConfig[] AsyncProviders;

        public ChainedAssetProviderService CreateAssetProviderService()
        {
            var service = new ChainedAssetProviderService();

            foreach (var config in SyncProviders)
                service.AddSyncProvider(config.CreateSyncProvider());

            foreach (var config in AsyncProviders)
                service.AddAsyncProvider(config.CreateAsyncProvider());

            return service;
        }
    }
}
