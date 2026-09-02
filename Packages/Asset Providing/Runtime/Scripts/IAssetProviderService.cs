namespace Arman.AssetProviding
{
    public interface IAssetProviderService
    {
        ISyncUnityAssetProvider ISyncUnityAssetProvider { get; }
        IAsyncUnityAssetProvider IAsyncUnityAssetProvider { get; }
    }
}