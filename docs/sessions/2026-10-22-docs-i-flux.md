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
    comitejar i pujar
- `.gitignore`: exclosos `site/` (sortida del lloc) i `.cache/`.

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

- `AGENTS.md` — secció *Session workflow*
- `.gitignore` — `site/` i `.cache/`

## Verificació

`python -m mkdocs build` genera el lloc sense errors i `mkdocs serve` el mostra a
`http://127.0.0.1:8000`.

## Decisions

- **Material** com a tema: navegació lateral, buscador i mode fosc sense feina extra.
- Les fitxes viuen a `docs/sessions/` i s'afegeixen a mà a la `nav` de `mkdocs.yml`
  (són poques i així l'ordre és explícit).
- La plantilla porta guió baix al davant i queda **exclosa** del lloc web.

## Pendents

- [ ] `mkdocs gh-deploy` si es vol publicar el lloc a GitHub Pages
- [ ] Fer portable `opencode.json` (arrossegat de la sessió anterior)
