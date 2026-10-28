# Session index

Every work session has a page with its objective, what was done, the files that changed
and what is still pending.

| Date | Session | Commits | One-line summary |
|---|---|---|---|
| 2026-10-21 | [Vertical slice](2026-10-21-vertical-slice.md) | `5ce0f83`, `700d2a7` | Map, combat, rewards and the first playable scene |
| 2026-10-21 | [GitHub and roadmap](2026-10-21-github-and-roadmap.md) | `98bd00a` | Remote repo, build scene and a 3-batch plan |
| 2026-10-22 | [Batch 1](2026-10-22-batch-1.md) | `4a352c0` | F1 panel, deck view, combat feedback |
| 2026-10-22 | [Docs and workflow](2026-10-22-docs-and-workflow.md) | — | This site and the session start/end routine |
| 2026-10-23 | [Batch 2: multi-enemy](2026-10-23-batch-2-multi-enemy.md) | this commit | Multiple enemies, target selection, compile tool and skills |
| 2026-10-25 | [Radial map: from the star to the rings](2026-10-25-radial-map.md) | `cf408e4`, `231e2cc`, `98b6304`, `4bd2079`, `7f00972` | Star → crossing-free rings, zoom/pan, simulation of the three maps, room distribution and wheel fix |
| 2026-10-27 | [External skills](2026-10-27-external-skills.md) | this commit | New skills from the skills.sh catalogue, vendored into the repo with provenance |
| 2026-10-28 | [Map with fog and connections](2026-10-28-map-fog-connections.md) | `1fc4e15`, `3132230` | Configurable fog, backtracking, 8 rings and validated lateral/diagonal connections |
| 2026-10-28 | [Content in CSV](2026-10-28-csv-content.md) | `1cacece`, this commit | Cards, enemies, relics and events in CSV, editing window and session-start context |

## Naming convention

```
docs/sessions/YYYY-MM-DD-short-title.md
```

If there are two sessions on the same day, a suffix is added (`-2`, `-3`…).

## Template

Copy `_template.md` (in the same folder) to start a new page. It is excluded from the
website, so it does not show up in the navigation.
