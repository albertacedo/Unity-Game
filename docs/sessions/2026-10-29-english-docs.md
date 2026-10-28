# 2026-10-29 · English documentation and stable URLs

**Duration:** night
**Commits:** `de90152` — English docs and renamed pages · `2e736a4` — merge the latest remote work into those docs · this commit — close the session

## Objective

Move the documentation site to English, rename its public pages to English slugs and keep
the old Catalan URLs working.

## What was done

- Translated every page under `docs/`: home, architecture, content, roadmap, session index,
  template and the nine existing session notes.
- Renamed pages, session slugs and map preview assets to English. Internal links, image
  references and the MkDocs navigation now use those names.
- Changed Material for MkDocs to English (`language: en`), including the theme toggle labels
  and the site description.
- Added `mkdocs-redirects` and ten redirects so old published Catalan URLs serve a canonical
  redirect page to the new English one.
- Added `requirements.txt` with the pinned MkDocs dependencies, so a fresh machine can build
  the site with `pip install -r requirements.txt`.
- Updated `AGENTS.md` and the affected project skills to point to the renamed documents and
  state that only the conversation remains in Catalan.
- Integrated five newer commits from the other machine. The current English docs keep their
  factual updates: completed playtests, portable `opencode.json`, current content counts and
  the radial-map redesign decision.
- Installed the repository's commit-message hook with `git config core.hooksPath tools/git-hooks`.
  New commits include a `Machine:` trailer.

## Files

### Added

- `requirements.txt` — MkDocs, Material and redirects dependencies
- `docs/architecture.md`, `docs/content.md` — renamed English pages
- `docs/sessions/2026-10-29-english-docs.md` — this page

### Modified

- `docs/index.md`, `docs/roadmap.md`, `docs/sessions/index.md` — English text and current
  state
- `docs/sessions/*.md` — English translations, English slugs and merged pending-item updates
- `docs/img/*.svg`, `docs/images/2026-10-28-radial-map.png` — English filenames and preview
  headings
- `mkdocs.yml` — English theme, navigation and redirect map
- `AGENTS.md` — documentation language, renamed paths and fresh-machine build instruction
- `.opencode/skills/deckbuilder-balance/SKILL.md`,
  `.opencode/skills/gameplay-architecture/SKILL.md`,
  `.opencode/skills/status-effects-pipeline/SKILL.md` — renamed documentation links

### Deleted

- The previous Catalan-named Markdown pages, replaced by English paths with redirect pages at
  build time.

## Verification

- `powershell -ExecutionPolicy Bypass -File tools/compile-check.ps1` → **0 errors,
  0 warnings**.
- `python -m mkdocs build --strict` → exit code **0**.
- The deployed site was checked at `/`, `/architecture/` and every old Catalan session URL:
  current pages return 200 and the old ones return 200 redirect pages to their English
  replacements.
- No conflict markers or Catalan documentation text remain under `docs/`.

## Decisions

- Public documentation and stable file names are English; the conversation remains in Catalan.
- Old page URLs are retained through `mkdocs-redirects`. Renamed SVG image assets do not have
  redirects because the plugin only produces HTML redirect pages.
- The remote updates were merged into the English versions rather than restoring the Catalan
  paths, so the site has one language and one canonical URL per page.

## Pending

- [ ] Batch 2b: add the new combat effects and 15-20 cards (roadmap #5).
- [ ] Tune `JawWormPack` and `CultistPack` using the completed playtest.
- [ ] Redesign the map: reveal radius, backtracking and run length are still open.
- [ ] Build the shop, relic and event gameplay systems.
- [ ] Decide whether to leave compatibility copies for the three old SVG asset filenames.

## Skills for the next session

| Skill | Why |
|---|---|
| `status-effects-pipeline` | Implement effects consistently in combat state, rules and UI. |
| `card-game` | Check card zones and effect resolution before expanding the card set. |
| `card-game-design` | Design the new cards' cost curve, roles and keywords. |
| `deckbuilder-balance` | Tune the new cards and the existing packs against the prototype numbers. |
| `unity-verify` | Verify the expanded combat in Play mode and capture its UI. |
