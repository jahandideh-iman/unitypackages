# UI Management

A window stack for Unity UI. One main window stays at the bottom; popups push on top of it, each
sorted above the last, with a shared dimming panel tucked in behind whichever popup has focus. Escape
goes to the focused window only.

It replaces the usual pile of `SetActive` calls and hand-tuned `sortingOrder` values with a stack that
knows what is on top.

## What it provides

Everything lives in the `Arman.UIManagement` namespace.

| Type              | Purpose                                                                                                                                           |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `UIManager`       | `MonoBehaviour` on a `Canvas`. Owns the stack: `Init`, `SetMainWindow`, `OpenPopUp<T>`, `Close`, `MainWindow`, `SetMainCamera`, `IsInputBlocked`. |
| `UIElement`       | Base `MonoBehaviour` with an `InternalOnDestroy` hook.                                                                                            |
| `Window`          | `UIElement` on its own `Canvas` + `GraphicRaycaster`; overridable `InternalInit`, `OnBackButtonPressed`, `OnFocused`.                             |
| `MainWindow`      | The bottom-of-stack window.                                                                                                                       |
| `PopupWindow`     | A window with `Close()` and a `closeOnBackButtonPressed` toggle.                                                                                  |
| `Panel`           | A `Window` with a `CanvasGroup` and background image — `SetVisible`, `SetAlpha`, `RestoreAlpha`. Used for the popup dimmer.                       |
| `PopupTransition` | Optional component on a popup's root that animates it in and out: implement `PlayIn` and `PlayOut`.                                               |

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

### Transitions

A popup animates in and out when its root GameObject carries a `PopupTransition`; without one it
appears and disappears at once. Subclass it with whatever drives the animation — a coroutine, an
`Animator`, a tween library — and call `onComplete` when the animation ends:

```csharp
[RequireComponent(typeof(CanvasGroup))]
public class FadeTransition : PopupTransition
{
    [SerializeField] float duration = 0.2f;

    public override void PlayIn(Action onComplete) => Fade(0f, 1f, onComplete);

    public override void PlayOut(Action onComplete) => Fade(1f, 0f, onComplete);

    void Fade(float from, float to, Action onComplete)
    {
        StopAllCoroutines();   // PlayOut can arrive while PlayIn is still running
        StartCoroutine(Run());

        IEnumerator Run()
        {
            var group = GetComponent<CanvasGroup>();
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
            onComplete();
        }
    }
}
```

`OpenPopUp` starts `PlayIn` after the popup is on the stack and focused, so `PlayIn` must put the
popup in its starting pose straight away. `Close` takes the popup off the stack and focuses the window
below at once, then starts `PlayOut` and destroys the popup when it completes; the dimmer stays until
then.

While any transition runs, the manager shows a transparent, full-screen image sorted above every
popup, so no click or tap reaches the UI, and it ignores Escape. `IsInputBlocked()` reports whether
that is the case.

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

## Things to know

- **`Close` works on any popup in the stack**, not only the focused one. Closing a popup further down
  leaves focus where it is.
- **`Close` destroys the window GameObject**, after its out transition if it has one. Popups are
  instantiate-and-discard, not show/hide; keep state outside the popup or reload it in `InternalInit`.
- **A transition must always call `onComplete`.** Input stays blocked until it does. Calling it more
  than once is harmless, and a `PlayIn` that `PlayOut` interrupted may skip its call.
- **Transitions should run on unscaled time** — popups such as a pause menu often open while
  `Time.timeScale` is 0.
- **`SetMainWindow` destroys every popup above the previous main window** at once, including popups
  mid-transition, but assumes the outgoing main window destroys itself — typically because the scene
  unloaded.
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
