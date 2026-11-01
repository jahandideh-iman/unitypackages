using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.UIManagement
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public abstract class Window : UIElement
    {
        protected UIManager UIManager;

        private Canvas _canvas;

        internal void Init(UIManager manager)
        {
            _canvas = GetComponent<Canvas>();
            this.UIManager = manager;
            InternalInit(manager);
        }

        protected virtual void InternalInit(UIManager manager) { }

        public virtual void OnBackButtonPressed() { }

        public virtual void OnFocused() { }

        public void SetSorting(int order, int layer)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = order;
            _canvas.sortingLayerID = layer;
        }

        public int SortingOrder()
        {
            return _canvas.sortingOrder;
        }
    }
}
