# Architecture

The whole game is built **from code**: the scene only holds a `Game` object with the
`GameManager` component. There are no prefabs and no hand-assembled screens. Content, on
the other hand, is **data**: four CSV tables in `Assets/Resources/Content` (see
[Content in CSV](content.md)).

## Flow

```
GameManager.Awake()
  ├─ creates Canvas + EventSystem
  ├─ creates DevPanel (F1)
  └─ StartNewRun(seed)
        └─ ShowMap() → MapScreen
              └─ node click → StartCombat() / ShowRest() / ShowPlaceholder()
                    └─ CombatScreen → OnCombatFinished()
                          ├─ victory → ShowReward() → Advance() → ShowMap()
                          └─ defeat  → ShowEnd()
```

`GameManager.Clear()` destroys every child of `Screens` before drawing the next screen.
The layers that must survive (the `DevPanel`) hang off the Canvas, not off `Screens`.

## Files

| File | Responsibility |
|---|---|
| `Scripts/GameManager.cs` | Entry point, screen switching, debug helpers |
| `Scripts/Run/RunState.cs` | HP, gold, seed, map and deck (what survives between rooms) |
| `Scripts/Mapgen/MapGenerator.cs` | Seeded, deterministic radial map |
| `Scripts/Mapgen/MapLayout.cs` | Geometry shared by map and screen (radius, angle and ellipse) |
| `Scripts/Combat/CombatSession.cs` | Combat rules (turns, energy, damage, block, target) |
| `Scripts/Combat/EnemyInstance.cs` | State of one enemy inside a fight (HP, block, Vulnerable, intent) |
| `Scripts/Content/ContentCsv.cs` | CSV table format: reading, writing and validation |
| `Scripts/Content/ContentDatabase.cs` | Loads `Resources/Content/*.csv` and caches them |
| `Scripts/Content/CardLibrary.cs` | Facade: starting deck, reward pool and encounters |
| `Scripts/Data/CardData.cs` | Card, effects and text generated from the effects |
| `Scripts/Data/EnemyData.cs` | Enemy, role (solo/group/boss) and intent queue |
| `Scripts/Data/RelicData.cs` | Relic (catalogue; the system does not exist yet) |
| `Scripts/Data/EventData.cs` | Event and options (catalogue; the screen does not exist yet) |
| `Editor/ContentWindow.cs` | *Prototype → Content (CSV)* window for editing the tables |
| `Scripts/UI/UIFactory.cs` | Helpers to build UI from code |
| `Scripts/UI/MapScreen.cs` | Map, nodes and connections |
| `Scripts/UI/CombatScreen.cs` | Combat, hand, visual feedback |
| `Scripts/UI/RewardScreen.cs` | Pick 3 cards |
| `Scripts/UI/SimpleScreens.cs` | Rest, placeholder and end of run |
| `Scripts/UI/DeckScreen.cs` | Overlay with the whole deck |
| `Scripts/DevPanel.cs` | Test panel (F1) |

## Implemented rules

- **Content**: cards, enemies, relics and events are data, not code. They live in
  `Assets/Resources/Content/*.csv`, are loaded once by `ContentDatabase` and there is no
  content `.asset` at all. Format, rules and editing workflow are in
  [Content in CSV](content.md).
- **Map**: radial and deterministic per seed. A `Start` node at the centre, 8 rings
  (6 room rings + 1 boss ring) and 4-7 bosses on the outer ring. Lateral connections
  within the room rings are generated with 55% probability and are always drawn as
  straight lines. 35% of the rooms may also have a diagonal connection to the
  neighbouring angular branch of the next ring. Diagonals are rejected if they cross a
  road, touch another node's rectangle or pass within 8 units of its visual edge.
- **Room type**: ring 1 is always combat; rings 2-5 roll 50% combat, 15% rest, 10% shop
  and 25% event. Bosses are always bosses, and `Elite` is not generated yet.
