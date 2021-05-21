using UnityEngine;
using UnityEngine.UI;

namespace Arman.UIManagement
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class Window : UIElement
    {
        protected UIManager uiManager;

        private Canvas canvas;

        public void Init(UIManager manager)
        {
            canvas = GetComponent<Canvas>();
            this.uiManager = manager;
            InternalInit(manager);
        }

        protected virtual void InternalInit(UIManager manager)
        {

        }

        virtual public void OnBackButtonPressed()
        {

        }

        public void SetSortingOrder(int order)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
        }

        public int SortingOrder()
        {
            return canvas.sortingOrder;
        }
    }
}