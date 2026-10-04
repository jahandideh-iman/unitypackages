# Popup in and out transitions

**Date:** 2026-10-04
**Status:** Planned

## 1. The problem

`UIManager` shows and removes popups instantly: `OpenPopUp` parents, sorts and focuses the popup in one
call, and `Close` destroys it in the same frame. A game that wants a popup to scale in or fade out has
nowhere to put that animation, and if it animates the popup by itself the manager neither waits for the
animation before destroying the popup nor stops the player from tapping buttons while it plays.

The package needs optional in and out transitions for popups, with input blocked while any transition
plays. A popup without a transition behaves exactly as it does today.

## 2. Scope

In scope:

- A `Transition` component and two getters on `PopupWindow` that name a popup's in and out transitions.
- `UIManager` playing those transitions on open and close, and blocking input while they play.
- Narrowing the manager's API to popups: `Window` becomes abstract, and `OpenPopUp` and `Close` take a
  `PopupWindow`.
- A dependency on UniTask, which the transition API returns.

Out of scope:

- Any concrete transition. The package ships the abstract component only; the README shows a fade as an
  example.
- Animating the popup background panel (the dimmer). It still appears and disappears instantly.
- Transitions for the main window, or when `SetMainWindow` clears lingering popups.

## 3. Design

### 3.1 `Transition`

```csharp
namespace Arman.UIManagement
{
    public abstract class Transition : MonoBehaviour
    {
        public abstract UniTask Play(CancellationToken cancellationToken);
    }
}
```

A transition is a component, so one implementation — a fade, a scale-in, an `Animator` driver — is
written once and reused on any popup prefab by configuring it in the Inspector. The component can sit on
the popup or on any of its children.

`Play` returns when the transition has finished. `cancellationToken` is cancelled when the transition is
interrupted: the popup is closed during its in transition, the popup or the manager is destroyed, or
`SetMainWindow` clears the popup. An implementation should stop promptly when it is cancelled (for
DOTween, `tween.ToUniTask(cancellationToken: cancellationToken)`), but the manager does not depend on
it: it stops waiting on cancellation either way (§3.4).

### 3.2 `PopupWindow`

```csharp
[field: SerializeField]
public Transition? InTransition { get; private set; }

[field: SerializeField]
public Transition? OutTransition { get; private set; }
```

Both are optional. A `null` getter means "no transition" for that direction, so a popup can have an in
transition only, an out transition only, both, or neither. The same component may be assigned to both.

### 3.3 API changes

- **`Window` becomes `abstract`.** It gains no abstract members; `MainWindow`, `Panel` and `PopupWindow`
  are unchanged by it. A bare `Window` component can no longer be added to a GameObject.
- **`OpenPopUp<T>(T popup) where T : PopupWindow`**, narrowed from `Window`, because only a
  `PopupWindow` has transitions.
- **`Close(PopupWindow window)`**, narrowed from `Window`. Only popups sit above the main window on the
  stack, and closing the main window through `Close` corrupts the stack today.
- **`IsTransitioning()`** — a new public method, `true` while any popup transition is playing.

`SetMainWindow(Window)` keeps its signature.

### 3.4 Playing transitions

**Opening.** `OpenPopUp` does everything it does today — parent, `Init`, sort, push, show the dimmer,
`OnFocused` — and then plays `InTransition` if the popup has one. `OnFocused` keeps firing at open time,
so a popup can fill in its content before it animates in. `OpenPopUp` returns the popup immediately; it
does not wait for the transition.

**Closing.** `Close` on a popup with an `OutTransition` leaves the popup on the stack while the
transition plays, so the dimmer stays behind it as it animates out. When the transition ends, the
existing close logic runs: remove the popup from the stack, destroy it, and refocus the window below or
hide the dimmer. `Close` returns immediately and does not wait for the transition.

**No transition stays synchronous.** When the getter is `null`, or `Play` returns an already-completed
task, the manager carries on in the same call: `Close` removes and destroys the popup before it returns,
exactly as today, and the input blocker never appears.

**Interruptions.**

| Situation                                           | Behaviour                                                                                                                                    |
| --------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `Close` while the popup's in transition plays       | The in transition is cancelled and the out transition starts at once.                                                                        |
| `Close` while the popup is already closing          | Ignored.                                                                                                                                     |
| `Close` on a window that is not on the stack        | Destroyed immediately, as today; no transition plays.                                                                                        |
| `OpenPopUp` while another transition plays          | Allowed. The new popup sorts above the current top of the stack, which may be a popup that is closing.                                       |
| `SetMainWindow` while transitions play              | Every running transition is cancelled; lingering popups are destroyed immediately, as today, with no out transitions; the blocker is hidden. |
| The popup is destroyed mid-transition by other code | The transition is cancelled through the popup's destroy token and the blocker is released.                                                   |
| `Play` throws                                       | The exception is logged with `Debug.LogException` and treated as the transition ending: the blocker is released and a close still completes. |

