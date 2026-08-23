using Arman.Foundation.Core.ServiceLocating;

namespace Arman.AssetProviding.Foundation
{
    public interface IAssetProviderService : IService
    {
        ISyncUnityAssetProvider ISyncUnityAssetProvider { get; }
        IAsyncUnityAssetProvider IAsyncUnityAssetProvider { get; }
    }
}