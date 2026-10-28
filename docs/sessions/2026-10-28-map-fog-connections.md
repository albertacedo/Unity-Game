# 2026-10-28 · Map with fog and connections

**Duration:** afternoon
**Commits:** `1fc4e15` feat: tune foggy radial map navigation · `1cacece` feat: author
cards, enemies, relics and events from CSV · this commit — docs: capture the radial map

## Objective

Try a radial map with progressively revealed information, configurable backtracking and
more options to switch branches without making the roads unreadable.

## What was done

- Added fog of war with a configurable reveal radius (1-5).
- Visibility is cumulative: it is computed from every visited room.
- Backtracking can now be configured from the F1 panel.
- The map went from 7 to 8 rings, with one more room before the bosses.
- Same-ring connections are now straight lines.
- Connections were adjusted: 55% normal lateral and 35% diagonals between branches of
  consecutive rings.
- Connections are validated against crossings and against proximity to the node rectangles;
  if a radial connection is not safe, an alternative branch is tried.
- The defaults are fog off and backtracking off.

## Capture

![Radial map with the new connections](../images/2026-10-28-radial-map.png)

*A new run with a random seed, zoom at 100% and fog off (the default). You can see the
8 rings, the straight lateral connections, the diagonals between branches and the four
bosses on the outer ring.*

## Files

### Added

- None.

### Modified

- `Assets/Scripts/Run/RunState.cs` — map options and default values.
- `Assets/Scripts/GameManager.cs` — cumulative reveal and development controls.
- `Assets/Scripts/DevPanel.cs` — F1 controls for fog, radius and backtracking.
- `Assets/Scripts/UI/MapScreen.cs` — hidden nodes, straight connections and availability.
- `Assets/Scripts/Mapgen/MapGenerator.cs` — eight rings, diagonal connections and geometric
  validation.
- `Assets/Scripts/Mapgen/MapLayout.cs` — removed the curved-road geometry.
- `docs/architecture.md`, `docs/roadmap.md`, `docs/index.md`, `docs/sessions/index.md`,
  `mkdocs.yml` — documentation and navigation.

### Deleted

- None.

## Verification

- `tools/compile-check.ps1`: compilation OK, 0 errors and 0 warnings.
- `python -m mkdocs build`: OK.
- `git diff --check`: no whitespace errors.
- The Unity console could not be checked because the Editor was not detected by the MCP
  environment in this session.

## Decisions

- Fog accumulates the visibility of every visited room, not only the current room.
- Diagonals only connect consecutive rings and pick a neighbouring angular branch.
- Geometric safety is validated during generation, not only while the UI draws.
- Provisions stay pending until the playtest shows that exploration needs to be limited.

## Pending

- [x] Visual playtest of the problematic seeds (especially `14195000`). **Done**: the
      playtest works and the fog does not need retouching for now.
- [x] Measure how many diagonal connections survive the geometric filter. **Done**.
- [~] Decide whether provisions are needed: **it is not a decision for this map**. It will
      be settled with the redesign (reveal radius, being able to go back…).

## Skills for the next session

| Skill | Why |
|---|---|
| `deckbuilder-balance` | Prepare the effects and cards batch with the prototype's numbers. |
| `status-effects-pipeline` | Integrate the new effects into combat and its UI. |
| `card-game` | Review zones, effect resolution and card flow. |
| `card-game-design` | Design the cost curve, roles and keywords of the new cards. |
