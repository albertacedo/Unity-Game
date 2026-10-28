# 2026-10-28 · Contingut en CSV i context d'inici de sessió

**Durada:** tarda
**Commits:** `1cacece` feat: author cards, enemies, relics and events from CSV ·
`3132230` docs: capture the radial map and read the full context at session start ·
aquest commit — skills: three design skills for the effect batch

## Objectiu

Dues coses que semblaven separades i no ho són:

1. **Treure el contingut del codi.** Afegir una carta no pot voler dir crear un
   *ScriptableObject* a mà, sinó escriure una fila en un full de càlcul.
2. **No perdre context entre sessions.** L'agent només llegia la nota més nova, i els
   *Pendents* de fa tres sessions no els veia mai.

## Què s'ha fet

### Pipeline de contingut

- **`Csv.cs`** — lector i escriptor de CSV amb detecció automàtica de delimitador
  (`;` per a Excel en català/castellà, `,` altrament) i conservació del mateix
  delimitador en desar.
- **`ContentCsv.cs`** — el format en un sol lloc: capçaleres amb àlies, validació amb
  número de fila, escriptura i lectura de les quatre taules. És l'única definició del
  format; ni el runtime ni l'editor en tenen una còpia.
- **`ContentDatabase.cs`** — càrrega des de `Resources/Content`, caché i `Reload()`.
  Cal perquè el projecte té el *domain reload* desactivat: `GameManager.StartNewRun`
  el crida perquè una edició del CSV es vegi sense reiniciar el Play.
- **Dades planes** — `CardData` i `EnemyData` deixen de ser `ScriptableObject` i passen
  a `[Serializable]`, per poder validar-les fora d'Unity. S'hi afegeixen `RelicData` i
  `EventData`, encara sense sistema de joc.
- **`copies`** — columna nova per conservar la baralla inicial de 4 Strike / 4 Defend.
- **Efectes en una cel·la** — `damage:8|vulnerable:2`, amb el text de la carta
  autogenerat quan la columna `text` és buida.
- **Façanes** — `CardLibrary` i `EnemyLibrary` mantenen l'API que ja feia servir el joc
  (`Normal`, `Pack`, `Boss(rng)`) però llegeixen del CSV.
- **Finestra d'editor** — `Prototype → Content (CSV)`: pestanyes per taula, afegir,
  copiar i esborrar files, recarregar, desar i validar. Avisa si el fitxer ha canviat
  a fora i marca les taules de relíquies i esdeveniments com a **inertes**.
- **`tools/content-check`** — validació i *round-trip* dels CSV amb `dotnet run`, sense
  obrir Unity. `tools/compile-check.ps1` ara compila també `Assets/Editor`.

### Documentació i context

- `docs/contingut.md` — el format, els efectes, els àlies i el flux de treball.
- Captura del mapa radial a `docs/images/2026-10-28-mapa-radial.png`, dins la fitxa del
  matí.
- **`AGENTS.md`** — la rutina d'inici ja no llegeix només l'última nota: llegeix
  `index`, `roadmap`, `arquitectura`, `contingut`, l'índex de sessions, les **tres**
  últimes notes senceres, els *Pendents* de **totes** les antigues i el `git log`.
  La resposta ha de citar els pendents, no reconstruir-los de memòria, i ha de dir-ho
  si el roadmap i l'última nota es contradiuen.

## Fitxers

### Afegits

- `Assets/Scripts/Content/Csv.cs`
- `Assets/Scripts/Content/ContentCsv.cs`
- `Assets/Scripts/Content/ContentDatabase.cs`
- `Assets/Scripts/Data/RelicData.cs`
- `Assets/Scripts/Data/EventData.cs`
- `Assets/Editor/ContentWindow.cs`
- `Assets/Resources/Content/` — `cards.csv`, `enemies.csv`, `relics.csv`, `events.csv`
- `tools/content-check/` — projecte i validador
- `docs/contingut.md`
- `docs/images/2026-10-28-mapa-radial.png`
- `.opencode/skills/game-mechanics-design/`
- `.opencode/skills/game-interface-feedback/`
- `.opencode/skills/game-playtest-planning/`

### Modificats

- `Assets/Scripts/Data/CardData.cs` — classe plana, `DisplayText`, `DescribeEffect`
- `Assets/Scripts/Data/EnemyData.cs` — classe plana
- `Assets/Scripts/Content/CardLibrary.cs` — passa a façana sobre el CSV
- `Assets/Scripts/GameManager.cs` — `ContentDatabase.Reload()` a `StartNewRun` i guarda
  d'encontre buit
