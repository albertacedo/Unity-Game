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

## Gotchas

- Unity AI package warnings pollute the console; filter to `Error,Exception`.
- `Editor.log` is per session and huge; tail it instead of grepping everything.
- No MCP connection? Ask the user to open the project. Never hand-edit serialized
  `.unity` / `.prefab` / `.asset` YAML as a workaround.
- New scripts need a Unity-generated `.meta`; let the Editor import them before
  committing.
