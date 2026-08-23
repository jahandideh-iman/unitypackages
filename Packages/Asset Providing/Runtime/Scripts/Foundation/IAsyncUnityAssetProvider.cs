using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Arman.AssetProviding.Foundation
{
    public interface IAsyncUnityAssetProvider
    {
        Task<T> LoadAssetByTypeAsync<T>() where T : Object;
        Task<T> LoadAssetByIdAsync<T>(string id) where T : Object;
    }
}