# Unity Game — Dev Log

2D card roguelike prototype, *Slay the Spire* style, built in Unity 6.3 (URP 2D).

This site is the **work log**: one page per *live coding* session, with what was done,
what was learned and which files changed.

!!! tip "Published site"
    The log is online at **<https://albertacedo.github.io/Unity-Game/>** and is updated
    with `python -m mkdocs gh-deploy` at the end of every session.

## Current state

| Item | Status |
|---|---|
| Playable loop | ✅ map → combat → reward → map |
| Map | ✅ radial: centre, 8 rings, 4-7 bosses, configurable fog, zoom/pan and safe connections |
| Combat | ✅ turns, energy, block, Vulnerable, multi-enemy with targeting |
| Content | 10 cards · 3 enemies (1 boss) + packs, and relic/event tables (still inert), all in editable CSV |
| Test tools | ✅ F1 panel, deck view, combat feedback, `tools/compile-check.ps1` |
| Content tools | ✅ `Prototype → Content (CSV)` window and `tools/content-check` |
| Gold | ⚠️ shown but cannot be spent (shop pending) |
| Audio / art | ❌ placeholder |

## How to run it

1. Open the project with **Unity 6000.3.26f1**.
2. Open `Assets/Scenes/Prototype.unity`.
3. Press **Play**.
4. In game: **F1** opens the development panel.

## Stack

- **Unity** 6000.3.26f1 (Unity 6.3), URP 2D
- New **Input System** (no legacy)
- **UI**: UGUI + TextMeshPro, built from code
- **Content**: cards and enemies in C# (`CardLibrary`, `EnemyLibrary`)

## Sessions

See the [session index](sessions/index.md), the [roadmap](roadmap.md) or how the
[CSV content](content.md) is edited.

| Date | Session | What you will find |
|---|---|---|
| 2026-10-21 | [Vertical slice](sessions/2026-10-21-vertical-slice.md) | Map, combat, rewards and a playable scene |
| 2026-10-21 | [GitHub and roadmap](sessions/2026-10-21-github-and-roadmap.md) | Remote repo, build scene and a 3-batch plan |
| 2026-10-22 | [Batch 1](sessions/2026-10-22-batch-1.md) | F1 panel, deck view, combat feedback |
| 2026-10-22 | [Docs and workflow](sessions/2026-10-22-docs-and-workflow.md) | This site and the session start/end routine |
| 2026-10-23 | [Batch 2: multi-enemy](sessions/2026-10-23-batch-2-multi-enemy.md) | Several enemies, target selection and skills |
| 2026-10-25 | [Radial map](sessions/2026-10-25-radial-map.md) | From the star to the rings, zoom/pan, simulation of the three maps and the wheel fix |
| 2026-10-27 | [External skills](sessions/2026-10-27-external-skills.md) | New skills from the skills.sh catalogue, vendored with provenance |
| 2026-10-28 | [Map with fog and connections](sessions/2026-10-28-map-fog-connections.md) | Configurable fog, backtracking, 8 rings and validated lateral/diagonal connections |
| 2026-10-28 | [Content in CSV](sessions/2026-10-28-csv-content.md) | Cards, enemies, relics and events editable from Excel, plus session-start context |
