# 2026-10-22 · Docs i flux de treball

**Commits:** aquest mateix commit.

## Objectiu

Tenir un lloc on quedi registrada cada sessió de treball i fixar **com comença i com
acaba** una sessió, perquè no s'hagi de repetir a mà cada dia.

## Què s'ha fet

- Creat el lloc **MkDocs** (tema Material) amb portada, arquitectura, roadmap i sessions.
- Reconstruïdes les **4 sessions** anteriors a partir de l'historial de git.
- Afegida una **plantilla** de sessió i la convenció de noms
  `AAAA-MM-DD-titol-curt.md`.
- Escrit a `AGENTS.md` el **flux de sessió**:
  - **en començar**: `git pull`, llegir l'última fitxa de sessió i proposar els
    següents passos
  - **en acabar**: crear la fitxa, afegir-la a la navegació, compilar el lloc,
    publicar-lo, comitejar i pujar
- `.gitignore`: exclosos `site/` (sortida del lloc) i `.cache/`.
- **Publicat a GitHub Pages**: el repo era privat i Pages gratuït exigeix repo públic,
  així que s'ha fet públic. El lloc viu a
  <https://albertacedo.github.io/Unity-Game/> i es desplega amb
  `python -m mkdocs gh-deploy --force` (branca `gh-pages`).

## Fitxers

### Afegits

- `mkdocs.yml`
- `docs/index.md`
- `docs/arquitectura.md`
- `docs/roadmap.md`
- `docs/sessions/index.md`
- `docs/sessions/_plantilla.md`
- `docs/sessions/2026-10-21-vertical-slice.md`
- `docs/sessions/2026-10-21-github-i-roadmap.md`
- `docs/sessions/2026-10-22-tanda-1.md`
- `docs/sessions/2026-10-22-docs-i-flux.md` (aquest fitxer)

### Modificats

- `AGENTS.md` — secció *Session workflow*, inclòs el pas de publicació
- `.gitignore` — `site/` i `.cache/`

## Verificació

- `python -m mkdocs build` genera 8 pàgines sense avisos.
- `mkdocs gh-deploy --force` publica la branca `gh-pages`.
- `https://albertacedo.github.io/Unity-Game/` respon **200** al cap de ~50 s amb el
  títol *Unity Game - Dev Log*.

## Decisions

- **Material** com a tema: navegació lateral, buscador i mode fosc sense feina extra.
- Les fitxes viuen a `docs/sessions/` i s'afegeixen a mà a la `nav` de `mkdocs.yml`
  (són poques i així l'ordre és explícit).
- La plantilla porta guió baix al davant i queda **exclosa** del lloc web.
- **Repo públic + branca `gh-pages`**: és la via gratuïta de Pages i no cal cap servei
  extern. Alternativa descartada: repo de documentació separat, més manteniment.

## Pendents

- [ ] Fer portable `opencode.json` (arrossegat de la sessió anterior)
- [ ] Playtest de la Tanda 1 (panell F1, vista del mas, feedback de combat)
- [ ] Tanda 2: multi-enemic amb selecció d'objectiu
