using UnityEngine;

namespace Arman.SceneManagement
{
    [DefaultExecutionOrder(-100)]
    public abstract class SceneInitializer : MonoBehaviour
    {
        private void Awake()
        {
            Init();
        }

        protected abstract void Init();
    }
}
