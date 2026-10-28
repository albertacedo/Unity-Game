# Contingut en CSV

Cartes, enemics, relíquies i esdeveniments **no són ScriptableObjects ni fitxers `.asset`**:
viuen en quatre taules CSV dins del projecte i el joc les carrega en arrencar.

L'objectiu és poder dissenyar contingut en un full de càlcul (o en una finestra d'Unity)
sense tocar codi ni crear assets a mà.

## On és tot

| Peça | Fitxer |
|---|---|
| Cartes | `Assets/Resources/Content/cards.csv` |
| Enemics | `Assets/Resources/Content/enemies.csv` |
| Relíquies | `Assets/Resources/Content/relics.csv` |
| Esdeveniments | `Assets/Resources/Content/events.csv` |
| Parser i format | `Assets/Scripts/Content/ContentCsv.cs` |
| Càrrega al joc | `Assets/Scripts/Content/ContentDatabase.cs` |
| Finestra d'edició | `Assets/Editor/ContentWindow.cs` |
| Validador sense Unity | `tools/content-check/` |

El joc llegeix els fitxers amb `Resources.Load<TextAsset>("Content/cards")`, o sigui que
també funcionen en una build, no només a l'editor. `ContentDatabase` els carrega un cop i
els té en memòria; `CardLibrary` i `EnemyLibrary` només en són façanes.

## Dues maneres d'editar

**Full de càlcul.** Obre el CSV amb Excel (o LibreOffice), edita'l i desa'l *com a CSV*.
El joc i la finestra el llegeixen igual. El separador es detecta tot sol: `;` si l'Excel
és en català o castellà, `,` si és en anglès. En desar es conserva el mateix separador.

**Finestra d'Unity.** Menú **Prototype → Content (CSV)**. Té quatre pestanyes, llista a
l'esquerra i formulari a la dreta, amb botons `+`, `Copy`, `Delete`, `Reload from CSV`,
`Save to CSV` i `Validate`. Desa exactament els mateixos fitxers, així que pots alternar
les dues eines. No té historial de desfer: si recarregues, perds els canvis no desats
(el títol ho indica amb un `*`).

Si deses mentre el joc és en Play mode, la caché es recarrega, però les cartes que ja
són al mas o a la mà són les velles: comença una run nova per veure els canvis.

## Format de les taules

La primera fila és la capçalera. Els noms de columna no distingeixen majúscules i
l'ordre és lliure; les columnes que no es coneixen s'ignoren (i es perden en desar).

Els efectes i els intents van en una sola cel·la amb la forma `nom:valor`, separats per
`|` o `+`:

```
damage:8|vulnerable:2
attack:11|block:6|attack:7
```

### cards.csv

| Columna | Valors | Notes |
|---|---|---|
| `id` | `[a-z0-9_]+`, únic | Identitat de la carta |
| `title` | text | El que es veu a la carta |
| `text` | text, opcional | Si és buit, es genera a partir dels efectes |
| `cost` | 0-3 | |
| `kind` | `attack`, `skill`, `power` | Decideix el color |
| `pool` | `starter`, `reward`, `both`, `none` | On surt la carta |
| `copies` | enter >= 1 | Còpies al mas inicial (ignorat si no és `starter`/`both`) |
| `effects` | llista d'efectes | |

```csv
id;title;text;cost;kind;pool;copies;effects
strike;Strike;;1;attack;starter;4;damage:6
bash;Bash;;2;attack;starter;1;damage:8|vulnerable:2
```

### enemies.csv

| Columna | Valors | Notes |
|---|---|---|
| `id` | `[a-z0-9_]+`, únic | |
| `title` | text | |
| `maxhp` | enter > 0 | |
| `role` | `normal`, `pack`, `boss` | `normal` = sol; `pack` = surt en grup de dos; `boss` = cap |
| `intents` | llista `attack`/`block` | Es repeteix en cicle |

```csv
id;title;maxhp;role;intents
jaw_worm;Jaw Worm;44;normal;attack:11|block:6|attack:7
```

