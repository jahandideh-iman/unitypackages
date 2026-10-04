# Popup in and out transitions — Implementation Plan

**Status:** Implemented

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a `PopupWindow` name optional in and out `Transition` components that `UIManager` plays on open and close, with a transparent input blocker above all popups while any transition plays.

**Architecture:** `Transition` is an abstract `MonoBehaviour` returning a `UniTask`. `PopupWindow` exposes it through two serialized auto-properties. `UIManager` tracks one `CancellationTokenSource` per transitioning popup, keeps a closing popup on the stack until its out transition ends, and shows a runtime-created blocker while any source is tracked. A popup without a transition runs through the same code synchronously, so today's behaviour is unchanged for it.

**Tech Stack:** Unity 6000.5 (see `ProjectSettings/ProjectVersion.txt`), C# with `-nullable:enable`, UniTask 2.5.11 (`com.cysharp.unitask` from OpenUPM), NUnit PlayMode tests via `com.unity.test-framework`.

**Spec:** [`docs/specs/2026-10-04-popup-transitions-design.md`](../specs/2026-10-04-popup-transitions-design.md)

## Global Constraints

- **Worktree:** `.worktrees/popup-transitions`, branch `feat/popup-transitions`, one PR into `dev`. Run git as `git -C <path>`, never `cd && git`.
- **The package folder has a space:** `Packages/UI Management`. Quote every path.
- **Never hand-create a `.meta`.** `Transition.cs` gets its `.meta` from Unity's import during the first test run in Task 2; commit the two together.
- **Never hand-edit `.unity`, `.prefab` or `.asset` YAML.** Nothing in this plan needs it.
- **Serialized fields are PascalCase auto-properties:** `[field: SerializeField] public Transition? InTransition { get; private set; }`. Do not rename the existing camelCase serialized fields (`closeOnBackButtonPressed`, `popupBackgroundPanel`, `sortingOffsetBetweenPopups`, `backgroundImage`).
- **New non-serialized private fields are `_camelCase`.** Leave the existing ones as they are.
- **Do not bump the package version, run `Tools/upm-release.mjs prepare`, or tag.** Changes go under `## [Unreleased]` only.
- **UniTask version is `2.5.11`**; the OpenUPM scope is `com.cysharp.unitask`.
- **The blocker sorts at `short.MaxValue`** in the manager's sorting layer, and is named `PopupInputBlocker`.
- **Commit messages:** Conventional Commits, scope `ui-management`, ending with the session's attribution lines.

## Test commands

The worktree has its own project path, so an Editor open on the main checkout does not block it. The first run imports the project from scratch and takes several minutes; run it in the background with a long timeout.

```bash
unity test --mode PlayMode --filter "Arman.UIManagement.Tests" --output Library/playmode-results.xml
```

Exit codes: `0` success, `8` tests ran and failed, `6` the run never produced results (usually compiler errors — read `Library/playmode-results.xml` or the Editor log). **`6` is never a pass.** Per-test results are in the XML (`grep -o 'name="[^"]*" [^>]*result="[^"]*"'`).

## Review Focus

1. **A cancelled in transition that completes late** must not release the blocker while the same popup's out transition still plays — pinned in Task 2 (`Close_DuringTheInTransition_CancelsItAndPlaysTheOutTransition`).
2. **`Play` throwing synchronously** instead of returning a faulted task must not escape `OpenPopUp` or leave input blocked — Task 2 (`AnInTransitionThatThrows_IsLoggedAndReleasesTheBlocker`).
3. **Two popups transitioning at once** keep the blocker up until both end — Task 2 (`TwoOverlappingTransitions_BlockInputUntilBothEnd`).
4. **`SetMainWindow` while a popup is mid-close** must not let the stale close touch the new stack — Task 2 (`SetMainWindow_DuringTransitions_CancelsThemAndReleasesTheBlocker`).
5. **The whole UI destroyed mid-transition** (a scene unload) must log no errors — Task 2 (`DestroyingTheManagerMidTransition_LogsNothing`).

---

### Task 1: Narrow the API to popups

**Files:**

