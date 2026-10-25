# 2026-10-21 · Vertical slice

**Commits:** `5ce0f83` (projecte base) · `700d2a7` (vertical slice)

## Objectiu

Passar d'un projecte Unity 6.3 acabat de crear a un **bucle jugable complet**: mapa,
combat de cartes i recompenses. Un *thin vertical slice* per poder jugar alguna cosa
al més aviat possible.

## Què s'ha fet

- S'ha localitzat i configurat el projecte existent (URP 2D, Input System nou).
- S'ha connectat l'editor amb l'agent a través del **relay MCP** (`opencode.json`).
- S'ha importat **TextMesh Pro Essential Resources** (sense això no compila).
- **Mapa** estil Slay the Spire: 6 pisos (5 normals + cap), 7 columnes, 4 camins,
  arestes que no es creuen, determinista per *seed*.
- **Combat** per torns: 3 energia, robes 5 cartes, bloqueig que es reinicia, Vulnerable ×1,5.
- **Contingut**: 10 cartes (Strike, Defend, Bash, Cleave, Pommel Strike, Iron Wave,
  Shrug It Off, Thunderclap, Heavy Blade, Battle Trance) i 3 enemics
  (Jaw Worm 44 HP, Cultist 50 HP, The Guardian 90 HP com a cap).
- **UI sencera construïda des de codi** amb `UIFactory`: sense prefabs ni pantalles
  muntades a l'editor.
- Pantalles: mapa, combat, recompensa, descans, placeholder i final de run.
- Escena `Assets/Scenes/Prototype.unity` amb un sol objecte `Game`.

## Simulació del mapa

El mapa de pisos és determinista per *seed*: 4 camins pugen del pis 0 al cap i es
rebutgen les arestes que es creuarien. L'he passat pel mateix banc de proves que els
dos mapes radials (vegeu la [sessió del 2026-10-25](2026-10-25-mapa-radial.md)):
**300 seeds × 40 excursions = 12.000 recorreguts**, amb un jugador que entra per una
sala a l'atzar del pis 0 i després tria una aresta a l'atzar.

<a href="../../img/mapa-v1-pisos.svg" target="_blank"><img src="../../img/mapa-v1-pisos.svg" alt="Mapa de pisos: 6 pisos, 7 columnes i 4 camins"></a>

*Seed 42. Les sales amb contorn discontinu són les entrades possibles; el cap és a dalt.
Clic a la imatge per veure-la a mida completa.*

| Mètrica | Valor |
|---|---|
| Sales per mapa | 15,1 |
| Arestes per mapa | 16,6 |
| Caps | 1 (sempre) |
| Sales amb més d'una sortida | 2,3 |
| Arestes que es creuen | 0 (pitjor mapa: 0) |
| Sales inabastables o sobreposades | 0 |
| Excursions que arriben al cap | **100%** |
| Excursions sense sortida | **0%** |
| Sales per excursió | 5,0 (sempre; 6 comptant l'entrada) |

El 100% no vol dir que el mapa sigui millor: vol dir que **no hi ha decisions de
direcció**. Les arestes només van cap amunt, cada sala en té almenys una i totes les del
darrer pis van al cap, així que tota partida fa exactament el mateix recorregut de 5
sales. El mapa és petit (15 sales) perquè els 4 camins es fusionen de seguida: només 2,3
sales per mapa ofereixen més d'una sortida. És el que va portar a provar el mapa radial
amb moviment lliure.

## Fitxers

### Afegits

- `Assets/Scripts/GameManager.cs` — punt d'entrada i canvi de pantalles
- `Assets/Scripts/Run/RunState.cs`
- `Assets/Scripts/Mapgen/MapGenerator.cs`
- `Assets/Scripts/Combat/CombatSession.cs`
- `Assets/Scripts/Content/CardLibrary.cs`
- `Assets/Scripts/Data/CardData.cs`, `Assets/Scripts/Data/EnemyData.cs`
- `Assets/Scripts/UI/UIFactory.cs`, `MapScreen.cs`, `CombatScreen.cs`, `RewardScreen.cs`, `SimpleScreens.cs`
- `Assets/Scenes/Prototype.unity`
- `Assets/TextMesh Pro/**` — recursos essencials del TMP

## Verificació

Partida completa en Play mode: mapa → combat guanyat amb **56/72 HP** → pantalla de
recompensa → tornada al mapa amb **10 cartes** al mas. **0 errors** de compilació.

## Decisions

- **UGUI + TextMeshPro**, no UI Toolkit: és el que demana el gènere i el que es veu a la
  referència.
- **UI per codi** en lloc de prefabs: iteració ràpida i tot versionat en text.
- **Contingut en C#** (`CardLibrary`) i no en ScriptableObjects encara: canviar números
  és editar una línia. Es migrarà quan el volum creixi.
- **Una sola escena**: les pantalles són panells de runtime.
- **Sense moviment**: el joc és mapa de nodes + combat per torns, no un top-down
  d'exploració.

## Pendents

- [x] Pujar-ho a GitHub (fet a la sessió següent)
- [ ] Veure el mas i tenir eines de test → es converteix en la **Tanda 1**
