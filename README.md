# LoadScreenManager

A modular, data-driven load-screen framework for Unity.  
Load screens are defined in plain JSON, displayed with a progress bar, animated spinner, and rotating tips — all configurable per-definition.  
Optionally integrates with [MapLoaderFramework](https://github.com/RolandKaechele/MapLoaderFramework), [GameManager](https://github.com/RolandKaechele/GameManager), and [StateManager](https://github.com/RolandKaechele/StateManager) for automatic show/hide, and with [DOTween Pro](https://assetstore.unity.com/packages/tools/visual-scripting/dotween-pro-32416) for eased fade transitions.


## Features

- **JSON-authored definitions** — define each load screen (background sprite, tips, fade durations, spinner, progress visibility) in plain JSON; no code required
- **Progress bar** — `SetProgress(float)` drives a `Slider` or Image fill directly; compatible with Unity's `AsyncOperation.progress`
- **Rotating tips** — tip strings are drawn randomly from each definition's `tipPool` array and rotated at a configurable interval
- **Spinner** — any `Transform` set to rotate continuously while the screen is visible
- **Coroutine fades** — built-in `CanvasGroup` alpha fade-in / fade-out without external dependencies
- **Runtime hot-loading** — JSON definitions in `persistentDataPath/LoadScreens/` are merged on top of bundled `Resources/LoadScreens/` files (mod support)
- **MapLoaderFramework integration** — `MapLoaderLoadScreenBridge` auto-shows on chapter change and auto-hides on map loaded (activated via `LOADSCREENMANAGER_MLF`)
- **GameManager integration** — `GameManagerLoadScreenBridge` shows on `OnBeforeChapterLoad` and hides on `OnAfterChapterLoad` (activated via `LOADSCREENMANAGER_GM`)
- **StateManager integration** — `StateManagerLoadScreenBridge` shows when `AppState.Loading` is pushed and hides when it is popped; optionally also pushes the state when shown (activated via `LOADSCREENMANAGER_SM`)
- **TitleScreenManager integration** — `TitleScreenLoadScreenBridge` shows the screen on New Game, Continue, and Load Slot (activated via `LOADSCREENMANAGER_TITLE`)
- **MiniGameManager integration** — `MiniGameLoadScreenBridge` shows the screen when a mini-game starts and hides it on completion or abort (activated via `LOADSCREENMANAGER_MGM`)
- **DOTween Pro integration** — `DotweenLoadScreenBridge` replaces coroutine fades with eased `CanvasGroup.DOFade` tweens (activated via `LOADSCREENMANAGER_DOTWEEN`)
- **Custom Inspector** — test show/hide, progress slider, and tip override controls directly in the Unity Editor
- **Odin Inspector integration** — `SerializedMonoBehaviour` for full Inspector serialization; runtime display fields marked `[ReadOnly]` in Play Mode (activated via `ODIN_INSPECTOR`)


## Installation

### Option A — Unity Package Manager (Git URL)

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL…**
3. Enter:

   ```
   https://github.com/RolandKaechele/LoadScreenManager.git
   ```

### Option B — Clone into Assets

```bash
git clone https://github.com/RolandKaechele/LoadScreenManager.git Assets/LoadScreenManager
```

### Option C — Manual copy

Copy the `LoadScreenManager/` folder into your project's `Assets/` directory.


## Folder Structure

After installation the post-install script creates the following working directories:

```
Assets/
  Resources/
    LoadScreens/      ← bundled LoadScreenDefinition JSON files (loaded via Resources.Load)
  LoadScreens/        ← external / mod JSON definitions (loaded from persistentDataPath)
  Sprites/
    LoadScreens/      ← background sprites for load screens
```


## Quick Start

### 1. Add LoadScreenManager to your scene

Create a persistent GameObject, then add:

| Component | Purpose |
| --------- | ------- |
| `LoadScreenManager` | Main orchestrator (required) |
| `LoadScreenController` | Visual sub-controller; auto-resolved if absent |

### 2. Build the Canvas

Create a full-screen Canvas (Screen Space — Overlay, `Sort Order` high so it renders on top).  
Inside it, add:

| Element | Purpose |
| ------- | ------- |
| `CanvasGroup` on root | Attach to `LoadScreenController.canvasGroup` — drives alpha fade |
| `Image` (full-screen) | Attach to `backgroundImage` |
| `Slider` or `Image` (fill) | Attach to `progressSlider` or `progressFillImage` |
| `Transform` (Image) | Attach to `spinnerTransform` |
| `TextMeshProUGUI` | Attach to `tipText` |

Set the root `GameObject` **inactive** in the Editor — `LoadScreenController` activates it on `Show()`.

### 3. Create a definition JSON

Place a `.json` file in `Resources/LoadScreens/`:

```json
{
  "id": "default_load",
  "label": "Default Loading Screen",
  "backgroundResource": "Sprites/LoadScreens/background_generic",
  "spinnerResource": "Sprites/LoadScreens/spinner_default",
  "progressFillResource": "Sprites/LoadScreens/progress_fill_default",
  "tipPool": [
    "Scanning for alien life forms...",
    "Calibrating quantum dampeners...",
    "Polishing spacesuit boots..."
  ],
  "showProgress": true,
  "showSpinner": true,
  "showTips": true,
  "tipRotationInterval": 4.0,
  "fadeInDuration": 0.3,
  "fadeOutDuration": 0.3
}
```

### 4. Show/hide from code

```csharp
var lsm = FindFirstObjectByType<LoadScreenManager.Runtime.LoadScreenManager>();

// Show with the default definition
lsm.Show();

// Show a specific load screen
lsm.Show("chapter_load");

// Update progress during an async load
lsm.SetProgress(asyncOp.progress);

// Hide when done
lsm.Hide();
```

### 5. Hook into async scene loading

```csharp
IEnumerator LoadChapterAsync(string sceneName)
{
    lsm.Show("chapter_load");

    var op = SceneManager.LoadSceneAsync(sceneName);
    op.allowSceneActivation = false;

    while (op.progress < 0.9f)
    {
        lsm.SetProgress(op.progress / 0.9f);
        yield return null;
    }

    lsm.SetProgress(1f);
    op.allowSceneActivation = true;

    yield return op;
    lsm.Hide();
}
```


## Runtime API

```csharp
var lsm = FindFirstObjectByType<LoadScreenManager.Runtime.LoadScreenManager>();

// Show / hide
lsm.Show();                      // uses defaultScreenId
lsm.Show("chapter_load");        // specific definition
lsm.Hide();

// Progress
lsm.SetProgress(0.75f);          // 0 – 1

// Override tip text at runtime
lsm.SetTip("Hacking the mainframe...");

// Override background sprite
lsm.SetBackground(mySprite);

// Query
bool showing = lsm.IsShowing;
LoadScreenDefinition def = lsm.GetDefinition("chapter_load");

// Events
lsm.OnScreenShown  += id => Debug.Log("Screen shown: " + id);
lsm.OnScreenHidden += ()  => Debug.Log("Screen hidden");

// Reload from disk (modding / hot-reload)
lsm.LoadAllDefinitions();
```


## JSON / Modding

Place definition JSON files in `Resources/LoadScreens/` (bundled) or `Application.persistentDataPath/LoadScreens/` (runtime / mods):

```json
{
  "id": "chapter_load",
  "label": "Chapter Loading",
  "backgroundResource": "Sprites/LoadScreens/background_space",
  "spinnerResource": "Sprites/LoadScreens/spinner_chapter",
  "progressFillResource": "Sprites/LoadScreens/progress_fill_chapter",
  "tipPool": [
    "The spider mutations are expanding beyond the quarantine zone...",
    "Jan Tenner is suiting up..."
  ],
  "showProgress": true,
  "showSpinner": true,
  "showTips": true,
  "tipRotationInterval": 5.0,
  "fadeInDuration": 0.4,
  "fadeOutDuration": 0.4
}
```

### JSON field reference

| Field | Type | Default | Description |
| ----- | ---- | ------- | ----------- |
| `id` | string | *(required)* | Unique identifier |
| `label` | string | `""` | Human-readable name (Editor only) |
| `backgroundResource` | string | `""` | `Resources/` path to a `Sprite` for the full-screen background (no extension) |
| `spinnerResource` | string | `""` | `Resources/` path to a `Sprite` applied to the spinner / loading-wheel `Image` (no extension) |
| `progressFillResource` | string | `""` | `Resources/` path to a `Sprite` applied to the progress-bar fill `Image` (no extension) |
| `tipPool` | string[] | `[]` | Array of tip strings; one shown at random |
| `showProgress` | bool | `true` | Show/hide the progress bar element |
| `showSpinner` | bool | `true` | Show/hide the spinner element |
| `showTips` | bool | `true` | Show/hide the tip text element |
| `tipRotationInterval` | float | `4` | Seconds between tip rotations (0 = no rotation) |
| `fadeInDuration` | float | `0.3` | Seconds for the fade-in transition |
| `fadeOutDuration` | float | `0.3` | Seconds for the fade-out transition |


## Optional Integrations

| Define | Effect |
| ------ | ------ |
| `LOADSCREENMANAGER_MLF` | Auto-show on `MapLoaderFramework.OnChapterChanged`; auto-hide on `OnMapLoaded` |
| `LOADSCREENMANAGER_GM` | Auto-show on `GameManager.OnBeforeChapterLoad`; auto-hide on `OnAfterChapterLoad` |
| `LOADSCREENMANAGER_SM` | Show when `StateManager` pushes `AppState.Loading`; hide when it pops. Optional bidirectional: push/pop `AppState.Loading` when load screen shows/hides |
| `LOADSCREENMANAGER_TITLE` | Auto-show on `TitleScreenManager.OnNewGame`, `OnContinue`, `OnLoadSlot` |
| `LOADSCREENMANAGER_MGM` | Auto-show on `MiniGameManager.OnMiniGameStarted`; auto-hide on `OnMiniGameCompleted` / `OnMiniGameAborted` |
| `LOADSCREENMANAGER_DOTWEEN` | Replace coroutine `CanvasGroup` fades with eased `DOTween` tweens (requires DOTween Pro) |

### `LOADSCREENMANAGER_MLF` — MapLoaderFramework

Add `MapLoaderLoadScreenBridge` to the manager GameObject.  
Configure `loadScreenId` in the Inspector (empty = use `defaultScreenId`).

### `LOADSCREENMANAGER_GM` — GameManager

Add `GameManagerLoadScreenBridge` to the manager GameObject.  
The bridge hooks `OnBeforeChapterLoad` → `Show()` and `OnAfterChapterLoad` → `Hide()`.

### `LOADSCREENMANAGER_SM` — StateManager

Add `StateManagerLoadScreenBridge` to the manager GameObject.  
Enable `pushStateOnShow` to make LoadScreenManager the authority (it will push/pop `AppState.Loading`);
disable it (default) to have StateManager or another bridge drive the state while LoadScreenManager
just reacts.

### `LOADSCREENMANAGER_DOTWEEN` — DOTween Pro *(optional)*

Add `DotweenLoadScreenBridge` to the manager GameObject.  
Configure `fadeInEase` and `fadeOutEase` in the Inspector.  
The bridge sets `ShowOverride` / `HideOverride` so all fades route through DOTween.

### `LOADSCREENMANAGER_TITLE` — TitleScreenManager *(optional)*

Add `TitleScreenLoadScreenBridge` to the manager GameObject.  
The bridge shows the load screen when the player starts a new game, continues, or loads a save slot.  
Use the `showOnNewGame`, `showOnContinue`, `showOnLoadSlot` toggles in the Inspector to limit which events trigger the show.  
Set `autoHideDelay` > 0 as a fallback if no `OnAfterChapterLoad` / `OnMapLoaded` bridge is present.

### `LOADSCREENMANAGER_MGM` — MiniGameManager *(optional)*

Add `MiniGameLoadScreenBridge` to the manager GameObject.  
The bridge shows the load screen on `OnMiniGameStarted` and hides it on `OnMiniGameCompleted` or `OnMiniGameAborted`.  
Individual phases can be disabled via `showOnStart`, `hideOnComplete`, `hideOnAbort` in the Inspector.
