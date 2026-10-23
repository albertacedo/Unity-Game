# 2026-10-23 · Tanda 2: multi-enemic i selecció d'objectiu

**Durada:** tarda
**Commits:** aquest mateix commit.

## Objectiu

Fer el salt de profunditat més gran del roadmap: diversos enemics per combat i
poder triar a qui ataques. Aprofitar la sessió per obrir Unity, provar-ho de debò
i muntar el sistema de *skills* per a les properes sessions.

## Què s'ha fet

- **Multi-enemic.** `CombatSession` ja no té un sol enemic: porta una llista
  d'`EnemyInstance`, cadascun amb HP, bloqueig, Vulnerable i cursor d'intent propis.
  El combat s'acaba quan cauen tots.
- **Selecció d'objectiu.** `CombatSession.Target` amb `SelectTarget`; en morir
  l'objectiu s'auto-selecciona el següent viu. Les cartes d'atac van a l'objectiu;
  `Cleave` passa a `DamageAll` i colpeja tots els enemics.
- **Pantalla de combat.** Fila d'enemics clicables amb barra de vida, bloqueig i
  intent individuals; l'objectiu porta contorn daurat, els morts queden atenuats
  amb `DEFEATED`; els números flotants i els flaixos van al panell de l'enemic colpejat.
- **Encontres.** `RandomEncounter` retorna 1 enemic normal (55%) o un pack de dues
  variants més febles (`JawWormPack` 22 HP, `CultistPack` 24 HP), repetides o
  barrejades; els noms duplicats reben sufix `A`/`B`.
- **Eines de prova.** Botó `KILL TARGET` al panell F1 i l'objectiu actual a la línia
  d'estat. Nou `tools/compile-check.ps1` per compilar sense obrir Unity.
- **Skills.** Creades `.opencode/skills/` (`unity-ugui-code`, `deckbuilder-balance`,
  `unity-verify`, `gameplay-architecture`) i preparada `status-effects-pipeline`
  per a la propera sessió. El flux d'inici/final de sessió d'`AGENTS.md` ara inclou
  carregar-les en començar i preparar-les en acabar.

## Fitxers

### Afegits

- `Assets/Scripts/Combat/EnemyInstance.cs` (+ `.meta` generat per Unity)
- `tools/compile-check.ps1`
- `.opencode/skills/unity-ugui-code/SKILL.md`
- `.opencode/skills/deckbuilder-balance/SKILL.md`
- `.opencode/skills/unity-verify/SKILL.md`
- `.opencode/skills/gameplay-architecture/SKILL.md`
- `.opencode/skills/status-effects-pipeline/SKILL.md`
- `docs/sessions/2026-10-23-tanda-2-multi-enemic.md` (aquest fitxer)

### Modificats

- `Assets/Scripts/Combat/CombatSession.cs` — llista d'enemics, objectiu, `DamageAll`,
  torn enemic per enemic, log multilínia, `DebugKillTarget`
- `Assets/Scripts/UI/CombatScreen.cs` — fila d'enemics, `EnemyWidget`, objectiu i
  feedback per enemic
- `Assets/Scripts/Content/CardLibrary.cs` — `RandomEncounter` amb packs, variants
  febles, `Cleave` d'àrea
- `Assets/Scripts/Data/CardData.cs` — `EffectKind.DamageAll`
- `Assets/Scripts/GameManager.cs` — combat amb llista d'enemics, `DebugKillTarget`
- `Assets/Scripts/DevPanel.cs` — botó `KILL TARGET`, objectiu a l'estat
- `AGENTS.md` — skills al flux de sessió
- `docs/sessions/_plantilla.md` — secció *Skills per a la propera sessió*
- `docs/arquitectura.md`, `docs/roadmap.md`, `docs/index.md`,
  `docs/sessions/index.md`, `mkdocs.yml` — estat, regles i navegació

## Verificació

- `powershell -File tools/compile-check.ps1`: **0 errors, 0 avisos**.
- Unity obert a la força amb `Unity.exe -projectPath`; import net i
  `Unity_GetConsoleLogs` amb `logTypes: "Error,Exception"` → **0 errors**.
- Play mode conduït amb `Unity_RunCommand` (MCP) i captures del Game view:
  - Combat de 2 enemics renderitzat (Jaw Worm + Cultist, intents 4 i 3).
  - Clic a Cultist → `Target = Cultist`.
  - `Cleave` → 22→14 i 24→16 HP; log amb les dues línies.
  - `KILL TARGET` → Cultist a 0, objectiu passa a Jaw Worm.
  - Torn enemic amb 2 enemics i 5 de bloqueig: 3+3 de dany → 1 HP perdut
    (el bloqueig compartit es consumeix entre enemics).
  - Victòria → overlay amb `CONTINUE` → pantalla de recompensa amb 3 cartes.
- Captures a `%TEMP%\opencode\unity-shots\` (no es comitegen).

## Decisions

- **`EnemyInstance` separat d'`EnemyData`**: la definició (ScriptableObject) no es
  toca; l'estat de combat viu en un objecte per enemic. És el que permetrà
  Strength/Weak per enemic sense embrutar el catàleg.
- **Packs amb variants febles en lloc d'escalar HP**: dos enemics de 44-50 HP amb
  11 de dany haurien estat injugables amb 3 d'energia. Variants explícites = números
  llegibles i fàcils d'ajustar.
- **`DamageDealt(int, EnemyInstance)`** amb `null` = jugador: un sol event per a tots
  els números flotants, sense duplicar codi de feedback.
- **Objectiu per clic** (Outline + color), no per tecles: menys codi i més descobrible.
  Auto-selecció del següent viu per no bloquejar la mà.
- **`DamageAll` com a efecte propi** en lloc d'un flag a la carta: el `switch`
  d'efectes ja és el punt d'extensió i la carta ho diu al text.
- **`compile-check.ps1` al repo**: la compilació amb Roslyn contra les DLLs de
  `Library/` triga 2 s i permet verificar sense obrir l'Editor. Complementa, no
  substitueix, la prova en Play mode.
- **Skills al repo**: el coneixement car (ancoratges, números, receptes MCP) no s'ha
  de re-derivar cada sessió. Es preparen en acabar, mirant els 1-2 ítems següents
  del roadmap.

## Pendents

- [ ] Playtest humà de la Tanda 2: sensació dels clics, llegibilitat de la fila
      d'enemics i del log de tres línies
- [ ] Ajustar els packs després del playtest (HP i dany de `JawWormPack`/`CultistPack`)
- [ ] Estètica del combat: queda un buit gran al mig; val la pena pujar el log o
      afegir-hi un marc
- [ ] Tanda 2b: 8 efectes nous (Weak, Strength, Dexterity, Frail, multi-cop…) i
      15-20 cartes — vegeu la skill `status-effects-pipeline`
- [ ] `NodeType.Elite` encara genera un encontre normal; hauria de tenir el seu propi
- [ ] `GameManager.combat` no es neteja fins a `Advance()`: inofensiu, però convé
      decidir si la recompensa ha de veure el combat acabat o `null`
- [ ] Fer portable `opencode.json` (arrossegat de sessions anteriors)

## Skills per a la propera sessió

| Skill | Per què |
|---|---|
| `status-effects-pipeline` | Nova. És exactament la feina de la Tanda 2b: on es resol cada efecte, quan decau, com es mostra i quins números de partida fer servir |
| `deckbuilder-balance` | Actualitzada amb els números observats dels packs (22/24 HP, 3-4 de dany) per ajustar-los després del playtest |
| `unity-verify` | Per tornar a conduir el Play mode per MCP i capturar pantalles sense dependre de l'usuari |
