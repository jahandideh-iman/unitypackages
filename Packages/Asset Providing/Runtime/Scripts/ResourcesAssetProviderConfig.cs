using UnityEngine;
using UnityEngine.Serialization;

namespace Arman.AssetProviding
{
    [CreateAssetMenu(menuName = ContextMenuConsts.PROVIDER_CATEGORY + "Resources Folder Provider")]
    public class ResourcesAssetProviderConfig : AssetProviderConfig
    {
        [FormerlySerializedAs("resourcesPathPrefix")]
        [SerializeField]
        string ResourcesPathPrefix;

        public override IAsyncUnityAssetProvider CreateAsyncProvider()
        {
            return new ResourcesAssetProvider(ResourcesPathPrefix);
        }

        public override ISyncUnityAssetProvider CreateSyncProvider()
        {
            return new ResourcesAssetProvider(ResourcesPathPrefix);
        }
    }
}
