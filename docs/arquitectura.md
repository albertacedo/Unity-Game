# Arquitectura

Tot el joc es construeix **des de codi**: l'escena només conté un objecte `Game` amb el
component `GameManager`. No hi ha prefabs ni cap pantalla muntada a mà.

## Flux

```
GameManager.Awake()
  ├─ crea Canvas + EventSystem
  ├─ crea DevPanel (F1)
  └─ StartNewRun(seed)
        └─ ShowMap() → MapScreen
              └─ clic a node → StartCombat() / ShowRest() / ShowPlaceholder()
                    └─ CombatScreen → OnCombatFinished()
                          ├─ victòria → ShowReward() → Advance() → ShowMap()
                          └─ derrota  → ShowEnd()
```

`GameManager.Clear()` destrueix tots els fills de `Screens` abans de pintar la pantalla
següent. Les capes que han de sobreviure (el `DevPanel`) pengen del Canvas, no de `Screens`.

## Fitxers

| Fitxer | Responsabilitat |
|---|---|
| `Scripts/GameManager.cs` | Punt d'entrada, canvi de pantalles, helpers de debug |
| `Scripts/Run/RunState.cs` | HP, or, seed, mapa i mas (el que sobreviu entre sales) |
| `Scripts/Mapgen/MapGenerator.cs` | Mapa radial determinista per seed |
| `Scripts/Mapgen/MapLayout.cs` | Geometria compartida (radi, angle i el·lipse) de mapa i pantalla |
| `Scripts/Combat/CombatSession.cs` | Regles del combat (torns, energia, dany, bloqueig, objectiu) |
| `Scripts/Combat/EnemyInstance.cs` | Estat d'un enemic dins d'un combat (HP, bloqueig, Vulnerable, intent) |
| `Scripts/Content/CardLibrary.cs` | Totes les cartes i enemics del joc |
| `Scripts/Data/CardData.cs` | Tipus de carta (`ScriptableObject`) i efectes |
| `Scripts/Data/EnemyData.cs` | Enemic i els seus intents |
| `Scripts/UI/UIFactory.cs` | Helpers per construir UI des de codi |
| `Scripts/UI/MapScreen.cs` | Mapa, nodes i connexions |
| `Scripts/UI/CombatScreen.cs` | Combat, mà, feedback visual |
| `Scripts/UI/RewardScreen.cs` | Elecció de 3 cartes |
| `Scripts/UI/SimpleScreens.cs` | Descans, placeholder i final de run |
| `Scripts/UI/DeckScreen.cs` | Capa amb tot el mas |
| `Scripts/DevPanel.cs` | Panell de proves (F1) |

## Regles implementades

- **Mapa**: radial i determinista per seed. Un node `Start` al centre, 8 anells
  (6 de sales + 1 de caps) i 4-7 caps a l'anell exterior. Les connexions laterals dels
  anells de sales es generen amb un 55% de probabilitat i es dibuixen sempre amb línies
  rectes. Un 35% de les sales també pot tenir una connexió diagonal amb la branca angular
  veïna de l'anell següent. Les diagonals es rebutgen si creuen una carretera, toquen el
  rectangle d'un altre node o passen a menys de 8 unitats del seu marge visual.
- **Tipus de sala**: l'anell 1 és sempre de combat; als anells 2-5 es tira 50% combat,
  15% descans, 10% botiga i 25% esdeveniment. Els caps són sempre caps, i `Elite` encara
  no es genera.
- **Connexions**: entre dos anells veïns, cada sala enllaça amb la sala (o cap) més
  propera en angle. El mapa queda partit en falques centrades en l'anell petit. Les sales
  del mateix anell s'uneixen amb línies directes, i l'anell just abans dels caps és complet.
