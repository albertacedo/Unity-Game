# Roadmap

## Current gaps

1. **Gold is useless** — it is displayed and cannot be spent (shop pending).
2. **There is only one effect** (Vulnerable) and 10 cards: combat still does not have
   enough decisions per card. The CSV format is ready to grow without touching code.
3. **Multi-enemy has to be played by human hands** — it has been there since Batch 2, but
   the playtest that says whether the packs are tuned is missing.

## Priorities

Ordered by value / effort ratio:

| # | What | Why | Effort |
|---|---|---|---|
| 1 | Development panel (F1) | Makes everything else testable in seconds | Low ✅ |
| 2 | Deck view + card detail | Without it the decisions cannot be evaluated | Low ✅ |
| 3 | Combat feedback | Floating numbers and flashes: changes how it feels | Low-mid ✅ |
| 4 | Multi-enemy + target selection | The biggest jump in depth | Mid-high ✅ |
| 5 | 8 new effects + 15-20 cards | Weak, Strength, Dexterity, multi-hit, area… | Mid |
| 6 | Shop | Gives gold a purpose | Mid |
| 7 | Relics | Identity for each run | Mid |
| 8 | Rest with a choice (heal vs upgrade) | Real decisions on the map | Low-mid |
| 9 | Events | Narrative variety | Mid |
| 10 | Save/continue + manual seed | Longer sessions and repeatability | Mid |
| 11 | AI art and sound | Polish (lots of generation time) | High |

## The three batches

### Batch 1 — make the prototype observable ✅

Dev panel + deck view + combat feedback. It adds no content, but it makes what is already
there visible and testable.

### Batch 2 — make combat interesting

Multi-enemy with target selection ✅ + the new effects pack + 15 cards. This is where the
game starts to have real decisions.

### Batch 3 — make the run interesting

Shop + relics + rest with a choice. Gold and the map stop being decorative.

## Batch 2b — information fog on the radial map

The map is radial and the player can walk in both directions. The topology is visible, but
room type only reveals itself at distance 1; bosses are always visible. Visited rooms do
not repeat content and are used to get back to a branch.

These decisions are still open:

- **Provisions**: if the player explores every branch, make each move consume a
  provision. A playtest without this resource comes first.
- **Frontier versus current position**: for now you can only enter a direct neighbour;
  nodes connected to other visited rooms are known but cannot be teleported to.
- **Run size**: measure how many rooms visited and how much backtracking the fog produces
  with `LateralChance = 0.55` and the diagonals validated in `MapGenerator`.

## Playtest questions

To answer with the game in hand:

- With 3 energy and 5 cards, do you end combats with energy to spare or unable to do
  anything?
- Does a combat last 2-3 turns or 4-6? A normal combat should last 4-6.
- With 72 HP, do you reach the boss alive or is the map too harsh?
- Do you always pick the same reward card? If so, the pool is too small.
- With the radial map: do you end up over-exploring (too many combats) or do you go
  straight for the boss? How many rooms per run? (The simulation says 11.4 on average,
  minimum 6.)
- Do you miss a limit on exploration, or is health enough already?
- With zoom: do you end up using the wheel, or is the whole map already readable at 100%?
- Is the outer ring clear enough as a "boss choice", or do the lateral arcs make it
  confusing?
