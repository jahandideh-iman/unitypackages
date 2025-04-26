
using Arman.Foundation.Core.ServiceLocating;

namespace Arman.SceneMangement
{
    public class SceneManager : Service
    {
        public void Open(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
