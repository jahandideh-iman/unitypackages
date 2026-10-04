using UnityEngine;

namespace Arman.UIManagement
{
    public class PopupWindow : Window
    {
        [SerializeField]
        bool closeOnBackButtonPressed;

        [field: SerializeField]
        public Transition? InTransition { get; private set; }

        [field: SerializeField]
        public Transition? OutTransition { get; private set; }

        protected override void InternalInit(UIManager manager) { }

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
