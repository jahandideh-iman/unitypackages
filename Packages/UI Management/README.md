# UI Management

A window stack for Unity UI. One main window stays at the bottom; popups push on top of it, each
sorted above the last, with a shared dimming panel tucked in behind whichever popup has focus. Escape
goes to the focused window only. Popups can animate in and out, with input blocked while they do.

It replaces the usual pile of `SetActive` calls and hand-tuned `sortingOrder` values with a stack that
knows what is on top.

## What it provides

Everything lives in the `Arman.UIManagement` namespace.

| Type          | Purpose                                                                                                                                            |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `UIManager`   | `MonoBehaviour` on a `Canvas`. Owns the stack: `Init`, `SetMainWindow`, `OpenPopUp<T>`, `Close`, `IsTransitioning`, `MainWindow`, `SetMainCamera`. |
| `UIElement`   | Base `MonoBehaviour` with an `InternalOnDestroy` hook.                                                                                             |
| `Window`      | Abstract `UIElement` on its own `Canvas` + `GraphicRaycaster`; overridable `InternalInit`, `OnBackButtonPressed`, `OnFocused`.                     |
| `MainWindow`  | The bottom-of-stack window.                                                                                                                        |
| `PopupWindow` | A window with `Close()`, a `closeOnBackButtonPressed` toggle, and optional `InTransition` and `OutTransition`.                                     |
| `Transition`  | Abstract component that animates a popup in or out: `UniTask Play(CancellationToken)`.                                                             |
| `Panel`       | A `Window` with a `CanvasGroup` and background image — `SetVisible`, `SetAlpha`, `RestoreAlpha`. Used for the popup dimmer.                        |

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

## Usage

Set the manager up once, then hand it the main window:

```csharp
using Arman.UIManagement;

uiManager.Init();                     // prepares the dimming panel
uiManager.SetMainWindow(mainWindow);  // clears any leftover popups
```

Open a popup. It is reparented under the manager, sorted above the current focus, and the dimmer
slides in behind it:

```csharp
SettingsPopup popup = uiManager.OpenPopUp(Instantiate(settingsPopupPrefab));
popup.Bind(playerSettings);           // OpenPopUp returns the popup, typed
```

Closing pops the stack and destroys the window, returning focus and the dimmer to whatever was
underneath:

```csharp
uiManager.Close(popup);   // or, from inside a PopupWindow: this.Close();
```

Windows react to being shown or dismissed by overriding the hooks:

```csharp
public class SettingsPopup : PopupWindow
{
    protected override void InternalInit(UIManager manager) => Load();

    public override void OnFocused() => Refresh();

    public override void OnBackButtonPressed() => Confirm();   // instead of closing
}
```

Tick `closeOnBackButtonPressed` in the Inspector to get the default Escape-closes-me behaviour without
writing an override.

For a world-space canvas, point the manager at your camera:

```csharp
uiManager.SetMainCamera(Camera.main);
```

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

## Things to know

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
- **`SetMainWindow` destroys every popup above the previous main window**, but assumes the outgoing
  main window destroys itself — typically because the scene unloaded.
- **Every `Window` needs its own `Canvas` and `GraphicRaycaster`** (`[RequireComponent]`), because
  sorting is done per-window with `overrideSorting`.
- **Popup spacing comes from `sortingOffsetBetweenPopups`** on the manager. Leave it at 0 and popups
  will share a sorting order with the dimmer sitting one below — set it to a value larger than the
  depth of any single window's internal sorting.
- **Escape is read through the legacy `Input` class** in `Update`, so this does not work with the new
  Input System package as-is.
- **`UIElement.Destroy()` destroys the component, not the GameObject.** It is not what `Close` uses.
  Prefer `uiManager.Close(window)`.
- **`Panel` requires a background `Image` assigned** — `Awake` reads its alpha, and `SetAlpha` writes
  to it.
