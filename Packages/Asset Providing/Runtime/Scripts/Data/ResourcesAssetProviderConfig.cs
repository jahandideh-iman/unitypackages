using Arman.AssetProviding.Foundation;
using UnityEngine;

namespace Arman.AssetProviding.Data
{
    [CreateAssetMenu(menuName = ContextMenuConsts.PROVIDER_CATEGORY + "Resources Folder Provider")]
    public class ResourcesAssetProviderConfig : AssetProviderConfig
    {
        [SerializeField] string resourcesPathPrefix;

        public override IAsyncUnityAssetProvider CreateAsyncProvider()
        {
            return new ResourcesAssetProvider(resourcesPathPrefix);
        }

        public override ISyncUnityAssetProvider CreateSyncProvider()
        {
            return new ResourcesAssetProvider(resourcesPathPrefix);
        }
    }
}