# Content in CSV

Cards, enemies, relics and events **are not ScriptableObjects or `.asset` files**: they
live in four CSV tables inside the project and the game loads them on startup.

The goal is to be able to design content in a spreadsheet (or in a Unity window) without
touching code or creating assets by hand.

## Where everything is

| Piece | File |
|---|---|
| Cards | `Assets/Resources/Content/cards.csv` |
| Enemies | `Assets/Resources/Content/enemies.csv` |
| Relics | `Assets/Resources/Content/relics.csv` |
| Events | `Assets/Resources/Content/events.csv` |
| Parser and format | `Assets/Scripts/Content/ContentCsv.cs` |
| Loading into the game | `Assets/Scripts/Content/ContentDatabase.cs` |
| Editing window | `Assets/Editor/ContentWindow.cs` |
| Validator without Unity | `tools/content-check/` |

The game reads the files with `Resources.Load<TextAsset>("Content/cards")`, which means
they also work in a build, not only in the Editor. `ContentDatabase` loads them once and
keeps them in memory; `CardLibrary` and `EnemyLibrary` are only facades over it.

## Two ways to edit

**Spreadsheet.** Open the CSV with Excel (or LibreOffice), edit it and save it *as CSV*.
The game and the window read it the same way. The separator is detected automatically:
`;` if Excel is in Catalan or Spanish, `,` if it is in English. Saving keeps the same
separator.

**Unity window.** **Prototype → Content (CSV)** menu. It has four tabs, a list on the
left and a form on the right, with `+`, `Copy`, `Delete`, `Reload from CSV`,
`Save to CSV` and `Validate` buttons. It saves exactly the same files, so you can
alternate between the two tools. It has no undo history: if you reload, you lose unsaved
changes (the title shows it with a `*`).

If you save while the game is in Play mode, the cache reloads, but the cards already in
the deck or in hand are the old ones: start a new run to see the changes.

## Table format

The first row is the header. Column names are case-insensitive and their order is free;
unknown columns are ignored (and lost when saving).

Effects and intents go in a single cell shaped `name:value`, separated by `|` or `+`:

```
damage:8|vulnerable:2
attack:11|block:6|attack:7
```

### cards.csv

| Column | Values | Notes |
|---|---|---|
| `id` | `[a-z0-9_]+`, unique | Identity of the card |
| `title` | text | What is shown on the card |
| `text` | text, optional | If empty, it is generated from the effects |
| `cost` | 0-3 | |
| `kind` | `attack`, `skill`, `power` | Decides the colour |
| `pool` | `starter`, `reward`, `both`, `none` | Where the card shows up |
| `copies` | integer >= 1 | Copies in the starting deck (ignored unless `starter`/`both`) |
| `effects` | list of effects | |

```csv
id;title;text;cost;kind;pool;copies;effects
strike;Strike;;1;attack;starter;4;damage:6
bash;Bash;;2;attack;starter;1;damage:8|vulnerable:2
```

### enemies.csv

| Column | Values | Notes |
|---|---|---|
| `id` | `[a-z0-9_]+`, unique | |
| `title` | text | |
| `maxhp` | integer > 0 | |
| `role` | `normal`, `pack`, `boss` | `normal` = solo; `pack` = comes in a group of two; `boss` = boss |
| `intents` | `attack`/`block` list | Repeats in a cycle |

```csv
id;title;maxhp;role;intents
jaw_worm;Jaw Worm;44;normal;attack:11|block:6|attack:7
```

The combat room picks like this: 55% one `normal` enemy; otherwise, two `pack` enemies
(they may repeat). The boss of the final room is drawn from among the `boss` rows.

### relics.csv

| Column | Values |
|---|---|
| `id` | `[a-z0-9_]+`, unique |
| `title` | text |
| `description` | text |
| `rarity` | `common`, `uncommon`, `rare`, `boss`, `event` |

!!! warning "Not used yet"
    The relic system does not exist: the table is a catalogue to have the content
    written down, but no screen reads it.

### events.csv

| Column | Values |
|---|---|
| `id` | `[a-z0-9_]+`, unique |
| `title` | text |
| `text` | text |
| `weight` | integer > 0 |
| `options` | `Label => effect+effect | Other => effect` |

```csv
id;title;text;weight;options
old_shrine;Old Shrine;A mossy shrine hums.;10;Pray => heal:15 | Desecrate => gain_gold:25+lose_hp:5 | Leave => nothing
```

Event effects: `nothing`, `heal`, `lose_hp`, `gain_gold`, `lose_gold`, `max_hp` and
`add_card:card_id` (for example `add_card:strike`).

!!! warning "Not used yet"
    The event room is still a placeholder. The table and the format are already validated
    so the content is ready when the screen arrives.

## Combat effect vocabulary

| Effect | Aliases | Meaning |
|---|---|---|
| `damage` | `attack` | Damage to the target |
| `damage_all` | `all`, `aoe` | Damage to every living enemy |
| `block` | `defend` | Player block |
| `draw` | | Draw cards |
| `gain_energy` | `energy` | Extra energy this turn |
| `vulnerable` | `vuln` | Applies Vulnerable (x1.5 incoming damage) |

Adding a new effect is still code work: `EffectKind`, the `switch` in
`CombatSession.ApplyEffects` and, if it is a status, a field in `EnemyInstance`. The
table only gets the new name.

## Validation

The same rules apply to the game, the window and the console tool:

- `id` unique, lowercase, no accents and no spaces.
- Title mandatory (and description for relics, text for events).
- Card cost between 0 and 3; HP, effect values, intents and weights greater than 0.
- No card without effects; no enemy without intents; no event without options.
- `add_card` must point to a card that exists.
- A warning if the enemy table has no `normal`, no `pack` or no `boss` rows.

If the content has errors, the game **does not crash**: it loads what it can and writes
every problem to the console with the file and the row
(`cards.csv: row 12: unknown effect 'dammage'`).

## Verifying without opening Unity

```powershell
dotnet run --project tools/content-check
```

It reads the four tables, validates them, checks that writing and reading back gives the
same file and exits with code 1 if there are errors. With the `--write` argument it
rewrites the files in normalised form (useful after editing them by hand).

`powershell -File tools/compile-check.ps1` compiles `Assets/Scripts` **and**
`Assets/Editor`, so the window is checked too without opening the Editor.

## Known limitations

- There are no card ScriptableObjects: you cannot drag them into an Inspector field.
  If we ever need that, a CSV → `.asset` importer can be written; today it does not exist
  on purpose.
- Unknown columns are lost when saving from the window.
- The window has no undo; the CSV does have history in git.
- The `id` is the identity of the card (and will be for save/continue): changing it
  afterwards is the same as deleting the card and creating another one.
