using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static Arman.UIManagement.Tests.UnityComponentCreationExtensions;

namespace Arman.UIManagement.Tests
{
    public class UIManagerTests
    {
        private const int _popupSortingOffset = 10;

        private UIManager _manager = null!;
        private Panel _backgroundPanel = null!;

        [SetUp]
        public void SetUp()
        {
            CreateGameObject(
                "UIManager",
                go =>
                {
                    go.Child(
                        "PopupBackgroundPanel",
                        go =>
                        {
                            go.AddComponent<Panel>(preAwake: panel =>
                                {
                                    SetField(panel, "backgroundImage", go.AddComponent<Image>());
                                })
                                .Out(out _backgroundPanel);
                        }
                    );
                    go.AddComponent<UIManager>(preAwake: manager =>
                        {
                            SetField(manager, "popupBackgroundPanel", _backgroundPanel);
                            SetField(manager, "sortingOffsetBetweenPopups", _popupSortingOffset);
                        })
                        .Out(out _manager);
                }
            );
            _manager.Init();

            // Update polls the legacy Input class, which throws when the project uses
            // the Input System package. Escape handling is not under test, so keep
            // Update from running across the frames the Close tests wait for.
            _manager.enabled = false;
        }

        [TearDown]
        public void Teardown()
        {
            CleanUpScene();
        }

        [Test]
        public void Init_HidesThePopupBackgroundPanel()
        {
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SetMainWindow_ExposesTheWindowAsMainWindow()
        {
            var main = CreateWindow("Main");

            _manager.SetMainWindow(main);

            Assert.That(_manager.MainWindow(), Is.SameAs(main));
        }

        [Test]
        public void BackgroundPanel_ReturnsThePanelItWasConfiguredWith()
        {
            Assert.That(_manager.BackgroundPanel(), Is.SameAs(_backgroundPanel));
        }

        [Test]
        public void OpenPopUp_ParentsThePopupUnderTheManager()
        {
            _manager.SetMainWindow(CreateWindow("Main"));

            var popup = _manager.OpenPopUp(CreateWindow("Popup"));

            Assert.That(popup.transform.parent, Is.SameAs(_manager.MainTransform()));
        }

        [Test]
        public void OpenPopUp_SortsEachPopupAboveTheFocusedWindow()
        {
            var main = CreateWindow("Main");
            _manager.SetMainWindow(main);

            var first = _manager.OpenPopUp(CreateWindow("First"));
            var second = _manager.OpenPopUp(CreateWindow("Second"));

            Assert.That(
                first.SortingOrder(),
                Is.EqualTo(main.SortingOrder() + _popupSortingOffset)
            );
            Assert.That(
                second.SortingOrder(),
                Is.EqualTo(first.SortingOrder() + _popupSortingOffset)
            );
        }

        [Test]
        public void OpenPopUp_ShowsTheBackgroundPanelJustBehindThePopupAndFocusesIt()
        {
            _manager.SetMainWindow(CreateWindow("Main"));

            var popup = _manager.OpenPopUp(CreateWindow("Popup"));

            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(popup.SortingOrder() - 1));
            Assert.That(popup.FocusedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CloseingAFocusedPopup_DestroysItAndHidesThePanel()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = _manager.OpenPopUp(CreateWindow("Popup"));

            _manager.Close(popup);
            yield return null;
            Assert.That(popup == null, Is.True);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ClosingAFocusedPopup_FocusesThePopupBelowIt()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var lower = _manager.OpenPopUp(CreateWindow("Lower"));
            var upper = _manager.OpenPopUp(CreateWindow("Upper"));
            var lowerFocusedBefore = lower.FocusedCount;

            _manager.Close(upper);
            yield return null;

            Assert.That(upper == null, Is.True);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(lower.SortingOrder() - 1));
            Assert.That(lower.FocusedCount, Is.EqualTo(lowerFocusedBefore + 1));
        }

        [UnityTest]
        public IEnumerator ClosingANonFocusedPopup_DestroysItAndKeepsTheFocusedPopup()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var lower = _manager.OpenPopUp(CreateWindow("Lower"));
            var upper = _manager.OpenPopUp(CreateWindow("Upper"));

            _manager.Close(lower);
            yield return null;

