using System.Collections;
using System.Reflection;
using NUnit.Framework;
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

        private TestWindow CreateWindow(string name)
        {
            return CreateGameObject(name).AddComponent<TestWindow>();
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
    }
}
