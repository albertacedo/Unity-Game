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

- **Mapa**: radial i determinista per seed. Un node `Start` al centre, 7 anells
  (5 de sales + 1 de caps) i 3-6 caps a l'anell exterior. Les arestes uneixen anells
  veïns pel veí més proper i es poden recórrer en tots dos sentits.
- **Geometria**: `MapLayout` és l'única font de posicions. El mapa és una el·lipse
  1,45 vegades més ampla que alta, per aprofitar la pantalla 16:9. Els caps se situen
  darrere d'una sala de l'últim anell perquè les seves carreteres siguin curtes.
- **Moviment**: lliure i gratis: es pot entrar a qualsevol sala veïna sense visitar,
  cap endins o cap enfora. Les sales visitades queden tancades. Si la sala actual no
  té cap veïna per visitar, la run s'acaba (pantalla de cul-de-sac amb `END RUN`).
- **Provisions**: pendents. El pla és que explorar costi energia; de moment no n'hi ha.
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
- `.opencode/skills/` guarda les instruccions reutilitzables de l'agent (UI des de
  codi, balanceig, verificació, efectes). Es preparen al final de cada sessió.

## Convencions

- Namespace `Prototype`, subnamespaces per subsistema (`Prototype.UI`, `Prototype.Combat`…).
- Una `MonoBehaviour` pública per fitxer, amb el nom del fitxer.
- Camps `[SerializeField] private`; res de `public` a l'inspector.
- Input System nou sempre (`Keyboard.current`, mai `Input.GetKey`).
- Els `.meta` els genera Unity: mai a mà.
