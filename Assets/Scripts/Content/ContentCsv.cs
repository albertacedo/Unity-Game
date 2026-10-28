using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Prototype.Data;

namespace Prototype.Content
{
    /// <summary>
    /// Maps the CSV tables under Assets/Resources/Content to the data classes, and back.
    /// This is the single definition of the file format: the game loads through it, the
    /// editor window saves through it and tools/content-check validates with it.
    ///
    /// Columns are matched by header name, so their order does not matter and empty
    /// columns can be added later. Unknown columns are ignored (and lost on save).
    /// </summary>
    public static class ContentCsv
    {
        public const char DefaultDelimiter = ';';

        public static readonly string[] CardColumns = { "id", "title", "text", "cost", "kind", "pool", "copies", "effects" };
        public static readonly string[] EnemyColumns = { "id", "title", "maxhp", "role", "intents" };
        public static readonly string[] RelicColumns = { "id", "title", "description", "rarity" };
        public static readonly string[] EventColumns = { "id", "title", "text", "weight", "options" };

        static readonly Regex Slug = new Regex("^[a-z0-9_]+$", RegexOptions.Compiled);

        // ---------------------------------------------------------------- cards

        public static List<CardData> ReadCards(string text, string source, List<string> problems, out char delimiter)
        {
            delimiter = Csv.DetectDelimiter(text);
            var cards = new List<CardData>();
            List<string[]> rows = Csv.Parse(text, delimiter);

            if (!TryHeader(rows, CardColumns, source, problems, out Dictionary<string, int> columns))
                return cards;

            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                string where = At(source, r + 1);
                string id = Field(row, columns, "id");

                if (string.IsNullOrEmpty(id) && IsEmptyRow(row))
                    continue;

                var card = new CardData
                {
                    Id = id,
                    Title = Field(row, columns, "title"),
                    Text = Field(row, columns, "text"),
                    Cost = 1,
                    Kind = CardKind.Attack,
                    Pool = CardPool.Reward,
                    Copies = 1
                };

                string cost = Field(row, columns, "cost");
                if (cost.Length > 0 && !TryInt(cost, out card.Cost))
                    problems.Add(where + "'" + cost + "' is not a whole number (cost)");

                string copies = Field(row, columns, "copies");
                if (copies.Length > 0 && !TryInt(copies, out card.Copies))
                    problems.Add(where + "'" + copies + "' is not a whole number (copies)");

                string kind = Field(row, columns, "kind");
                if (kind.Length > 0 && !TryCardKind(kind, out card.Kind))
                    problems.Add(where + "unknown card kind '" + kind + "' (attack, skill, power)");

                string pool = Field(row, columns, "pool");
                if (pool.Length > 0 && !TryCardPool(pool, out card.Pool))
                    problems.Add(where + "unknown pool '" + pool + "' (starter, reward, both, none)");

                card.Effects = ParseEffects(Field(row, columns, "effects"), where, problems);
                cards.Add(card);
            }

            ValidateCards(cards, source, problems);
            WarnExtraColumns(rows, CardColumns, source, problems);
            return cards;
        }

        public static string WriteCards(IList<CardData> cards, char delimiter = DefaultDelimiter)
        {
            var rows = new List<string[]> { CardColumns };
            foreach (CardData card in cards)
            {
                rows.Add(new[]
                {
                    card.Id ?? "",
                    card.Title ?? "",
                    card.Text ?? "",
                    card.Cost.ToString(CultureInfo.InvariantCulture),
                    CardKindName(card.Kind),
                    CardPoolName(card.Pool),
                    card.Copies.ToString(CultureInfo.InvariantCulture),
                    WriteEffects(card.Effects)
                });
            }
            return Csv.Write(rows, delimiter);
        }

