# AGENTS.md — Unity 2D Top-Down Prototype

This file tells AI agents how to work in this repository. Read it before making changes.

## Project facts

- **Engine:** Unity `6000.3.26f1` (Unity 6.3), installed at `C:\Program Files\Unity\Hub\Editor\6000.3.26f1`
- **Render pipeline:** URP 2D (`Assets/Settings/UniversalRP.asset`, `Renderer2D.asset`)
- **Input:** new Input System only (`activeInputHandler: 1`). Assets: `Assets/Settings/InputSystem_Actions.inputactions`
- **Game type:** 2D top-down prototype. No gameplay code exists yet — everything is greenfield.
- **Key package:** `com.unity.ai.assistant` (Unity's in-editor assistant) is installed.
- **MCP:** the `unity` server in `opencode.json` connects to the running Editor through `~/.unity/relay/relay_win.exe`.

## Golden rules

1. **Unity must be open** for scene/prefab/asset work. If MCP tools are unavailable, tell the user to open the project instead of editing serialized files by hand.
2. **Never hand-edit `.meta` files.** Let Unity generate them.
3. **Never hand-edit `.unity`, `.prefab`, `.asset`, `.controller`, or `.anim` YAML** unless there is no alternative and the user explicitly asks. Prefer MCP tools or the Editor UI. Serialized YAML has GUIDs, fileIDs and object references that are easy to corrupt.
4. **Never touch generated folders:** `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `Build/`, `*.csproj`, `*.sln`.
5. **Do not add or upgrade packages** without asking. `Packages/manifest.json` is user-owned.
6. **Do not change project-wide settings** (physics, quality, graphics, input handling, tags/layers, build scenes) without asking first.
7. **Placeholder art is fine.** Use Unity primitives / solid-color sprites for the prototype; do not block on assets.
8. **Commit small and often** once the Editor compiles cleanly.

## Session workflow (agreed with the user)

The user works in live-coding sessions. Follow this every time, without being asked.

### First message of a session

1. Run `git pull --ff-only`. If incoming changes touch `Assets/Scenes`, `Assets/Prefabs`
   or other serialized assets, ask the user to close Unity first.
2. Read the newest file in `docs/sessions/` (plus `docs/index.md` on a fresh clone).
3. Reply with 3-5 lines on where the project stands and propose 2-3 concrete next steps
   from `docs/roadmap.md`. Ask which one to take if it is not obvious.

### When the user says the session is over

Words like *"ja estic per avui"*, *"tanquem"*, *"fi de sessió"* mean: wrap up.

1. Make sure the project compiles with zero errors (Unity console via MCP, or Unity's
   `Editor.log`).
2. Write `docs/sessions/AAAA-MM-DD-<slug>.md` from `docs/sessions/_plantilla.md`:
   objective, what was done, files added / modified / deleted, verification, decisions,
   pending items.
3. Add the page to `nav` in `mkdocs.yml`, to the table in `docs/index.md` and to
   `docs/sessions/index.md`.
4. Run `python -m mkdocs build` and fix any broken link.
5. Commit code and docs together and push to `origin/main`.
6. Publish the site with `python -m mkdocs gh-deploy --force`
   (live at <https://albertacedo.github.io/Unity-Game/>).

### Documentation language

The docs site (`docs/`) is written in **Catalan**, like the conversation. Code,
comments and commit messages stay in **English**.

## Folder layout

```
Assets/
  Scenes/     gameplay scenes (SampleScene is the template scene)
  Scripts/    all C# runtime code
  Prefabs/    reusable GameObjects
  Art/Sprites 2D art
  Audio/      music and SFX
  UI/         UI prefabs, sprites, fonts
  Settings/   URP + input assets (do not reorganize)
```

## C# conventions

- Namespace: `Prototype` (sub-namespaces for subsystems, e.g. `Prototype.Player`).
- One public MonoBehaviour per file, file name must match the class name.
- Serialize with `[SerializeField] private` fields; avoid `public` fields.
- Cache references in `Awake()`; never call `GetComponent`/`Find*` every frame.
- Use the new Input System (`InputAction`, `InputSystem_Actions`, or `UnityEngine.InputSystem`), never `Input.GetKey`.
- Prefer `Rigidbody2D` + `Collider2D` for movement; use `FixedUpdate` for physics.
- Public API gets XML-doc comments only where the intent is non-obvious.
- No `Debug.Log` left in shipped paths; use it while debugging and remove after.

## Definition of done for a change

1. Scripts compile with **zero errors** (check the Unity console after import).
2. The change is visible in Play mode or in the Editor without manual repair.
3. New assets have Unity-generated `.meta` files, committed together with the asset.
4. The commit message states *what* changed and *how it was verified*.

## Verifying work

- Read the Unity console for compile errors before claiming success:
  `Unity_ReadConsole` (via MCP), or ask the user to check the Console window.
- Prefer small verifiable steps over large speculative refactors.