La sala de combat tria així: 55% un enemic `normal`; si no, dos enemics `pack`
(poden repetir-se). El cap de la sala final surt d'entre els `boss`.

### relics.csv

| Columna | Valors |
|---|---|
| `id` | `[a-z0-9_]+`, únic |
| `title` | text |
| `description` | text |
| `rarity` | `common`, `uncommon`, `rare`, `boss`, `event` |

!!! warning "Encara no s'utilitza"
    El sistema de relíquies no existeix: la taula és un catàleg per tenir el contingut
    escrit, però cap pantalla el llegeix.

### events.csv

| Columna | Valors |
|---|---|
| `id` | `[a-z0-9_]+`, únic |
| `title` | text |
| `text` | text |
| `weight` | enter > 0 |
| `options` | `Etiqueta => efecte+efecte | Altres => efecte` |

```csv
id;title;text;weight;options
old_shrine;Old Shrine;A mossy shrine hums.;10;Pray => heal:15 | Desecrate => gain_gold:25+lose_hp:5 | Leave => nothing
```

Efectes d'esdeveniment: `nothing`, `heal`, `lose_hp`, `gain_gold`, `lose_gold`,
`max_hp` i `add_card:id_de_carta` (per exemple `add_card:strike`).

!!! warning "Encara no s'utilitza"
    La sala d'esdeveniments encara és un placeholder. La taula i el format ja es
    validen perquè el contingut estigui a punt quan arribi la pantalla.

## Vocabulari d'efectes de combat

| Efecte | Àlies | Significat |
|---|---|---|
| `damage` | `attack` | Dany a l'objectiu |
| `damage_all` | `all`, `aoe` | Dany a tots els enemics vius |
| `block` | `defend` | Bloqueig del jugador |
| `draw` | | Roba cartes |
| `gain_energy` | `energy` | Energia extra aquest torn |
| `vulnerable` | `vuln` | Aplica Vulnerable (x1,5 dany d'entrada) |

Afegir un efecte nou encara és feina de codi: `EffectKind`, el `switch` de
`CombatSession.ApplyEffects` i, si és un estat, un camp a `EnemyInstance`. La taula
només rep el nom nou.

## Validació

Les mateixes regles s'apliquen al joc, a la finestra i a l'eina de consola:

- `id` únic, en minúscules, sense accents ni espais.
- Títol obligatori (i descripció a les relíquies, text als esdeveniments).
- Cost de carta entre 0 i 3; HP, valors d'efecte, intents i pesos més grans que 0.
- Cap carta sense efectes; cap enemic sense intents; cap esdeveniment sense opcions.
- `add_card` ha d'apuntar a una carta que existeixi.
- Avís si la taula d'enemics no té cap `normal`, cap `pack` o cap `boss`.

Si el contingut té errors, el joc **no peta**: carrega el que pot i escriu tots els
problemes a la consola amb el fitxer i la fila (`cards.csv: row 12: unknown effect 'dammage'`).

## Verificar sense obrir Unity

```powershell
dotnet run --project tools/content-check
```

Llegeix les quatre taules, valida, comprova que escriure i tornar a llegir dona el
mateix fitxer i surt amb codi 1 si hi ha errors. Amb l'argument `--write` reescriu els
fitxers en forma normalitzada (útil després d'editar-los a mà).

`powershell -File tools/compile-check.ps1` compila `Assets/Scripts` **i** `Assets/Editor`,
així que la finestra també es comprova sense obrir l'Editor.

## Limitacions conegudes

- No hi ha ScriptableObjects de carta: no es poden arrossegar a un camp de l'Inspector.
  Si algun dia cal, es fa un importador CSV → `.asset`; avui no existeix a posta.
- Les columnes desconegudes es perden en desar des de la finestra.
- La finestra no té desfer; el CSV sí que té historial al git.
- L'`id` és la identitat de la carta (i ho serà del guardat/continuar): canviar-lo
  després equival a esborrar-la i crear-ne una altra.
