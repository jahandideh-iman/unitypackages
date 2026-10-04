using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.UIManagement
{
    [RequireComponent(typeof(Canvas))]
    public class UIManager : MonoBehaviour
    {
        [SerializeField]
        private Panel popupBackgroundPanel = default;

        [SerializeField]
        private int sortingOffsetBetweenPopups = default;

        private Canvas canvas;
        private Window mainWindow;
        private GameObject inputBlocker;
        private Canvas inputBlockerCanvas;

        private readonly List<Window> windowsStack = new();
        private readonly List<RunningTransition> runningTransitions = new();

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            CreateInputBlocker();
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
            if (IsInputBlocked())
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                CurrentFocusedWindow().OnBackButtonPressed();
        }

        public T OpenPopUp<T>(T popup)
            where T : Window
        {
            System.Diagnostics.Debug.Assert(mainWindow != null, "Main window must not be null");
            AttachToSelf(popup);
            popup.Init(this);
            SetPopupSortingOrder(popup);
            PushOnStack(popup);
            FocusPopupPanelOn(popup);

            var transition = popup.GetComponent<PopupTransition>();
            if (transition != null)
                PlayTransition(popup, transition.PlayIn, isClosing: false, onComplete: null);

            return popup;
        }

        public bool IsInputBlocked()
        {
            return runningTransitions.Count > 0;
        }

        private void AttachToSelf(Window popup)
        {
            popup.transform.SetParent(MainTransform(), false);
        }

        private void SetPopupSortingOrder(Window popup)
        {
            popup.SetSorting(
                CurrentFocusedWindow().SortingOrder() + sortingOffsetBetweenPopups,
                canvas.sortingLayerID
            );
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
            if (IsClosing(window))
                return;

            windowsStack.Remove(window);

            var transition = window.GetComponent<PopupTransition>();
            if (transition == null)
                DestroyWindow(window);
            else
                PlayTransition(
                    window,
                    transition.PlayOut,
                    isClosing: true,
                    onComplete: () =>
                    {
                        if (window != null)
                            DestroyWindow(window);
                        HidePopupPanelIfNothingIsShowing();
                    }
                );

            if (FocusedWindowIsMainWindow())
                HidePopupPanelIfNothingIsShowing();
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

        // A popup playing its out transition stays on screen after leaving the
        // stack, so the panel stays behind it until the transition ends.
        private void HidePopupPanelIfNothingIsShowing()
        {
            if (FocusedWindowIsMainWindow() && !runningTransitions.Any(r => r.IsClosing))
                HidePopupPanel();
        }

        private void PlayTransition(
            Window window,
            Action<Action> play,
            bool isClosing,
            Action? onComplete
        )
        {
            // A new transition replaces any still running on the same window, and
            // the replaced one's late completion is ignored.
            runningTransitions.RemoveAll(r => ReferenceEquals(r.Window, window));
            var running = new RunningTransition(window, isClosing);
            runningTransitions.Add(running);
            RefreshInputBlocker();

            play(() =>
            {
                if (this == null || !runningTransitions.Remove(running))
                    return;

                RefreshInputBlocker();
                onComplete?.Invoke();
            });
        }

        private bool IsClosing(Window window)
        {
            return runningTransitions.Any(r => r.IsClosing && ReferenceEquals(r.Window, window));
        }

        private void CreateInputBlocker()
        {
            inputBlocker = new GameObject("PopupTransitionInputBlocker", typeof(RectTransform));
            var rect = (RectTransform)inputBlocker.transform;
            rect.SetParent(MainTransform(), false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            inputBlockerCanvas = inputBlocker.AddComponent<Canvas>();
            inputBlocker.AddComponent<GraphicRaycaster>();
            inputBlocker.AddComponent<Image>().color = Color.clear;
            inputBlocker.SetActive(false);
        }

        private void RefreshInputBlocker()
        {
            var blocked = IsInputBlocked();
            inputBlocker.SetActive(blocked);
            if (!blocked)
                return;

            // Above every popup, whatever their sorting orders.
            inputBlockerCanvas.overrideSorting = true;
            inputBlockerCanvas.sortingOrder = short.MaxValue;
            inputBlockerCanvas.sortingLayerID = canvas.sortingLayerID;
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
            return windowsStack.Last();
        }

        private void PushOnStack(Window window)
        {
            windowsStack.Add(window);
        }

        private void ClearLingeringWindows()
        {
            // It is assumed that Main Window will be destroyed on its own.
            while (windowsStack.Count > 1)
            {
                var window = windowsStack.Last();
                windowsStack.RemoveAt(windowsStack.Count - 1);
                DestroyWindow(window);
            }

            windowsStack.Clear();

            foreach (var running in runningTransitions)
            {
                if (running.IsClosing && running.Window != null)
                    DestroyWindow(running.Window);
            }
            runningTransitions.Clear();
            RefreshInputBlocker();

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

        private sealed class RunningTransition
        {
            public RunningTransition(Window window, bool isClosing)
            {
                Window = window;
                IsClosing = isClosing;
            }

            public Window Window { get; }
            public bool IsClosing { get; }
        }
    }
}
