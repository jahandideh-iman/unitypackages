namespace Arman.AssetProviding.Foundation
{
    public class ChainedAssetProviderService : AssetProviderService
    {
        public SyncUnityAssetProvider SyncUnityAssetProvider => rootSyncAssetProvider;

        public AsyncUnityAssetProvider AsyncUnityAssetProvider => rootAsyncAssetProvider;

        ChainedSyncUnityAssetProvider rootSyncAssetProvider = new ChainedSyncUnityAssetProvider();
        ChainedAsyncUnityAssetProvider rootAsyncAssetProvider = new ChainedAsyncUnityAssetProvider();

        public ChainedAssetProviderService()
        {

        }

        public void AddSyncProvider(SyncUnityAssetProvider syncProvider)
        {
            rootSyncAssetProvider.Add(syncProvider);
        }

        public void AddAsyncProvider(AsyncUnityAssetProvider asyncProvider)
        {
            rootAsyncAssetProvider.Add(asyncProvider);
        }

    }
}