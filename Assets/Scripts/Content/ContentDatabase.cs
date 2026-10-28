using System.Collections.Generic;
using Prototype.Data;
using UnityEngine;

namespace Prototype.Content
{
    /// <summary>
    /// Loads the CSV tables in Assets/Resources/Content and keeps them in memory.
    /// Gameplay reads content from here; nothing is loaded from .asset files.
    /// </summary>
    public static class ContentDatabase
    {
        public const string Folder = "Content/";
        public const string CardsFile = "cards";
        public const string EnemiesFile = "enemies";
        public const string RelicsFile = "relics";
        public const string EventsFile = "events";

        static readonly List<string> problems = new List<string>();
        static readonly List<CardData> cards = new List<CardData>();
        static readonly List<EnemyData> enemies = new List<EnemyData>();
        static readonly List<RelicData> relics = new List<RelicData>();
        static readonly List<EventData> events = new List<EventData>();

        static bool loaded;

        /// <summary>Every problem found while loading, in "file: row: message" form.</summary>
        public static IList<string> Problems { get { EnsureLoaded(); return problems; } }

        public static IList<CardData> Cards { get { EnsureLoaded(); return cards; } }
        public static IList<EnemyData> Enemies { get { EnsureLoaded(); return enemies; } }
        public static IList<RelicData> Relics { get { EnsureLoaded(); return relics; } }
        public static IList<EventData> Events { get { EnsureLoaded(); return events; } }

        /// <summary>Drops the cache so the next access re-reads the CSV files.</summary>
        public static void Reload()
        {
            loaded = false;
            problems.Clear();
            cards.Clear();
            enemies.Clear();
            relics.Clear();
            events.Clear();
        }

        static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            string cardText = ReadText(CardsFile);
            if (cardText != null)
                cards.AddRange(ContentCsv.ReadCards(cardText, CardsFile + ".csv", problems, out _));

            string enemyText = ReadText(EnemiesFile);
            if (enemyText != null)
                enemies.AddRange(ContentCsv.ReadEnemies(enemyText, EnemiesFile + ".csv", problems, out _));

            string relicText = ReadText(RelicsFile);
            if (relicText != null)
                relics.AddRange(ContentCsv.ReadRelics(relicText, RelicsFile + ".csv", problems, out _));

            string eventText = ReadText(EventsFile);
            if (eventText != null)
                events.AddRange(ContentCsv.ReadEvents(eventText, EventsFile + ".csv", problems, out _));

            ContentCsv.ValidateReferences(cards, events, problems);

            if (problems.Count > 0)
                Debug.LogError("Content: " + problems.Count + " problem(s) in Resources/" + Folder + "\n" +
                               string.Join("\n", problems));
        }

        static string ReadText(string file)
        {
            var asset = Resources.Load<TextAsset>(Folder + file);
            if (asset != null) return asset.text;

            problems.Add(file + ".csv: not found at Assets/Resources/" + Folder + file + ".csv");
            return null;
        }
    }
}
