namespace Arman.AssetProviding.Foundation
{
    public interface IAssetProviderService
    {
        ISyncUnityAssetProvider ISyncUnityAssetProvider { get; }
        IAsyncUnityAssetProvider IAsyncUnityAssetProvider { get; }
    }
}