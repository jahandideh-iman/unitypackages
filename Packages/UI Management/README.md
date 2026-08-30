# UI Management

A window stack for Unity UI. One main window stays at the bottom; popups push on top of it, each
sorted above the last, with a shared dimming panel tucked in behind whichever popup has focus. Escape
goes to the focused window only.

It replaces the usual pile of `SetActive` calls and hand-tuned `sortingOrder` values with a stack that
knows what is on top.

## What it provides

Everything lives in the `Arman.UIManagement` namespace.

| Type | Purpose |
|---|---|
| `UIManager` | `MonoBehaviour` on a `Canvas`. Owns the stack: `Init`, `SetMainWindow`, `OpenPopUp<T>`, `Close`, `MainWindow`, `SetMainCamera`. |
| `UIElement` | Base `MonoBehaviour` with an `InternalOnDestroy` hook. |
| `Window` | `UIElement` on its own `Canvas` + `GraphicRaycaster`; overridable `InternalInit`, `OnBackButtonPressed`, `OnFocused`. |
| `MainWindow` | The bottom-of-stack window. |
| `PopupWindow` | A window with `Close()` and a `closeOnBackButtonPressed` toggle. |
| `Panel` | A `Window` with a `CanvasGroup` and background image — `SetVisible`, `SetAlpha`, `RestoreAlpha`. Used for the popup dimmer. |

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

## Things to know

- **`Close` only works on the focused window.** Closing anything that is not on top of the stack is
  silently ignored — popups come off in the order they went on.
- **`Close` destroys the window GameObject.** Popups are instantiate-and-discard, not show/hide; keep
  state outside the popup or reload it in `InternalInit`.
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
