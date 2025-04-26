using System;
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

        internal void Init(UIManager manager)
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

        virtual public void OnFocused()
        {
        }

        public void SetSorting(int order, int layer)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            canvas.sortingLayerID = layer;
        }

        public int SortingOrder()
        {
            return canvas.sortingOrder;
        }

    }
}