        public static void ValidateCards(IList<CardData> cards, string source, List<string> problems)
        {
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                string where = At(source, i + 2) + Id(card.Id, "no id");

                if (string.IsNullOrEmpty(card.Id))
                    problems.Add(At(source, i + 2) + "missing id");
                else if (!Slug.IsMatch(card.Id))
                    problems.Add(where + "id must be lowercase letters, digits and underscores");
                else if (seen.ContainsKey(card.Id))
                    problems.Add(where + "duplicate id");
                else
                    seen[card.Id] = i;

                if (string.IsNullOrEmpty(card.Title))
                    problems.Add(where + "missing title");

                if (card.Cost < 0 || card.Cost > 3)
                    problems.Add(where + "cost " + card.Cost + " is outside 0..3");

                if (card.Copies < 1)
                    problems.Add(where + "copies must be 1 or more");

                if (card.Effects == null || card.Effects.Length == 0)
                {
                    problems.Add(where + "card has no effects");
                }
                else
                {
                    foreach (CardEffect effect in card.Effects)
                    {
                        if (effect == null) continue;
                        if (effect.Value <= 0)
                            problems.Add(where + EffectName(effect.Kind) + " must be greater than 0");
                    }
                }
            }
        }

        // -------------------------------------------------------------- enemies

        public static List<EnemyData> ReadEnemies(string text, string source, List<string> problems, out char delimiter)
        {
            delimiter = Csv.DetectDelimiter(text);
            var enemies = new List<EnemyData>();
            List<string[]> rows = Csv.Parse(text, delimiter);

            if (!TryHeader(rows, EnemyColumns, source, problems, out Dictionary<string, int> columns))
                return enemies;

            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                string where = At(source, r + 1);
                string id = Field(row, columns, "id");

                if (string.IsNullOrEmpty(id) && IsEmptyRow(row))
                    continue;

                var enemy = new EnemyData
                {
                    Id = id,
                    Title = Field(row, columns, "title"),
                    MaxHp = 40,
                    Role = EnemyRole.Normal
                };

                string hp = Field(row, columns, "maxhp");
                if (hp.Length > 0 && !TryInt(hp, out enemy.MaxHp))
                    problems.Add(where + "'" + hp + "' is not a whole number (maxhp)");

                string role = Field(row, columns, "role");
                if (role.Length > 0 && !TryEnemyRole(role, out enemy.Role))
                    problems.Add(where + "unknown role '" + role + "' (normal, pack, boss)");

                enemy.Intents = ParseIntents(Field(row, columns, "intents"), where, problems);
                enemies.Add(enemy);
            }

            ValidateEnemies(enemies, source, problems);
            WarnExtraColumns(rows, EnemyColumns, source, problems);
            return enemies;
        }

        public static string WriteEnemies(IList<EnemyData> enemies, char delimiter = DefaultDelimiter)
        {
            var rows = new List<string[]> { EnemyColumns };
            foreach (EnemyData enemy in enemies)
            {
                rows.Add(new[]
                {
                    enemy.Id ?? "",
                    enemy.Title ?? "",
                    enemy.MaxHp.ToString(CultureInfo.InvariantCulture),
                    EnemyRoleName(enemy.Role),
                    WriteIntents(enemy.Intents)
                });
            }
            return Csv.Write(rows, delimiter);
        }

        public static void ValidateEnemies(IList<EnemyData> enemies, string source, List<string> problems)
        {
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            bool hasNormal = false, hasPack = false, hasBoss = false;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyData enemy = enemies[i];
                string where = At(source, i + 2) + Id(enemy.Id, "no id");

                if (string.IsNullOrEmpty(enemy.Id))
                    problems.Add(At(source, i + 2) + "missing id");
                else if (!Slug.IsMatch(enemy.Id))
                    problems.Add(where + "id must be lowercase letters, digits and underscores");
                else if (seen.ContainsKey(enemy.Id))
                    problems.Add(where + "duplicate id");
                else
                    seen[enemy.Id] = i;

                if (string.IsNullOrEmpty(enemy.Title))
                    problems.Add(where + "missing title");

                if (enemy.MaxHp <= 0)
                    problems.Add(where + "maxhp must be greater than 0");

                if (enemy.Intents == null || enemy.Intents.Length == 0)
                {
                    problems.Add(where + "enemy has no intents");
                }
                else
                {
                    foreach (EnemyIntent intent in enemy.Intents)
                    {
                        if (intent == null) continue;
                        if (intent.Value <= 0)
                            problems.Add(where + "intent value must be greater than 0");
                    }
                }

                if (enemy.Role == EnemyRole.Normal) hasNormal = true;
                else if (enemy.Role == EnemyRole.Pack) hasPack = true;
                else if (enemy.Role == EnemyRole.Boss) hasBoss = true;
            }

            if (enemies.Count > 0 && !hasNormal)
                problems.Add("warning: " + source + " has no 'normal' enemy, solo fights will fall back to packs");
            if (enemies.Count > 0 && !hasPack)
                problems.Add("warning: " + source + " has no 'pack' enemy, group fights will use two normals");
            if (enemies.Count > 0 && !hasBoss)
                problems.Add("warning: " + source + " has no 'boss' enemy, the map exit will have no fight");
        }

        // --------------------------------------------------------------- relics

        public static List<RelicData> ReadRelics(string text, string source, List<string> problems, out char delimiter)
        {
            delimiter = Csv.DetectDelimiter(text);
            var relics = new List<RelicData>();
            List<string[]> rows = Csv.Parse(text, delimiter);

            if (!TryHeader(rows, RelicColumns, source, problems, out Dictionary<string, int> columns))
                return relics;

            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                string where = At(source, r + 1);
                string id = Field(row, columns, "id");

                if (string.IsNullOrEmpty(id) && IsEmptyRow(row))
                    continue;

                var relic = new RelicData
                {
                    Id = id,
                    Title = Field(row, columns, "title"),
                    Description = Field(row, columns, "description"),
                    Rarity = RelicRarity.Common
                };

                string rarity = Field(row, columns, "rarity");
                if (rarity.Length > 0 && !TryRelicRarity(rarity, out relic.Rarity))
                    problems.Add(where + "unknown rarity '" + rarity + "' (common, uncommon, rare, boss, event)");

                relics.Add(relic);
            }

            ValidateRelics(relics, source, problems);
            WarnExtraColumns(rows, RelicColumns, source, problems);
            return relics;
        }

        public static string WriteRelics(IList<RelicData> relics, char delimiter = DefaultDelimiter)
        {
            var rows = new List<string[]> { RelicColumns };
            foreach (RelicData relic in relics)
            {
                rows.Add(new[]
                {
                    relic.Id ?? "",
                    relic.Title ?? "",
                    relic.Description ?? "",
                    RelicRarityName(relic.Rarity)
                });
            }
            return Csv.Write(rows, delimiter);
        }

        public static void ValidateRelics(IList<RelicData> relics, string source, List<string> problems)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < relics.Count; i++)
            {
                RelicData relic = relics[i];
                string where = At(source, i + 2) + Id(relic.Id, "no id");

                if (string.IsNullOrEmpty(relic.Id))
                    problems.Add(At(source, i + 2) + "missing id");
                else if (!Slug.IsMatch(relic.Id))
                    problems.Add(where + "id must be lowercase letters, digits and underscores");
                else if (!seen.Add(relic.Id))
                    problems.Add(where + "duplicate id");

                if (string.IsNullOrEmpty(relic.Title))
                    problems.Add(where + "missing title");
                if (string.IsNullOrEmpty(relic.Description))
                    problems.Add(where + "missing description");
            }
        }

        // --------------------------------------------------------------- events

        public static List<EventData> ReadEvents(string text, string source, List<string> problems, out char delimiter)
        {
            delimiter = Csv.DetectDelimiter(text);
            var events = new List<EventData>();
            List<string[]> rows = Csv.Parse(text, delimiter);

            if (!TryHeader(rows, EventColumns, source, problems, out Dictionary<string, int> columns))
                return events;

            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                string where = At(source, r + 1);
                string id = Field(row, columns, "id");

                if (string.IsNullOrEmpty(id) && IsEmptyRow(row))
                    continue;

                var entry = new EventData
                {
                    Id = id,
                    Title = Field(row, columns, "title"),
                    Text = Field(row, columns, "text"),
                    Weight = 10
                };

                string weight = Field(row, columns, "weight");
                if (weight.Length > 0 && !TryInt(weight, out entry.Weight))
                    problems.Add(where + "'" + weight + "' is not a whole number (weight)");

                entry.Options = ParseOptions(Field(row, columns, "options"), where, problems);
                events.Add(entry);
            }

            ValidateEvents(events, source, problems);
            WarnExtraColumns(rows, EventColumns, source, problems);
            return events;
        }

        public static string WriteEvents(IList<EventData> events, char delimiter = DefaultDelimiter)
        {
            var rows = new List<string[]> { EventColumns };
            foreach (EventData entry in events)
            {
                rows.Add(new[]
                {
                    entry.Id ?? "",
                    entry.Title ?? "",
                    entry.Text ?? "",
                    entry.Weight.ToString(CultureInfo.InvariantCulture),
                    WriteOptions(entry.Options)
                });
            }
            return Csv.Write(rows, delimiter);
        }

        public static void ValidateEvents(IList<EventData> events, string source, List<string> problems)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < events.Count; i++)
            {
                EventData entry = events[i];
                string where = At(source, i + 2) + Id(entry.Id, "no id");

                if (string.IsNullOrEmpty(entry.Id))
                    problems.Add(At(source, i + 2) + "missing id");
                else if (!Slug.IsMatch(entry.Id))
                    problems.Add(where + "id must be lowercase letters, digits and underscores");
                else if (!seen.Add(entry.Id))
                    problems.Add(where + "duplicate id");

                if (string.IsNullOrEmpty(entry.Title))
                    problems.Add(where + "missing title");
                if (string.IsNullOrEmpty(entry.Text))
                    problems.Add(where + "missing text");
                if (entry.Weight <= 0)
                    problems.Add(where + "weight must be greater than 0");

                if (entry.Options == null || entry.Options.Length == 0)
                {
                    problems.Add(where + "event has no options");
                    continue;
                }

                foreach (EventOption option in entry.Options)
                {
                    if (option == null) continue;
                    if (string.IsNullOrEmpty(option.Label))
                        problems.Add(where + "an option has no label");
                }
            }
        }

        /// <summary>Cross-table checks: an event may only add cards that exist.</summary>
        public static void ValidateReferences(IList<CardData> cards, IList<EventData> events, List<string> problems)
        {
            var cardIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CardData card in cards)
                if (!string.IsNullOrEmpty(card.Id)) cardIds.Add(card.Id);

            for (int i = 0; i < events.Count; i++)
            {
                EventData entry = events[i];
                if (entry.Options == null) continue;

                foreach (EventOption option in entry.Options)
                {
                    if (option == null || option.Effects == null) continue;

                    foreach (EventEffect effect in option.Effects)
                    {
                        if (effect == null || effect.Kind != EventEffectKind.AddCard) continue;
                        if (string.IsNullOrEmpty(effect.Target) || !cardIds.Contains(effect.Target))
                            problems.Add(At("events.csv", i + 2) + "unknown card '" + effect.Target + "' in add_card");
                    }
                }
            }
        }

        // -------------------------------------------------------------- helpers

        static bool TryHeader(List<string[]> rows, string[] required, string source, List<string> problems,
            out Dictionary<string, int> columns)
        {
            columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (rows.Count == 0)
            {
                problems.Add(source + ": the file is empty (missing header row)");
                return false;
            }

            string[] header = rows[0];
            for (int i = 0; i < header.Length; i++)
            {
                string name = (header[i] ?? "").Trim().ToLowerInvariant();
                if (name.Length > 0 && !columns.ContainsKey(name)) columns[name] = i;
            }

            bool ok = true;
            foreach (string name in required)
            {
                if (columns.ContainsKey(name)) continue;
                problems.Add(source + ": missing required column '" + name + "'");
                ok = false;
            }

            return ok;
        }

        static void WarnExtraColumns(List<string[]> rows, string[] known, string source, List<string> problems)
        {
            if (rows.Count == 0) return;

            var allowed = new HashSet<string>(known, StringComparer.OrdinalIgnoreCase);
            foreach (string raw in rows[0])
            {
                string name = (raw ?? "").Trim();
                if (name.Length == 0 || allowed.Contains(name)) continue;
                problems.Add("warning: " + source + ": column '" + name + "' is not known and will be lost on save");
            }
        }

        static string Field(string[] row, Dictionary<string, int> columns, string name)
        {
            int index;
            if (!columns.TryGetValue(name, out index) || index >= row.Length) return "";
            return (row[index] ?? "").Trim();
        }

        static bool IsEmptyRow(string[] row)
        {
            foreach (string value in row)
                if (!string.IsNullOrEmpty(value) && value.Trim().Length > 0) return false;
            return true;
        }

        static CardEffect[] ParseEffects(string raw, string where, List<string> problems)
        {
            var effects = new List<CardEffect>();
            foreach (string token in SplitTokens(raw))
            {
                string name = token;
                int value = 1;

                int colon = token.IndexOf(':');
                if (colon >= 0)
                {
                    name = token.Substring(0, colon).Trim();
                    if (!TryInt(token.Substring(colon + 1).Trim(), out value))
                    {
                        problems.Add(where + "effect '" + token + "' has a value that is not a whole number");
                        continue;
                    }
                }

                EffectKind kind;
                if (!TryEffectKind(name, out kind))
                {
                    problems.Add(where + "unknown effect '" + name + "'");
                    continue;
                }

                effects.Add(new CardEffect(kind, value));
            }
            return effects.ToArray();
        }

        static EnemyIntent[] ParseIntents(string raw, string where, List<string> problems)
        {
            var intents = new List<EnemyIntent>();
            foreach (string token in SplitTokens(raw))
            {
                string name = token;
                int value = 6;

                int colon = token.IndexOf(':');
                if (colon >= 0)
                {
                    name = token.Substring(0, colon).Trim();
                    if (!TryInt(token.Substring(colon + 1).Trim(), out value))
                    {
                        problems.Add(where + "intent '" + token + "' has a value that is not a whole number");
                        continue;
                    }
                }

                IntentKind kind;
                if (!TryIntentKind(name, out kind))
                {
                    problems.Add(where + "unknown intent '" + name + "' (attack, block)");
                    continue;
                }

                intents.Add(new EnemyIntent(kind, value));
            }
            return intents.ToArray();
        }

        static EventOption[] ParseOptions(string raw, string where, List<string> problems)
        {
            var options = new List<EventOption>();
            if (string.IsNullOrEmpty(raw)) return options.ToArray();

            foreach (string part in raw.Split('|'))
            {
                if (part.Trim().Length == 0) continue;

                string[] halves = part.Split(new[] { "=>" }, 2, StringSplitOptions.None);
                if (halves.Length < 2)
                {
                    problems.Add(where + "option '" + part.Trim() + "' needs 'label => effects'");
                    continue;
                }

                var option = new EventOption
                {
                    Label = halves[0].Trim(),
                    Effects = ParseEventEffects(halves[1], where, problems)
                };

                options.Add(option);
            }

            return options.ToArray();
        }

        static EventEffect[] ParseEventEffects(string raw, string where, List<string> problems)
        {
            var effects = new List<EventEffect>();
            foreach (string token in SplitTokens(raw))
            {
                string name = token;
                int value = 0;
                string target = null;

                int colon = token.IndexOf(':');
                if (colon >= 0)
                {
                    name = token.Substring(0, colon).Trim();
                    string argument = token.Substring(colon + 1).Trim();

                    int number;
                    if (TryInt(argument, out number)) value = number;
                    else target = argument;
                }

                EventEffectKind kind;
                if (!TryEventEffectKind(name, out kind))
                {
                    problems.Add(where + "unknown event effect '" + name + "'");
                    continue;
                }

                if (kind == EventEffectKind.AddCard && string.IsNullOrEmpty(target))
                {
                    problems.Add(where + "add_card needs a card id, e.g. add_card:strike");
                    continue;
                }

                effects.Add(new EventEffect(kind, kind == EventEffectKind.Nothing ? 0 : value, target));
            }
            return effects.ToArray();
        }

        static IEnumerable<string> SplitTokens(string raw)
        {
            if (string.IsNullOrEmpty(raw)) yield break;

            foreach (string part in raw.Split('|', '+'))
            {
                string token = part.Trim();
                if (token.Length > 0) yield return token;
            }
        }

        static bool TryInt(string raw, out int value)
        {
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        static string Normalize(string raw)
        {
            return (raw ?? "").Trim().ToLowerInvariant()
                .Replace("_", "").Replace("-", "").Replace(" ", "");
        }

        static bool TryEffectKind(string raw, out EffectKind kind)
        {
            switch (Normalize(raw))
            {
                case "damage": case "attack": kind = EffectKind.Damage; return true;
                case "damageall": case "all": case "aoe": kind = EffectKind.DamageAll; return true;
                case "block": case "defend": kind = EffectKind.Block; return true;
                case "draw": kind = EffectKind.Draw; return true;
                case "gainenergy": case "energy": kind = EffectKind.GainEnergy; return true;
                case "vulnerable": case "vuln": kind = EffectKind.Vulnerable; return true;
                default: kind = EffectKind.Damage; return false;
            }
        }

        public static string EffectName(EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.Damage: return "damage";
                case EffectKind.DamageAll: return "damage_all";
                case EffectKind.Block: return "block";
                case EffectKind.Draw: return "draw";
                case EffectKind.GainEnergy: return "gain_energy";
                default: return "vulnerable";
            }
        }

        static bool TryIntentKind(string raw, out IntentKind kind)
        {
            switch (Normalize(raw))
            {
                case "attack": case "hit": case "damage": kind = IntentKind.Attack; return true;
                case "block": case "defend": kind = IntentKind.Block; return true;
                default: kind = IntentKind.Attack; return false;
            }
        }

        public static string IntentName(IntentKind kind)
        {
            return kind == IntentKind.Attack ? "attack" : "block";
        }

        static bool TryCardKind(string raw, out CardKind kind)
        {
            switch (Normalize(raw))
            {
                case "attack": kind = CardKind.Attack; return true;
                case "skill": kind = CardKind.Skill; return true;
                case "power": kind = CardKind.Power; return true;
                default: kind = CardKind.Attack; return false;
            }
        }

        public static string CardKindName(CardKind kind)
        {
            return kind.ToString().ToLowerInvariant();
        }

        static bool TryCardPool(string raw, out CardPool pool)
        {
            switch (Normalize(raw))
            {
                case "starter":
                case "start":
                    pool = CardPool.Starter; return true;
                case "reward":
                    pool = CardPool.Reward; return true;
                case "both":
                case "all":
                    pool = CardPool.Both; return true;
                case "none":
                    pool = CardPool.None; return true;
                default:
                    pool = CardPool.Reward; return false;
            }
        }

        public static string CardPoolName(CardPool pool)
        {
            return pool.ToString().ToLowerInvariant();
        }

        static bool TryEnemyRole(string raw, out EnemyRole role)
        {
            switch (Normalize(raw))
            {
                case "normal": case "solo": role = EnemyRole.Normal; return true;
                case "pack": case "group": role = EnemyRole.Pack; return true;
                case "boss": role = EnemyRole.Boss; return true;
                default: role = EnemyRole.Normal; return false;
            }
        }

        public static string EnemyRoleName(EnemyRole role)
        {
            return role.ToString().ToLowerInvariant();
        }

        static bool TryRelicRarity(string raw, out RelicRarity rarity)
        {
            switch (Normalize(raw))
            {
                case "common": rarity = RelicRarity.Common; return true;
                case "uncommon": rarity = RelicRarity.Uncommon; return true;
                case "rare": rarity = RelicRarity.Rare; return true;
                case "boss": rarity = RelicRarity.Boss; return true;
                case "event": rarity = RelicRarity.Event; return true;
                default: rarity = RelicRarity.Common; return false;
            }
        }

        public static string RelicRarityName(RelicRarity rarity)
        {
            return rarity.ToString().ToLowerInvariant();
        }

        static bool TryEventEffectKind(string raw, out EventEffectKind kind)
        {
            switch (Normalize(raw))
            {
                case "nothing": case "none": kind = EventEffectKind.Nothing; return true;
                case "heal": kind = EventEffectKind.Heal; return true;
                case "losehp": case "damage": kind = EventEffectKind.LoseHp; return true;
                case "gaingold": case "gold": kind = EventEffectKind.GainGold; return true;
                case "losegold": kind = EventEffectKind.LoseGold; return true;
                case "maxhp": kind = EventEffectKind.MaxHp; return true;
                case "addcard": case "card": kind = EventEffectKind.AddCard; return true;
                default: kind = EventEffectKind.Nothing; return false;
            }
        }

        public static string EventEffectName(EventEffectKind kind)
        {
            switch (kind)
            {
                case EventEffectKind.Heal: return "heal";
                case EventEffectKind.LoseHp: return "lose_hp";
                case EventEffectKind.GainGold: return "gain_gold";
                case EventEffectKind.LoseGold: return "lose_gold";
                case EventEffectKind.MaxHp: return "max_hp";
                case EventEffectKind.AddCard: return "add_card";
                default: return "nothing";
            }
        }

        static string WriteEffects(CardEffect[] effects)
        {
            if (effects == null) return "";
            var parts = new List<string>();
            foreach (CardEffect effect in effects)
            {
                if (effect == null) continue;
                parts.Add(EffectName(effect.Kind) + ":" + effect.Value.ToString(CultureInfo.InvariantCulture));
            }
            return string.Join("|", parts);
        }

        static string WriteIntents(EnemyIntent[] intents)
        {
            if (intents == null) return "";
            var parts = new List<string>();
            foreach (EnemyIntent intent in intents)
            {
                if (intent == null) continue;
                parts.Add(IntentName(intent.Kind) + ":" + intent.Value.ToString(CultureInfo.InvariantCulture));
            }
            return string.Join("|", parts);
        }

        static string WriteOptions(EventOption[] options)
        {
            if (options == null) return "";
            var parts = new List<string>();
            foreach (EventOption option in options)
            {
                if (option == null) continue;

                var effects = new List<string>();
                if (option.Effects != null)
                {
                    foreach (EventEffect effect in option.Effects)
                    {
                        if (effect == null) continue;

                        string name = EventEffectName(effect.Kind);
                        if (effect.Kind == EventEffectKind.AddCard) effects.Add(name + ":" + effect.Target);
                        else if (effect.Kind != EventEffectKind.Nothing)
                            effects.Add(name + ":" + effect.Value.ToString(CultureInfo.InvariantCulture));
                        else effects.Add(name);
                    }
                }

                parts.Add((option.Label ?? "") + " => " + string.Join("+", effects));
            }
            return string.Join("|", parts);
        }

        static string At(string source, int row)
        {
            return source + ": row " + row + ": ";
        }

        static string Id(string id, string fallback)
        {
            return "(" + (string.IsNullOrEmpty(id) ? fallback : id) + "): ";
        }
    }
}
