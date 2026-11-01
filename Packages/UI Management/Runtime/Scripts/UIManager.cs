using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Arman.UIManagement
{
    [RequireComponent(typeof(Canvas))]
    public class UIManager : MonoBehaviour
    {
        [FormerlySerializedAs("popupBackgroundPanel")]
        [SerializeField]
        private Panel PopupBackgroundPanel = default;

        [FormerlySerializedAs("sortingOffsetBetweenPopups")]
        [SerializeField]
        private int SortingOffsetBetweenPopups = default;

        private Canvas _canvas;
        private Window _mainWindow;

        private readonly List<Window> _windowsStack = new();

        private readonly Dictionary<PopupWindow, CancellationTokenSource> _runningTransitions =
            new();
        private readonly HashSet<PopupWindow> _closingPopups = new();
        private Canvas _inputBlocker = null!;

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        public void Init()
        {
            PopupBackgroundPanel.Init(this);
            HidePopupPanel();
            _inputBlocker = CreateInputBlocker(MainTransform());
        }

        public void SetMainWindow(Window window)
        {
            Assert(window != null, "Main window must not be null");

            this._mainWindow = window;
            ClearLingeringWindows();
            PushOnStack(window);

            this._mainWindow.Init(this);
        }

        public Window MainWindow()
        {
            return _mainWindow;
        }

        void Update()
        {
            if (IsTransitioning())
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                CurrentFocusedWindow().OnBackButtonPressed();
        }

        public bool IsTransitioning()
        {
            return _runningTransitions.Count > 0;
        }

        public T OpenPopUp<T>(T popup)
            where T : PopupWindow
        {
            Assert(_mainWindow != null, "Main window must not be null");
            AttachToSelf(popup);
            popup.Init(this);
            SetPopupSortingOrder(popup);
            PushOnStack(popup);
            FocusPopupPanelOn(popup);
            if (popup.InTransition != null)
            {
                PlayTransition(popup, popup.InTransition).Forget();
            }
            return popup;
        }

        private void AttachToSelf(Window popup)
        {
            popup.transform.SetParent(MainTransform(), false);
        }

        private void SetPopupSortingOrder(Window popup)
        {
            popup.SetSorting(
                CurrentFocusedWindow().SortingOrder() + SortingOffsetBetweenPopups,
                _canvas.sortingLayerID
            );
        }

        private void FocusPopupPanelOn(Window window)
        {
            PopupBackgroundPanel.SetVisible(true);
            PopupBackgroundPanel.RestoreAlpha();
            PopupBackgroundPanel.SetSorting(window.SortingOrder() - 1, _canvas.sortingLayerID);

            window.OnFocused();
        }

        public void Close(PopupWindow window)
        {
            Assert(_windowsStack.Contains(window), "Window must be in the stack to close it");
            Assert(!_closingPopups.Contains(window), "Window is already closing");

            InternalClose().Forget();

            async UniTask InternalClose()
            {
                if (window.OutTransition != null)
                {
                    _closingPopups.Add(window);
                    await PlayTransition(window, window.OutTransition);
                    // SetMainWindow and OnDestroy abandon pending closes by emptying the set.
                    if (!_closingPopups.Remove(window))
                        return;
                }

                _windowsStack.Remove(window);
                DestroyWindow(window);
                if (FocusedWindowIsMainWindow())
                    HidePopupPanel();
                else
                    FocusPopupPanelOn(CurrentFocusedWindow());
            }
        }

        private async UniTask PlayTransition(PopupWindow popup, Transition transition)
        {
            CancelTransitionOf(popup);
            // Not disposed: its only registration is on the popup's destroy token, which goes
            // away with the popup, and the source may be disposed from inside its own Cancel.
            var source = CancellationTokenSource.CreateLinkedTokenSource(
                popup.GetCancellationTokenOnDestroy()
            );
            _runningTransitions[popup] = source;
            RefreshInputBlocker();
            try
            {
                await transition.Play(source.Token).AttachExternalCancellation(source.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (_runningTransitions.TryGetValue(popup, out var current) && current == source)
                    _runningTransitions.Remove(popup);
                RefreshInputBlocker();
            }
        }

        private void CancelTransitionOf(PopupWindow popup)
        {
            if (_runningTransitions.TryGetValue(popup, out var source))
                source.Cancel();
        }

        private void CancelAllTransitions()
        {
            if (_runningTransitions.Count == 0)
                return;

            // Cancel runs each transition's cleanup synchronously, which edits the dictionary.
            foreach (var source in _runningTransitions.Values.ToList())
                source.Cancel();
            _runningTransitions.Clear();
            RefreshInputBlocker();
        }

        private void RefreshInputBlocker()
        {
            if (_inputBlocker == null)
                return;

            var blocking = IsTransitioning();
            if (_inputBlocker.gameObject.activeSelf == blocking)
                return;

            _inputBlocker.gameObject.SetActive(blocking);
            if (blocking)
            {
                // Set on every show, as Window.SetSorting is, so the order survives reactivation.
                _inputBlocker.overrideSorting = true;
                _inputBlocker.sortingLayerID = _canvas.sortingLayerID;
                _inputBlocker.sortingOrder = short.MaxValue;
            }
        }

        public void SetMainCamera(Camera camera)
        {
            _canvas.worldCamera = camera;
        }

        private void HidePopupPanel()
        {
            PopupBackgroundPanel.SetVisible(false);
        }

        private bool FocusedWindowIsMainWindow()
        {
            return CurrentFocusedWindow() == _mainWindow;
        }

        private bool IsNotFocused(Window window)
        {
            return CurrentFocusedWindow() != window;
        }

        private Window CurrentFocusedWindow()
        {
            return _windowsStack.Last();
        }

        private void PushOnStack(Window window)
        {
            _windowsStack.Add(window);
        }

        private void ClearLingeringWindows()
        {
            // Emptied before cancelling, so the cancelled closes find nothing left to do.
            _closingPopups.Clear();
            CancelAllTransitions();

            // It is assumed that Main Window will be destroyed on its own.
            for (int i = 1; i < _windowsStack.Count; i++)
                DestroyWindow(_windowsStack[i]);
            //var lingering = windowsStack.Skip(1).ToList();
            _windowsStack.Clear();

            HidePopupPanel();
        }

        private void OnDestroy()
        {
            _closingPopups.Clear();
            CancelAllTransitions();
        }

        private void DestroyWindow(Window window)
        {
            if (window != null)
                Destroy(window.gameObject);
        }

        public Transform MainTransform()
        {
            return this.transform;
        }

        public Panel BackgroundPanel()
        {
            return PopupBackgroundPanel;
        }

        private static Canvas CreateInputBlocker(Transform parent)
        {
            var blocker = new GameObject("PopupInputBlocker", typeof(RectTransform));
            var rect = (RectTransform)blocker.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = blocker.AddComponent<Canvas>();
            blocker.AddComponent<GraphicRaycaster>();
            var image = blocker.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            blocker.SetActive(false);

            return canvas;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