- **Connections**: between two neighbouring rings, each room links to the room (or boss)
  closest in angle. The map ends up split into wedges centred on the small ring. Rooms on
  the same ring are joined with straight lines, and the ring just before the bosses is
  complete.
- **Geometry**: `MapLayout` is the only source of positions. The map is an ellipse
  1.45 times wider than tall, to make the most of the 16:9 screen. Each boss sits on the
  angular centre of its rooms, so that no edge leaves its ring (an edge steeper than
  ~35° would enter the inner ring and cross roads).
- **Zoom and pan**: mouse wheel (or the `+` / `−` buttons) to zoom between 60% and 220%,
  drag with the right or middle button to move the map. The map is clipped to its area
  (`RectMask2D`), so it never covers the HUD, and `RESET VIEW` returns to the initial
  state. Rooms can be clicked at any zoom level. While an overlay is open (the deck),
  `MapScreen.Update` ignores wheel and drag: scrolling the cards must not move the map
  behind.
- **Movement**: every choice moves exactly one node and you can walk in both directions.
  Visited rooms do not repeat content, but you can cross them to get back to a branch.
  Room types only reveal themselves within the configured radius (`1-5`, `1` by default);
  bosses are visible from the start. The topology and the roads are always visible. The
  F1 panel lets you try fog, reveal radius `1-5` and backtracking without restarting.
  There are no provisions yet: first we check whether movement and single-use content
  already create enough commitment.
- **Combat**: 3 energy per turn, draw 5, block resets every turn.
- **Multi-enemy**: 1 normal enemy or a pack of 2 weak variants. Each enemy has its own
  HP, block, Vulnerable and intent queue; the fight ends when they all fall.
- **Target**: attack cards hit `Target`; when it dies, targeting moves to the next living
  enemy. `DamageAll` (Cleave) hits every living enemy.
- **Vulnerable**: multiplies incoming damage by 1.5, one counter per enemy (and one for
  the player) that drops by 1 at the end of the round.
- **Run**: 72 HP, 99 gold (still unused).

## Extension points

| I want to… | Where I touch it |
|---|---|
| Add a card | `Resources/Content/cards.csv` (or the *Prototype → Content* window): one row |
| Add an enemy | `Resources/Content/enemies.csv`: one row with its `role` |
| Add an effect | `EffectKind` (in `CardData`) + the `switch` in `CombatSession.ApplyEffects` + a field in `EnemyInstance` if it is a status + the name in `ContentCsv` |
| Add a relic or an event | `relics.csv` / `events.csv`; the game system still has to be written |
| Add a screen | `GameManager.ScreenKind` + a `ShowX()` + a class in `UI/` |
| Change how an element looks | `UIFactory` (affects every screen at once) |

## Tools

- `tools/compile-check.ps1` compiles `Assets/Scripts` and `Assets/Editor` against the
  DLLs in `Library/` using the .NET SDK, without opening Unity. It is the first
  verification step; after that you still need to check the console and try Play mode.
- `tools/content-check/` validates the content CSVs without opening Unity: same parser,
  same rules and a round-trip test of every table.
- `Prototype → Content (CSV)` is the content editing window inside the Editor.
- `.opencode/skills/` holds the agent's reusable instructions. There are two kinds: the
  **project** ones (UI from code, balance, verification, effects: what only this repo
  knows) and the **external** ones, copied from the [skills.sh](https://www.skills.sh/)
  catalogue with the source and date in the footer. At the end of every session both are
  reviewed: the project ones are updated, and for general knowledge an external skill is
  looked up before writing a new one. See
  [External skills](sessions/2026-10-27-external-skills.md).

## Conventions

- `Prototype` namespace, sub-namespaces per subsystem (`Prototype.UI`, `Prototype.Combat`…).
- One public `MonoBehaviour` per file, named after the file.
- `[SerializeField] private` fields; nothing `public` in the Inspector.
- New Input System always (`Keyboard.current`, never `Input.GetKey`).
- `.meta` files are generated by Unity: never by hand.
