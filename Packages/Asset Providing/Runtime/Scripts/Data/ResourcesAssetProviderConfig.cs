using Arman.AssetProviding.Foundation;
using UnityEngine;

namespace Arman.AssetProviding.Data
{
    [CreateAssetMenu(menuName = ContextMenuConsts.PROVIDER_CATEGORY + "Resources Folder Provider")]
    public class ResourcesAssetProviderConfig : AssetProviderConfig
    {
        [SerializeField] string resourcesPathPrefix;

        public override AsyncUnityAssetProvider CreateAsyncProvider()
        {
            return new ResourcesAssetProvider(resourcesPathPrefix);
        }

        public override SyncUnityAssetProvider CreateSyncProvider()
        {
            return new ResourcesAssetProvider(resourcesPathPrefix);
        }
    }
}