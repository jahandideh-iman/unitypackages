using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
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

            var popup = _manager.OpenPopUp(CreatePopup("Popup"));

            Assert.That(popup.transform.parent, Is.SameAs(_manager.MainTransform()));
        }

        [Test]
        public void OpenPopUp_SortsEachPopupAboveTheFocusedWindow()
        {
            var main = CreateWindow("Main");
            _manager.SetMainWindow(main);

            var first = _manager.OpenPopUp(CreatePopup("First"));
            var second = _manager.OpenPopUp(CreatePopup("Second"));

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

            var popup = _manager.OpenPopUp(CreatePopup("Popup"));

            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(popup.SortingOrder() - 1));
            Assert.That(popup.FocusedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CloseingAFocusedPopup_DestroysItAndHidesThePanel()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = _manager.OpenPopUp(CreatePopup("Popup"));

            _manager.Close(popup);
            yield return null;
            Assert.That(popup == null, Is.True);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ClosingAFocusedPopup_FocusesThePopupBelowIt()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var lower = _manager.OpenPopUp(CreatePopup("Lower"));
            var upper = _manager.OpenPopUp(CreatePopup("Upper"));
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
            var lower = _manager.OpenPopUp(CreatePopup("Lower"));
            var upper = _manager.OpenPopUp(CreatePopup("Upper"));

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
            var lower = _manager.OpenPopUp(CreatePopup("Lower"));
            var upper = _manager.OpenPopUp(CreatePopup("Upper"));

            _manager.Close(lower);
            _manager.Close(upper);
            yield return null;

            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ClosingPopupsInOpeningOrder_HidesThePanelOnlyAfterTheLast()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var first = _manager.OpenPopUp(CreatePopup("First"));
            var second = _manager.OpenPopUp(CreatePopup("Second"));
            var third = _manager.OpenPopUp(CreatePopup("Third"));

            _manager.Close(first);
            _manager.Close(second);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            _manager.Close(third);
            yield return null;

            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ClosingAWindowThatIsNotOnTheStack_ThrowsAndLeavesTheStackIntact()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = _manager.OpenPopUp(CreatePopup("Popup"));
            var stranger = CreatePopup("Stranger");

            Assert.That(() => _manager.Close(stranger), Throws.InvalidOperationException);
            yield return null;

            Assert.That(stranger == null, Is.False);
            Assert.That(popup == null, Is.False);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.True);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(popup.SortingOrder() - 1));
        }

        [UnityTest]
        public IEnumerator SetMainWindow_WithPopupsStillOpen_DestroysThePopupsAndHidesThePanel()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var lower = _manager.OpenPopUp(CreatePopup("Lower"));
            var upper = _manager.OpenPopUp(CreatePopup("Upper"));
            var newMain = CreateWindow("NewMain");

            _manager.SetMainWindow(newMain);
            yield return null;

            Assert.That(lower == null, Is.True);
            Assert.That(upper == null, Is.True);
            Assert.That(_manager.MainWindow(), Is.SameAs(newMain));
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Init_CreatesAnInactiveTransparentInputBlocker()
        {
            var blocker = InputBlocker();

            Assert.That(blocker.activeSelf, Is.False);
            Assert.That(blocker.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            var image = blocker.GetComponent<Image>();
            Assert.That(image.color.a, Is.EqualTo(0f));
            Assert.That(image.raycastTarget, Is.True);
        }

        [Test]
        public void OpenPopUp_WithAnInTransition_BlocksInputAboveAllPopupsUntilItCompletes()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopup("Popup");
            var transition = AddInTransition(popup);

            _manager.OpenPopUp(popup);

            Assert.That(transition.PlayCount, Is.EqualTo(1));
            Assert.That(_manager.IsTransitioning(), Is.True);
            Assert.That(InputBlocker().activeSelf, Is.True);
            var blockerCanvas = InputBlocker().GetComponent<Canvas>();
            Assert.That(blockerCanvas.overrideSorting, Is.True);
            Assert.That(blockerCanvas.sortingOrder, Is.EqualTo(short.MaxValue));

            transition.Complete();

            Assert.That(_manager.IsTransitioning(), Is.False);
            Assert.That(InputBlocker().activeSelf, Is.False);
        }

        [Test]
        public void OpenPopUp_WithAnInTransition_FocusesThePopupBeforeTheTransitionEnds()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopup("Popup");
            var transition = AddInTransition(popup);

            _manager.OpenPopUp(popup);

            Assert.That(popup.FocusedCount, Is.EqualTo(1));
            transition.Complete();
        }

        [Test]
        public void OpenPopUp_WithATransitionThatCompletesSynchronously_NeverBlocksInput()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopup("Popup");
            var transition = AddInTransition(popup);
            transition.CompletesSynchronously = true;

            _manager.OpenPopUp(popup);

            Assert.That(transition.PlayCount, Is.EqualTo(1));
            Assert.That(_manager.IsTransitioning(), Is.False);
            Assert.That(InputBlocker().activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Close_WithAnOutTransition_KeepsThePopupOnTheStackUntilItCompletes()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var lower = _manager.OpenPopUp(CreatePopup("Lower"));
            var upper = CreatePopup("Upper");
            var transition = AddOutTransition(upper);
            _manager.OpenPopUp(upper);
            var lowerFocusedBefore = lower.FocusedCount;

            _manager.Close(upper);
            yield return null;

            Assert.That(upper == null, Is.False);
            Assert.That(InputBlocker().activeSelf, Is.True);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(upper.SortingOrder() - 1));
            Assert.That(lower.FocusedCount, Is.EqualTo(lowerFocusedBefore));

            transition.Complete();
            yield return null;

            Assert.That(upper == null, Is.True);
            Assert.That(InputBlocker().activeSelf, Is.False);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(lower.SortingOrder() - 1));
            Assert.That(lower.FocusedCount, Is.EqualTo(lowerFocusedBefore + 1));
        }

        [Test]
        public void Close_TwiceDuringTheOutTransition_ThrowsAndPlaysItOnce()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopup("Popup");
            var transition = AddOutTransition(popup);
            _manager.OpenPopUp(popup);

            _manager.Close(popup);

            Assert.That(() => _manager.Close(popup), Throws.InvalidOperationException);
            Assert.That(transition.PlayCount, Is.EqualTo(1));
            transition.Complete();
            Assert.That(_manager.IsTransitioning(), Is.False);
        }

        [Test]
        public void Close_DuringTheInTransition_CancelsItAndPlaysTheOutTransition()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopup("Popup");
            var inTransition = AddInTransition(popup);
            var outTransition = AddOutTransition(popup);
            _manager.OpenPopUp(popup);

            _manager.Close(popup);

            Assert.That(inTransition.LastToken.IsCancellationRequested, Is.True);
            Assert.That(outTransition.PlayCount, Is.EqualTo(1));
            Assert.That(_manager.IsTransitioning(), Is.True);

            inTransition.Complete();
            Assert.That(
                _manager.IsTransitioning(),
                Is.True,
                "a late in transition ended the out one"
            );

            outTransition.Complete();
            Assert.That(_manager.IsTransitioning(), Is.False);
        }

        [Test]
        public void TwoOverlappingTransitions_BlockInputUntilBothEnd()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var first = CreatePopup("First");
            var firstTransition = AddInTransition(first);
            var second = CreatePopup("Second");
            var secondTransition = AddInTransition(second);
            _manager.OpenPopUp(first);
            _manager.OpenPopUp(second);

            firstTransition.Complete();
            Assert.That(InputBlocker().activeSelf, Is.True);

            secondTransition.Complete();
            Assert.That(InputBlocker().activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator SetMainWindow_DuringTransitions_CancelsThemAndReleasesTheBlocker()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var closing = CreatePopup("Closing");
            var outTransition = AddOutTransition(closing);
            _manager.OpenPopUp(closing);
            var opening = CreatePopup("Opening");
            var inTransition = AddInTransition(opening);
            _manager.OpenPopUp(opening);
            _manager.Close(closing);
            var newMain = CreateWindow("NewMain");

            _manager.SetMainWindow(newMain);

            Assert.That(inTransition.LastToken.IsCancellationRequested, Is.True);
            Assert.That(outTransition.LastToken.IsCancellationRequested, Is.True);
            Assert.That(_manager.IsTransitioning(), Is.False);
            Assert.That(InputBlocker().activeSelf, Is.False);
            yield return null;

            Assert.That(closing == null, Is.True);
            Assert.That(opening == null, Is.True);
            Assert.That(_manager.MainWindow(), Is.SameAs(newMain));
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator DestroyingAPopupMidTransition_ReleasesTheBlocker()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopup("Popup");
            var transition = AddInTransition(popup);
            _manager.OpenPopUp(popup);

            UnityEngine.Object.Destroy(popup.gameObject);
            yield return null;

            Assert.That(transition.LastToken.IsCancellationRequested, Is.True);
            Assert.That(_manager.IsTransitioning(), Is.False);
            Assert.That(InputBlocker().activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator DestroyingAClosingPopup_StillRemovesItAndFocusesThePopupBelow()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var lower = _manager.OpenPopUp(CreatePopup("Lower"));
            var upper = CreatePopup("Upper");
            AddOutTransition(upper);
            _manager.OpenPopUp(upper);
            _manager.Close(upper);
            var lowerFocusedBefore = lower.FocusedCount;

            UnityEngine.Object.Destroy(upper.gameObject);
            yield return null;
            yield return null;

            Assert.That(_manager.IsTransitioning(), Is.False);
            Assert.That(_backgroundPanel.SortingOrder(), Is.EqualTo(lower.SortingOrder() - 1));
            Assert.That(lower.FocusedCount, Is.EqualTo(lowerFocusedBefore + 1));
        }

        [UnityTest]
        public IEnumerator AFaultingOutTransition_IsLoggedAndStillClosesThePopup()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopup("Popup");
            var transition = AddOutTransition(popup);
            _manager.OpenPopUp(popup);
            _manager.Close(popup);
            LogAssert.Expect(LogType.Exception, new Regex("transition failed"));

            transition.Fail(new InvalidOperationException("transition failed"));
            yield return null;

            Assert.That(popup == null, Is.True);
            Assert.That(InputBlocker().activeSelf, Is.False);
            Assert.That(_backgroundPanel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void AnInTransitionThatThrows_IsLoggedAndReleasesTheBlocker()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopup("Popup");
            var transition = AddInTransition(popup);
            transition.ThrowsOnPlay = new InvalidOperationException("transition failed");
            LogAssert.Expect(LogType.Exception, new Regex("transition failed"));

            _manager.OpenPopUp(popup);

            Assert.That(_manager.IsTransitioning(), Is.False);
            Assert.That(InputBlocker().activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator DestroyingTheManagerMidTransition_LogsNothing()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var closing = CreatePopup("Closing");
            AddOutTransition(closing);
            _manager.OpenPopUp(closing);
            var opening = CreatePopup("Opening");
            AddInTransition(opening);
            _manager.OpenPopUp(opening);
            _manager.Close(closing);

            UnityEngine.Object.Destroy(_manager.gameObject);
            yield return null;
            yield return null;

            LogAssert.NoUnexpectedReceived();
        }

        private TestWindow CreateWindow(string name)
        {
            return CreateGameObject(name).AddComponent<TestWindow>();
        }

        private TestPopup CreatePopup(string name)
        {
            return CreateGameObject(name).AddComponent<TestPopup>();
        }

        private static void SetField(object target, string name, object value)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo? field = null;
            for (var type = target.GetType(); field == null && type != null; type = type.BaseType)
                field = type.GetField(name, flags);
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

        private class TestPopup : PopupWindow
        {
            public int FocusedCount { get; private set; }

            public override void OnFocused()
            {
                FocusedCount++;
            }
        }

        private GameObject InputBlocker()
        {
            return _manager.transform.Find("PopupInputBlocker").gameObject;
        }

        private static TestTransition AddInTransition(PopupWindow popup)
        {
            var transition = popup.gameObject.AddComponent<TestTransition>();
            SetField(popup, "<InTransition>k__BackingField", transition);
            return transition;
        }

        private static TestTransition AddOutTransition(PopupWindow popup)
        {
            var transition = popup.gameObject.AddComponent<TestTransition>();
            SetField(popup, "<OutTransition>k__BackingField", transition);
            return transition;
        }

        // Ignores its token on purpose: the manager must stop waiting on cancellation by itself.
        private class TestTransition : Transition
        {
            private UniTaskCompletionSource? _pending;

            public int PlayCount { get; private set; }
            public CancellationToken LastToken { get; private set; }
            public bool CompletesSynchronously { get; set; }
            public Exception? ThrowsOnPlay { get; set; }

            public override UniTask Play(CancellationToken cancellationToken)
            {
                PlayCount++;
                LastToken = cancellationToken;
                if (ThrowsOnPlay != null)
                    throw ThrowsOnPlay;
                if (CompletesSynchronously)
                    return UniTask.CompletedTask;
                _pending = new UniTaskCompletionSource();
                return _pending.Task;
            }

            public void Complete()
            {
                _pending!.TrySetResult();
            }

            public void Fail(Exception exception)
            {
                _pending!.TrySetException(exception);
            }
        }
    }
}
