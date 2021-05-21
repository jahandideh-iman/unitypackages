using UnityEngine;

namespace Arman.UIManagement
{
    public class UIElement : MonoBehaviour
    {
        public void Destroy()
        {
            GameObject.Destroy(this);
        }

        protected virtual void InternalOnDestroy()
        {

        }

        public void OnDestroy()
        {
            InternalOnDestroy();
        }
    }
}