using UnityEngine;

namespace Arman.UIManagement
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Panel : Window
    {
        
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

    }
}