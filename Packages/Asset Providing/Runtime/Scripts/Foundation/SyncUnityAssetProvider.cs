using System.Collections;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding.Foundation
{
    public interface SyncUnityAssetProvider
    {
        T LoadAssetByType<T>() where T : Object;
        T LoadAssetById<T>(string id) where T : Object;
    }
}