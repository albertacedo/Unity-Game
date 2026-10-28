# 2026-10-22 · Docs and workflow

**Commits:** this very commit.

## Objective

Have a place where every work session is recorded and fix **how a session starts and
ends**, so it does not have to be repeated by hand every day.

## What was done

- Created the **MkDocs** site (Material theme) with a home page, architecture, roadmap and
  sessions.
- Rebuilt the **4** previous sessions from the git history.
- Added a session **template** and the naming convention
  `YYYY-MM-DD-short-title.md`.
- Wrote the **session workflow** in `AGENTS.md`:
  - **at the start**: `git pull`, read the last session page and propose the next steps
  - **at the end**: create the page, add it to the navigation, build the site, publish it,
    commit and push
- `.gitignore`: excluded `site/` (site output) and `.cache/`.
- **Published to GitHub Pages**: the repo was private and free Pages requires a public
  repo, so it was made public. The site lives at
  <https://albertacedo.github.io/Unity-Game/> and is deployed with
  `python -m mkdocs gh-deploy --force` (`gh-pages` branch).

## Files

### Added

- `mkdocs.yml`
- `docs/index.md`
- `docs/architecture.md`
- `docs/roadmap.md`
- `docs/sessions/index.md`
- `docs/sessions/_template.md`
- `docs/sessions/2026-10-21-vertical-slice.md`
- `docs/sessions/2026-10-21-github-and-roadmap.md`
- `docs/sessions/2026-10-22-batch-1.md`
- `docs/sessions/2026-10-22-docs-and-workflow.md` (this file)

### Modified

- `AGENTS.md` — *Session workflow* section, including the publish step
- `.gitignore` — `site/` and `.cache/`

## Verification

- `python -m mkdocs build` generates 8 pages with no warnings.
- `mkdocs gh-deploy --force` publishes the `gh-pages` branch.
- `https://albertacedo.github.io/Unity-Game/` answers **200** after ~50 s with the title
  *Unity Game - Dev Log*.

## Decisions

- **Material** as the theme: side navigation, search and dark mode with no extra work.
- The pages live in `docs/sessions/` and are added by hand to the `nav` in `mkdocs.yml`
  (there are few and that keeps the order explicit).
- The template starts with an underscore and is **excluded** from the website.
- **Public repo + `gh-pages` branch**: it is the free way through Pages and needs no
  external service. Rejected alternative: a separate docs repo, more maintenance.

## Pending

- [ ] Make `opencode.json` portable (dragged over from the previous session)
- [ ] Playtest of Batch 1 (F1 panel, deck view, combat feedback)
- [ ] Batch 2: multi-enemy with target selection