- Modify: `Packages/UI Management/Runtime/Scripts/Window.cs`
- Modify: `Packages/UI Management/Runtime/Scripts/UIManager.cs` (`OpenPopUp`, `Close`)
- Test: `Packages/UI Management/Tests/PlayMode/UIManagerTests.cs`

**Interfaces:**

- Produces: `public abstract class Window : UIElement`; `public T OpenPopUp<T>(T popup) where T : PopupWindow`; `public void Close(PopupWindow window)`; in the tests, `private TestPopup CreatePopup(string name)` and `private class TestPopup : PopupWindow` with `public int FocusedCount { get; }`.

This task is a refactor: the existing tests are its specification and must pass with the same names before and after.

- [ ] **Step 1: Move the tests' popups to a `PopupWindow` subclass**

In `UIManagerTests.cs`, replace every `OpenPopUp(CreateWindow(` with `OpenPopUp(CreatePopup(`, and change the stranger in `ClosingAWindowThatIsNotOnTheStack_LeavesTheStackIntact` to:

```csharp
            var stranger = CreatePopup("Stranger");
```

Add next to `CreateWindow`:

```csharp
        private TestPopup CreatePopup(string name)
        {
            return CreateGameObject(name).AddComponent<TestPopup>();
        }
```

and next to `TestWindow`:

```csharp
        private class TestPopup : PopupWindow
        {
            public int FocusedCount { get; private set; }

            public override void OnFocused()
            {
                FocusedCount++;
            }
        }
```

`CreateWindow` and `TestWindow` stay for the main windows.

- [ ] **Step 2: Run the suite — it still passes**

Run the test command. Expected: exit `0`, all 13 `UIManagerTests` pass (this is the first run, so it also imports the project).

- [ ] **Step 3: Narrow the API**

`Window.cs`:

```csharp
    public abstract class Window : UIElement
```

`UIManager.cs`:

```csharp
        public T OpenPopUp<T>(T popup)
            where T : PopupWindow
```

```csharp
        public void Close(PopupWindow window)
```

- [ ] **Step 4: Run the suite — it still passes**

Expected: exit `0`, the same 13 tests pass.

- [ ] **Step 5: Format and commit**

```bash
npm run format
git -C . add "Packages/UI Management"
git -C . commit -m "refactor(ui-management): make Window abstract and take PopupWindow in OpenPopUp and Close"
```

---

### Task 2: Play transitions and block input

**Files:**

- Create: `Packages/UI Management/Runtime/Scripts/Transition.cs` (+ the `.meta` Unity generates)
- Modify: `Packages/UI Management/Runtime/Scripts/PopupWindow.cs`
- Modify: `Packages/UI Management/Runtime/Scripts/UIManager.cs`
- Modify: `Packages/UI Management/Runtime/Arman.UIManagement.asmdef`
- Modify: `Packages/UI Management/Tests/PlayMode/Arman.UIManagement.Tests.PlayMode.asmdef`
- Modify: `Packages/UI Management/package.json`
- Modify: `Packages/manifest.json`, `Packages/packages-lock.json` (Unity rewrites the lock)
- Test: `Packages/UI Management/Tests/PlayMode/UIManagerTests.cs`

**Interfaces:**

- Consumes: Task 1's `OpenPopUp<T>(T) where T : PopupWindow`, `Close(PopupWindow)`, `CreatePopup`, `TestPopup`.
- Produces: `public abstract class Transition : MonoBehaviour { public abstract UniTask Play(CancellationToken cancellationToken); }`; `PopupWindow.InTransition` / `OutTransition` (`Transition?`, serialized backing fields `<InTransition>k__BackingField` / `<OutTransition>k__BackingField`); `public bool UIManager.IsTransitioning()`; a child GameObject of the manager named `PopupInputBlocker`.

- [ ] **Step 1: Add the UniTask dependency**

`Packages/manifest.json` — add a top-level `scopedRegistries` before `dependencies`, keeping Unity's two-space layout:

```json
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.cysharp.unitask"]
    }
  ],
```

`Packages/UI Management/package.json` — add after `"author"`:

