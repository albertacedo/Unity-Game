# Unity Game — Dev Log

Prototip de roguelike de cartes en 2D, estil *Slay the Spire*, fet a Unity 6.3 (URP 2D).

Aquest lloc és el **diari de treball**: una pàgina per cada sessió de *live coding*, amb què
s'ha fet, què s'ha après i quins fitxers han canviat.

!!! tip "Lloc publicat"
    El diari és en línia a **<https://albertacedo.github.io/Unity-Game/>** i s'actualitza
    amb `python -m mkdocs gh-deploy` al final de cada sessió.

## Estat actual

| Cosa | Estat |
|---|---|
| Bucle jugable | ✅ mapa → combat → recompensa → mapa |
| Mapa | ⚠️ radial: centre, 8 anells, 4-7 caps, boira configurable, zoom/pan i connexions segures; es redissenya (radi de descobriment i retrocés) |
| Combat | ✅ torns, energia, bloqueig, Vulnerable, multi-enemic amb objectiu |
| Contingut | 10 cartes · 2 enemics, 2 packs i 1 cap, i taules de relíquies/esdeveniments (encara inertes), tot en CSV editable |
| Eines de test | ✅ panell F1, vista del mas, feedback de combat, `tools/compile-check.ps1` |
| Eines de contingut | ✅ finestra `Prototype → Content (CSV)` i `tools/content-check` |
| Or | ⚠️ es mostra però no es pot gastar (botiga pendent) |
| Àudio / art | ❌ placeholder |

## Com arrencar-lo

1. Obre el projecte amb **Unity 6000.3.26f1**.
2. Obre `Assets/Scenes/Prototype.unity`.
3. Prem **Play**.
4. Dins del joc: **F1** obre el panell de desenvolupament.

## Stack

- **Unity** 6000.3.26f1 (Unity 6.3), URP 2D
- **Input System** nou (no legacy)
- **UI**: UGUI + TextMeshPro, construïda des de codi
- **Contingut**: cartes, enemics, relíquies i esdeveniments en CSV (`Assets/Resources/Content/`), llegits per `ContentDatabase`

## Sessions

Consulta l'[índex de sessions](sessions/index.md), el [roadmap](roadmap.md) o com
s'edita el [contingut en CSV](contingut.md).

| Data | Sessió | Què hi trobaràs |
|---|---|---|
| 2026-10-21 | [Vertical slice](sessions/2026-10-21-vertical-slice.md) | Mapa, combat, recompenses i escena jugable |
| 2026-10-21 | [GitHub i roadmap](sessions/2026-10-21-github-i-roadmap.md) | Repo remot, build scene i pla de 3 tandes |
| 2026-10-22 | [Tanda 1](sessions/2026-10-22-tanda-1.md) | Panell F1, vista del mas, feedback de combat |
| 2026-10-22 | [Docs i flux de treball](sessions/2026-10-22-docs-i-flux.md) | Aquest lloc i la rutina d'inici/final de sessió |
| 2026-10-23 | [Tanda 2: multi-enemic](sessions/2026-10-23-tanda-2-multi-enemic.md) | Diversos enemics, selecció d'objectiu i skills |
| 2026-10-25 | [Mapa radial](sessions/2026-10-25-mapa-radial.md) | De l'estrella als anells, zoom/pan, simulació dels tres mapes i el fix de la rodeta |
| 2026-10-27 | [Skills externes](sessions/2026-10-27-skills-externes.md) | Nou skills del catàleg de skills.sh, vendoritzades amb procedència |
| 2026-10-28 | [Mapa amb boira i connexions](sessions/2026-10-28-mapa-boira-connexions.md) | Boira configurable, retrocés, 8 anells i connexions laterals/diagonals validades |
| 2026-10-28 | [Contingut en CSV](sessions/2026-10-28-contingut-csv.md) | Cartes, enemics, relíquies i esdeveniments editables des d'Excel, i context d'inici de sessió |
