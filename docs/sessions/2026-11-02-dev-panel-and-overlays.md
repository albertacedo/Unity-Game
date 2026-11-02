# 2026-11-02 · Dev panel, overlay opacity and a cleaner backlog

**Duration:** afternoon (opened on 28/10, closed today)
**Commits:** `c57ba95` — fix the dev panel · `5a3be18` — portable repo + machine rule ·
`4ec943e` — verification lessons · `8901827` — drop the DEV tab, darker overlays ·
`3ca3345` — retire the finished playtests · this commit — close the session

## Objective

Close what was left open on 28/10: prove the prototype works end to end, fix the dev
panel that did not answer to F1, make the repo safe to work from two computers and
reconcile the backlog with what has actually been played.

## What was done

- **Full Play mode smoke test**: map → combat room → victory → reward → map, with HP,
  gold, deck and room counter all consistent.
- **F1 diagnosis**: the code was right (`Keyboard.current.f1Key` inside `Update`); the
  key never arrived (the Game view did not hold focus, or the laptop sends the media
  key). A `DEV` tab was added to the canvas as a safety net.
- **The user confirmed F1 works with Fn** → the tab was removed. F1, the `` ` `` key and
  the `X` button remain.
- **Machine rule for commits**: `tools/git-hooks/prepare-commit-msg` appends
  `Machine: <COMPUTERNAME>`; installed once per clone with
  `git config core.hooksPath tools/git-hooks`.
- **Portable repo**: `opencode.json` uses the `.` project path instead of the OneDrive
  one; `.gitignore` covers the Unity 6.3 `*.slnx` solution and `.vscode/`.
- **Overlay opacity**: victory `0.78 → 0.95` and deck dim `0.86 → 0.97`. The canvas
  renders in linear space, which is why the low values looked so transparent.
- **Docs**: `docs/index.md` claimed the content lived in C#; it is CSV.
- **Backlog cleanup**: the Tanda 2 playtest, the fog playtest, the content window and
  `opencode.json` are done; the radial-map numbers (24.9% dead ends, 11.35 rooms per run,
  last-jump edge, zoom questions) leave the list because the map is being redesigned.
- **`NodeType.Elite` reviewed**: the generator never creates one
  (`MapGenerator.PickType` does not return it) and, if it existed, it would run a normal
  fight. Decision: **keep it as a marker** until the map redesign.

## Files

### Added

- `tools/git-hooks/prepare-commit-msg`
- `docs/sessions/2026-11-02-dev-panel-and-overlays.md` (this page)

### Modified

- `Assets/Scripts/DevPanel.cs` — F1 + `` ` ``, `X` button, no `DEV` tab
- `Assets/Scripts/UI/CombatScreen.cs` — result overlay at `0.95`
- `Assets/Scripts/UI/DeckScreen.cs` — deck dim at `0.97`
- `AGENTS.md` — golden rule 9 (machine in the commit) and the *Commits* section
- `opencode.json`, `.gitignore`
- `.opencode/skills/unity-verify/SKILL.md` — relay on domain reload, focus for frames,
  `ScreenCapture`, `FindObjectsInactive`
- `.opencode/skills/unity-ugui-code/SKILL.md` — linear-space alpha and Game view aspect
- `docs/roadmap.md`, `docs/index.md`
- the four session notes from 23, 25 and 28 October (pending lists)
- `mkdocs.yml`, `docs/sessions/index.md`

### Deleted

- None.

## Verification

- `powershell -ExecutionPolicy Bypass -File tools/compile-check.ps1` → **0 errors,
  0 warnings**.
- `dotnet run --project tools/content-check` → 10 cards, 5 enemies, 0 relics,
  `Content OK`.
- Play mode driven through MCP: the tab opened and closed the panel, a synthetic F1
  toggled it and the `X` button closed it; victory with two enemies and a deck capture at
  `0.97`; the Unity console showed no errors.
- `python -m mkdocs build` clean, no broken links.

## Decisions

- **A test tool must not depend on a single key.** The always-visible tab was tried; it
  was removed once F1 + Fn was confirmed, but the `X` button and the backtick stay because
  they cost nothing.
- **The machine goes in the commit automatically.** A hook is deterministic; a written
  rule depends on whoever remembers it.
- **Raise the opacity instead of rebuilding the overlay.** It is the smallest change that
  fixes what the eye saw and it does not touch the screen flow.
- **`NodeType.Elite` is neither removed nor implemented now.** The map is being redesigned
  and the room mix will be decided again; that is when we will know whether elites belong.
- **The radial map stops being the reference.** Run length will be measured on the new
  map (discovery radius, going back), not with `LateralChance`.

## Pending

- [ ] `NodeType.Elite`: decide on the new map whether to implement or delete it; in the
  meantime, a comment in `MapGenerator.PickType` saying it is not generated yet.
- [ ] `GameManager.combat` is not cleared until `Advance()`.
- [ ] Rename `DeckScreen.OpenCount`.
- [ ] Tune `JawWormPack`/`CultistPack` with what the playtest says.
- [ ] Combat aesthetics: there is still a big hole in the middle.
- [ ] Batch 2b: 8 new effects and 15-20 cards.
- [ ] Relic and event systems (the tables are already there).
- [ ] Map test bench inside `tools/` if the numbers are worth reproducing.
- [ ] Map redesign: discovery radius, backtracking and run length.

## Skills for the next session

| Skill | Why |
|---|---|
| `roguelike` | The redesign is run structure and level generation |
| `roguelike-game-designer` | Decide the discovery radius, backtracking and run length without breaking risk |
| `game-mechanics-design` | Specify the new map (rules, state, failure modes) before coding it |
| `unity-ugui-code` | The map UI is built from code and now has the alpha numbers |
| `unity-verify` | Drive Play mode through MCP and take screenshots again |
| `status-effects-pipeline` + `card-game-design` | When the effects and cards batch comes up |