```json
  "dependencies": {
    "com.cysharp.unitask": "2.5.11"
  }
```

Both asmdefs — add `"UniTask"` to `references`. The runtime one becomes `"references": ["UniTask"]`; the test one gains `"UniTask"` after `"UnityEngine.UI"`. Leave `overrideReferences` as it is.

- [ ] **Step 2: Add `Transition` and the `PopupWindow` getters**

`Packages/UI Management/Runtime/Scripts/Transition.cs`:

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Arman.UIManagement
{
    /// <summary>
    /// Animates a popup in or out. <see cref="UIManager"/> plays a popup's
    /// <see cref="PopupWindow.InTransition"/> when it opens and its
    /// <see cref="PopupWindow.OutTransition"/> before destroying it on close.
    /// </summary>
    public abstract class Transition : MonoBehaviour
    {
        /// <summary>
        /// Plays the transition and completes when it has finished. The token is cancelled when the
        /// transition is interrupted; the manager stops waiting at that point either way.
        /// </summary>
        public abstract UniTask Play(CancellationToken cancellationToken);
    }
}
```

`PopupWindow.cs` — add below `closeOnBackButtonPressed`:

```csharp
        [field: SerializeField]
        public Transition? InTransition { get; private set; }

        [field: SerializeField]
        public Transition? OutTransition { get; private set; }
