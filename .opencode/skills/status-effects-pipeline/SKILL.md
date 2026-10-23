---
name: Status Effects Pipeline
description: Use when adding or changing combat effects and statuses (Weak, Strength, Dexterity, Frail, multi-hit, AoE, exhaust) in this prototype: where the effect resolves, when it decays, how the UI shows it, and the numbers to start from.
---

# Adding a combat effect

## Where things live today

| Piece | File |
|---|---|
| `EffectKind` enum + `CardEffect(Kind, Value)` | `Assets/Scripts/Data/CardData.cs` |
| Effect resolution switch | `CombatSession.ApplyEffects` |
| Damage + Vulnerable math, one choke point | `CombatSession.DealDamageToEnemy` |
| Player status (`PlayerVulnerable`, `PlayerBlock`) | `CombatSession` fields |
| Per-enemy status (`Vulnerable`, `Block`) | `Combat.EnemyInstance` |
| Card values | `Content/CardLibrary.cs` |
| Display | `UI/CombatScreen.cs` (`Refresh`, `EnemyWidget`) |

## How an effect resolves (current behaviour)

1. `PlayCard` -> `LastEvent = ""` -> `ApplyEffects(card)` -> `CheckCombatEnd()`.
2. `Damage` hits `Target`; `DamageAll` loops over alive enemies; `Vulnerable`
   applies to `Target` only; `Block` / `Draw` / `GainEnergy` affect the player.
3. `DealDamageToEnemy` is the only place where incoming damage is modified
   (Vulnerable x1.5, then block absorbs, then HP).
4. Enemy turns reset that enemy's block, act, then each enemy's Vulnerable decays
   by 1 at the end of the round. `PlayerVulnerable` decays at the start of the
   player turn; `PlayerBlock` resets at the start of the player turn.

## Add an effect end to end

1. **Data**: add the field/duration to `EnemyInstance` (enemies) and/or
   `CombatSession` (player). Keep counters as `int` stacks/remaining turns.
2. **Enum**: add an `EffectKind` value at the end of the enum (order is serialized
   by name, but appending keeps diffs clean).
3. **Damage math**: modify `DealDamageToEnemy` and the enemy-attack path in
   `EnemyTurn` only. If both outgoing and incoming modifiers grow, extract
   `int OutgoingDamage(int baseDamage)` / `int IncomingDamage(int rawDamage)` so
   later effects do not scatter across the class.
4. **Decay**: write the decay rule in the existing end-of-round block, once.
   Block is reset, not decayed; durations decrease by 1 per round.
5. **UI**: add a small status chip row to `EnemyWidget` and the player panel
   (e.g. `Weak 2`, `Str 3`); update it in `Refresh`.
6. **Log**: one line per application/resolution, e.g. `"Jaw Worm gains 2 Weak."`
7. **Balance**: check numbers against the `deckbuilder-balance` skill.
8. **Docs**: tick the roadmap item and update the effect table in
   `docs/arquitectura.md`.

## Numbers to start from (Slay-the-Spire-like)

| Effect | Rule | Starting value |
|---|---|---|
| Vulnerable | incoming damage x1.5 | 2 turns |
| Weak | target deals 25% less damage | 2 turns |
| Strength | +N damage per attack hit | +2 |
| Dexterity | +N block per block card | +2 |
| Frail | -25% block gained | 2 turns |
| Multi-hit | repeat a damage effect N times | 3x3 |
| AoE | hits every alive enemy | 8 at 1 energy |
| Exhaust | card leaves the fight after play | card-specific |

Rules of thumb: percentage debuffs last 2 turns, flat buffs are permanent for the
fight, and Strength/Dexterity apply **per hit** (multi-hit gets them each time).
Weak and Frail round down; Vulnerable uses `amount * 3 / 2` (integer math).

## Pitfalls

- Applying Vulnerable and Damage in the same card: resolve damage first, then the
  debuff, unless the card text says otherwise.
- Do not read `Target` after it may have died inside `ApplyEffects`; capture
  `EnemyInstance victim = Target` before the loop if the card has multiple effects.
- Multi-hit must not check `IsOver` between hits in a way that skips the log line;
  stop only when the enemy dies, and log each hit.
- Statuses on the player need an explicit decay point; `StartPlayerTurn` is the
  current one.
- The UI currently shows only HP, block and intent. Statuses are invisible until
  you add chips — do not ship a status the player cannot see.
- Keep the effect value meaning consistent: duration for debuffs, amount for
  buffs. Mixing both in `CardEffect.Value` is fine, but document it in the card text.

## Test recipe

1. `tools/compile-check.ps1`.
2. Reach a two-enemy fight (F1 -> SKIP ROOM) and apply the new effect.
3. End the turn and compare damage with and without it (the `LastEvent` log and
   the HP delta give the numbers).
4. Re-run with a fresh `StartNewRun(seed)` to confirm determinism.
