---
name: Unity UGUI From Code
description: Use when building, restyling or debugging runtime UI in this project. Every screen is C# (UGUI + TextMeshPro) on a 1600x900 overlay canvas; no prefabs or scene-authored UI exists.
---

# Building UGUI from code

All UI lives in `Assets/Scripts/UI/`. The canvas is created at runtime by
`UIFactory.MakeCanvas()`: `ScreenSpaceOverlay`, `CanvasScaler` with reference
resolution 1600x900 and `matchWidthOrHeight = 0.5`. There is no prefab and no
layout authored in the Editor.

## Workflow

1. Read `Assets/Scripts/UI/UIFactory.cs` first. Reuse `MakeLabel`, `MakeButton`,
   `MakePanel`, `MakeBar`, `MakeCard`, `Place` and `Stretch` instead of adding raw
   components by hand.
2. Each screen is a `MonoBehaviour` with `Init(...)` that builds it and subscribes
   to events, and `OnDestroy` that unsubscribes.
3. Compile with `tools/compile-check.ps1` before opening the Editor.
4. Verify visually in Play mode (see the `unity-verify` skill).

## Geometry rules

`UIFactory.Place(rt, anchor, pivot, position, size)` pins a rect:

- `anchor` is the point of the parent it sticks to; `pivot` is the point of itself.
- `(0,0)` bottom-left, `(1,1)` top-right, `(0.5,1)` top-center.
- `position` is an offset from the anchor; negative Y goes down from a top anchor.
- `UIFactory.Stretch` fills the parent (optionally inset).

When items must keep exact sizes, use a `HorizontalLayoutGroup` with
`childControlWidth = false`, `childControlHeight = false` and both
`childForceExpand*` false — see the hand and enemy row in `CombatScreen`.

## Pitfalls that cost real time

- **Labels and decorative images must not eat clicks.** Set
  `raycastTarget = false`. `MakeLabel` and `MakeBar` already do; custom `Image`s do not.
- **`Destroy` is deferred.** Rebuilding a list leaves the old children alive for
  the rest of the frame. Rebuild in one pass, then let layout settle a frame before
  measuring or screenshotting.
- **Buttons need a graphic target**: `button.targetGraphic = image`, otherwise
  hover/disabled states do nothing.
- **Selection state**: keep the widget alive and toggle `Outline.enabled` +
  background colour + `button.interactable`. Do not destroy and rebuild the panel.
- **Overlay canvas is invisible to cameras.** A camera render does not include
  `ScreenSpaceOverlay` UI; use a Game view screenshot instead.
- **Canvas is 1600x900.** Existing margins are 24-40 px; reuse them so screens match.

## Recipes

- Player panel + HP bar: `CombatScreen.Build()`.
- Clickable enemy cards with per-enemy state: `CombatScreen.EnemyWidget`.
- Card tile (cost box, title, body): `UIFactory.MakeCard` (150x205).
- Transient feedback (floating number, flash): `SpawnPopup` + `FadeFlash` coroutines.
- Overlay that survives screen changes: parent it to the canvas, not to `Screens`
  (see `GameManager.ShowDeck`).

## Checklist before adding a screen

1. Does an existing helper cover the layout? If not, add it to `UIFactory`.
2. Does the screen mutate game state directly? It should call into the state class.
3. Did you wire a way to reach it from `GameManager` (and `DevPanel` for testing)?
4. Is every interactive element raycastable and every label not?
