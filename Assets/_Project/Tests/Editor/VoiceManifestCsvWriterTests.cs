using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using _Project.Editor.Voice;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class VoiceManifestCsvWriterTests
    {
        [Test]
        public void Write_RoundTripsCommaQuotesNewlineEllipsisAndUnicode()
        {
            string path = Path.Combine(Path.GetTempPath(), $"voice-manifest-{Guid.NewGuid():N}.csv");
            const string text = "Comma, quote \"\"\" and newline\r\nUnicode Việt Nam …";
            var record = CreateRecord(text);

            try
            {
                VoiceManifestCsvWriter.Write(path, new[] { record });
                byte[] bytes = File.ReadAllBytes(path);
                Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));

                string csv = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
                List<List<string>> rows = ParseCsv(csv);

                Assert.That(rows, Has.Count.EqualTo(2));
                Assert.That(rows[0], Has.Count.EqualTo(13));
                Assert.That(rows[1], Has.Count.EqualTo(13));
                Assert.That(rows[1][8], Is.EqualTo(text.Replace("\r\n", "\n")));
                Assert.That(rows[1][12], Is.EqualTo("NOTE_A;NOTE_B"));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void Serialize_IsDeterministicAndContainsNoTimestamp()
        {
            string first = VoiceManifestCsvWriter.Serialize(VoiceManifestCollector.CollectAll());
            string second = VoiceManifestCsvWriter.Serialize(VoiceManifestCollector.CollectAll());

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.StartWith(VoiceManifestCsvWriter.Header + "\r\n"));
            Assert.That(first, Does.Not.Contain("Exported:"));
        }

        private static VoiceManifestRecord CreateRecord(string text)
        {
            return new VoiceManifestRecord
            {
                VoiceId = "VO_TEST_001",
                SourceType = "STORY",
                SourceId = "TEST",
                LineIndex = 1,
                Speaker = "UNIT-07",
                Emotion = "calm",
                PsychologyPhase = string.Empty,
                Tag = string.Empty,
                Text = text,
                OutputPath = "Assets/_Project/Audio/Voice/Unit07/VO_TEST_001.wav",
                Status = "PENDING",
                DuplicateOf = string.Empty,
                Notes = "NOTE_A;NOTE_B"
            };
        }

        private static List<List<string>> ParseCsv(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int index = 0; index < csv.Length; index++)
            {
                char character = csv[index];
                if (inQuotes)
                {
                    if (character == '"')
                    {
                        if (index + 1 < csv.Length && csv[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(character);
                    }

                    continue;
                }

                if (character == '"' && field.Length == 0)
                {
                    inQuotes = true;
                }
                else if (character == ',')
                {
                    row.Add(field.ToString());
                    field.Length = 0;
                }
                else if (character == '\r' || character == '\n')
                {
                    if (character == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n')
                    {
                        index++;
                    }

                    row.Add(field.ToString());
                    field.Length = 0;
                    rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(character);
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
