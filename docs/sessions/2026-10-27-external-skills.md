# 2026-10-27 · External skills from skills.sh

**Duration:** afternoon
**Commits:** `3183de6` — skills: nine skills vendored from the skills.sh catalogue

## Objective

Stop writing skills by hand. The engineering and design knowledge we use every session
(uGUI, MonoBehaviour lifecycle, Input System, card zones, *run* economy) is not ours: it
exists in public *agent skills* catalogues. Today's job is to **bring it in from outside**,
versioned in the repo, instead of reinventing it.

## Method: how they are found and installed

1. **The API is closed.** `https://skills.sh/api/v1/skills/search?q=...` returns
   `401 authentication_required`: it wants a Vercel OIDC token. Not an option for a local
   project.
2. **The web search does work.** The `https://www.skills.sh/search?q=<term>` pages are
   server-rendered: opening them in a browser shows the whole list (skill, repo, installs).
   The pages `https://www.skills.sh/<owner>/<repo>/<slug>` carry the install command and the
   **security audits**.
3. **Vendored installation.** Instead of `npx skills add` (which writes to the agent's
   global folder and does not stay in the repo), the upstream is downloaded and copied to
   `.opencode/skills/<slug>/`, with an *Upstream source* footer.

The full loop, to repeat it whenever a skill needs updating or adding:

```powershell
# 1. Find the real path inside the repo (routes vary: skills/, skills/unity/, SKILL.md at the root)
Invoke-RestMethod "https://api.github.com/repos/<owner>/<repo>/git/trees/main?recursive=1" |
  Select-Object -ExpandProperty tree | Where-Object { $_.path -like "*SKILL.md" }

# 2. Download the SKILL.md and the files it references (references/, scripts/)
Invoke-WebRequest "https://raw.githubusercontent.com/<owner>/<repo>/main/<path>/SKILL.md" `
  -OutFile ".opencode/skills/<slug>/SKILL.md" -UseBasicParsing