            Assert.That(lower == null, Is.True);
            Assert.That(upper == null, Is.False);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(upper.SortingOrder() - 1));
        }

        [UnityTest]
        public IEnumerator ClosingNonFocusedPopup_ThenFocusedPopup_HidesThePanel()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var lower = _manager.OpenPopUp(CreateWindow("Lower"));
            var upper = _manager.OpenPopUp(CreateWindow("Upper"));

            _manager.Close(lower);
            _manager.Close(upper);
            yield return null;

            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ClosingPopupsInOpeningOrder_HidesThePanelOnlyAfterTheLast()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var first = _manager.OpenPopUp(CreateWindow("First"));
            var second = _manager.OpenPopUp(CreateWindow("Second"));
            var third = _manager.OpenPopUp(CreateWindow("Third"));

            _manager.Close(first);
            _manager.Close(second);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            _manager.Close(third);
            yield return null;

            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ClosingAWindowThatIsNotOnTheStack_LeavesTheStackIntact()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = _manager.OpenPopUp(CreateWindow("Popup"));
            var stranger = CreateWindow("Stranger");

            _manager.Close(stranger);
            yield return null;

            Assert.That(stranger == null, Is.True);
            Assert.That(popup == null, Is.False);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(popup.SortingOrder() - 1));
        }

        [UnityTest]
        public IEnumerator SetMainWindow_WithPopupsStillOpen_DestroysThePopupsAndHidesThePanel()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var lower = _manager.OpenPopUp(CreateWindow("Lower"));
            var upper = _manager.OpenPopUp(CreateWindow("Upper"));
            var newMain = CreateWindow("NewMain");

            _manager.SetMainWindow(newMain);
            yield return null;

            Assert.That(lower == null, Is.True);
            Assert.That(upper == null, Is.True);
            Assert.That(_manager.MainWindow(), Is.SameAs(newMain));
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void OpenPopUp_WithoutATransition_DoesNotBlockInput()
        {
            _manager.SetMainWindow(CreateWindow("Main"));

            _manager.OpenPopUp(CreateWindow("Popup"));

            Assert.That(_manager.IsInputBlocked(), Is.False);
        }

        [Test]
        public void OpenPopUp_WithATransition_BlocksInputUntilTheInTransitionCompletes()
        {
            _manager.SetMainWindow(CreateWindow("Main"));

            _manager.OpenPopUp(CreatePopupWithTransition("Popup", out var transition));

            Assert.That(transition.InCount, Is.EqualTo(1));
            Assert.That(_manager.IsInputBlocked(), Is.True);
            transition.FinishIn();
            Assert.That(_manager.IsInputBlocked(), Is.False);
        }

        // A headless test run never renders the canvases, so UGUI never assigns the
        // graphic depths an EventSystem raycast needs. This checks instead what makes
        // UGUI route every click to the blocker.
        [Test]
        public void OpenPopUp_WithATransition_ShowsATransparentRaycastTargetAboveThePopup()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = _manager.OpenPopUp(CreatePopupWithTransition("Popup", out var transition));

            var blocker = InputBlocker();
            Assert.That(blocker.isActiveAndEnabled, Is.True);
            Assert.That(blocker.raycastTarget, Is.True);
            Assert.That(blocker.color.a, Is.Zero);
            Assert.That(blocker.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(blocker.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(blocker.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(blocker.canvas.overrideSorting, Is.True);
            Assert.That(blocker.canvas.sortingOrder, Is.GreaterThan(popup.SortingOrder()));

            transition.FinishIn();

            Assert.That(blocker.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Close_WithATransition_KeepsThePopupAndPanelUntilTheOutTransitionCompletes()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = _manager.OpenPopUp(CreatePopupWithTransition("Popup", out var transition));
            transition.FinishIn();

            _manager.Close(popup);
            yield return null;

            Assert.That(transition.OutCount, Is.EqualTo(1));
            Assert.That(popup == null, Is.False);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            Assert.That(_manager.IsInputBlocked(), Is.True);

            transition.FinishOut();
            yield return null;

            Assert.That(popup == null, Is.True);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
            Assert.That(_manager.IsInputBlocked(), Is.False);
        }

        [Test]
        public void Close_WithATransition_FocusesThePopupBelowWithoutWaiting()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var lower = _manager.OpenPopUp(CreateWindow("Lower"));
            var upper = _manager.OpenPopUp(CreatePopupWithTransition("Upper", out var transition));
            transition.FinishIn();
            var lowerFocusedBefore = lower.FocusedCount;

            _manager.Close(upper);

            Assert.That(lower.FocusedCount, Is.EqualTo(lowerFocusedBefore + 1));
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(lower.SortingOrder() - 1));
        }

        [Test]
        public void Close_WhileTheOutTransitionRuns_DoesNotPlayItAgain()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = _manager.OpenPopUp(CreatePopupWithTransition("Popup", out var transition));
            transition.FinishIn();

            _manager.Close(popup);
            _manager.Close(popup);

            Assert.That(transition.OutCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Close_DuringTheInTransition_PlaysTheOutTransitionAndIgnoresTheLateInCompletion()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = _manager.OpenPopUp(CreatePopupWithTransition("Popup", out var transition));

            _manager.Close(popup);
            transition.FinishIn();

            Assert.That(transition.OutCount, Is.EqualTo(1));
            Assert.That(_manager.IsInputBlocked(), Is.True);

            transition.FinishOut();
            yield return null;

            Assert.That(popup == null, Is.True);
            Assert.That(_manager.IsInputBlocked(), Is.False);
        }

        [UnityTest]
        public IEnumerator ATransitionThatCompletesImmediately_NeverLeavesInputBlocked()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopupWithTransition("Popup", out var transition);
            transition.CompletesImmediately = true;

            _manager.OpenPopUp(popup);
            Assert.That(_manager.IsInputBlocked(), Is.False);
            _manager.Close(popup);
            yield return null;

            Assert.That(popup == null, Is.True);
            Assert.That(_manager.IsInputBlocked(), Is.False);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void TwoPopupsTransitioningIn_BlockInputUntilBothComplete()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            _manager.OpenPopUp(CreatePopupWithTransition("First", out var first));
            _manager.OpenPopUp(CreatePopupWithTransition("Second", out var second));

            first.FinishIn();
            Assert.That(_manager.IsInputBlocked(), Is.True);
            second.FinishIn();
            Assert.That(_manager.IsInputBlocked(), Is.False);
        }

        [Test]
        public void OpenPopUp_WhileAnotherPopupTransitionsOut_KeepsThePanelBehindTheNewPopup()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var closing = _manager.OpenPopUp(
                CreatePopupWithTransition("Closing", out var transition)
            );
            transition.FinishIn();
            _manager.Close(closing);

            var opened = _manager.OpenPopUp(CreateWindow("Opened"));
            transition.FinishOut();

            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(opened.SortingOrder() - 1));
        }

        [UnityTest]
        public IEnumerator SetMainWindow_DuringTransitions_DestroysEveryPopupAndUnblocksInput()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var closing = _manager.OpenPopUp(
                CreatePopupWithTransition("Closing", out var closingTransition)
            );
            closingTransition.FinishIn();
            _manager.Close(closing);
            var opening = _manager.OpenPopUp(CreatePopupWithTransition("Opening", out _));

            _manager.SetMainWindow(CreateWindow("NewMain"));
            yield return null;

            Assert.That(closing == null, Is.True);
            Assert.That(opening == null, Is.True);
            Assert.That(_manager.IsInputBlocked(), Is.False);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        private TestWindow CreateWindow(string name)
        {
            return CreateGameObject(name).AddComponent<TestWindow>();
        }

        private TestWindow CreatePopupWithTransition(string name, out FakeTransition transition)
        {
            var window = CreateWindow(name);
            transition = window.gameObject.AddComponent<FakeTransition>();
            return window;
        }

        // The blocker is the one Image under the manager that belongs to no window.
        private Image InputBlocker()
        {
            return _manager
                .GetComponentsInChildren<Image>(includeInactive: true)
                .Single(image => image.GetComponentInParent<Window>(includeInactive: true) == null);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target
                .GetType()
                .GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
            Assert.That(field, Is.Not.Null, $"{target.GetType().Name} has no field '{name}'");
            field!.SetValue(target, value);
        }

        private class TestWindow : Window
        {
            public int FocusedCount { get; private set; }

            public override void OnFocused()
            {
                FocusedCount++;
            }
        }

        // Holds each transition open until the test finishes it.
        private class FakeTransition : PopupTransition
        {
            private Action? _pendingIn;
            private Action? _pendingOut;

            public bool CompletesImmediately { get; set; }
            public int InCount { get; private set; }
            public int OutCount { get; private set; }

            public override void PlayIn(Action onComplete)
            {
                InCount++;
                if (CompletesImmediately)
                    onComplete();
                else
                    _pendingIn = onComplete;
            }

            public override void PlayOut(Action onComplete)
            {
                OutCount++;
                if (CompletesImmediately)
                    onComplete();
                else
                    _pendingOut = onComplete;
            }

            public void FinishIn()
            {
                Assert.That(_pendingIn, Is.Not.Null, "No in transition is running");
                _pendingIn!.Invoke();
            }

            public void FinishOut()
            {
                Assert.That(_pendingOut, Is.Not.Null, "No out transition is running");
                _pendingOut!.Invoke();
            }
        }
    }
}
