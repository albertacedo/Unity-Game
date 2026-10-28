# Roadmap

## Els buits actuals

1. **L'or no serveix per a res** — es mostra i no es pot gastar (botiga pendent).
2. **Només hi ha un efecte** (Vulnerable) i 10 cartes: el combat encara no té prou
   decisions per carta.
3. **El multi-enemic s'ha de jugar amb mans humanes** — hi és des de la Tanda 2, però
   falta el playtest que digui si els packs estan ben ajustats.

## Prioritats

Ordenades per relació valor / esforç:

| # | Què | Per què | Esforç |
|---|---|---|---|
| 1 | Panell de desenvolupament (F1) | Fa que tota la resta sigui testeable en segons | Baix ✅ |
| 2 | Vista del mas + detall de carta | Sense això no es poden avaluar les decisions | Baix ✅ |
| 3 | Feedback de combat | Números flotants i flaixos: canvia la sensació | Baix-mig ✅ |
| 4 | Multi-enemic + selecció d'objectiu | El salt de profunditat més gran | Mig-alt ✅ |
| 5 | 8 efectes nous + 15-20 cartes | Debilitat, Força, Destresa, multi-cop, àrea… | Mig |
| 6 | Botiga | Dona utilitat a l'or | Mig |
| 7 | Relíquies | Identitat de cada run | Mig |
| 8 | Descans amb elecció (curar vs millorar) | Decisions reals al mapa | Baix-mig |
| 9 | Esdeveniments | Varietat narrativa | Mig |
| 10 | Guardar/continuar + seed manual | Sessions llargues i repetibilitat | Mig |
| 11 | Art i so amb IA | Polish (molt temps de generació) | Alt |

## Les tres tandes

### Tanda 1 — fer el prototip observable ✅

Panell dev + vista del mas + feedback de combat. No afegeix contingut, però deixa
veure i provar el que ja hi ha.

### Tanda 2 — fer el combat interessant

Multi-enemic amb selecció d'objectiu ✅ + el paquet d'efectes nous + 15 cartes.
Aquí el joc comença a tenir decisions de veritat.

### Tanda 3 — fer la run interessant

Botiga + relíquies + descans amb elecció. L'or i el mapa deixen de ser decoratius.

## Tanda 2b — boira d'informació del mapa radial

El mapa és radial i el jugador pot caminar en ambdues direccions. La topologia és
visible, però el tipus de sala només es revela a distància 1; els caps sempre es veuen.
Les sales visitades no repeteixen contingut i serveixen per tornar a una branca.

Queden aquestes decisions obertes:

- **Provisions**: si el jugador explora totes les branques, fer que cada moviment
  consumeixi una provisió. Primer es farà un playtest sense aquest recurs.
- **Frontier versus posició actual**: de moment només es pot entrar en un veí directe;
  els nodes connectats a altres sales visitades es coneixen però no són teleportables.
- **Mida del run**: mesurar quantes sales visitades i quants retrocessos produeix la
  boira amb `LateralChance = 0,55` i les connexions diagonals validades a `MapGenerator`.

## Preguntes de playtest

Per respondre amb el joc a la mà:

- Amb 3 energia i 5 cartes, acabes els combats amb energia sobrant o sense poder fer res?
- El combat dura 2-3 torns o 4-6? Un combat normal hauria de durar 4-6.
- Amb 72 HP, arribes al boss amb vida o el mapa és massa dur?
- Tries sempre la mateixa carta de recompensa? Si sí, el pool és massa petit.
- Amb el mapa radial: acabes explorant de més (massa combats) o vas directe al boss?
  Quantes sales visités per run? (La simulació diu 11,4 de mitjana, mínim 6.)
- Trobes a faltar un límit a l'exploració, o la vida ja n'hi fa prou?
- Amb zoom: acabes fent servir la roda o el mapa sencer ja es llegeix bé a 100%?
- L'anell exterior és prou clar com a "tria de cap", o els arcs laterals el fan confús?
