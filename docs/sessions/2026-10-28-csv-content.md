# 2026-10-28 · Content in CSV and session-start context

**Duration:** afternoon
**Commits:** `1cacece` feat: author cards, enemies, relics and events from CSV ·
`3132230` docs: capture the radial map and read the full context at session start ·
this commit — skills: three design skills for the effect batch

## Objective

Two things that looked separate and are not:

1. **Take content out of the code.** Adding a card cannot mean creating a *ScriptableObject*
   by hand; it should mean writing a row in a spreadsheet.
2. **Not losing context between sessions.** The agent only read the newest note, and the
   *Pending* items from three sessions ago were never seen.

## What was done

### Content pipeline

- **`Csv.cs`** — CSV reader and writer with automatic delimiter detection (`;` for Excel in
  Catalan/Spanish, `,` otherwise) and keeping the same delimiter when saving.
- **`ContentCsv.cs`** — the format in a single place: headers with aliases, validation with
  row numbers, writing and reading the four tables. It is the only definition of the
  format; neither the runtime nor the editor keeps a copy.
- **`ContentDatabase.cs`** — loading from `Resources/Content`, cache and `Reload()`. It is
  needed because the project has *domain reload* disabled: `GameManager.StartNewRun` calls
  it so a CSV edit shows up without restarting Play.
- **Flat data** — `CardData` and `EnemyData` stop being `ScriptableObject` and become
  `[Serializable]`, so they can be validated outside Unity. `RelicData` and `EventData` are
  added, still without a game system.
- **`copies`** — new column to preserve the starting deck of 4 Strike / 4 Defend.
- **Effects in one cell** — `damage:8|vulnerable:2`, with the card text auto-generated when
  the `text` column is empty.
- **Facades** — `CardLibrary` and `EnemyLibrary` keep the API the game already used
  (`Normal`, `Pack`, `Boss(rng)`) but read from the CSV.
- **Editor window** — `Prototype → Content (CSV)`: tabs per table, add, copy and delete rows,
  reload, save and validate. It warns if the file changed outside and marks the relic and
  event tables as **inert**.
- **`tools/content-check`** — validation and *round-trip* of the CSVs with `dotnet run`,
  without opening Unity. `tools/compile-check.ps1` now compiles `Assets/Editor` too.

### Documentation and context

- `docs/content.md` — the format, the effects, the aliases and the workflow.
- Radial map capture at `docs/images/2026-10-28-radial-map.png`, inside the morning page.
- **`AGENTS.md`** — the start routine no longer reads only the latest note: it reads
  `index`, `roadmap`, `architecture`, `content`, the session index, the **three** latest
  notes in full, the *Pending* sections of **every** old one and the `git log`. The reply
  must quote the pending items, not reconstruct them from memory, and must say so if the
  roadmap and the latest note contradict each other.

## Files

### Added

- `Assets/Scripts/Content/Csv.cs`
- `Assets/Scripts/Content/ContentCsv.cs`
- `Assets/Scripts/Content/ContentDatabase.cs`
- `Assets/Scripts/Data/RelicData.cs`
- `Assets/Scripts/Data/EventData.cs`
- `Assets/Editor/ContentWindow.cs`
- `Assets/Resources/Content/` — `cards.csv`, `enemies.csv`, `relics.csv`, `events.csv`
- `tools/content-check/` — project and validator
- `docs/content.md`
- `docs/images/2026-10-28-radial-map.png`
- `.opencode/skills/game-mechanics-design/`
- `.opencode/skills/game-interface-feedback/`
- `.opencode/skills/game-playtest-planning/`

### Modified

- `Assets/Scripts/Data/CardData.cs` — flat class, `DisplayText`, `DescribeEffect`
- `Assets/Scripts/Data/EnemyData.cs` — flat class
- `Assets/Scripts/Content/CardLibrary.cs` — becomes a facade over the CSV
- `Assets/Scripts/GameManager.cs` — `ContentDatabase.Reload()` in `StartNewRun` and empty
  encounter guard
