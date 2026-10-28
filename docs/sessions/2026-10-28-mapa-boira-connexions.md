# 2026-10-28 · Mapa amb boira i connexions

**Durada:** tarda
**Commits:** aquest commit — feat: tune foggy radial map navigation

## Objectiu

Provar un mapa radial amb informació descoberta progressivament, retrocés configurable
i més opcions per canviar de branca sense fer que les carreteres siguin il·legibles.

## Què s'ha fet

- S'ha afegit boira de guerra amb radi configurable de revelació (1-5).
- La visibilitat és acumulativa: es calcula des de totes les sales visitades.
- S'ha permès configurar el retrocés des del panell F1.
- El mapa ha passat de 7 a 8 anells, amb una sala més abans dels caps.
- Les connexions del mateix anell ara són línies rectes.
- S'han ajustat les connexions: 55% laterals normals i 35% diagonals entre branques
  d'anells consecutius.
- Les connexions es validen contra creuaments i contra la proximitat als rectangles dels
  nodes; si una connexió radial no és segura, es prova una branca alternativa.
- Els valors per defecte són boira apagada i retrocés apagat.

## Fitxers

### Afegits

- Cap.

### Modificats

- `Assets/Scripts/Run/RunState.cs` — opcions de mapa i valors per defecte.
- `Assets/Scripts/GameManager.cs` — revelació acumulativa i controls de desenvolupament.
- `Assets/Scripts/DevPanel.cs` — controls F1 de boira, radi i retrocés.
- `Assets/Scripts/UI/MapScreen.cs` — nodes ocults, connexions rectes i disponibilitat.
- `Assets/Scripts/Mapgen/MapGenerator.cs` — vuit anells, connexions diagonals i validació geomètrica.
- `Assets/Scripts/Mapgen/MapLayout.cs` — eliminada la geometria de carreteres corbes.
- `docs/arquitectura.md`, `docs/roadmap.md`, `docs/index.md`, `docs/sessions/index.md`,
  `mkdocs.yml` — documentació i navegació.

### Esborrats

- Cap.

## Verificació

- `tools/compile-check.ps1`: compilació correcta, 0 errors i 0 avisos.
- `python -m mkdocs build`: correcte.
- `git diff --check`: sense errors de whitespace.
- La consola de Unity no s'ha pogut consultar perquè l'Editor no ha estat detectat per
  l'entorn MCP en aquesta sessió.

## Decisions

- La boira acumula la visibilitat de totes les sales visitades, no només de la sala actual.
- Les diagonals només connecten anells consecutius i trien una branca angular veïna.
- La seguretat geomètrica es valida durant la generació, no només durant el dibuix de la UI.
- Les provisions queden pendents fins que el playtest demostri que cal limitar l'exploració.

## Pendents

- [ ] Fer un playtest visual de les seeds problemàtiques (especialment `14195000`).
- [ ] Mesurar quantes connexions diagonals sobreviuen al filtre geomètric.
- [ ] Decidir si cal afegir provisions després de provar la boira i el retrocés.

## Skills per a la propera sessió

| Skill | Per què |
|---|---|
| `deckbuilder-balance` | Preparar la tanda d'efectes i cartes amb els números del prototip. |
| `status-effects-pipeline` | Integrar els nous efectes en el combat i la seva UI. |
| `card-game` | Revisar zones, resolució d'efectes i flux de cartes. |
| `card-game-design` | Dissenyar la corba de costos, rols i paraules clau de les noves cartes. |
