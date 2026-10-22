# 2026-10-21 · GitHub i roadmap

**Commits:** `98bd00a` (escena de build)

## Objectiu

Posar el projecte en un repositori remot i decidir com continuar.

## Què s'ha fet

- Creat el repositori **`github.com/albertacedo/Unity-Game`** i pujats els 3 commits
  inicials a `main`.
- Autenticació amb **Git Credential Manager** (login al navegador); les credencials
  queden desades i els pushes següents no demanen res.
- `Prototype.unity` marcada com a **única escena de build**.
- S'ha discutit com treballar des d'un **segon PC**:
  - cal Unity Hub + **exactament** la versió `6000.3.26f1`
  - Git, OpenCode i el relay MCP (l'instal·la Unity en obrir el projecte)
  - `Library/` no viatja: es regenera en la primera obertura
  - clonar **fora d'OneDrive** al segon equip
  - tancar Unity abans de fer `pull` i no treballar als dos PCs alhora
- **Roadmap**: identificats els buits (or sense ús, un sol enemic, sense vista del mas)
  i acordades **3 tandes**. Triada la Tanda 1.

## Fitxers

### Modificats

- `ProjectSettings/EditorBuildSettings.asset` — només `Prototype.unity`

## Verificació

`git ls-remote --heads origin` retorna `98bd00a`, igual que `HEAD` local; arbre de treball net.

## Decisions

- **Repo públic** a GitHub, branca `main`, commits petits amb missatge de *què* i *com*
  s'ha verificat.
- El `opencode.json` del repo porta **rutes absolutes** (`C:\Users\alber\...`). Funciona
  en aquest PC, però al segon caldrà editar-lo o fer-lo portable. Queda pendent.
- No es configura **Git LFS** encara: el repo només té codi. Caldrà quan entri art.

## Pendents

- [ ] Fer portable `opencode.json` (opció: treure'l del repo + `opencode.example.json`)
- [ ] Tanda 1: eines de test, vista del mas i feedback de combat
