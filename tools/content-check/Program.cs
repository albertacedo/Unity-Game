using System;
using System.Collections.Generic;
using System.IO;
using Prototype.Content;
using Prototype.Data;

namespace Prototype.Tools
{
    /// <summary>
    /// Reads, validates and round-trips the content CSV files. Exit code 1 when there
    /// is at least one hard error; warnings alone do not fail the check.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            bool write = false;
            string folder = FindContentFolder();

            foreach (string arg in args)
            {
                if (arg == "--write") write = true;
                else if (!arg.StartsWith("-")) folder = arg;
            }

            if (folder == null || !Directory.Exists(folder))
            {
                Console.Error.WriteLine("content-check: cannot find Assets/Resources/Content. Pass the folder as an argument.");
                return 2;
            }

            var problems = new List<string>();

            List<CardData> cards = ReadCards(folder, problems, out char cardDelimiter);
            List<EnemyData> enemies = ReadEnemies(folder, problems, out char enemyDelimiter);
            List<RelicData> relics = ReadRelics(folder, problems, out char relicDelimiter);
            List<EventData> events = ReadEvents(folder, problems, out char eventDelimiter);

            ContentCsv.ValidateReferences(cards, events, problems);

            // Round-trip: writing the parsed data and reading it again must produce the
            // same file, otherwise the editor window would silently rewrite a user file.
            CheckRoundTrip(cards, enemies, relics, events, problems);

            Console.WriteLine("cards.csv    " + cards.Count + " cards");
            Console.WriteLine("enemies.csv  " + enemies.Count + " enemies");
            Console.WriteLine("relics.csv   " + relics.Count + " relics");
            Console.WriteLine("events.csv   " + events.Count + " events");
            Console.WriteLine();

            int errors = 0;
            foreach (string problem in problems)
            {
                bool warning = problem.StartsWith("warning:", StringComparison.Ordinal);
                if (!warning) errors++;
                Console.WriteLine((warning ? "  WARN  " : "  ERROR ") + problem);
            }

            if (write && errors == 0)
            {
                File.WriteAllText(Path.Combine(folder, "cards.csv"), ContentCsv.WriteCards(cards, cardDelimiter));
                File.WriteAllText(Path.Combine(folder, "enemies.csv"), ContentCsv.WriteEnemies(enemies, enemyDelimiter));
                File.WriteAllText(Path.Combine(folder, "relics.csv"), ContentCsv.WriteRelics(relics, relicDelimiter));
                File.WriteAllText(Path.Combine(folder, "events.csv"), ContentCsv.WriteEvents(events, eventDelimiter));
                Console.WriteLine("Files rewritten in normalised form.");
            }

            Console.WriteLine(errors == 0
                ? "Content OK."
                : errors + " error(s) found.");

            return errors == 0 ? 0 : 1;
        }

        static List<CardData> ReadCards(string folder, List<string> problems, out char delimiter)
        {
            return ContentCsv.ReadCards(Read(folder, "cards.csv", problems), "cards.csv", problems, out delimiter);
        }

        static List<EnemyData> ReadEnemies(string folder, List<string> problems, out char delimiter)
        {
            return ContentCsv.ReadEnemies(Read(folder, "enemies.csv", problems), "enemies.csv", problems, out delimiter);
        }

        static List<RelicData> ReadRelics(string folder, List<string> problems, out char delimiter)
        {
            return ContentCsv.ReadRelics(Read(folder, "relics.csv", problems), "relics.csv", problems, out delimiter);
        }

        static List<EventData> ReadEvents(string folder, List<string> problems, out char delimiter)
        {
            return ContentCsv.ReadEvents(Read(folder, "events.csv", problems), "events.csv", problems, out delimiter);
        }

        static string Read(string folder, string file, List<string> problems)
        {
            string path = Path.Combine(folder, file);
            if (!File.Exists(path))
            {
                problems.Add(file + ": file not found");
                return "";
            }

            string text = File.ReadAllText(path);
            if (text.Trim().Length == 0)
            {
                problems.Add(file + ": file is empty");
                return "";
            }

            return text;
        }

        static void CheckRoundTrip(List<CardData> cards, List<EnemyData> enemies,
            List<RelicData> relics, List<EventData> events, List<string> problems)
        {
            var again = new List<string>();

            List<CardData> cards2 = ContentCsv.ReadCards(ContentCsv.WriteCards(cards), "cards.csv", again, out _);
            List<EnemyData> enemies2 = ContentCsv.ReadEnemies(ContentCsv.WriteEnemies(enemies), "enemies.csv", again, out _);
            List<RelicData> relics2 = ContentCsv.ReadRelics(ContentCsv.WriteRelics(relics), "relics.csv", again, out _);
            List<EventData> events2 = ContentCsv.ReadEvents(ContentCsv.WriteEvents(events), "events.csv", again, out _);

            if (ContentCsv.WriteCards(cards2) != ContentCsv.WriteCards(cards))
                problems.Add("cards.csv: writing and reading the table does not round-trip");
            if (ContentCsv.WriteEnemies(enemies2) != ContentCsv.WriteEnemies(enemies))
                problems.Add("enemies.csv: writing and reading the table does not round-trip");
            if (ContentCsv.WriteRelics(relics2) != ContentCsv.WriteRelics(relics))
                problems.Add("relics.csv: writing and reading the table does not round-trip");
            if (ContentCsv.WriteEvents(events2) != ContentCsv.WriteEvents(events))
                problems.Add("events.csv: writing and reading the table does not round-trip");
        }

        /// <summary>Walks up from the working directory looking for Assets/Resources/Content.</summary>
        static string FindContentFolder()
        {
            string current = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8 && current != null; i++)
            {
                string candidate = Path.Combine(current, "Assets", "Resources", "Content");
                if (Directory.Exists(candidate)) return candidate;

                DirectoryInfo parent = Directory.GetParent(current);
                current = parent?.FullName;
            }
            return null;
        }
    }
}