```

- [ ] **Step 3: Write the failing tests**

In `UIManagerTests.cs`, add usings:

```csharp
using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
```

Make `SetField` find private fields declared on a base class (the transition backing fields live on `PopupWindow`, the tests hold a `TestPopup`):

```csharp
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
```

Add the helpers and the transition double next to `TestPopup`:

```csharp
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
```

Add the tests:

```csharp
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
        public void Close_TwiceDuringTheOutTransition_PlaysItOnce()
        {
            _manager.SetMainWindow(CreateWindow("Main"));
            var popup = CreatePopup("Popup");
            var transition = AddOutTransition(popup);
            _manager.OpenPopUp(popup);

            _manager.Close(popup);
            _manager.Close(popup);

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
            Assert.That(_manager.IsTransitioning(), Is.True, "a late in transition ended the out one");

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
```

- [ ] **Step 4: Run the tests to verify they fail**

Run the test command. Expected: exit `6` — the tests do not compile, because `UIManager.IsTransitioning` does not exist yet. The run still imports `Transition.cs` and writes `Transition.cs.meta`; confirm the `.meta` exists.

- [ ] **Step 5: Implement transitions in `UIManager`**

Replace `UIManager.cs` with:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        private readonly List<Window> windowsStack = new();

        private readonly Dictionary<PopupWindow, CancellationTokenSource> _runningTransitions =
            new();
        private readonly HashSet<PopupWindow> _closingPopups = new();
        private Canvas _inputBlocker = null!;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        public void Init()
        {
            popupBackgroundPanel.Init(this);
            HidePopupPanel();
            CreateInputBlocker();
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
            System.Diagnostics.Debug.Assert(mainWindow != null, "Main window must not be null");
            AttachToSelf(popup);
            popup.Init(this);
            SetPopupSortingOrder(popup);
            PushOnStack(popup);
            FocusPopupPanelOn(popup);
            PlayTransition(popup, popup.InTransition).Forget();
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

        public void Close(PopupWindow window)
        {
            if (!windowsStack.Contains(window))
            {
                DestroyWindow(window);
                return;
            }

            if (!_closingPopups.Add(window))
                return;

            CloseAfterOutTransition(window).Forget();
        }

        private async UniTaskVoid CloseAfterOutTransition(PopupWindow window)
        {
            CancelTransitionOf(window);
            await PlayTransition(window, window.OutTransition);

            // SetMainWindow empties the closing set when it clears lingering popups,
            // and a destroyed manager has nothing left to refocus.
            if (!_closingPopups.Remove(window) || this == null)
                return;

            windowsStack.Remove(window);
            DestroyWindow(window);

            if (FocusedWindowIsMainWindow())
                HidePopupPanel();
            else
                FocusPopupPanelOn(CurrentFocusedWindow());
        }

        // Completes synchronously when there is no transition or it is already finished,
        // so a popup without one opens and closes exactly as it would without this step.
        private async UniTask PlayTransition(PopupWindow popup, Transition? transition)
        {
            if (transition == null)
                return;

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

        private void CreateInputBlocker()
        {
            var blocker = new GameObject("PopupInputBlocker", typeof(RectTransform));
            var rect = (RectTransform)blocker.transform;
            rect.SetParent(MainTransform(), false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _inputBlocker = blocker.AddComponent<Canvas>();
            blocker.AddComponent<GraphicRaycaster>();
            var image = blocker.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            blocker.SetActive(false);
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
                _inputBlocker.sortingLayerID = canvas.sortingLayerID;
                _inputBlocker.sortingOrder = short.MaxValue;
            }
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
            return windowsStack.Last();
        }

        private void PushOnStack(Window window)
        {
            windowsStack.Add(window);
        }

        private void ClearLingeringWindows()
        {
            // It is assumed that Main Window will be destroyed on its own.
            var lingering = windowsStack.Skip(1).ToList();
            windowsStack.Clear();

            // Emptied before cancelling, so the cancelled closes find nothing left to do.
            _closingPopups.Clear();
            CancelAllTransitions();

            foreach (var window in lingering)
                DestroyWindow(window);
            HidePopupPanel();
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
            return popupBackgroundPanel;
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run the test command. Expected: exit `0`, 26 `UIManagerTests` pass (13 existing, 13 new). If one fails, use superpowers:systematic-debugging before changing code; in particular, do not weaken `DestroyingTheManagerMidTransition_LogsNothing` — an error there is a real scene-unload bug.

- [ ] **Step 7: Validate the package**

```bash
node Tools/upm-release.mjs validate --only "UI Management"
```

Expected: exit `0` (this checks that `Transition.cs.meta` exists and that `npm pack` succeeds).

- [ ] **Step 8: Format and commit**

```bash
npm run format
git -C . add "Packages/UI Management" Packages/manifest.json Packages/packages-lock.json
git -C . commit -m "feat(ui-management): play popup in and out transitions and block input while they run"
```

Check `git -C . status` first: `Transition.cs.meta` must be in the commit, and nothing under `Library/` or `Assets/` should be.

---

### Task 3: Documentation, changelog and status

**Files:**

- Modify: `Packages/UI Management/README.md`
- Modify: `Packages/UI Management/CHANGELOG.md`
- Modify: `docs/specs/2026-10-04-popup-transitions-design.md`, `docs/plans/2026-10-04-popup-transitions.md`, `docs/INDEX.md` (status)

**Interfaces:**

- Consumes: the public API from Tasks 1 and 2.

- [ ] **Step 1: Changelog**

Replace the `## [Unreleased]` section of `Packages/UI Management/CHANGELOG.md` with:

```markdown
## [Unreleased]

### Added

- `Transition`, an abstract component whose `Play(CancellationToken)` returns a `UniTask`, for animating a popup in or out.
- `PopupWindow.InTransition` and `PopupWindow.OutTransition`, optional transitions that `UIManager` plays when the popup opens and before it destroys the popup on close.
- `UIManager.IsTransitioning`, and a transparent input blocker above all popups that swallows pointer input and the back button while a transition plays.

### Changed

- `UIManager.Close` to allow closing windows that are not focused
- `Window` is abstract.
- `UIManager.OpenPopUp` and `UIManager.Close` take a `PopupWindow` instead of any `Window`.
- The package depends on `com.cysharp.unitask` 2.5.11.
```

- [ ] **Step 2: README**

In `Packages/UI Management/README.md`:

Append to the intro paragraph that ends "…sorted above the last, …":

```markdown
Popups can animate in and out, with input blocked while they do.
```

Replace the type table rows for `UIManager`, `Window` and `PopupWindow`, and add a `Transition` row after `PopupWindow`:

```markdown
| `UIManager`   | `MonoBehaviour` on a `Canvas`. Owns the stack: `Init`, `SetMainWindow`, `OpenPopUp<T>`, `Close`, `IsTransitioning`, `MainWindow`, `SetMainCamera`. |
| `Window`      | Abstract `UIElement` on its own `Canvas` + `GraphicRaycaster`; overridable `InternalInit`, `OnBackButtonPressed`, `OnFocused`.                     |
| `PopupWindow` | A window with `Close()`, a `closeOnBackButtonPressed` toggle, and optional `InTransition` and `OutTransition`.                                       |
| `Transition`  | Abstract component that animates a popup in or out: `UniTask Play(CancellationToken)`.                                                              |
```

Add a section before `## Usage`:

````markdown
## Dependencies

The package depends on [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`). The openupm
CLI adds its scope for you; when editing `Packages/manifest.json` by hand, the OpenUPM scoped registry needs
the `com.cysharp.unitask` scope alongside this package's:

```json
"scopedRegistries": [
  {
    "name": "package.openupm.com",
    "url": "https://package.openupm.com",
    "scopes": ["com.arman.ui-management", "com.cysharp.unitask"]
  }
]
```
````

Add a subsection at the end of `## Usage`, before `## Things to know`:

````markdown
### Transitions

A transition is a component. Write one, add it to the popup prefab, and assign it to the popup's
`InTransition` or `OutTransition` in the Inspector — either, both, or neither. A fade, used twice on the
same popup with opposite `From` and `To`:

```csharp
using System.Threading;
using Arman.UIManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeTransition : Transition
{
    [SerializeField] private float Duration = 0.25f;
    [SerializeField] private float From;
    [SerializeField] private float To = 1f;

    public override async UniTask Play(CancellationToken cancellationToken)
    {
        var group = GetComponent<CanvasGroup>();
        for (var elapsed = 0f; elapsed < Duration; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(From, To, elapsed / Duration);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
        group.alpha = To;
    }
}
```

`OpenPopUp` focuses the popup, then plays its in transition. `Close` plays the out transition with the popup
still on the stack and the dimmer behind it, then destroys it. Neither waits: both return at once. While any
transition plays, a transparent full-screen image above every popup swallows pointer input, Escape is
ignored, and `IsTransitioning()` is `true`.
````

In `## Things to know`, replace the first two bullets (`Close` only works on the focused window; `Close` destroys the window GameObject) with:

```markdown
- **`Close` works on any popup on the stack.** Closing one below the top removes it and leaves focus
  where it is.
- **`Close` destroys the popup GameObject**, after its out transition if it has one. Popups are
  instantiate-and-discard, not show/hide; keep state outside the popup or reload it in `InternalInit`.
- **Only a `PopupWindow` can be opened or closed.** `Window` is abstract; the main window is any
  `Window` subclass, usually `MainWindow`.
- **Interrupted transitions are cancelled, not awaited.** `Close` during an in transition cancels it
  and starts the out transition; a second `Close` on a closing popup is ignored; `SetMainWindow`
  cancels every transition and destroys lingering popups at once. Honour the `CancellationToken` so
  the animation stops too — the manager stops waiting either way.
- **A transition that never completes blocks input for good.** The manager releases the blocker when
  `Play` completes, throws (the exception is logged), or is cancelled — nothing else.
- **The input blocker sorts at `short.MaxValue`** in the manager's sorting layer, so for the length of
  a transition it also covers other canvases in that layer.
```

- [ ] **Step 3: Set the status to Implemented**

Change `**Status:** Designed` in the spec and `**Status:** Planned` in this plan to `**Status:** Implemented`, and the `Popup transitions` row in `docs/INDEX.md` to `Implemented`.

- [ ] **Step 4: Check and commit**

```bash
npm run format
git -C . add "Packages/UI Management" docs
npm run format:check
npm run check:docs
node --test Tools/*.test.mjs
git -C . commit -m "docs(ui-management): document popup transitions"
node Tools/changelog-check.mjs --base origin/dev --head HEAD --base-branch dev
```

Expected: every check exits `0`. `changelog-check` runs after the commit because it compares committed refs.
