using Arman.AssetProviding.Foundation;
using UnityEngine;

namespace Arman.AssetProviding.Data
{
    [CreateAssetMenu(menuName = ContextMenuConsts.ROOT_CATEGORY + "Asset Provider Service Config")]
    public class BasicAssetProviderServiceConfig : ScriptableObject
    {
        [SerializeField] AssetProviderConfig[] syncProviders;
        [SerializeField] AssetProviderConfig[] asyncProviders;

        public ChainedAssetProviderService CreateAssetProviderService()
        {
            var service = new ChainedAssetProviderService();

            foreach (var config in syncProviders)
                service.AddSyncProvider(config.CreateSyncProvider());

            foreach (var config in asyncProviders)
                service.AddAsyncProvider(config.CreateAsyncProvider());

            return service;
        }
    }
}