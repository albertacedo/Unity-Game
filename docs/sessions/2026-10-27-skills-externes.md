# 2026-10-27 · Skills externes des de skills.sh

**Durada:** tarda
**Commits:** `3183de6` — skills: nou skills vendoritzades del catàleg de skills.sh

## Objectiu

Deixar d'escriure les skills a mà. El coneixement d'enginyeria i de disseny que fem
servir cada sessió (uGUI, cicle de vida de MonoBehaviour, Input System, zones de cartes,
economia d'una *run*) no és nostre: existeix en catàlegs públics d'*agent skills*. La feina
d'avui és **portar-lo de fora**, versionat al repo, en lloc d'anar-lo reinventant.

## Mètode: com es troben i s'instal·len

1. **La API queda tancada.** `https://skills.sh/api/v1/skills/search?q=...` retorna
   `401 authentication_required`: vol un token OIDC de Vercel. No és una opció per a
   un projecte local.
2. **El cercador web sí que serveix.** Les pàgines `https://www.skills.sh/search?q=<terme>`
   es renderitzen al servidor: obrint-les amb el navegador s'hi llegeix la llista sencera
   (skill, repo, instal·lacions). Les fitxes `https://www.skills.sh/<owner>/<repo>/<slug>`
   porten la comanda d'instal·lació i els **audits de seguretat**.
3. **Instal·lació vendoritzada.** En lloc de `npx skills add` (que escriu a la carpeta
   global de l'agent i no queda al repo), es baixa l'upstream i es copia a
   `.opencode/skills/<slug>/`, amb un peu *Upstream source*.

El cercle complet, per repetir-lo quan calgui actualitzar o afegir una skill:

```powershell
# 1. Trobar el camí real dins el repo (les rutes varien: skills/, skills/unity/, SKILL.md a l'arrel)
Invoke-RestMethod "https://api.github.com/repos/<owner>/<repo>/git/trees/main?recursive=1" |
  Select-Object -ExpandProperty tree | Where-Object { $_.path -like "*SKILL.md" }

# 2. Baixar el SKILL.md i els fitxers que referenciï (references/, scripts/)
Invoke-WebRequest "https://raw.githubusercontent.com/<owner>/<repo>/main/<ruta>/SKILL.md" `
  -OutFile ".opencode/skills/<slug>/SKILL.md" -UseBasicParsing
```

## Cercat i descartat

| Cerca | Resultat |
|---|---|
| `unity` | `unity-technologies/skills` (oficial) i `gamedev-skills/awesome-gamedev-agent-skills` són els dos jaciments bons |
| `deckbuilding` | Gairebé tot és *Magic*: generadors de baralles de Commander. D'interessant, `card-game-design` |
| `card game` | `gamedev-skills/.../card-game` i `unity-card-games` (12 instal·lacions, sense historial: descartada) |
| `roguelike` | `gamedev-skills/.../roguelike` (motor de graella) i `roguelike-game-designer` (disseny, més útil per a nosaltres) |
| `game balance` | `game-balance-economy` i diverses de Godot/Roblox/Phaser, que no ens toquen |
| `procedural generation` | Godot/Unreal o amb 1-6 instal·lacions: res avaluable |

Descartades de sortida: `unity-ecs-patterns` (no fem servir ECS), `unity-navmesh`,
`unity-tilemap-2d` i `2d-pixel-perfect` (no hi ha món de tiles ni art final encara).

## Què s'ha instal·lat

Tots nou skills tenen **PASS** als tres auditors de skills.sh (Gen Agent Trust Hub,
Socket, Snyk) el dia de la baixada.

| Skill | Repo | Instal·lacions | Per a què el volem |
|---|---|---|---|
| `ui-ugui` | `unity-technologies/skills` | 6,6K | Jerarquies de Canvas, `RectTransform`, `ScrollRect` — porta `references/scrollview-setup.md`, que és just el terreny del bug de la rodeta |
| `optimize-text-mesh-pro` | `unity-technologies/skills` | 5,8K | Atles i fallbacks de TMP: tota la UI és TextMeshPro |
| `unity-csharp-scripting` | `gamedev-skills/awesome-gamedev-agent-skills` | 6,3K | Cicle de vida, serialització i coroutines, declarats per a Unity 6.3 LTS |
| `unity-input-system` | `gamedev-skills/awesome-gamedev-agent-skills` | 5,9K | Input Actions i *rebinding*; l'únic sistema d'entrada que fem servir |
| `card-game` | `gamedev-skills/awesome-gamedev-agent-skills` | 6,4K | Model de zones (deck → hand → play → discard) i resolució d'efectes |
| `roguelike` | `gamedev-skills/awesome-gamedev-agent-skills` | 6,5K | Estructura de *run*, permadeath i generació de nivells |
| `card-game-design` | `omer-metin/skills-for-antigravity` | 150 | Corba de costos, raresa, paraules clau i disseny de conjunts |
| `game-balance-economy` | `lvtd-llc/skills` | 149 | Economia, recompenses i risc-benefici: la botiga que tenim pendent |
| `roguelike-game-designer` | `hanjo92/roguelike-game-designer-skill` | 6 | Revisió de la *run*: rejugabilitat, accessibilitat, decisions amb pes |

## Fitxers

### Afegits

- `.opencode/skills/ui-ugui/SKILL.md` i `references/scrollview-setup.md`
- `.opencode/skills/optimize-text-mesh-pro/SKILL.md`
- `.opencode/skills/unity-csharp-scripting/SKILL.md` i `scripts/check_unity_project.py`
- `.opencode/skills/unity-input-system/SKILL.md`
- `.opencode/skills/card-game/SKILL.md`
- `.opencode/skills/roguelike/SKILL.md`
- `.opencode/skills/card-game-design/SKILL.md`
- `.opencode/skills/game-balance-economy/SKILL.md`
- `.opencode/skills/roguelike-game-designer/SKILL.md`

### Modificats

- `AGENTS.md` — la taula de skills es parteix en **pròpies** i **externes**, amb repo
  d'origen i quan fer-les servir; s'hi afegeix la regla de no reescriure el cos d'una
  skill externa i `status-effects-pipeline`, que faltava a la taula.
- `.opencode/skills/card-game-design/SKILL.md` — la línia `description` de l'upstream
  venia sense espai (`balanceUse when...`) i amb la llista de paraules clau enganxada;
  queda normalitzada perquè el model la pugui fer servir.

## Verificació

- Les nou skills apareixen al catàleg del harness (15 en total, amb les cinc pròpies).
- Els enllaços relatius es mantenen: `references/scrollview-setup.md` dins `ui-ugui`
  i `scripts/check_unity_project.py` dins `unity-csharp-scripting` són a la carpeta.
- Auditors PASS a les nou fitxes de skills.sh.
- `python -m mkdocs build` sense errors.

## Decisions

- **Vendoritzar, no instal·lar globalment.** El repo és el que ha de saber amb què treballa;
  una instal·lació global no viatja entre màquines ni queda a l'historial.
- **El cos és de l'upstream.** Només s'hi afegeix el peu de procedència (repo, ruta, data,
  audits) i, si cal, la normalització d'una `description` trencada. Les diferències amb
  el projecte es documenten a la skill pròpia, no editant l'externa.
- **Quan xoquen, mana la pròpia.** `unity-ugui-code` sap que la UI es construeix per codi
  i a 1600×900; `ui-ugui` assumeix prefabs i Editor. Les externes són doctrina general.

## Pendents

- [ ] Estrenar `card-game` + `card-game-design` a la tanda d'efectes i cartes (roadmap #5).
- [ ] Veure si `roguelike`/`roguelike-game-designer` aporten amb un mapa per nodes i no
      per graella, o si només se'n salva la part d'economia de *run*.
- [ ] Repassar les skills externes al principi de cada sessió gran: si l'upstream ha
      canviat, tornar-les a baixar amb el cercle de dalt.

## Skills per a la propera sessió

| Skill | Per què |
|---|---|
| `status-effects-pipeline` | La tanda #5 és sobretot efectes: on es resolen i quan decauen |
| `card-game` | Model de zones i resolució d'efectes: contrastar-lo amb el nostre `CombatManager` |
| `card-game-design` | Corba de costos i paraules clau per a les 15-20 cartes noves |
| `deckbuilder-balance` | Els números concrets del prototip (l'externa no els coneix) |
