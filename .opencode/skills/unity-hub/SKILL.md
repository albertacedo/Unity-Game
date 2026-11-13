---
name: unity-hub
description: Use when the Unity Hub does not list the project, or when its project list must be read or changed without the UI. Hub 3.22 keeps the list in hub.db, not projects-v1.json.
---

# Skill: Unity Hub project list

Use this when the Unity Hub does not list the project, or when you have to inspect or
change its list without the UI. Verified on Hub 3.22.5 (Microsoft Store build).

## Where the list lives

It is **not** `projects-v1.json` any more. That file is a one-way compatibility mirror,
and the meta key `projects_json_migrated=1` makes the Hub ignore it entirely. Editing
it changes nothing (this cost a whole detour on 2026-11-09).

The source of truth is a SQLite database:

```
%LOCALAPPDATA%\Packages\UnityTechnologies.UnityHub_<family>\LocalCache\Roaming\UnityHub\hub.db
```

Path with the current family name:

```
C:\Users\alber\AppData\Local\Packages\UnityTechnologies.UnityHub_2vrhnee42bhxm\LocalCache\Roaming\UnityHub\hub.db
```

Schema:

```sql
CREATE TABLE meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
CREATE TABLE projects (path TEXT, data TEXT, updated_at INTEGER);
```

`data` is `JSON.stringify(ProjectEntity)`; `updated_at` is `Date.now()` in ms. The only
field the Hub validates on load is `path` (`isProject`), so a minimal row is enough.

## Adding a project

1. **Close the Hub** (graceful: `CloseMainWindow()` on the window process; the helpers
   follow). Writing while it runs risks a lost update.
2. Back up `hub.db`, `hub.db-wal`, `hub.db-shm` and `projects-v1.json`.
3. Insert the row with Python's built-in `sqlite3`:

```python
entity = {"path": project_path, "key": project_path,
          "lastModified": int(os.path.getmtime(version_file) * 1000)}
cur.execute("INSERT INTO projects (path, data, updated_at) VALUES (?, ?, ?) "
            "ON CONFLICT(path) DO UPDATE SET data = excluded.data, "
            "updated_at = excluded.updated_at",
            (project_path, json.dumps(entity), int(time.time() * 1000)))
```

4. Reopen the Hub. On start it **derives** the entity from the project files
   (`title`, `version`, `changeset`, `renderPipeline`, `buildTarget`, VCS, cloud ids)
   and persists it, so the row looks complete afterwards. That enrichment is the proof
   it was accepted.

Do not try to write the derived fields by hand: they come from a fingerprint over
`ProjectSettings/` (`ProjectVersion.txt`, `ProjectSettings.asset`, `QualitySettings.asset`
…), `.git/config`, and the render pipeline assets.

## Notes

- The Hub is an MSIX package, so its `%APPDATA%` is virtualised: the real files are
  under `...\Packages\UnityTechnologies.UnityHub_2vrhnee42bhxm\LocalCache\Roaming\`.
  `%APPDATA%\UnityHub` only holds logs and language config.
- The Hub writes `projects-v1.json` as a mirror for older versions; leave it alone.
- Everything here is per machine: the repo has no say in the Hub list, and nothing in
  the project needs to change to bring a project back.
