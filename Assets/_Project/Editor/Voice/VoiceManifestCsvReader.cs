using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace _Project.Editor.Voice
{
    internal static class VoiceManifestCsvReader
    {
        private const int ExpectedColumnCount = 13;

        public static IReadOnlyList<VoiceManifestRecord> Read(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Voice manifest was not found.", path);
            }

            List<List<string>> rows = Parse(File.ReadAllText(path, Encoding.UTF8));
            if (rows.Count == 0 || string.Join(",", rows[0]) != VoiceManifestCsvWriter.Header)
            {
                throw new InvalidDataException("Voice manifest header does not match the expected schema.");
            }

            var records = new List<VoiceManifestRecord>(rows.Count - 1);
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                List<string> row = rows[rowIndex];
                if (row.Count == 1 && string.IsNullOrEmpty(row[0]))
                {
                    continue;
                }

                if (row.Count != ExpectedColumnCount)
                {
                    throw new InvalidDataException(
                        $"Voice manifest row {rowIndex + 1} has {row.Count} columns; expected {ExpectedColumnCount}.");
                }

                if (!int.TryParse(row[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int lineIndex))
                {
                    throw new InvalidDataException($"Voice manifest row {rowIndex + 1} has an invalid LineIndex.");
                }

                records.Add(new VoiceManifestRecord
                {
                    VoiceId = row[0],
                    SourceType = row[1],
                    SourceId = row[2],
                    LineIndex = lineIndex,
                    Speaker = row[4],
                    Emotion = row[5],
                    PsychologyPhase = row[6],
                    Tag = row[7],
                    Text = row[8],
                    OutputPath = row[9],
                    Status = row[10],
                    DuplicateOf = row[11],
                    Notes = row[12]
                });
            }

            return records;
        }

        private static List<List<string>> Parse(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;

            for (int index = 0; index < csv.Length; index++)
            {
                char character = csv[index];
                if (quoted)
                {
                    if (character == '"' && index + 1 < csv.Length && csv[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else if (character == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        field.Append(character);
                    }

                    continue;
                }

                if (character == '"' && field.Length == 0)
                {
                    quoted = true;
                }
                else if (character == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (character == '\r' || character == '\n')
                {
                    if (character == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n')
                    {
                        index++;
                    }

                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(character);
                }
            }

            if (quoted)
            {
                throw new InvalidDataException("Voice manifest contains an unterminated quoted field.");
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            if (rows.Count > 0 && rows[0].Count > 0)
            {
                rows[0][0] = rows[0][0].TrimStart('\uFEFF');
            }

            return rows;
        }
    }
}
