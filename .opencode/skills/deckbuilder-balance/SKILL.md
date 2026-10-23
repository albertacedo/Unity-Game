---
name: Card Game Balance
description: Use when designing or tuning cards, enemies, intents, encounters, rewards or run difficulty in this Slay-the-Spire-like prototype. Current economy, damage benchmarks and a tuning loop.
---

# Balance for a Slay-the-Spire-like

## Current rules (code is the source of truth)

- Player: 72 HP, 3 energy per turn, draw 5, hand cap 10, block resets every turn.
- Vulnerable: x1.5 incoming damage, decays by 1 at the end of the round
  (per enemy and for the player).
- Starter deck: 4 Strike (1 energy, 6 damage), 4 Defend (1 energy, 5 block),
  1 Bash (2 energy, 8 damage + 2 Vulnerable).
- Content lives in `Assets/Scripts/Content/CardLibrary.cs` (`CardLibrary` +
  `EnemyLibrary`); enemies get per-fight state in `Combat.EnemyInstance`.
- Map: 6 floors with combat / elite / boss / rest / shop / event nodes.

## Benchmarks (use before inventing numbers)

| Thing | Reference |
|---|---|
| 1 energy | ~6 damage, ~5 block, ~1 card drawn |
| 2-cost attack | 1.6-1.8x a 1-cost attack, never a flat 2x |
| Normal single enemy | 22-50 HP, 6-11 damage per turn |
| Normal pack (2 enemies) | total HP ~= one normal enemy, total damage ~= 1.5x one enemy |
| Boss | ~90 HP, 14-18 damage every other turn |
| Normal fight length | 4-6 turns, ending at 60-80% HP |
| Boss fight length | 8-12 turns |
| AoE premium | worth ~1.5x single target when 2+ enemies are alive |

## Encounter design

- Intents must telegraph: the attack number is the promise the player blocks against.
- Mix patterns, e.g. attack -> block -> attack (Jaw Worm) vs escalating attacks
  (Cultist). A pure attack loop is only fair at low damage.
- Multi-enemy is not "one enemy x2": cut HP to ~50% and damage to ~50-60%.
  `EnemyLibrary.RandomEncounter` returns either one regular enemy (55%) or a pack of
  two weaker variants (`JawWormPack`, `CultistPack`).
- Duplicated titles get `A`/`B` suffixes from `CombatSession.UniqueTitle`.

## Observed numbers (first playtest, seed 530950890)

- Pack: Jaw Worm 22 HP / attack 4, Cultist 24 HP / attack 3-6. Two enemies
  attacking every turn equals ~7-9 damage, i.e. one Defend covers most of it.
- Solo: Jaw Worm 44 HP / attack 11, Cultist 50 HP / attack 6-9.
- Boss: The Guardian 90 HP, 14 attack + 10 block + 18 attack.
- Combat with 2 enemies finishes in 3-5 turns; watch for turns where both enemies
  attack and the player has no block in hand.

## Tuning loop

1. Change one variable at a time in `CardLibrary` / `EnemyLibrary`.
2. Run `powershell -File tools/compile-check.ps1`.
3. Play 2-3 fights with F1 -> SKIP ROOM, using the dev panel (`KILL TARGET`,
   `+1 ENERGY`, `WIN COMBAT`).
4. Record: turns taken, HP lost, energy left over, cards played, times you switched
   target, and whether you ever had nothing useful to do.
5. Check the playtest questions in `docs/roadmap.md` (§ *Preguntes de playtest*).

## Anti-patterns

- A card that is strictly better than Strike/Defend at the same cost.
- Difficulty through HP inflation only: longer fights, same decisions.
- Scaling block loops before the game has scaling enemies.
- Reward pools with one obvious pick. If you always choose the same card, the pool
  is too small or one card is overpriced.
- Multi-enemy fights where every enemy attacks every turn; alternate intents so the
  player has a turn to breathe.
- Relying on a single lucky draw: with 5 cards in hand, a 1-of in a 9-card deck
  appears often enough to hide bad balance.

## What to log in playtests

- Turn count and HP at the end of each fight.
- Damage taken per turn vs block generated per turn.
- Energy wasted (unspent at end of turn).
- Which enemy was targeted and why.
- Which reward card was picked and which were ignored.
