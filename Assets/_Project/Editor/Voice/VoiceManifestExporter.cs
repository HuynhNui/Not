using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Voice
{
    internal static class VoiceManifestExporter
    {
        internal const string ManifestPath = "Assets/_Project/Data/Voice/VoiceManifest.csv";
        internal const string ReportPath = "Docs/Audio/VoiceManifestReport.md";

        [MenuItem("Tools/True Gate/Voice/Export Voice Manifest")]
        private static void ExportVoiceManifest()
        {
            IReadOnlyList<VoiceManifestRecord> records = CollectSafely(out VoiceManifestValidationResult validation);
            VoiceManifestReportWriter.Write(ReportPath, records, validation, DateTimeOffset.UtcNow);

            if (!validation.IsValid)
            {
                AssetDatabase.Refresh();
                LogSummary("Voice manifest export failed; the existing manifest was not overwritten.", records, validation);
                return;
            }

            VoiceManifestCsvWriter.Write(ManifestPath, records);
            AssetDatabase.Refresh();
            LogSummary($"Voice manifest exported to {ManifestPath}.", records, validation);
        }

        [MenuItem("Tools/True Gate/Voice/Validate Voice Manifest")]
        private static void ValidateVoiceManifest()
        {
            IReadOnlyList<VoiceManifestRecord> records = CollectSafely(out VoiceManifestValidationResult validation);
            LogSummary("Voice manifest validation complete. No files were written.", records, validation);
        }

        internal static IReadOnlyList<VoiceManifestRecord> CollectSafely(
            out VoiceManifestValidationResult validation)
        {
            try
            {
                IReadOnlyList<VoiceManifestRecord> records = VoiceManifestCollector.CollectAll();
                validation = VoiceManifestValidation.Validate(records);
                return records;
            }
            catch (Exception exception)
            {
                validation = new VoiceManifestValidationResult();
                validation.AddError($"Collection failed: {exception.GetType().Name}: {exception.Message}");
                return Array.Empty<VoiceManifestRecord>();
            }
        }

        private static void LogSummary(
            string heading,
            IReadOnlyList<VoiceManifestRecord> records,
            VoiceManifestValidationResult validation)
        {
            int story = CountSource(records, VoiceManifestCollector.StorySourceType);
            int tutorial = CountSource(records, VoiceManifestCollector.TutorialSourceType);
            int gameplay = CountSource(records, VoiceManifestCollector.GameplaySourceType);
            int skipped = records.Count(record => string.Equals(record.Status, "SKIP", StringComparison.Ordinal));
            int duplicates = records.Count(record => !string.IsNullOrEmpty(record.DuplicateOf));

            string summary =
                $"{heading}\n"
                + $"Total records: {records.Count}\n"
                + $"Story records: {story}\n"
                + $"Gameplay records: {gameplay}\n"
                + $"Tutorial records: {tutorial}\n"
                + $"Skipped records: {skipped}\n"
                + $"Duplicate candidates: {duplicates}\n"
                + $"Warnings: {validation.Warnings.Count}\n"
                + $"Errors: {validation.Errors.Count}";

            if (validation.IsValid)
            {
                Debug.Log(summary);
            }
            else
            {
                Debug.LogError(summary + "\n" + string.Join("\n", validation.Errors));
            }

            foreach (string warning in validation.Warnings)
            {
                Debug.LogWarning(warning);
            }
        }

        private static int CountSource(IReadOnlyList<VoiceManifestRecord> records, string sourceType)
        {
            return records.Count(record => string.Equals(record.SourceType, sourceType, StringComparison.Ordinal));
        }
    }
}
