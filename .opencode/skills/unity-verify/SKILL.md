---
name: Unity Verification
description: Use when you must prove a Unity change compiles or works without a human at the keyboard: compile check, Editor log/console, driving play mode with the Unity MCP RunCommand tool, and game view screenshots.
---

# Verifying Unity work

## Ladder — cheapest first

1. **Compile** without Unity: `powershell -File tools/compile-check.ps1`.
   It builds `Assets/Scripts/**/*.cs` against the Unity assemblies already in
   `Library/`. Fails in ~2 s and prints Roslyn errors.
2. **Import**: open the project, then read `%LOCALAPPDATA%\Unity\Editor\Editor.log`
   or call `Unity_GetConsoleLogs` with `logTypes: "Error,Exception"`.
3. **Play mode logic**: drive `Prototype.GameManager` / `CombatSession` through
   `Unity_RunCommand` and write assertions to a temp file.
4. **Play mode visuals**: capture the Game view (see recipe). Only trust a screenshot
   after checking that it is not a stale frame (compare file size/hash or read the
   state file written in the same run).

Never claim success from a compile alone when the change is visual or gameplay.

## Unity MCP RunCommand

`Unity_RunCommand` compiles a snippet with Roslyn and runs it in the Editor:

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        Prototype.GameManager gm = Object.FindFirstObjectByType<Prototype.GameManager>();
        result.Log("hp {0}", gm.Run.Hp);
    }
}
```

Rules learned the hard way:

- The class **must** be `internal class CommandScript : IRunCommand` with
  `public void Execute(ExecutionResult result)`. Top-level statements fail.
- One call, no coroutine. For multi-frame work chain
  `EditorApplication.CallbackFunction tick` on `EditorApplication.update`, and
  unsubscribe when finished.
- The snippet is compiled against `Assembly-CSharp` + editor assemblies, so it can
  reach anything public in `Prototype`.
- Calls during Play mode are discarded on exit (the tool says so).
- Multiple one-shot calls are fine, but do not call `EnterPlaymode` twice.

## Getting useful state out

`result.Log` only returns logs produced during that command's own frame. For work
spread over frames, write to a file and read it from the shell:

```csharp
File.WriteAllText("C:/Users/<user>/AppData/Local/Temp/opencode/state.txt", text);
```

Store screenshots in `%TEMP%\opencode\unity-shots\` too.

## Screenshots

`ScreenCapture.CaptureScreenshotAsTexture()` only returns a fresh frame when the
Unity window is foregrounded and the Game view repaints:

1. Bring the Editor window to the foreground (`SetForegroundWindow` on the Unity
   process' `MainWindowHandle`).
2. `UnityEditorInternal.InternalEditorUtility.RepaintAllViews()`.
3. Wait 5-10 frames, then capture.
4. Sanity-check dimensions and file hash; if the image is cropped or stale, focus
   the Game view and retry. Treat screenshots as a sanity check, not proof of logic.

`Unity_Camera_Capture` renders a camera only — the overlay canvas is not included.

## Playtest recipes

```csharp
// Jump around the run (call once per frame, Destroy is deferred)
gm.DebugSkipRoom();

// Force a win, then press the overlay button like a player would
gm.Combat.ForceVictory();
GameObject.Find("Result").GetComponentInChildren<Button>().onClick.Invoke();

// Deterministic restart
gm.StartNewRun(seed);
```

Assertions worth writing after combat changes: enemy count, `Target`, HP/block
deltas, `LastEvent` lines, `IsOver` / `PlayerWon`, hand and discard counts.

## Injecting mouse and keyboard input

`Mouse.current` / `Keyboard.current` are read-only from a script, but you can queue
events on the device and flush them yourself:

```csharp
InputSystem.QueueDeltaStateEvent(Mouse.current.scroll, new Vector2(0f, 120f));
InputSystem.Update();   // the value is visible for the rest of this frame
```

The native backend zeroes deltas on the next input update, so a
`QueueStateEvent` sent from a `RunCommand` is already gone when the game's `Update`
runs. Inject from a throwaway component that runs first instead:

```csharp
[DefaultExecutionOrder(-10000)]
internal class WheelProbe : MonoBehaviour
{
    public static bool Pending;

    void Update()
    {
        if (!Pending) return;
        Pending = false;
        InputSystem.QueueDeltaStateEvent(Mouse.current.scroll, new Vector2(0f, 120f));
        InputSystem.Update();
    }
}
```

Create it in one call (`new GameObject("WheelProbe").AddComponent<WheelProbe>()`),
set `Pending = true`, read the effect in the next call (`ZoomLabel.text`,
`anchoredPosition`, `ScrollRect.verticalNormalizedPosition`) and check the result
against a control run with the effect disabled. Every `RunCommand` recompiles into a
new assembly, so a probe created in an earlier call has a different type: create a
fresh one (`WheelProbe2`) if you need to inject again.

Pointer position is different: `Mouse.current.position` follows the real OS cursor and
overwrites queued values, so `EventSystem`/`ScrollRect` hover only happens when the
cursor is physically inside the Game view. That is fine for code that reads
`Mouse.current.scroll` directly (`MapScreen`); for UI-event behaviour, move the real
cursor or drive the handler directly.

## Gotchas

- Editing scripts while the Editor is in Play mode reloads the domain: statics reset
  (`GameManager.Instance` becomes null) and non-serialised fields rebuild as null
  (`run`, `content`), while the runtime UI objects survive. The result is a "zombie"
  session where `FindFirstObjectByType<GameManager>()` returns an object whose
  `Run` is null. Detect it with `GameManager.Instance == null || Instance != gm` and
  leave/re-enter Play mode before asserting anything.
- `result.Log` expands `{0}` but not format specifiers: `{0:0.0}` prints literally.
  Pre-format with `ToString("0.0")` and concatenate.
- Unity AI package warnings pollute the console; filter to `Error,Exception`.
- `Editor.log` is per session and huge; tail it instead of grepping everything.
- No MCP connection? Ask the user to open the project. Never hand-edit serialized
  `.unity` / `.prefab` / `.asset` YAML as a workaround.
- New scripts need a Unity-generated `.meta`; let the Editor import them before
  committing.
