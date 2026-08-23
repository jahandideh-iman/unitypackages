
namespace Arman.SceneMangement
{
    public class SceneManager
    {
        public void Open(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