- `Assets/Scripts/UI/UIFactory.cs` — `CardTint(kind)` per pintar les cartes per tipus
- `tools/compile-check.ps1` — compila `Assets/Scripts` + `Assets/Editor`
- `AGENTS.md` — rutina d'inici, taula de skills i format del contingut
- `docs/arquitectura.md`, `docs/index.md`, `mkdocs.yml`, `.gitignore`
- `.opencode/skills/deckbuilder-balance`, `status-effects-pipeline`, `unity-verify`
- `docs/sessions/2026-10-28-mapa-boira-connexions.md` — captura i commits

### Esborrats

- Cap.

## Verificació

- `powershell -ExecutionPolicy Bypass -File tools/compile-check.ps1` → **0 errors,
  0 warnings**.
- `dotnet run --project tools/content-check` → 10 cartes, 5 enemics, 0 relíquies,
  0 esdeveniments i *Content OK*. Prova d'error feta: imprimeix la fila i surt amb codi 1.
- Unity, Play mode: **Deck 9 cards**, **Rooms 1/65**, HP 72/72 i cap error a la consola
  nova. Les 9 cartes confirmen que `copies` i la càrrega des del CSV funcionen.
- `python -m mkdocs build --strict` → sense errors.

## Decisions

- **El CSV és l'única font de veritat.** No es generen `.asset`: el que hi ha al full és
  el que carrega el joc, i la finestra de l'editor desa al mateix fitxer.
- **Classes planes en lloc de `ScriptableObject`.** Permeten validar i fer *round-trip*
  amb `dotnet`, fora d'Unity, dins del mateix repositori.
- **El delimitador es detecta i es preserva.** Un fitxer desat per Excel en català no ha
  de convertir-se en un fitxer amb comes trencant els números decimals.
- **Els efectes viuen en una cel·la.** Una columna per efecte hauria fet créixer la taula
  sense límit; el text de la carta es genera sol quan no hi és.
- **Relíquies i esdeveniments existeixen com a catàleg, no com a sistema.** És millor
  tenir les taules i l'editor a punt abans que el codi que les fa servir; la finestra ho
  diu explícitament per no vendre fum.
- **L'inici de sessió llegeix tot el context.** El cost és una mica de lectura; el cost
  de l'alternativa és repetir feina o contradir una decisió de fa tres sessions.

## Pendents

- [ ] Estrenar `card-game`, `card-game-design`, `game-mechanics-design` i
      `game-interface-feedback` a la tanda d'efectes (roadmap #5).
- [ ] Playtest humà de la Tanda 2: senyalització de l'objectiu i feedback de combat.
- [ ] Playtest de la boira amb seeds conegudes, inclosa `14195000`; mesurar les diagonals
      que sobreviuen al filtre geomètric i decidir si calen provisions.
- [ ] Ajustar els packs `JawWormPack` i `CultistPack` amb el que surti del playtest.
- [ ] `NodeType.Elite` encara genera un encontre normal.
- [ ] `GameManager.combat` no es neteja fins a `Advance()`.
- [ ] 24,9% de carrerons sense sortida i 11,35 sales per run: decidir si és massa.
- [ ] L'extrem de l'últim salt queda sota un botó (74 de 300 mapes).
- [ ] Renombrar `DeckScreen.OpenCount`.
- [ ] Fer portable `opencode.json` (arrossegat des del 21/10).
- [ ] Mirar la finestra de contingut a ull: encara no s'ha vist en captura.
- [ ] Sistemes de relíquies i esdeveniments (les taules ja hi són, el codi no).

## Skills per a la propera sessió

| Skill | Per què |
|---|---|
| `status-effects-pipeline` | La tanda #5 és sobretot efectes: on es resolen, quan decauen i com es veuen |
| `deckbuilder-balance` | Els números concrets del prototip per a les 15-20 cartes noves |
| `card-game` | Model de zones i resolució d'efectes: contrastar-lo amb el nostre `CombatSession` |
| `card-game-design` | Corba de costos, raresa i paraules clau del conjunt |
| `game-mechanics-design` | Escriure l'especificació de cada efecte (regles, estat, modes de fallada) abans de tocar codi |
| `game-interface-feedback` | Els estats han de ser visibles: xips a l'enemic i al panell del jugador |
| `game-playtest-planning` | Convertir les preguntes de playtest pendents en un protocol amb telemetria |

Les tres últimes són noves d'aquesta sessió, vendoritzades de
`lvtd-llc/skills` (MIT) amb els tres auditors de skills.sh en **PASS**:
`game-mechanics-design` (86 instal·lacions), `game-interface-feedback` (60) i
`game-playtest-planning` (74).
