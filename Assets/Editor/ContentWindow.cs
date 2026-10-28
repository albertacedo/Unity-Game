using System;
using System.Collections.Generic;
using System.IO;
using Prototype.Content;
using Prototype.Data;
using UnityEditor;
using UnityEngine;

namespace Prototype.EditorTools
{
    /// <summary>
    /// Authoring window for the CSV tables in Assets/Resources/Content. It edits the
    /// same files the game loads, so there is no import step and no .asset files:
    /// load, edit, save, play.
    ///
    /// The window works on plain C# objects (no SerializedObject), so there is no
    /// undo history for the fields; "Reload from CSV" discards unsaved edits.
    /// </summary>
    public class ContentWindow : EditorWindow
    {
        enum Tab { Cards, Enemies, Relics, Events }

        const string ContentFolder = "Assets/Resources/Content";
        static readonly string[] Files = { "cards.csv", "enemies.csv", "relics.csv", "events.csv" };

        readonly List<CardData> cards = new List<CardData>();
        readonly List<EnemyData> enemies = new List<EnemyData>();
        readonly List<RelicData> relics = new List<RelicData>();
        readonly List<EventData> events = new List<EventData>();

        readonly List<string> problems = new List<string>();
        readonly char[] delimiters = { ContentCsv.DefaultDelimiter, ContentCsv.DefaultDelimiter, ContentCsv.DefaultDelimiter, ContentCsv.DefaultDelimiter };
        readonly DateTime[] loadedAt = new DateTime[4];

        Tab tab;
        int selected;
        Vector2 listScroll;
        Vector2 formScroll;
        Vector2 problemScroll;
        bool dirty;
        string status = "";

        [MenuItem("Prototype/Content (CSV)")]
        public static void Open()
        {
            var window = GetWindow<ContentWindow>("Content (CSV)");
            window.minSize = new Vector2(820f, 520f);
            window.Show();
        }

        void OnEnable()
        {
            Load();
        }

        // ------------------------------------------------------------------ files

        void Load()
        {
            cards.Clear();
            enemies.Clear();
            relics.Clear();
            events.Clear();
            problems.Clear();
            selected = 0;
            dirty = false;

            string cardText = ReadFile("cards.csv");
            if (cardText != null)
            {
                cards.AddRange(ContentCsv.ReadCards(cardText, "cards.csv", problems, out delimiters[0]));
                if (!HasHeader(cardText)) delimiters[0] = ContentCsv.DefaultDelimiter;
            }
            else
            {
                problems.Add("cards.csv: file not found; Save will create it");
            }

            string enemyText = ReadFile("enemies.csv");
            if (enemyText != null)
            {
                enemies.AddRange(ContentCsv.ReadEnemies(enemyText, "enemies.csv", problems, out delimiters[1]));
                if (!HasHeader(enemyText)) delimiters[1] = ContentCsv.DefaultDelimiter;
            }
            else
            {
                problems.Add("enemies.csv: file not found; Save will create it");
            }

            string relicText = ReadFile("relics.csv");
            if (relicText != null)
            {
                relics.AddRange(ContentCsv.ReadRelics(relicText, "relics.csv", problems, out delimiters[2]));
                if (!HasHeader(relicText)) delimiters[2] = ContentCsv.DefaultDelimiter;
            }
            else
            {
                problems.Add("relics.csv: file not found; Save will create it");
            }

            string eventText = ReadFile("events.csv");
            if (eventText != null)
            {
                events.AddRange(ContentCsv.ReadEvents(eventText, "events.csv", problems, out delimiters[3]));
                if (!HasHeader(eventText)) delimiters[3] = ContentCsv.DefaultDelimiter;
            }
            else
            {
                problems.Add("events.csv: file not found; Save will create it");
            }

            ContentCsv.ValidateReferences(cards, events, problems);
            RememberTimestamps();
            status = "Loaded " + cards.Count + " cards, " + enemies.Count + " enemies, " +
                     relics.Count + " relics, " + events.Count + " events.";
        }

        void Save()
        {
            if (FilesChangedSinceLoad())
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Content changed on disk",
                    "One or more CSV files were modified after this window loaded them " +
                    "(for example in Excel). Overwriting will lose those edits.",
                    "Overwrite", "Cancel");

                if (!overwrite) return;
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources", "Content"));

