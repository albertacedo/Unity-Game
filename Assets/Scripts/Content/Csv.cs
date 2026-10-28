using System;
using System.Collections.Generic;
using System.Text;

namespace Prototype.Content
{
    /// <summary>
    /// Small RFC-4180 CSV reader/writer. Deliberately dependency free: it runs both in
    /// the game and in the editor window, and the delimiter is detected because Excel
    /// writes ';' on Catalan/Spanish locales and ',' on English ones.
    /// </summary>
    public static class Csv
    {
        /// <summary>Most common delimiter on the first line, outside quotes.</summary>
        public static char DetectDelimiter(string text)
        {
            if (string.IsNullOrEmpty(text)) return ',';

            int commas = 0, semicolons = 0, tabs = 0;
            bool quoted = false;

            foreach (char c in text)
            {
                if (c == '"') { quoted = !quoted; continue; }
                if (quoted) continue;

                if (c == ',') commas++;
                else if (c == ';') semicolons++;
                else if (c == '\t') tabs++;
                else if (c == '\n') break;
            }

            if (tabs > commas && tabs > semicolons) return '\t';
            return semicolons > commas ? ';' : ',';
        }

        /// <summary>Parses the whole text into rows. Blank trailing rows are dropped.</summary>
        public static List<string[]> Parse(string text, char delimiter)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(text)) return rows;

            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;

            int start = text.Length > 0 && text[0] == '\uFEFF' ? 1 : 0;

            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];

                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                    continue;
                }

                if (c == '"' && field.Length == 0)
                {
                    quoted = true;
                }
                else if (c == delimiter)
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\n')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    AddRow(rows, row);
                }
                else if (c != '\r')
                {
                    field.Append(c);
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                AddRow(rows, row);
            }

            return rows;
        }

        /// <summary>Writes rows back to CSV with '\n' endings so git diffs stay small.</summary>
        public static string Write(IList<string[]> rows, char delimiter)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rows.Count; i++)
            {
                string[] row = rows[i];
                for (int c = 0; c < row.Length; c++)
                {
                    if (c > 0) sb.Append(delimiter);
                    sb.Append(Escape(row[c], delimiter));
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static void AddRow(List<string[]> rows, List<string> row)
        {
            bool empty = true;
            foreach (string value in row)
            {
                if (!string.IsNullOrEmpty(value)) { empty = false; break; }
            }

            if (!empty) rows.Add(row.ToArray());
            row.Clear();
        }

        static string Escape(string value, char delimiter)
        {
            value = value ?? "";
            bool needsQuotes = value.IndexOf(delimiter) >= 0
                || value.IndexOf('"') >= 0
                || value.IndexOf('\n') >= 0
                || value.IndexOf('\r') >= 0;

            return needsQuotes ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
