using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Arman.UIManagement.Tests
{
    public class UIManagerTests
    {
        private const int PopupSortingOffset = 10;

        private readonly List<GameObject> createdObjects = new();

        private UIManager manager = null!;
        private Panel backgroundPanel = null!;

        [SetUp]
        public void SetUp()
        {
            // Serialized fields are filled while the objects are inactive, because
            // Awake reads them and runs as soon as the object is activated.
            var panelObject = Track(new GameObject("PopupBackgroundPanel"));
            panelObject.SetActive(false);
            backgroundPanel = panelObject.AddComponent<Panel>();
            SetField(backgroundPanel, "backgroundImage", panelObject.AddComponent<Image>());

            var managerObject = Track(new GameObject("UIManager"));
            managerObject.SetActive(false);
            manager = managerObject.AddComponent<UIManager>();
            SetField(manager, "popupBackgroundPanel", backgroundPanel);
            SetField(manager, "sortingOffsetBetweenPopups", PopupSortingOffset);
            panelObject.transform.SetParent(managerObject.transform, false);

            managerObject.SetActive(true);
            panelObject.SetActive(true);
            manager.Init();

            // Update polls the legacy Input class, which throws when the project uses
            // the Input System package. Escape handling is not under test, so keep
            // Update from running across the frames the Close tests wait for.
            manager.enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in createdObjects)
                Object.Destroy(created);
            createdObjects.Clear();
        }

        [Test]
        public void SetMainWindow_ExposesTheWindowAsMainWindow()
        {
            var main = CreateWindow("Main");

            manager.SetMainWindow(main);

            Assert.AreSame(main, manager.MainWindow());
        }

        [Test]
        public void Init_HidesThePopupBackgroundPanel()
        {
            Assert.IsFalse(backgroundPanel.gameObject.activeSelf);
        }

        [Test]
        public void BackgroundPanel_ReturnsThePanelItWasConfiguredWith()
        {
            Assert.AreSame(backgroundPanel, manager.BackgroundPanel());
        }

        [Test]
        public void OpenPopUp_ParentsThePopupUnderTheManager()
        {
            manager.SetMainWindow(CreateWindow("Main"));

            var popup = manager.OpenPopUp(CreateWindow("Popup"));

            Assert.AreSame(manager.MainTransform(), popup.transform.parent);
        }

        [Test]
        public void OpenPopUp_SortsEachPopupAboveTheFocusedWindow()
        {
            var main = CreateWindow("Main");
            manager.SetMainWindow(main);

            var first = manager.OpenPopUp(CreateWindow("First"));
            var second = manager.OpenPopUp(CreateWindow("Second"));

            Assert.AreEqual(main.SortingOrder() + PopupSortingOffset, first.SortingOrder());
            Assert.AreEqual(first.SortingOrder() + PopupSortingOffset, second.SortingOrder());
        }

        [Test]
        public void OpenPopUp_ShowsTheBackgroundPanelJustBehindThePopupAndFocusesIt()
        {
            manager.SetMainWindow(CreateWindow("Main"));

            var popup = manager.OpenPopUp(CreateWindow("Popup"));

            Assert.IsTrue(backgroundPanel.gameObject.activeSelf);
            Assert.AreEqual(popup.SortingOrder() - 1, backgroundPanel.SortingOrder());
            Assert.AreEqual(1, popup.FocusedCount);
        }

        [UnityTest]
        public IEnumerator Close_FocusedPopup_DestroysItAndHidesThePanel()
        {
            manager.SetMainWindow(CreateWindow("Main"));
            var popup = manager.OpenPopUp(CreateWindow("Popup"));

            manager.Close(popup);
            yield return null;

            Assert.IsTrue(popup == null);
            Assert.IsFalse(backgroundPanel.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Close_FocusedPopup_FocusesThePopupBelowIt()
        {
            manager.SetMainWindow(CreateWindow("Main"));
            var lower = manager.OpenPopUp(CreateWindow("Lower"));
            var upper = manager.OpenPopUp(CreateWindow("Upper"));
            var lowerFocusedBefore = lower.FocusedCount;

            manager.Close(upper);
            yield return null;

            Assert.IsTrue(upper == null);
            Assert.IsTrue(backgroundPanel.gameObject.activeSelf);
            Assert.AreEqual(lower.SortingOrder() - 1, backgroundPanel.SortingOrder());
            Assert.AreEqual(lowerFocusedBefore + 1, lower.FocusedCount);
        }

        [UnityTest]
        public IEnumerator Close_NonFocusedPopup_DestroysItAndKeepsTheFocusedPopup()
        {
            manager.SetMainWindow(CreateWindow("Main"));
            var lower = manager.OpenPopUp(CreateWindow("Lower"));
            var upper = manager.OpenPopUp(CreateWindow("Upper"));

            manager.Close(lower);
            yield return null;

            Assert.IsTrue(lower == null);
            Assert.IsFalse(upper == null);
            Assert.IsTrue(backgroundPanel.gameObject.activeSelf);
            Assert.AreEqual(upper.SortingOrder() - 1, backgroundPanel.SortingOrder());
        }

        [UnityTest]
        public IEnumerator Close_NonFocusedPopup_ThenFocusedPopup_HidesThePanel()
        {
            manager.SetMainWindow(CreateWindow("Main"));
            var lower = manager.OpenPopUp(CreateWindow("Lower"));
            var upper = manager.OpenPopUp(CreateWindow("Upper"));

            manager.Close(lower);
            manager.Close(upper);
            yield return null;

            Assert.IsFalse(backgroundPanel.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Close_PopupsInOpeningOrder_HidesThePanelOnlyAfterTheLast()
        {
            manager.SetMainWindow(CreateWindow("Main"));
            var first = manager.OpenPopUp(CreateWindow("First"));
            var second = manager.OpenPopUp(CreateWindow("Second"));
            var third = manager.OpenPopUp(CreateWindow("Third"));

            manager.Close(first);
            manager.Close(second);
            Assert.IsTrue(backgroundPanel.gameObject.activeSelf);
            manager.Close(third);
            yield return null;

            Assert.IsFalse(backgroundPanel.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Close_AWindowThatIsNotOnTheStack_LeavesTheStackIntact()
        {
            manager.SetMainWindow(CreateWindow("Main"));
            var popup = manager.OpenPopUp(CreateWindow("Popup"));
            var stranger = CreateWindow("Stranger");

            manager.Close(stranger);
            yield return null;

            Assert.IsTrue(stranger == null);
            Assert.IsFalse(popup == null);
            Assert.IsTrue(backgroundPanel.gameObject.activeSelf);
            Assert.AreEqual(popup.SortingOrder() - 1, backgroundPanel.SortingOrder());
        }

        private TestWindow CreateWindow(string name)
        {
            return Track(new GameObject(name)).AddComponent<TestWindow>();
        }

        private GameObject Track(GameObject created)
        {
            createdObjects.Add(created);
            return created;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target
                .GetType()
                .GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
            Assert.IsNotNull(field, $"{target.GetType().Name} has no field '{name}'");
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
