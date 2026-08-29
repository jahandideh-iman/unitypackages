# UI Management

Component-based helpers for managing UI windows on a Canvas. A `UIManager` tracks a stack of `Window`s (a main window plus popups), drives popups' sorting order, and closes the focused window when Escape is pressed. Includes window/popup base types, panels, and a UI-element base.

## What it provides

- `UIManager` (a `MonoBehaviour` on a `Canvas`) — `Init`, `SetMainWindow`, `OpenPopUp<T>`, `Close`, `MainWindow`, `SetMainCamera`.
- `Window` → `MainWindow` / `PopupWindow` — the window base and its two flavours.
- `Panel` — a window base without focus behaviour.
- `UIElement` — base for a UI element, with a destroy hook (`InternalOnDestroy`).

## Usage

```csharp
// From the UIManager (a MonoBehaviour on a Canvas):
uiManager.Init();
uiManager.SetMainWindow(mainWindow);

// Open a popup window over the focused window (Escape closes it via OnBackButtonPressed).
uiManager.OpenPopUp(myPopupWindow);
```
