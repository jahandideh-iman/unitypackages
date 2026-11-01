using UnityEngine;
using UnityEngine.Serialization;

namespace Arman.UIManagement
{
    public class PopupWindow : Window
    {
        [FormerlySerializedAs("closeOnBackButtonPressed")]
        [SerializeField]
        bool CloseOnBackButtonPressed;

        [field: SerializeField]
        public Transition? InTransition { get; private set; }

        [field: SerializeField]
        public Transition? OutTransition { get; private set; }

        protected override void InternalInit(UIManager manager) { }

        public override void OnBackButtonPressed()
        {
            if (CloseOnBackButtonPressed)
                Close();
        }

        public void Close()
        {
            UIManager.Close(this);
        }
    }
}
