# 2026-11-13 · Event rooms and shop rooms: gold gets a cost and a sink

**Duration:** night
**Commits:** `66656ac` — event rooms, from the CSV to the screen · `0d321f0` — shop
rooms, the second gold sink · this commit — close the session

## Objective

Resolve the two room types the map already rolled but still sent to the placeholder: the
events (roadmap #9) and the shop (roadmap #6). The point was to make gold mean something:
before this session it was displayed next to the HP and could not be spent.

## What was done

### Event rooms

- **Rules** in `Run/EventResolver.cs`: one event per event room, picked by weight from
  `events.csv` with a roll seeded by the run and the room, and never repeating a seen
  event while unseen ones remain (`RunState.SeenEvents`). An option is described with its
  own numbers, `lose_gold` is a real cost (greyed out and unclickable when you cannot pay
  it), healing clamps at max HP, gold clamps at 0, `max_hp` moves the ceiling and the
  current HP together, and an option that takes the last HP ends the run.
- **`UI/EventScreen.cs`** only renders: title, body, the options with their price or
  effect, and a result view with `CONTINUE` back to the map. Event damage to 0 HP goes to
  the same end screen as a lost fight.
- **Content**: six events in `events.csv` — shrine, scout, chest, pool, forge, gamble —
  covering heal, gold, max HP, damage and a gamble.
- **F1**: `OPEN RANDOM EVENT` (the panel grew from 650 to 700 px).

### Shop rooms

- **Rules** in `Run/ShopState.cs`: four duplicate-free cards from the reward pool, with a
  roll seeded by run and room, one bandage per visit, and no gold can go negative.
- **Prices** are `30 + 15 × cost`, so 0-cost cards cost 30, 1-cost 45 and 2-cost 60. The
  bandage is 15 HP for 20 gold.
- **`UI/ShopScreen.cs`** renders the four columns (card + price), the bandage and `LEAVE`,
  with HP and gold in the header. A bought card reads `SOLD`; a card you cannot pay for is
  disabled with its price in red; the bandage explains itself (`sold`, `need 20 gold`,
  `you are at full HP`).
- **One stock per visit**: `GameManager` keeps the `ShopState` while the room is open and
  redraws the screen after each purchase, so buying cannot re-roll the counter. Leaving
  the room or starting a new run clears it.
- **F1**: `OPEN SHOP`, which opens a fresh stock without needing a room on the map.

## Files

### Added

- `Assets/Scripts/Run/EventResolver.cs` (and its Unity-generated `.meta`)
- `Assets/Scripts/UI/EventScreen.cs` (and its `.meta`)
- `Assets/Scripts/Run/ShopState.cs` (and its `.meta`)
- `Assets/Scripts/UI/ShopScreen.cs` (and its `.meta`)
- `docs/sessions/2026-11-13-events-and-shop.md` — this page

### Modified

- `Assets/Scripts/GameManager.cs` — `ScreenKind.Event` and `ScreenKind.Shop`, `ShowEvent`
  with `OnEventChosen`, `ShowShop`/`LeaveShop`, the `shop` field and its resets,
  `DebugOpenEvent`/`DebugOpenShop`, `NodeType.Event`/`NodeType.Shop` cases
- `Assets/Scripts/DevPanel.cs` — the two buttons; panel height 700
- `Assets/Scripts/Run/RunState.cs` — `SeenEvents`
- `Assets/Resources/Content/events.csv` — six events
- `docs/architecture.md` — file table, the *Events* and *Shop* rules, the flow, the gold
  line and a shop row in the extension points
- `docs/content.md` — the event table fields and what `pool: reward` means for the shop
- `docs/index.md` — playable loop and gold rows
- `docs/roadmap.md` — #6 and #9 done; the gold gap replaced by the thin card pool
- `.opencode/skills/deckbuilder-balance/SKILL.md` — gold income and sinks, shop prices
- `mkdocs.yml`, `docs/sessions/index.md`, `docs/index.md` — this page

### Deleted

- None.

## Verification

- `tools/compile-check.ps1` → **0 errors, 0 warnings** after both features.
- `tools/content-check` → `6 events`, `Content OK.`
- Events, Play mode: `-15 gold / +5 max HP` landed exactly (99→84 and 72→77); the heal
  clamped at 72/72; an unaffordable option was disabled and showed its price; a lethal
  option left HP 0 and the run over at the end screen. Screenshots reviewed.
- Shop, Play mode: buying the 45-gold card took gold 99→54, the deck 9→10 and the card
  read `SOLD`, while the 60-gold card became disabled with 54 gold; with 0 gold all four
  cards and the bandage were disabled with their reasons; the bandage took HP 40→55 and
  gold 99→79 and read `sold`.
- Real map integration: a generated room (ring 2, index 5, `Shop`) opened the shop through
  `OnNodeSelected`, and `LEAVE` returned to the map with `current = 2/5 Shop visited=True`.
- Unity console after both tests: **0 errors, 0 warnings**. `shop.png` reviewed.

## Decisions

- **Two classes per room, not one.** The rules (`EventResolver`, `ShopState`) are plain C#
  and the screen only renders them: that is what makes the numbers testable without the
  Editor, and it is the same split the combat already uses.
- **The stock belongs to the visit.** Keeping `ShopState` in `GameManager` (instead of
  rebuilding it on every click) means a purchase cannot re-roll the counter, and the
  "sold" state survives the redraw.
- **Prices come from the cost, not from a new CSV column.** With seven reward cards a
  `price` column would be seven copies of the same rule; add it when cards disagree with
  it. The skill says where the line is.
- **Gold income stays event-only.** The shop is a trade-off, not a shopping spree: with 99
  gold a run buys one 2-cost card or two 1-cost ones. If prices are ever raised, income has
  to be raised first.
- **An unaffordable event option is disabled, not auto-clamped.** Paying gold is a
  decision; silently paying what the player has left would hide the cost.

## Pending

- [ ] The reward pool is seven cards with one status effect, so combat rewards and the
      shop keep offering the same small set (roadmap #5, batch 2b).
- [ ] Relics (roadmap #7): run identity and a third sink for gold.
- [ ] Rest with a choice, heal vs upgrade (roadmap #8).
- [ ] Map redesign: reveal radius, backtracking and run length (gap 2 of the roadmap).
- [ ] `NodeType.Elite` and `GameManager.ShowPlaceholder` are still unreachable; decide
      them when the room mix is settled.
- [ ] The shop has no card removal and no reroll; revisit after relics.
- [ ] Combat aesthetics: the middle of the screen is still empty.

## Skills for the next session

| Skill | Why |
|---|---|
| `status-effects-pipeline` | Batch 2b is mostly effects: where each one resolves and when it decays |
| `deckbuilder-balance` | The numbers for the new cards and the shop economy, updated this session |
| `card-game-design` | Cost curve, roles and keywords of the new set |
| `game-mechanics-design` | Specify each effect before coding it |
| `game-interface-feedback` | New statuses have to stay legible on the player and the enemies |
| `unity-ugui-code` | Every new screen is UGUI built from code with the known constants |
| `unity-verify` | Drive Play mode with assertions, screenshots and the console check |
