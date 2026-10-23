---
name: Gameplay Architecture
description: Use when adding a feature or subsystem to this prototype (shop, relics, events, save/load, status effects, new screens): how to keep rules pure and testable, wire screens through GameManager, and keep scope small.
---

# Architecture rules of thumb

- **Rules are plain C#, views are MonoBehaviours.** `CombatSession` owns the fight
  and knows nothing about UGUI; `CombatScreen` renders it and forwards clicks.
  Keep that split: every new system gets a pure state class plus a screen.
- **One owner per piece of state:** `RunState` is the run, `CombatSession` the fight,
  `GameManager` the current screen. Views never mutate state directly.
- **Events, not polling.** `CombatSession.Changed` and `DamageDealt` tell the view
  when to rebuild. Always unsubscribe in `OnDestroy`.
- **Content is data.** `CardLibrary` / `EnemyLibrary` build `ScriptableObject`s in
  code. Add a field to the data type before adding a special case in the rules.
- **Screens are a function of state.** `GameManager.Clear()` then `ShowX()`; overlays
  (deck) hang off the canvas root so they survive screen changes.
- **Determinism.** All randomness comes from a seeded `System.Random`; keep one rng
  per system so a seed reproduces a run.

## Adding a feature — checklist

1. Write the rule in a small plain class with the narrowest public surface.
2. Put numbers/content in the library, not in the view.
3. Build or extend a screen with `UIFactory`; keep the old screen's visual language.
4. Add a `DevPanel` hook so the feature is reachable in two clicks.
5. Verify: compile check -> play mode -> screenshot/state assertions.
6. Update `docs/arquitectura.md` (file table + rules) and `docs/roadmap.md`.

## Scope discipline

Prototype first: no save system, localization, animation framework or generic
event bus until the core loop is fun. One honest combat beats five half-built
systems. Placeholder art is fine; feedback (numbers, flashes, timing) is not.

## Feel — what players actually notice

- Every input gets immediate feedback (popup, flash, log line).
- No more than ~300 ms between action and visible result.
- One readable line per event; newest event last or on top, consistently.
- Telegraph danger before it lands (enemy intents).
- Numbers should be legible at a glance: big delta, small context.

## Code cleanliness (project conventions)

- Namespace `Prototype` + subsystem (`Prototype.Combat`, `Prototype.UI`, ...).
- `[SerializeField] private` fields; no public inspector fields.
- Cache references in `Awake()`; no `GetComponent` / `Find*` per frame.
- New Input System only (`Keyboard.current`, actions), never `Input.GetKey`.
- No leftover `Debug.Log` in shipped paths.

## Designing a new system (example: shop)

1. `ShopState` (plain class): stock list, prices, rerolls, `Buy(card) -> bool`.
2. `ShopLibrary`/content: what can appear.
3. `ShopScreen`: renders `ShopState`, forwards clicks, closes with a callback.
4. `GameManager`: `ScreenKind.Shop`, `ShowShop(node)`, `Advance(node)` on exit.
5. `DevPanel`: button to open the shop from the map.
6. Docs: architecture table row + roadmap tick.