                File.WriteAllText(PathOf("cards.csv"), ContentCsv.WriteCards(cards, delimiters[0]));
                File.WriteAllText(PathOf("enemies.csv"), ContentCsv.WriteEnemies(enemies, delimiters[1]));
                File.WriteAllText(PathOf("relics.csv"), ContentCsv.WriteRelics(relics, delimiters[2]));
                File.WriteAllText(PathOf("events.csv"), ContentCsv.WriteEvents(events, delimiters[3]));

                AssetDatabase.Refresh();
                RememberTimestamps();
                dirty = false;
                Validate();

                if (Application.isPlaying)
                {
                    ContentDatabase.Reload();
                    status = "Saved. Runtime cache reloaded; rebuild the run to pick up new cards.";
                }
                else
                {
                    status = "Saved at " + DateTime.Now.ToString("HH:mm:ss") + ".";
                }
            }
            catch (Exception exception)
            {
                problems.Insert(0, "save failed: " + exception.Message);
                status = "Save failed.";
            }
        }

        void RememberTimestamps()
        {
            for (int i = 0; i < Files.Length; i++) loadedAt[i] = Stamp(Files[i]);
        }

        bool FilesChangedSinceLoad()
        {
            for (int i = 0; i < Files.Length; i++)
                if (Stamp(Files[i]) != loadedAt[i]) return true;
            return false;
        }

        static DateTime Stamp(string file)
        {
            string path = PathOf(file);
            return File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue;
        }

        static string PathOf(string file)
        {
            return Path.Combine(Application.dataPath, "Resources", "Content", file);
        }

        static string ReadFile(string file)
        {
            string path = PathOf(file);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        /// <summary>False when the file is missing or blank, so save can create it.</summary>
        static bool HasHeader(string text)
        {
            return !string.IsNullOrEmpty(text) && text.Trim().Length > 0;
        }

        void Validate()
        {
            problems.Clear();
            ContentCsv.ValidateCards(cards, "cards.csv", problems);
            ContentCsv.ValidateEnemies(enemies, "enemies.csv", problems);
            ContentCsv.ValidateRelics(relics, "relics.csv", problems);
            ContentCsv.ValidateEvents(events, "events.csv", problems);
            ContentCsv.ValidateReferences(cards, events, problems);
        }

        // --------------------------------------------------------------------- ui

        void OnGUI()
        {
            DrawToolbar();

            tab = (Tab)GUILayout.Toolbar((int)tab, new[] { "Cards", "Enemies", "Relics", "Events" });

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(230f));
            DrawList();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            formScroll = EditorGUILayout.BeginScrollView(formScroll, "box", GUILayout.ExpandHeight(true));
            DrawForm();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            DrawFooter();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Reload from CSV", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                Load();

            if (GUILayout.Button(dirty ? "Save to CSV *" : "Save to CSV", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                Save();

            if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                Validate();

            GUILayout.FlexibleSpace();

            GUILayout.Label(status, EditorStyles.miniLabel);
            GUILayout.Space(8f);

            if (GUILayout.Button("Open folder", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                EditorUtility.RevealInFinder(ContentFolder);

            EditorGUILayout.EndHorizontal();
        }

        void DrawFooter()
        {
            if (problems.Count == 0)
            {
                EditorGUILayout.HelpBox(dirty ? "No problems. Unsaved changes." : "No problems.", MessageType.Info);
                return;
            }

            int errors = 0;
            foreach (string problem in problems)
                if (!problem.StartsWith("warning:", StringComparison.Ordinal)) errors++;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(errors + " error(s), " + (problems.Count - errors) + " warning(s)",
                EditorStyles.boldLabel);

            problemScroll = EditorGUILayout.BeginScrollView(problemScroll, GUILayout.Height(110f));
            foreach (string problem in problems)
            {
                if (problem.StartsWith("warning:", StringComparison.Ordinal))
                    EditorGUILayout.LabelField(problem, EditorStyles.miniLabel);
                else
                    EditorGUILayout.HelpBox(problem, MessageType.Error);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ------------------------------------------------------------------- list

        void DrawList()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24f)))
                AddNew();
            if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.Width(46f)))
                Duplicate();
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                Delete();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            listScroll = EditorGUILayout.BeginScrollView(listScroll, "box", GUILayout.ExpandHeight(true));

            int count = Count();
            for (int i = 0; i < count; i++)
            {
                bool isSelected = i == selected;
                string label = EntryLabel(i);

                if (GUILayout.Toggle(isSelected, label, "Button") && !isSelected)
                {
                    selected = i;
                    formScroll = Vector2.zero;
                    GUI.FocusControl(null);
                }
            }

            if (count == 0)
                EditorGUILayout.LabelField("Empty. Use + to add the first one.", EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        int Count()
        {
            switch (tab)
            {
                case Tab.Cards: return cards.Count;
                case Tab.Enemies: return enemies.Count;
                case Tab.Relics: return relics.Count;
                default: return events.Count;
            }
        }

        string EntryLabel(int index)
        {
            switch (tab)
            {
                case Tab.Cards:
                {
                    CardData card = cards[index];
                    return "  " + Safe(card.Id) + "  ·  " + Safe(card.Title);
                }
                case Tab.Enemies:
                {
                    EnemyData enemy = enemies[index];
                    return "  " + Safe(enemy.Id) + "  ·  " + Safe(enemy.Title);
                }
                case Tab.Relics:
                {
                    RelicData relic = relics[index];
                    return "  " + Safe(relic.Id) + "  ·  " + Safe(relic.Title);
                }
                default:
                {
                    EventData entry = events[index];
                    return "  " + Safe(entry.Id) + "  ·  " + Safe(entry.Title);
                }
            }
        }

        static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "(no id)" : value;
        }

        void AddNew()
        {
            string id = UniqueId("new_card");

            switch (tab)
            {
                case Tab.Cards:
                    cards.Add(new CardData
                    {
                        Id = id,
                        Title = "New card",
                        Cost = 1,
                        Kind = CardKind.Attack,
                        Pool = CardPool.Reward,
                        Effects = new[] { new CardEffect(EffectKind.Damage, 6) }
                    });
                    selected = cards.Count - 1;
                    break;

                case Tab.Enemies:
                    enemies.Add(new EnemyData
                    {
                        Id = UniqueId("new_enemy"),
                        Title = "New enemy",
                        MaxHp = 30,
                        Role = EnemyRole.Normal,
                        Intents = new[] { new EnemyIntent(IntentKind.Attack, 6) }
                    });
                    selected = enemies.Count - 1;
                    break;

                case Tab.Relics:
                    relics.Add(new RelicData
                    {
                        Id = UniqueId("new_relic"),
                        Title = "New relic",
                        Description = "",
                        Rarity = RelicRarity.Common
                    });
                    selected = relics.Count - 1;
                    break;

                default:
                    events.Add(new EventData
                    {
                        Id = UniqueId("new_event"),
                        Title = "New event",
                        Text = "",
                        Weight = 10,
                        Options = new[]
                        {
                            new EventOption
                            {
                                Label = "Leave",
                                Effects = new[] { new EventEffect(EventEffectKind.Nothing, 0) }
                            }
                        }
                    });
                    selected = events.Count - 1;
                    break;
            }

            dirty = true;
            formScroll = Vector2.zero;
        }

        void Duplicate()
        {
            switch (tab)
            {
                case Tab.Cards when selected < cards.Count:
                    cards.Insert(selected + 1, Clone(cards[selected]));
                    selected++;
                    break;
                case Tab.Enemies when selected < enemies.Count:
                    enemies.Insert(selected + 1, Clone(enemies[selected]));
                    selected++;
                    break;
                case Tab.Relics when selected < relics.Count:
                    relics.Insert(selected + 1, Clone(relics[selected]));
                    selected++;
                    break;
                case Tab.Events when selected < events.Count:
                    events.Insert(selected + 1, Clone(events[selected]));
                    selected++;
                    break;
                default:
                    return;
            }

            // A duplicate must not keep the same id, so the table stays valid after save.
            switch (tab)
            {
                case Tab.Cards: cards[selected].Id = UniqueId(cards[selected].Id); break;
                case Tab.Enemies: enemies[selected].Id = UniqueId(enemies[selected].Id); break;
                case Tab.Relics: relics[selected].Id = UniqueId(relics[selected].Id); break;
                default: events[selected].Id = UniqueId(events[selected].Id); break;
            }

            dirty = true;
            formScroll = Vector2.zero;
        }

        void Delete()
        {
            if (selected < 0 || selected >= Count()) return;

            switch (tab)
            {
                case Tab.Cards: cards.RemoveAt(selected); break;
                case Tab.Enemies: enemies.RemoveAt(selected); break;
                case Tab.Relics: relics.RemoveAt(selected); break;
                default: events.RemoveAt(selected); break;
            }

            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, Count() - 1));
            dirty = true;
        }

        /// <summary>Next free id based on a base name: new_card, new_card_2, new_card_3...</summary>
        string UniqueId(string baseId)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (CardData card in cards) if (card != null) used.Add(card.Id);
            foreach (EnemyData enemy in enemies) if (enemy != null) used.Add(enemy.Id);
            foreach (RelicData relic in relics) if (relic != null) used.Add(relic.Id);
            foreach (EventData entry in events) if (entry != null) used.Add(entry.Id);

            string slug = Slugify(baseId);
            if (!used.Contains(slug)) return slug;

            for (int i = 2; i < 1000; i++)
            {
                string candidate = slug + "_" + i;
                if (!used.Contains(candidate)) return candidate;
            }
            return slug + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        static string Slugify(string value)
        {
            string lower = (value ?? "").Trim().ToLowerInvariant();
            var builder = new System.Text.StringBuilder();
            foreach (char c in lower)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) builder.Append(c);
                else if (c == '_' || c == '-' || c == ' ') builder.Append('_');
            }

            string slug = builder.ToString().Trim('_');
            return slug.Length == 0 ? "new_item" : slug;
        }

        static T Clone<T>(T value) where T : class
        {
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        }

        // ------------------------------------------------------------------- form

        void DrawForm()
        {
            if (selected < 0 || selected >= Count())
            {
                EditorGUILayout.LabelField("Select an entry on the left, or add one with +.", EditorStyles.wordWrappedLabel);
                return;
            }

            EditorGUI.BeginChangeCheck();

            switch (tab)
            {
                case Tab.Cards: DrawCard(cards[selected]); break;
                case Tab.Enemies: DrawEnemy(enemies[selected]); break;
                case Tab.Relics: DrawRelic(relics[selected]); break;
                default: DrawEvent(events[selected]); break;
            }

            if (EditorGUI.EndChangeCheck()) dirty = true;
        }

        void DrawCard(CardData card)
        {
            EditorGUILayout.LabelField("Card", EditorStyles.boldLabel);

            card.Id = EditorGUILayout.TextField("Id", card.Id);
            card.Title = EditorGUILayout.TextField("Title", card.Title);

            EditorGUILayout.LabelField("Text (leave empty to generate it from the effects)");
            card.Text = EditorGUILayout.TextArea(card.Text ?? "", GUILayout.Height(54f));

            card.Cost = EditorGUILayout.IntSlider("Cost", card.Cost, 0, 3);
            card.Kind = (CardKind)EditorGUILayout.EnumPopup("Kind", card.Kind);
            card.Pool = (CardPool)EditorGUILayout.EnumPopup("Pool", card.Pool);
            if (card.IsStarter)
                card.Copies = EditorGUILayout.IntSlider("Copies in starter deck", card.Copies, 1, 8);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

            CardEffect[] effects = card.Effects ?? new CardEffect[0];
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] == null) continue;

                EditorGUILayout.BeginHorizontal();
                effects[i].Kind = (EffectKind)EditorGUILayout.EnumPopup(effects[i].Kind, GUILayout.Width(130f));
                effects[i].Value = EditorGUILayout.IntField(effects[i].Value, GUILayout.Width(60f));
                GUILayout.Label(CardData.DescribeEffect(effects[i].Kind, effects[i].Value), EditorStyles.miniLabel);
                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    var list = new List<CardEffect>(effects);
                    list.RemoveAt(i);
                    card.Effects = list.ToArray();
                    GUI.changed = true;
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add effect"))
            {
                var list = new List<CardEffect>(card.Effects ?? new CardEffect[0]);
                list.Add(new CardEffect(EffectKind.Damage, 6));
                card.Effects = list.ToArray();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(card.DisplayText, MessageType.None);
        }

        void DrawEnemy(EnemyData enemy)
        {
            EditorGUILayout.LabelField("Enemy", EditorStyles.boldLabel);

            enemy.Id = EditorGUILayout.TextField("Id", enemy.Id);
            enemy.Title = EditorGUILayout.TextField("Title", enemy.Title);
            enemy.MaxHp = EditorGUILayout.IntField("Max HP", enemy.MaxHp);
            enemy.Role = (EnemyRole)EditorGUILayout.EnumPopup("Role", enemy.Role);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Intent cycle (repeats)", EditorStyles.boldLabel);

            EnemyIntent[] intents = enemy.Intents ?? new EnemyIntent[0];
            for (int i = 0; i < intents.Length; i++)
            {
                if (intents[i] == null) continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Turn " + (i + 1), GUILayout.Width(60f));
                intents[i].Kind = (IntentKind)EditorGUILayout.EnumPopup(intents[i].Kind, GUILayout.Width(110f));
                intents[i].Value = EditorGUILayout.IntField(intents[i].Value, GUILayout.Width(60f));
                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    var list = new List<EnemyIntent>(intents);
                    list.RemoveAt(i);
                    enemy.Intents = list.ToArray();
                    GUI.changed = true;
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add intent"))
            {
                var list = new List<EnemyIntent>(enemy.Intents ?? new EnemyIntent[0]);
                list.Add(new EnemyIntent(IntentKind.Attack, 6));
                enemy.Intents = list.ToArray();
            }
        }

        void DrawRelic(RelicData relic)
        {
            EditorGUILayout.LabelField("Relic", EditorStyles.boldLabel);

            relic.Id = EditorGUILayout.TextField("Id", relic.Id);
            relic.Title = EditorGUILayout.TextField("Title", relic.Title);
            relic.Rarity = (RelicRarity)EditorGUILayout.EnumPopup("Rarity", relic.Rarity);
            relic.Description = EditorGUILayout.TextArea(relic.Description ?? "", GUILayout.Height(90f));

            EditorGUILayout.HelpBox("Relics are catalogued here, but the relic system is not implemented yet: " +
                                    "nothing in gameplay reads this table.", MessageType.Info);
        }

        void DrawEvent(EventData entry)
        {
            EditorGUILayout.LabelField("Event", EditorStyles.boldLabel);

            entry.Id = EditorGUILayout.TextField("Id", entry.Id);
            entry.Title = EditorGUILayout.TextField("Title", entry.Title);
            entry.Text = EditorGUILayout.TextArea(entry.Text ?? "", GUILayout.Height(80f));
            entry.Weight = EditorGUILayout.IntField("Weight", entry.Weight);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

            EventOption[] options = entry.Options ?? new EventOption[0];
            for (int i = 0; i < options.Length; i++)
            {
                EventOption option = options[i];
                if (option == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                option.Label = EditorGUILayout.TextField("Label", option.Label);
                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    var list = new List<EventOption>(options);
                    list.RemoveAt(i);
                    entry.Options = list.ToArray();
                    GUI.changed = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EventEffect[] effects = option.Effects ?? new EventEffect[0];
                for (int e = 0; e < effects.Length; e++)
                {
                    EventEffect effect = effects[e];
                    if (effect == null) continue;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16f);
                    effect.Kind = (EventEffectKind)EditorGUILayout.EnumPopup(effect.Kind, GUILayout.Width(120f));

                    if (effect.Kind == EventEffectKind.AddCard)
                        effect.Target = EditorGUILayout.TextField(effect.Target ?? "");
                    else if (effect.Kind != EventEffectKind.Nothing)
                        effect.Value = EditorGUILayout.IntField(effect.Value, GUILayout.Width(60f));

                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        var list = new List<EventEffect>(effects);
                        list.RemoveAt(e);
                        option.Effects = list.ToArray();
                        GUI.changed = true;
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("+ Add effect"))
                {
                    var list = new List<EventEffect>(option.Effects ?? new EventEffect[0]);
                    list.Add(new EventEffect(EventEffectKind.GainGold, 10));
                    option.Effects = list.ToArray();
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add option"))
            {
                var list = new List<EventOption>(entry.Options ?? new EventOption[0]);
                list.Add(new EventOption
                {
                    Label = "Option",
                    Effects = new[] { new EventEffect(EventEffectKind.Nothing, 0) }
                });
                entry.Options = list.ToArray();
            }

            EditorGUILayout.HelpBox("Events are catalogued here, but the event screen is still a placeholder: " +
                                    "nothing in gameplay reads this table.", MessageType.Info);
        }
    }
}
