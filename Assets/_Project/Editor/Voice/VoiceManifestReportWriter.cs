using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace _Project.Editor.Voice
{
    internal static class VoiceManifestReportWriter
    {
        public static void Write(
            string path,
            IReadOnlyList<VoiceManifestRecord> records,
            VoiceManifestValidationResult validation,
            DateTimeOffset exportedAt)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                path,
                Build(records, validation, exportedAt),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        internal static string Build(
            IReadOnlyList<VoiceManifestRecord> records,
            VoiceManifestValidationResult validation,
            DateTimeOffset exportedAt)
        {
            records = records ?? Array.Empty<VoiceManifestRecord>();
            validation = validation ?? new VoiceManifestValidationResult();

            var report = new StringBuilder();
            report.AppendLine("# Voice Manifest Report");
            report.AppendLine();
            report.Append("Exported: ")
                .Append(exportedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture))
                .AppendLine();
            report.Append("Validation: **")
                .Append(validation.IsValid ? "PASS" : "FAIL")
                .AppendLine("**");
            report.Append("Total records: **").Append(records.Count).AppendLine("**");
            report.Append("Skipped records: **")
                .Append(records.Count(record => string.Equals(record.Status, "SKIP", StringComparison.Ordinal)))
                .AppendLine("**");
            report.Append("Duplicate candidates: **")
                .Append(records.Count(record => !string.IsNullOrEmpty(record.DuplicateOf)))
                .AppendLine("**");
            report.AppendLine();

            AppendCountSection(report, "Records By Source Type", records, record => record.SourceType);
            AppendCountSection(report, "Records By Speaker", records, record => record.Speaker);
            AppendCountSection(report, "Records By Emotion", records, record => record.Emotion);

            AppendListSection(
                report,
                "Unmapped Gameplay Emotions",
                records
                    .Where(record => ContainsNote(record, "UNMAPPED_EMOTION"))
                    .Select(record =>
                        $"{record.VoiceId}: {record.PsychologyPhase}/{record.Tag}"));

            AppendDuplicateSection(report, "Duplicate Voice IDs", records, record => record.VoiceId);
            AppendDuplicateSection(report, "Duplicate Output Paths", records, record => record.OutputPath);

            AppendListSection(
                report,
                "Duplicate Candidates",
                records
                    .Where(record => !string.IsNullOrEmpty(record.DuplicateOf))
                    .Select(record => $"{record.VoiceId} -> {record.DuplicateOf}"));

            AppendListSection(
                report,
                "Missing Required Data",
                records
                    .Where(record => string.IsNullOrWhiteSpace(record.SourceId)
                        || string.IsNullOrWhiteSpace(record.Speaker)
                        || record.Text == null
                        || string.IsNullOrWhiteSpace(record.Text))
                    .Select(record =>
                        $"{record.VoiceId}: source='{record.SourceId}', speaker='{record.Speaker}', text="
                        + (record.Text == null ? "null" : "present")));

            AppendListSection(report, "Validation Errors", validation.Errors);
            AppendListSection(report, "Validation Warnings", validation.Warnings);

            report.AppendLine("## Validation Summary");
            report.AppendLine();
            report.Append("- Result: ").AppendLine(validation.IsValid ? "PASS" : "FAIL");
            report.Append("- Errors: ").AppendLine(validation.Errors.Count.ToString(CultureInfo.InvariantCulture));
            report.Append("- Warnings: ").AppendLine(validation.Warnings.Count.ToString(CultureInfo.InvariantCulture));
            return report.ToString();
        }

        private static void AppendCountSection(
            StringBuilder report,
            string title,
            IReadOnlyList<VoiceManifestRecord> records,
            Func<VoiceManifestRecord, string> selector)
        {
            report.Append("## ").AppendLine(title);
            report.AppendLine();
            report.AppendLine("| Value | Count |");
            report.AppendLine("| --- | ---: |");

            IEnumerable<IGrouping<string, VoiceManifestRecord>> groups = records
                .GroupBy(record => selector(record) ?? string.Empty, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal);
            foreach (IGrouping<string, VoiceManifestRecord> group in groups)
            {
                string key = string.IsNullOrEmpty(group.Key) ? "(empty)" : group.Key;
                report.Append("| ").Append(EscapeMarkdown(key)).Append(" | ")
                    .Append(group.Count().ToString(CultureInfo.InvariantCulture)).AppendLine(" |");
            }

            report.AppendLine();
        }

        private static void AppendDuplicateSection(
            StringBuilder report,
            string title,
            IReadOnlyList<VoiceManifestRecord> records,
            Func<VoiceManifestRecord, string> selector)
        {
            IEnumerable<string> duplicateLines = records
                .Where(record => !string.IsNullOrWhiteSpace(selector(record)))
                .GroupBy(selector, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group =>
                    $"{group.Key}: " + string.Join(", ", group.Select(record =>
                        $"{record.SourceType}/{record.SourceId}/{record.LineIndex}")));
            AppendListSection(report, title, duplicateLines);
        }

        private static void AppendListSection(
            StringBuilder report,
            string title,
            IEnumerable<string> items)
        {
            report.Append("## ").AppendLine(title);
            report.AppendLine();
            string[] values = items?.Where(item => !string.IsNullOrEmpty(item)).ToArray()
                ?? Array.Empty<string>();
            if (values.Length == 0)
            {
                report.AppendLine("None.");
            }
            else
            {
                foreach (string value in values)
                {
                    report.Append("- ").AppendLine(value.Replace("\r", " ").Replace("\n", " "));
                }
            }

            report.AppendLine();
        }

        private static bool ContainsNote(VoiceManifestRecord record, string note)
        {
            return (record.Notes ?? string.Empty)
                .Split(';')
                .Any(value => string.Equals(value, note, StringComparison.Ordinal));
        }

        private static string EscapeMarkdown(string value)
        {
            return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
