# 2026-11-09 · Overlay counter, a combat with a clear end, and the project back in the Hub

**Duration:** opened on 6/11 afternoon (code and commit), closed on 9/11 (docs)
**Commits:** `aacdab8` — shared overlay input counter and a combat that ends with the
fight · this commit — close the session

## Objective

Clear the four small items the 2026-11-02 session left behind (`NodeType.Elite`,
`GameManager.combat`, `DeckScreen.OpenCount`, pack tuning) before opening the next big
batch, and get the project listed in the Unity Hub again.

## What was done

### Cleanup

- **`NodeType.Elite`** — `MapGenerator.PickType` now says in its own doc comment why it
  never returns `Elite`: the room mix is decided again in the map redesign. The marker is
  kept; the decision moves to that session with the rest of the room mix.
- **Combat lifetime** — `GameManager.OnCombatFinished` reads `PlayerWon` and clears
  `combat` **before** the reward or the end screen, instead of leaving it alive until
  `Advance()`. The redundant clear in `Advance()` is gone: one owner, one moment.
- **Overlay counter** — `DeckScreen.OpenCount` becomes `OverlayInput.Count`
  (`Assets/Scripts/UI/OverlayInput.cs`), a static counter with `Push()`/`Pop()` that any
  overlay can use. `MapScreen` asks `OverlayInput.Count > 0` before reading the wheel, so
  the shop and the events will not need to touch the map to block zoom.
- **Pack tuning** — asked and answered with the playtest in hand: the packs feel right.
  No number changed and the roadmap question is marked as answered, so the next session
  does not re-open it.

### Unity Hub

The Hub (Store build 3.22.5) did not list the project. Two things came out of it:

- The list does **not** live in `projects-v1.json` any more. Hub 3.22 keeps it in
  `hub.db` (SQLite, table `projects(path, data, updated_at)`), and the meta key
  `projects_json_migrated=1` turns the JSON file into a one-way legacy mirror. Editing
  the JSON, which is what the file name suggests, changes nothing.
- Writing the row with `path` + `lastModified` is enough: on the next start the Hub
  **derives** the entity from the project files (title, version `6000.3.26f1`, changeset,
  URP, build target, VCS provider `github`, cloud project id) and persists it. The
  project is back in the list.

## Files

### Added

- `Assets/Scripts/UI/OverlayInput.cs` (and its Unity-generated `.meta`)
- `.opencode/skills/unity-hub/SKILL.md` — how the Hub stores its project list
- `docs/sessions/2026-11-09-overlay-counter-and-unity-hub.md` — this page

### Modified

- `Assets/Scripts/GameManager.cs` — combat cleared in `OnCombatFinished`
- `Assets/Scripts/Mapgen/MapGenerator.cs` — why `Elite` is not generated
- `Assets/Scripts/UI/DeckScreen.cs` — registers with `OverlayInput`
- `Assets/Scripts/UI/MapScreen.cs` — the wheel guard reads `OverlayInput.Count`
- `docs/architecture.md` — new file-table row, fight lifetime, overlay rule
- `docs/roadmap.md` — the pack question is answered
- `.opencode/skills/unity-verify/SKILL.md` — the MCP catalog follows the Editor, not the
  session start; Smart App Control note
- `AGENTS.md` — `unity-hub` in the project-skills table
- `mkdocs.yml`, `docs/index.md`, `docs/sessions/index.md`

### Deleted

- None.

## Verification

- `powershell -File tools/compile-check.ps1` → **0 errors, 0 warnings** (twice: before
  and after the docs work).
- Play mode, driven with MCP and an A/B on the same injected wheel event:
  deck open → `overlays=1` and the zoom stays at `100%`; close with the real `CLOSE`
  button → `overlays=0`, the same input zooms to `112%`. So the counter lives, and it is
  what blocks the map.
- Play mode, combat lifetime: first room → combat screen (`Combat != null`) → `VICTORY`
  → `CONTINUE` → reward screen with `Combat == null` and `InCombat == false`.
- Unity console after both tests: no errors or exceptions.
- Unity Hub: after reopening, the row in `hub.db` contains the derived entity (version,
  changeset, pipeline, VCS); the project appears in the list.
- `content-check` ran green on 6/11 (`10 cards, 5 enemies, Content OK`). On 9/11 the same
  command fails **before** reading any CSV with `FileLoadException 0x800711C7`, which is
  Smart App Control (`PolicyState=1`) blocking the freshly built assembly. Not a content
  problem; see *Pending*.

## Decisions

- **The counter belongs to the overlays, not to the deck.** `OverlayInput` is three lines
  and one name; keeping `DeckScreen.OpenCount` would have meant renaming it again when
  the shop arrives.
- **A fight stops existing when it ends.** The reward screen is not a combat screen, and
  the XML doc already promised `Combat == null` between rooms.
- **No pack numbers without a felt problem.** The benchmark said the total HP was right
  and the playtest said the fights were fine; changing damage because a formula says so
  would have been tuning by spreadsheet.
- **The Hub database is the source of truth.** `projects-v1.json` is a compatibility
  mirror; the entity is derived from the project files, so a minimal row is enough and
  nothing in the project has to be faked.

## Pending

- [ ] `content-check` is blocked by Smart App Control (`dotnet` + freshly built DLL).
      Either allow-list the output or accept that the CSV check runs inside the Editor
      until the policy is off.
- [ ] Batch 2b: 8 new effects and 15-20 cards (roadmap #5), the next big item.
- [ ] Map redesign: reveal radius, backtracking and run length (roadmap #3).
- [ ] `NodeType.Elite`: implement or delete once the new room mix is decided.
- [ ] Shop (roadmap #6), relics and events (the tables are already there).
- [ ] Combat aesthetics: the big hole in the middle of the screen.
- [ ] Map test bench inside `tools/` if the run-length numbers are worth reproducing.
- [ ] The four Git LFS hooks in `tools/git-hooks/` (`post-checkout`, `post-commit`,
      `post-merge`, `pre-push`) are untracked and must not be staged.

## Skills for the next session

| Skill | Why |
|---|---|
| `status-effects-pipeline` | Batch 2b is mostly effects: where each one resolves and when it decays |
| `deckbuilder-balance` | The prototype numbers for the 15-20 new cards and the enemies they meet |
| `card-game-design` | Cost curve, roles and keywords of the new set |
| `game-mechanics-design` | Specify each effect (rules, state, failure modes) before coding it |
| `game-interface-feedback` | Strength/Weak/Dexterity have to be visible on enemies and player |
| `unity-ugui-code` | The status chips are UGUI built from code, with the known alpha values |
| `unity-verify` | Drive Play mode; the MCP catalog now follows the Editor |
| `unity-hub` | Only if the Hub loses the project again |
