using System.Collections;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding
{
    public interface ISyncUnityAssetProvider
    {
        T LoadAssetByType<T>() where T : Object;
        T LoadAssetById<T>(string id) where T : Object;
    }
}