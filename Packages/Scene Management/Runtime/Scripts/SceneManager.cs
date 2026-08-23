
using Arman.Foundation.Core.ServiceLocating;

namespace Arman.SceneMangement
{
    public class SceneManager : IService
    {
        public void Open(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
