namespace Arman.AssetProviding.Foundation
{
    public class ChainedAssetProviderService : IAssetProviderService
    {
        public ISyncUnityAssetProvider ISyncUnityAssetProvider => rootSyncAssetProvider;

        public IAsyncUnityAssetProvider IAsyncUnityAssetProvider => rootAsyncAssetProvider;

        ChainedSyncUnityAssetProvider rootSyncAssetProvider = new ChainedSyncUnityAssetProvider();
        ChainedAsyncUnityAssetProvider rootAsyncAssetProvider = new ChainedAsyncUnityAssetProvider();

        public ChainedAssetProviderService()
        {

        }

        public void AddSyncProvider(ISyncUnityAssetProvider syncProvider)
        {
            rootSyncAssetProvider.Add(syncProvider);
        }

        public void AddAsyncProvider(IAsyncUnityAssetProvider asyncProvider)
        {
            rootAsyncAssetProvider.Add(asyncProvider);
        }

    }
}