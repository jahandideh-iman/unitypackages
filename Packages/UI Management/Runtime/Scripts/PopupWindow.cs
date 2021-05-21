using UnityEngine;

namespace Arman.UIManagement
{
    public class PopupWindow : Window
    {
        [SerializeField] bool closeOnBackButtonPressed;

        protected override void InternalInit(UIManager manager)
        {
        }

        public override void OnBackButtonPressed()
        {
            if (closeOnBackButtonPressed)
                Close();
        }

        public void Close()
        {
            uiManager.Close(this);
        }
    }
}