- `Assets/Scripts/UI/UIFactory.cs` — `CardTint(kind)` to colour cards by type
- `tools/compile-check.ps1` — compiles `Assets/Scripts` + `Assets/Editor`
- `AGENTS.md` — start routine, skills table and content format
- `docs/architecture.md`, `docs/index.md`, `mkdocs.yml`, `.gitignore`
- `.opencode/skills/deckbuilder-balance`, `status-effects-pipeline`, `unity-verify`
- `docs/sessions/2026-10-28-map-fog-connections.md` — capture and commits

### Deleted

- None.

## Verification

- `powershell -ExecutionPolicy Bypass -File tools/compile-check.ps1` → **0 errors,
  0 warnings**.
- `dotnet run --project tools/content-check` → 10 cards, 5 enemies, 0 relics, 0 events and
  *Content OK*. Error test done: it prints the row and exits with code 1.
- Unity, Play mode: **Deck 9 cards**, **Rooms 1/65**, HP 72/72 and no errors in the fresh
  console. The 9 cards confirm that `copies` and loading from the CSV work.
- `python -m mkdocs build --strict` → no errors.

## Decisions

- **The CSV is the single source of truth.** No `.asset` files are generated: what is in the
  spreadsheet is what the game loads, and the Editor window saves to the same file.
- **Flat classes instead of `ScriptableObject`.** They allow validating and round-tripping
  with `dotnet`, outside Unity, inside the same repository.
- **The delimiter is detected and preserved.** A file saved by Excel in Catalan must not
  become a comma-separated file that breaks decimal numbers.
- **Effects live in one cell.** One column per effect would have made the table grow without
  limit; the card text generates itself when it is not there.
- **Relics and events exist as a catalogue, not as a system.** It is better to have the
  tables and the editor ready before the code that uses them; the window says so explicitly
  to avoid selling smoke.
- **The session start reads the whole context.** The cost is a bit of reading; the cost of
  the alternative is repeating work or contradicting a decision from three sessions ago.

## Pending

- [x] Batch 2 playtest (targeting, feedback and feel of the clicks): **done**, accepted.
- [x] Fog playtest with known seeds, `14195000` included: **done**, the playtest works.
- [x] Make `opencode.json` portable. **Done** in commit `5a3be18` (`.` path and machine
      rule).
- [x] Look at the `Prototype → Content (CSV)` window with your own eyes. **Done**; it works
      for editing.
- [~] 24.9% dead ends and 11.35 rooms per run: **it stops being relevant** with the map
      redesign (reveal radius, being able to go back…). It will be decided with the new map.
- [~] The tip of the last hop ends up under a button (74 of 300 maps): **accepted** for now;
      it will be polished when the map is finished.
- [ ] Break in `card-game`, `card-game-design`, `game-mechanics-design` and
      `game-interface-feedback` on the effects batch (roadmap #5).
- [ ] Tune the `JawWormPack` and `CultistPack` packs with whatever the playtest shows.
- [ ] `NodeType.Elite` still generates a normal encounter.
- [ ] `GameManager.combat` is not cleared until `Advance()`.
- [ ] Rename `DeckScreen.OpenCount`.
- [ ] Relic and event systems (the tables are there, the code is not).

## Skills for the next session

| Skill | Why |
|---|---|
| `status-effects-pipeline` | Batch #5 is mostly effects: where they resolve, when they decay and how they look |
| `deckbuilder-balance` | The prototype's concrete numbers for the 15-20 new cards |
| `card-game` | Zone model and effect resolution: contrast it with our `CombatSession` |
| `card-game-design` | Cost curve, rarity and keywords of the set |
| `game-mechanics-design` | Write the spec of each effect (rules, state, failure modes) before touching code |
| `game-interface-feedback` | Statuses have to be visible: chips on the enemy and on the player panel |
| `game-playtest-planning` | Turn the pending playtest questions into a protocol with telemetry |

The last three are new from this session, vendored from `lvtd-llc/skills` (MIT) with the
three skills.sh auditors at **PASS**: `game-mechanics-design` (86 installs),
`game-interface-feedback` (60) and `game-playtest-planning` (74).
