namespace Arman.AssetProviding
{
    public class ChainedAssetProviderService : IAssetProviderService
    {
        public ISyncUnityAssetProvider ISyncUnityAssetProvider => _rootSyncAssetProvider;

        public IAsyncUnityAssetProvider IAsyncUnityAssetProvider => _rootAsyncAssetProvider;

        ChainedSyncUnityAssetProvider _rootSyncAssetProvider = new ChainedSyncUnityAssetProvider();
        ChainedAsyncUnityAssetProvider _rootAsyncAssetProvider =
            new ChainedAsyncUnityAssetProvider();

        public ChainedAssetProviderService() { }

        public void AddSyncProvider(ISyncUnityAssetProvider syncProvider)
        {
            _rootSyncAssetProvider.Add(syncProvider);
        }

        public void AddAsyncProvider(IAsyncUnityAssetProvider asyncProvider)
        {
            _rootAsyncAssetProvider.Add(asyncProvider);
        }
    }
}
