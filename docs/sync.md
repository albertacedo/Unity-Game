# Two machines

The repo is edited from two computers. The rule is simple: **the first thing a session
does is `tools/sync-check.ps1`**, which brings this machine up to date with whatever the
other one pushed and then checks what landed. It replaces the bare `git pull`.

```powershell
powershell -File tools/sync-check.ps1
```

## What the routine compares

| Step | What it looks at | Why it matters here |
|---|---|---|
| 1. Prerequisites | `core.hooksPath`, `git lfs version` | The commit hook adds the `Machine:` trailer; without LFS the art and audio commits arrive as pointer files |
| 2. Local state | uncommitted and untracked files | A pull that overwrites your work is the only unrecoverable mistake |
| 3. Remote | `git fetch`, ahead/behind, commit list with `Machine:` trailers | Tells you *which* machine did what and whether you still have something unpushed |
| 4. Apply | `git pull --ff-only` | No merge commits: a divergent history is stopped and left for a human |
| 5. Environment | `ProjectSettings/ProjectVersion.txt` vs the Hub, `Packages/manifest.json` diff | The other machine may have bumped Unity or a package |
| 6. Verify | compile check, `content-check`, `mkdocs build`, skills | Each kind of change has exactly one check |

The files that arrived are grouped first, because the group decides the check:

| Group | Paths | Check run |
|---|---|---|
| `code` | `Assets/**/*.cs`, `tools/*.ps1` | `tools/compile-check.ps1` |
| `content` | `Assets/Resources/Content/*.csv` | `dotnet run --project tools/content-check` |
| `docs` | `docs/**`, `mkdocs.yml` | `python -m mkdocs build` |
| `skills` | `.opencode/skills/**` | listed by name: the agent re-reads them |
| `unity-version` | `ProjectSettings/ProjectVersion.txt`, `Packages/**` | Editor installed? package diff |
| `serialized` | scenes, prefabs, `.asset`, `ProjectSettings/**` | Unity has to be closed for the pull, then reopened to reimport |

## What it refuses to do

The routine stops, pulls nothing and exits with `1` when:

- your uncommitted edits touch a file the pull has to write;
- an untracked file is a file the incoming commit adds;
- serialized Unity assets are incoming **and the Editor is running** (Unity rewrites
  those files on focus, so pulling under a live Editor is how scenes get corrupted).

In all three cases the message says what to do. Nothing is ever discarded, stashed or
force-pushed by the script.

## Flags and exit codes

| Flag | Effect |
|---|---|
| `-NoPull` | Report only: says what is behind without touching the working copy |
| `-SkipVerify` | Pull, but run no check |
| `-AuditSkills` | Also compares the vendored skills against upstream |

| Exit | Meaning |
|---|---|
| `0` | In sync, and the checks for what landed passed |
| `1` | A human decision is needed (conflict, Unity open, a check failed) |
| `2` | No network, the fetch did not run |

## Skills have two sources of change

1. **The other machine** edited or added a skill: that arrives with the pull and step 6
   lists the paths. Nothing else to do, the skill is picked up on the next read.
2. **Upstream** changed the original `SKILL.md`: that is `tools/skills-check.ps1`, which
   reads the `Source` line of every footer, downloads the raw file and compares it
   ignoring line wrapping. What counts as a change is the **body**: the `description`
   line is ours (upstream ones are sometimes broken), so a different description is
   reported as "local description kept" and never as drift. `-Update` rewrites only the
   bodies that changed, keeps our description, and refreshes the footer (fetch date,
   upstream commit, installs, audits) at the same time.

```powershell
powershell -File tools/skills-check.ps1           # report
powershell -File tools/skills-check.ps1 -Update   # refresh the ones that changed
```

Last full audit: **2026-11-13**, 7 of 12 external skills had changed upstream.

## Without network

`sync-check.ps1` cannot fetch, so it exits with `2` and does nothing. When the other
machine changed serialized assets, the honest order is: close Unity, `git pull --ff-only`,
open Unity, let it reimport, then `tools/compile-check.ps1`.

## Git LFS on a fresh clone

`git lfs` installs its four hooks (`post-checkout`, `post-commit`, `post-merge`,
`pre-push`) wherever `core.hooksPath` points, which is `tools/git-hooks/`. They therefore
show up as generated files the first time this routine runs: `.gitignore` covers them,
never stage them and never delete them. If a clone arrives with LFS pointer files instead
of real art, `git lfs pull` fixes it.
