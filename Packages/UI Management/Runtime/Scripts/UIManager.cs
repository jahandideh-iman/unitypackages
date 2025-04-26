using UnityEngine;
using System.Collections.Generic;
using Arman.Foundation.Core.ServiceLocating;

namespace Arman.UIManagement
{
    [RequireComponent(typeof(Canvas))]
    public class UIManager : MonoBehaviour, Service
    {
        [SerializeField] private Panel popupBackgroundPanel = default;
        [SerializeField] private int sortingOffsetBetweenPopups = default;

        private Canvas canvas;
        private Window mainWindow;

        private readonly Stack<Window> windowsStack = new Stack<Window>();

        void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        public void Init()
        {
            popupBackgroundPanel.Init(this);
            HidePopupPanel();
        }

        public void SetMainWindow(Window window)
        {
            System.Diagnostics.Debug.Assert(window != null, "Main window must not be null");

            this.mainWindow = window;
            ClearLingeringWindows();
            PushOnStack(window);

            this.mainWindow.Init(this);
        }

        public Window MainWindow()
        {
            return mainWindow;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CurrentFocusedWindow().OnBackButtonPressed();
        }

        public T OpenPopUp<T>(T popup) where T : Window
        {
            AttachToSelf(popup);
            popup.Init(this);
            SetPopupSortingOrder(popup);
            PushOnStack(popup);
            FocusPopupPanelOn(popup);
            return popup;
        }

        private void AttachToSelf(Window popup)
        {
            popup.transform.SetParent(MainTransform(), false);
        }

        private void SetPopupSortingOrder(Window popup)
        {
            popup.SetSorting(
                CurrentFocusedWindow().SortingOrder() + sortingOffsetBetweenPopups,
                canvas.sortingLayerID);
        }

        private void FocusPopupPanelOn(Window window)
        {
            popupBackgroundPanel.SetVisible(true);
            popupBackgroundPanel.RestoreAlpha();
            popupBackgroundPanel.SetSorting(window.SortingOrder() - 1, canvas.sortingLayerID);

            window.OnFocused();
        }

        public void Close(Window window)
        {
            if (IsNotFocused(window))
                return;

            windowsStack.Pop();
            DestroyWindow(window);

            if (FocusedWindowIsMainWindow())
                HidePopupPanel();
            else
                FocusPopupPanelOn(CurrentFocusedWindow());
            
        }

        public void SetMainCamera(Camera camera)
        {
            canvas.worldCamera = camera;
        }

        private void HidePopupPanel()
        {
            popupBackgroundPanel.SetVisible(false);
        }

        private bool FocusedWindowIsMainWindow()
        {
            return CurrentFocusedWindow() == mainWindow;
        }

        private bool IsNotFocused(Window window)
        {
            return CurrentFocusedWindow() != window;
        }

        private Window CurrentFocusedWindow()
        {
            return windowsStack.Peek();
        }

        private void PushOnStack(Window window)
        {
            windowsStack.Push(window);
        }

        private void ClearLingeringWindows()
        {
            // It is assumed that Main Window will be destroyed on its own.
            while (windowsStack.Count > 1)
                DestroyWindow(windowsStack.Pop());

            windowsStack.Clear();
            HidePopupPanel();
        }

        private void DestroyWindow(Window window)
        {
            Destroy(window.gameObject);
        }

        public Transform MainTransform()
        {
            return this.transform;
        }

        public Panel BackgroundPanel()
        {
            return popupBackgroundPanel;
        }
    }
}