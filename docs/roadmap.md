# Roadmap

## Current gaps

1. **Gold is useless** — it is displayed and cannot be spent (shop pending).
2. **There is only one effect** (Vulnerable) and 10 cards: combat still does not have
   enough decisions per card. The CSV format is ready to grow without touching code.
3. **The map has to be redesigned.** The current radial one (fog + `LateralChance`) worked,
   but the shape of the run will be decided with the new map: reveal radius, being able to
   go back and how many rooms a run should last. The 24.9% dead-end and 11.35 rooms-per-run
   figures no longer rule.

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

**Status (2026-10-28):** the playtest works and the fog is accepted. The decisions that were
left (provisions, frontier vs. current position, run size) move to the map redesign; they
are not closed here.

- **Provisions**: if the player explores every branch, make each move consume a provision.
  Open decision in the new map.
- **Frontier versus current position**: for now you can only enter a direct neighbour;
  nodes connected to other visited rooms are known but cannot be teleported to. It is the
  point to revisit with "being able to go back".
- **Run size**: the `LateralChance = 0.55` figures (11.35 rooms per run) stop being the
  reference; they will be measured on the new map.

## Playtest questions

To answer with the game in hand:

**Combat and rewards** (Batch 2 already tested for clicks, targeting and feedback):

- With 3 energy and 5 cards, do you end combats with energy to spare or unable to do
  anything?
- Does a combat last 2-3 turns or 4-6? A normal combat should last 4-6.
- With 72 HP, do you reach the boss alive or is the map too harsh?
- Do you always pick the same reward card? If so, the pool is too small.
- Are the packs (`JawWormPack`, `CultistPack`) the right size and damage?

**Map** (these will be asked again on the new map):

- Do you end up over-exploring (too many combats) or do you go straight for the boss? How
  many rooms per run?
- Do you miss a limit on exploration, or is health enough already?
- Is the reveal radius generous enough to decide the route, or do you lack information?
- Does being able to go back make you explore more or give you agency?
- Zoom (wheel vs. buttons) and how the outer ring reads: these will be sharpened with the
  new map.
