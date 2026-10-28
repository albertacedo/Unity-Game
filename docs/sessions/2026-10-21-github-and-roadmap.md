# 2026-10-21 · GitHub and roadmap

**Commits:** `98bd00a` (build scene)

## Objective

Put the project in a remote repository and decide how to continue.

## What was done

- Created the **`github.com/albertacedo/Unity-Game`** repository and pushed the 3 initial
  commits to `main`.
- Authentication with **Git Credential Manager** (browser login); the credentials are
  stored and the following pushes ask for nothing.
- `Prototype.unity` marked as the **only build scene**.
- Discussed how to work from a **second PC**:
  - you need Unity Hub + **exactly** version `6000.3.26f1`
  - Git, OpenCode and the MCP relay (Unity installs it when it opens the project)
  - `Library/` does not travel: it is regenerated on the first open
  - clone **outside OneDrive** on the second machine
  - close Unity before running `pull` and do not work on both PCs at once
- **Roadmap**: identified the gaps (unused gold, a single enemy, no deck view) and agreed
  on **3 batches**. Batch 1 chosen.

## Files

### Modified

- `ProjectSettings/EditorBuildSettings.asset` — only `Prototype.unity`

## Verification

`git ls-remote --heads origin` returns `98bd00a`, the same as the local `HEAD`; clean
working tree.

## Decisions

- **Public repo** on GitHub, `main` branch, small commits with a message saying *what*
  changed and *how* it was verified.
- The repo's `opencode.json` carries **absolute paths** (`C:\Users\alber\...`). It works on
  this PC, but on the second one it will have to be edited or made portable. Still pending.
- **Git LFS** is not configured yet: the repo only holds code. It will be needed when art
  comes in.

## Pending

- [ ] Make `opencode.json` portable (option: take it out of the repo + `opencode.example.json`)
- [ ] Batch 1: test tools, deck view and combat feedback