```

## Searched and discarded

| Search | Result |
|---|---|
| `unity` | `unity-technologies/skills` (official) and `gamedev-skills/awesome-gamedev-agent-skills` are the two good deposits |
| `deckbuilding` | Almost everything is *Magic*: Commander deck generators. The interesting one is `card-game-design` |
| `card game` | `gamedev-skills/.../card-game` and `unity-card-games` (12 installs, no history: discarded) |
| `roguelike` | `gamedev-skills/.../roguelike` (grid engine) and `roguelike-game-designer` (design, more useful for us) |
| `game balance` | `game-balance-economy` and several Godot/Roblox/Phaser ones, which do not concern us |
| `procedural generation` | Godot/Unreal or with 1-6 installs: nothing worth evaluating |

Discarded from the start: `unity-ecs-patterns` (we do not use ECS), `unity-navmesh`,
`unity-tilemap-2d` and `2d-pixel-perfect` (there is no tile world or final art yet).

## What was installed

All nine skills have **PASS** from the three skills.sh auditors (Gen Agent Trust Hub,
Socket, Snyk) on the day they were downloaded.

| Skill | Repo | Installs | What we want it for |
|---|---|---|---|
| `ui-ugui` | `unity-technologies/skills` | 6.6K | Canvas, `RectTransform` and `ScrollRect` hierarchies — it brings `references/scrollview-setup.md`, which is exactly the ground of the wheel bug |
| `optimize-text-mesh-pro` | `unity-technologies/skills` | 5.8K | TMP atlases and fallbacks: the whole UI is TextMeshPro |
| `unity-csharp-scripting` | `gamedev-skills/awesome-gamedev-agent-skills` | 6.3K | Lifecycle, serialization and coroutines, declared for Unity 6.3 LTS |
| `unity-input-system` | `gamedev-skills/awesome-gamedev-agent-skills` | 5.9K | Input Actions and *rebinding*; the only input system we use |
| `card-game` | `gamedev-skills/awesome-gamedev-agent-skills` | 6.4K | Zone model (deck → hand → play → discard) and effect resolution |
| `roguelike` | `gamedev-skills/awesome-gamedev-agent-skills` | 6.5K | *Run* structure, permadeath and level generation |
| `card-game-design` | `omer-metin/skills-for-antigravity` | 150 | Cost curve, rarity, keywords and set design |
| `game-balance-economy` | `lvtd-llc/skills` | 149 | Economy, rewards and risk-reward: the shop we still have pending |
| `roguelike-game-designer` | `hanjo92/roguelike-game-designer-skill` | 6 | *Run* review: replayability, accessibility, decisions with weight |

## Files

### Added

- `.opencode/skills/ui-ugui/SKILL.md` and `references/scrollview-setup.md`
- `.opencode/skills/optimize-text-mesh-pro/SKILL.md`
- `.opencode/skills/unity-csharp-scripting/SKILL.md` and `scripts/check_unity_project.py`
- `.opencode/skills/unity-input-system/SKILL.md`
- `.opencode/skills/card-game/SKILL.md`
- `.opencode/skills/roguelike/SKILL.md`
- `.opencode/skills/card-game-design/SKILL.md`
- `.opencode/skills/game-balance-economy/SKILL.md`
- `.opencode/skills/roguelike-game-designer/SKILL.md`

### Modified

- `AGENTS.md` — the skills table is split into **project** and **external**, with source
  repo and when to use them; the rule not to rewrite the body of an external skill is
  added, plus `status-effects-pipeline`, which was missing from the table.
- `.opencode/skills/card-game-design/SKILL.md` — the upstream `description` line came
  without a space (`balanceUse when...`) and with the keyword list glued on; it is
  normalised so the model can use it.
- `docs/architecture.md` — the tools section explains the two kinds of skill and links to
  this page.
- `docs/sessions/2026-10-23-batch-2-multi-enemy.md` — note in the *Skills* section so the
  five original ones are not read as the only ones there are.
- `docs/sessions/_template.md` — the *Skills for the next session* section reminds you to
  search skills.sh first.
- `ProjectSettings/ProjectSettings.asset` and the `LiberationSans SDF - Fallback` atlas —
  they are not from this work; they were pending changes in the *working tree* and were
  committed with the rest (`runInBackground: 1` to be able to test with the window behind).

## Verification

- The nine skills show up in the harness catalogue (15 in total, with the five project
  ones).
- Relative links still hold: `references/scrollview-setup.md` inside `ui-ugui` and
  `scripts/check_unity_project.py` inside `unity-csharp-scripting` are in the folder.
- Auditors PASS on all nine skills.sh pages.
- `python -m mkdocs build` with no errors.

## Decisions

- **Vendor, do not install globally.** The repo is what has to know what it works with; a
  global install does not travel between machines and does not stay in the history.
- **The body belongs to upstream.** Only the provenance footer is added (repo, path, date,
  audits) and, if needed, the normalisation of a broken `description`. Divergences with the
  project are documented in the project skill, not by editing the external one.
- **When they clash, the project one wins.** `unity-ugui-code` knows the UI is built from
  code and at 1600×900; `ui-ugui` assumes prefabs and the Editor. The external ones are
  general doctrine.
- **Searching outside is step 1.** The end-of-session routine in `AGENTS.md` now says, in
  this order: search skills.sh for the general knowledge behind the next 1-2 roadmap items;
  write a project skill only for what upstream cannot know (this repo's numbers, classes,
  pitfalls and verification); and update the existing ones if the session changed how they
  work.

## Pending

- [ ] Break in `card-game` + `card-game-design` on the effects and cards batch (roadmap #5).
- [ ] See whether `roguelike`/`roguelike-game-designer` contribute anything with a node map
      instead of a grid, or whether only the *run* economy part survives.
- [ ] Review the external skills at the start of every big session: if upstream has changed,
      download them again with the loop above.

## Skills for the next session

| Skill | Why |
|---|---|
| `status-effects-pipeline` | Batch #5 is mostly effects: where they resolve and when they decay |
| `card-game` | Zone model and effect resolution: contrast it with our `CombatManager` |
| `card-game-design` | Cost curve and keywords for the 15-20 new cards |
| `deckbuilder-balance` | The prototype's concrete numbers (the external one does not know them) |