- **Geometria**: `MapLayout` és l'única font de posicions. El mapa és una el·lipse
  1,45 vegades més ampla que alta, per aprofitar la pantalla 16:9. Cada cap se situa
  sobre el centre angular de les seves sales, perquè cap aresta surti del seu anell
  (una aresta inclinada més de ~35° entraria a l'anell interior i creuaria carreteres).
- **Zoom i desplaçament**: roda del ratolí (o botons `+` / `−`) per ampliar entre el
  60% i el 220%, arrossegant amb el botó dret o el central per moure el mapa. El mapa
  es retalla dins la seva àrea (`RectMask2D`), així que no trepitja el HUD, i
  `RESET VIEW` torna a l'estat inicial. Es pot clicar les sales a qualsevol zoom.
  Mentre hi ha una capa oberta (el deck), `MapScreen.Update` ignora roda i
  arrossegament: l'scroll de les cartes no ha de moure el mapa del darrere.
- **Moviment**: cada elecció mou exactament un node i es pot caminar en ambdues direccions.
  Les sales visitades no repeteixen contingut, però es poden travessar per tornar a una
  branca. Els tipus de sala només es revelen dins del radi configurat (`1-5`, `1` per defecte);
  els caps són visibles des del principi. La topologia i les carreteres sempre són visibles.
  El panell F1 permet provar boira, radi de revelació `1-5` i retrocés sense reiniciar.
  No hi ha provisions encara: primer es prova si el moviment i el contingut d'un sol ús
  ja creen prou compromís.
- **Combat**: 3 energia per torn, robes 5, el bloqueig es reinicia cada torn.
- **Multi-enemic**: 1 enemic normal o un pack de 2 variants febles. Cada enemic té
  la seva HP, bloqueig, Vulnerable i cua d'intents; el combat acaba quan cauen tots.
- **Objectiu**: les cartes d'atac van a `Target`; en morir passa al següent viu.
  `DamageAll` (Cleave) colpeja tots els enemics vius.
- **Vulnerable**: multiplica el dany d'entrada per 1,5, un contador per enemic
  (i un per al jugador) que baixa 1 al final de la ronda.
- **Run**: 72 HP, 99 d'or (encara sense ús).

## Punts d'extensió

| Vull… | On toco |
|---|---|
| Afegir una carta | `CardLibrary` (mètode `Make`) + afegir-la a `RewardPool` |
| Afegir un enemic | `EnemyLibrary` + incloure'l a `RandomEncounter` |
| Afegir un efecte | `EffectKind` (a `CardData`) + el `switch` de `CombatSession.ApplyEffects` + un camp a `EnemyInstance` si és un estat |
| Afegir una pantalla | `GameManager.ScreenKind` + un `ShowX()` + una classe a `UI/` |
| Canviar l'aspecte d'un element | `UIFactory` (afecta totes les pantalles alhora) |

## Eines

- `tools/compile-check.ps1` compila `Assets/Scripts` contra les DLLs de `Library/`
  amb el SDK de .NET, sense obrir Unity. És el primer esglaó de verificació;
  després cal comprovar la consola i provar en Play mode.
- `.opencode/skills/` guarda les instruccions reutilitzables de l'agent. N'hi ha de dos
  tipus: les **pròpies** (UI des de codi, balanceig, verificació, efectes: el que només
  sap aquest repo) i les **externes**, copiades del catàleg de [skills.sh](https://www.skills.sh/)
  amb l'origen i la data al peu. Al final de cada sessió es revisen les dues: les pròpies
  s'actualitzen i, per al coneixement general, es busca una skill externa abans d'escriure'n
  una de nova. Vegeu [Skills externes](sessions/2026-10-27-skills-externes.md).

## Convencions

- Namespace `Prototype`, subnamespaces per subsistema (`Prototype.UI`, `Prototype.Combat`…).
- Una `MonoBehaviour` pública per fitxer, amb el nom del fitxer.
- Camps `[SerializeField] private`; res de `public` a l'inspector.
- Input System nou sempre (`Keyboard.current`, mai `Input.GetKey`).
- Els `.meta` els genera Unity: mai a mà.