Each transition gets the token of a `CancellationTokenSource` the manager creates for it, linked to the
popup's `GetCancellationTokenOnDestroy()`. The manager cancels the source itself on `Close` during an in
transition and on `SetMainWindow`; the link cancels it when the popup is destroyed. The manager's own
destruction needs no separate link: popups are parented under the manager, so destroying it destroys
them. The source is not disposed after the transition: its only registration is on the popup's destroy
token, which goes away with the popup, and disposing a source from inside its own `Cancel` call is
unsafe. The manager awaits
`Play(token).AttachExternalCancellation(token)`, so it stops waiting on cancellation even when an
implementation ignores the token. A transition that never completes and is never cancelled keeps input
blocked; that is the implementation's bug, and the README says so.

### 3.5 Blocking input

`Init` creates the input blocker as a child of the manager: a full-screen `RectTransform` with its own
`Canvas` (`overrideSorting`, in the manager's sorting layer, at sorting order `short.MaxValue`), a
`GraphicRaycaster`, and an `Image` coloured `Color.clear` with `raycastTarget` on. A transparent `Image`
still receives raycasts, so it swallows every pointer event above all popups. Creating it at runtime
means existing scenes and prefabs need no new Inspector wiring.

The blocker is active while at least one transition is playing and inactive otherwise. Sorting at
`short.MaxValue` keeps it above every popup whatever `sortingOffsetBetweenPopups` is, at the cost of also
covering other canvases in the same sorting layer for as long as a transition lasts.

`Update` skips the back button while `IsTransitioning()` is `true`, so Escape cannot close or re-close a
popup mid-transition.

### 3.6 The UniTask dependency

UI Management becomes the first `com.arman.*` package with a dependency outside this repo:

- `Packages/UI Management/package.json` adds `"com.cysharp.unitask": "2.5.11"`, the current OpenUPM
  release. UniTask's minimum Unity is 2018.4, below the package's `2019.1`.
- The runtime asmdef and the PlayMode test asmdef reference `UniTask`.
- `Packages/manifest.json` gains an OpenUPM scoped registry with the scope `com.cysharp.unitask`. The
  project resolves UniTask through the embedded package's own dependency, so the manifest does not list
  it again.

Consumers installing through the openupm CLI get the `com.cysharp.unitask` scope added for them; anyone
editing `manifest.json` by hand needs the scope too, which the README states.

## 4. Testing

PlayMode tests in `UIManagerTests` use a `TestPopup : PopupWindow` with a `TestTransition : Transition`
whose `Play` returns a `UniTaskCompletionSource` task the test completes, faults, or leaves pending, and
which records whether its token was cancelled. They cover:

- Opening a popup with an in transition activates the blocker and reports `IsTransitioning()`; completing
  it deactivates the blocker.
- Closing a popup with an out transition keeps it alive and on the stack, with the dimmer behind it,
  until the transition completes; then it is destroyed and focus moves down.
- A second `Close` during the out transition plays nothing new.
- `Close` during the in transition cancels it and starts the out transition.
- `SetMainWindow` during a transition cancels it, destroys the popups and hides the blocker.
- Destroying a popup mid-transition releases the blocker.
- A faulting transition releases the blocker and still completes the close.
- A popup with a transition that completes synchronously never shows the blocker.

The existing tests move from `TestWindow` popups to `TestPopup` without transitions, and keep passing
unchanged otherwise — that is the "no transition stays synchronous" guarantee. The back-button skip is
not tested: `Update` reads the legacy `Input` class, which throws in this project, so the tests disable
the manager.

## 5. Documentation and release

- `CHANGELOG.md` `## [Unreleased]`: **Added** — `Transition`, `PopupWindow.InTransition` and
  `OutTransition`, `UIManager.IsTransitioning`, and the input blocker. **Changed** — `Window` is
  abstract; `OpenPopUp` and `Close` take a `PopupWindow`; the package depends on `com.cysharp.unitask`.
  These are breaking changes, released as the next minor version under 0.x.
- `README.md`: the new types, a fade transition example, the scoped-registry note, and the
  interruption rules. Its "`Close` only works on the focused window" item is corrected, because `Close`
  has accepted any window on the stack since `Close` was relaxed for non-focused windows.
