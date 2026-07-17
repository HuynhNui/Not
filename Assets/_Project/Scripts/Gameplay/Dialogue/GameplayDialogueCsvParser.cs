using System;
using System.Collections.Generic;
using System.Text;

namespace _Project.Scripts.Gameplay.Dialogue
{
    public static class GameplayDialogueCsvParser
    {
        private static readonly string[] ExpectedHeaders =
        {
            "DialogueId",
            "PsychologyPhase",
            "Tag",
            "Text"
        };

        public static List<GameplayDialogueEntry> Parse(string csvText, string sourceName = "GameplayDialogueContent")
        {
            if (string.IsNullOrEmpty(csvText))
            {
                throw new FormatException($"{sourceName}: CSV content is empty.");
            }

            List<CsvRow> rows = ReadRows(csvText);
            if (rows.Count == 0)
            {
                throw new FormatException($"{sourceName}: CSV content has no rows.");
            }

            Dictionary<string, int> headerIndexes = BuildHeaderIndex(rows[0], sourceName);
            var entries = new List<GameplayDialogueEntry>(rows.Count - 1);
            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                CsvRow row = rows[rowIndex];
                if (IsBlankRow(row))
                {
                    continue;
                }

                string dialogueId = GetRequired(row, headerIndexes, "DialogueId", sourceName);
                string phaseValue = GetRequired(row, headerIndexes, "PsychologyPhase", sourceName);
                string tag = GetRequired(row, headerIndexes, "Tag", sourceName);
                string text = GetRequired(row, headerIndexes, "Text", sourceName);

                if (!ids.Add(dialogueId))
                {
                    throw new FormatException($"{sourceName}: duplicate DialogueId '{dialogueId}' at line {row.LineNumber}.");
                }

                if (!TryParsePhase(phaseValue, out PsychologyPhase phase))
                {
                    throw new FormatException($"{sourceName}: invalid PsychologyPhase '{phaseValue}' at line {row.LineNumber}.");
                }

                entries.Add(new GameplayDialogueEntry(dialogueId, phase, tag, text));
            }

            return entries;
        }

        private static Dictionary<string, int> BuildHeaderIndex(CsvRow headerRow, string sourceName)
        {
            var headerIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < headerRow.Fields.Count; index++)
            {
                string header = index == 0
                    ? StripUtf8Bom(headerRow.Fields[index]).Trim()
                    : headerRow.Fields[index].Trim();

                if (!string.IsNullOrEmpty(header))
                {
                    headerIndexes[header] = index;
                }
            }

            for (int index = 0; index < ExpectedHeaders.Length; index++)
            {
                if (!headerIndexes.ContainsKey(ExpectedHeaders[index]))
                {
                    throw new FormatException($"{sourceName}: missing required header '{ExpectedHeaders[index]}'.");
                }
            }

            return headerIndexes;
        }

        private static string GetRequired(
            CsvRow row,
            Dictionary<string, int> headerIndexes,
            string fieldName,
            string sourceName)
        {
            int index = headerIndexes[fieldName];
            if (index >= row.Fields.Count)
            {
                throw new FormatException($"{sourceName}: missing field '{fieldName}' at line {row.LineNumber}.");
            }

            string value = row.Fields[index];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException($"{sourceName}: empty field '{fieldName}' at line {row.LineNumber}.");
            }

            return value.Trim();
        }

        private static bool TryParsePhase(string value, out PsychologyPhase phase)
        {
            switch (value.Trim().ToUpperInvariant())
            {
                case "PROTOCOL":
                    phase = PsychologyPhase.Protocol;
                    return true;
                case "DOUBT":
                    phase = PsychologyPhase.Doubt;
                    return true;
                case "AWAKENING":
                    phase = PsychologyPhase.Awakening;
                    return true;
                default:
                    phase = PsychologyPhase.Protocol;
                    return false;
            }
        }

        private static List<CsvRow> ReadRows(string csvText)
        {
            var rows = new List<CsvRow>();
            var currentFields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;
            bool fieldWasQuoted = false;
            int lineNumber = 1;
            int rowLineNumber = 1;

            for (int index = 0; index < csvText.Length; index++)
            {
                char c = csvText[index];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        bool escapedQuote = index + 1 < csvText.Length && csvText[index + 1] == '"';
                        if (escapedQuote)
                        {
                            currentField.Append('"');
                            index++;
                            continue;
                        }

                        inQuotes = false;
                        continue;
                    }

                    if (c == '\n')
                    {
                        lineNumber++;
                    }

                    currentField.Append(c);
                    continue;
                }

                if (c == '"' && currentField.Length == 0 && !fieldWasQuoted)
                {
                    inQuotes = true;
                    fieldWasQuoted = true;
                    continue;
                }

                if (c == ',')
                {
                    AddField(currentFields, currentField, fieldWasQuoted);
                    fieldWasQuoted = false;
                    continue;
                }

                if (c == '\r' || c == '\n')
                {
                    AddField(currentFields, currentField, fieldWasQuoted);
                    rows.Add(new CsvRow(rowLineNumber, currentFields));
                    currentFields = new List<string>();
                    fieldWasQuoted = false;

                    if (c == '\r' && index + 1 < csvText.Length && csvText[index + 1] == '\n')
                    {
                        index++;
                    }

                    lineNumber++;
                    rowLineNumber = lineNumber;
                    continue;
                }

                currentField.Append(c);
            }

            if (inQuotes)
            {
                throw new FormatException($"CSV has an unterminated quoted field at line {rowLineNumber}.");
            }

            if (currentField.Length > 0 || currentFields.Count > 0 || fieldWasQuoted)
            {
                AddField(currentFields, currentField, fieldWasQuoted);
                rows.Add(new CsvRow(rowLineNumber, currentFields));
            }

            return rows;
        }

        private static void AddField(List<string> fields, StringBuilder currentField, bool wasQuoted)
        {
            string value = currentField.ToString();
            fields.Add(wasQuoted ? value : value.Trim());
            currentField.Length = 0;
        }

        private static bool IsBlankRow(CsvRow row)
        {
            if (row.Fields.Count == 0)
            {
                return true;
            }

            for (int index = 0; index < row.Fields.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(row.Fields[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string StripUtf8Bom(string value)
        {
            return !string.IsNullOrEmpty(value) && value[0] == '\uFEFF'
                ? value.Substring(1)
                : value;
        }

        private readonly struct CsvRow
        {
            public CsvRow(int lineNumber, List<string> fields)
            {
                LineNumber = lineNumber;
                Fields = fields;
            }

            public int LineNumber { get; }
            public List<string> Fields { get; }
        }
    }
}
