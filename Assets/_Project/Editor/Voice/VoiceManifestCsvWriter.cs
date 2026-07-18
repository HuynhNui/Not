using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace _Project.Editor.Voice
{
    internal static class VoiceManifestCsvWriter
    {
        internal const string Header =
            "VoiceId,SourceType,SourceId,LineIndex,Speaker,Emotion,PsychologyPhase,Tag,Text,OutputPath,Status,DuplicateOf,Notes";

        private const string LineEnding = "\r\n";

        public static string Serialize(IReadOnlyList<VoiceManifestRecord> records)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            var csv = new StringBuilder();
            csv.Append(Header).Append(LineEnding);
            for (int index = 0; index < records.Count; index++)
            {
                VoiceManifestRecord record = records[index]
                    ?? throw new InvalidOperationException($"Record at index {index} is null.");

                AppendField(csv, record.VoiceId);
                AppendField(csv, record.SourceType);
                AppendField(csv, record.SourceId);
                AppendField(csv, record.LineIndex.ToString(CultureInfo.InvariantCulture));
                AppendField(csv, record.Speaker);
                AppendField(csv, record.Emotion);
                AppendField(csv, record.PsychologyPhase);
                AppendField(csv, record.Tag);
                AppendField(csv, NormalizeLineEndings(record.Text));
                AppendField(csv, record.OutputPath);
                AppendField(csv, record.Status);
                AppendField(csv, record.DuplicateOf);
                AppendLastField(csv, record.Notes);
                csv.Append(LineEnding);
            }

            return csv.ToString();
        }

        public static void Write(string path, IReadOnlyList<VoiceManifestRecord> records)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = path + ".tmp";
            try
            {
                using (var writer = new StreamWriter(
                    temporaryPath,
                    append: false,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
                {
                    writer.NewLine = LineEnding;
                    writer.Write(Serialize(records));
                }

                File.Copy(temporaryPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static void AppendField(StringBuilder csv, string value)
        {
            csv.Append(Escape(value)).Append(',');
        }

        private static void AppendLastField(StringBuilder csv, string value)
        {
            csv.Append(Escape(value));
        }

        private static string Escape(string value)
        {
            string safe = value ?? string.Empty;
            bool needsQuotes = safe.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!needsQuotes)
            {
                return safe;
            }

            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }

        private static string NormalizeLineEndings(string value)
        {
            return value?.Replace("\r\n", "\n").Replace('\r', '\n') ?? string.Empty;
        }
    }
}
