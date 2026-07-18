using System;
using System.Collections.Generic;
using System.Linq;

namespace _Project.Editor.Voice
{
    internal sealed class VoiceManifestValidationResult
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool IsValid => _errors.Count == 0;

        public void AddError(string message)
        {
            _errors.Add(message);
        }

        public void AddWarning(string message)
        {
            _warnings.Add(message);
        }
    }

    internal static class VoiceManifestValidation
    {
        public static VoiceManifestValidationResult Validate(IReadOnlyList<VoiceManifestRecord> records)
        {
            var result = new VoiceManifestValidationResult();
            if (records == null)
            {
                result.AddError("Record collection is null.");
                return result;
            }

            for (int index = 0; index < records.Count; index++)
            {
                ValidateRecord(records[index], index, result);
            }

            AddDuplicateErrors(records, result, record => record.VoiceId, "Voice ID");
            AddDuplicateErrors(records, result, record => record.OutputPath, "output path");
            return result;
        }

        private static void ValidateRecord(
            VoiceManifestRecord record,
            int index,
            VoiceManifestValidationResult result)
        {
            if (record == null)
            {
                result.AddError($"Record at index {index} is null.");
                return;
            }

            string label = string.IsNullOrEmpty(record.VoiceId) ? $"record[{index}]" : record.VoiceId;
            if (string.IsNullOrWhiteSpace(record.SourceId))
            {
                result.AddError($"{label}: source ID is missing.");
            }

            if (record.Text == null)
            {
                result.AddError($"{label}: text is null.");
            }

            bool indexedSource = string.Equals(record.SourceType, VoiceManifestCollector.StorySourceType, StringComparison.Ordinal)
                || string.Equals(record.SourceType, VoiceManifestCollector.TutorialSourceType, StringComparison.Ordinal);
            if (indexedSource && record.LineIndex < 0)
            {
                result.AddError($"{label}: line index cannot be negative.");
            }

            if (string.Equals(record.SourceType, VoiceManifestCollector.GameplaySourceType, StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(record.SourceId))
            {
                result.AddError($"{label}: gameplay DialogueId is missing.");
            }

            if (string.IsNullOrWhiteSpace(record.VoiceId))
            {
                result.AddError($"{label}: Voice ID is missing.");
            }

            if (string.IsNullOrWhiteSpace(record.OutputPath))
            {
                result.AddError($"{label}: output path is missing.");
            }

            if (string.IsNullOrWhiteSpace(record.Speaker))
            {
                result.AddWarning($"{label}: speaker is empty.");
            }
            else if (!VoiceManifestCollector.HasOfficialSpeakerFolder(record.Speaker))
            {
                result.AddWarning($"{label}: speaker '{record.Speaker}' uses a generated folder mapping.");
            }

            if (string.IsNullOrWhiteSpace(record.Emotion))
            {
                result.AddWarning($"{label}: emotion is empty.");
            }

            if (record.Text != null && string.IsNullOrWhiteSpace(record.Text))
            {
                result.AddWarning($"{label}: text is empty after trimming.");
            }

            if (ContainsNote(record, "UNMAPPED_EMOTION"))
            {
                result.AddWarning(
                    $"{label}: unmapped gameplay emotion for {record.PsychologyPhase}/{record.Tag}.");
            }

            if (!string.IsNullOrEmpty(record.DuplicateOf))
            {
                result.AddWarning($"{label}: duplicate candidate of {record.DuplicateOf}.");
            }

            if (IsUnsupportedPunctuationOnly(record.Text))
            {
                result.AddWarning($"{label}: text contains punctuation only and is not an ellipsis skip line.");
            }

            if (!string.Equals(record.SourceType, VoiceManifestCollector.StorySourceType, StringComparison.Ordinal)
                && !string.Equals(record.SourceType, VoiceManifestCollector.TutorialSourceType, StringComparison.Ordinal)
                && !string.Equals(record.SourceType, VoiceManifestCollector.GameplaySourceType, StringComparison.Ordinal))
            {
                result.AddWarning($"{label}: unknown source type '{record.SourceType}'.");
            }
        }

        private static void AddDuplicateErrors(
            IReadOnlyList<VoiceManifestRecord> records,
            VoiceManifestValidationResult result,
            Func<VoiceManifestRecord, string> selector,
            string fieldName)
        {
            IEnumerable<IGrouping<string, VoiceManifestRecord>> duplicateGroups = records
                .Where(record => record != null && !string.IsNullOrWhiteSpace(selector(record)))
                .GroupBy(selector, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);

            foreach (IGrouping<string, VoiceManifestRecord> group in duplicateGroups)
            {
                string sources = string.Join(", ", group.Select(record =>
                    $"{record.SourceType}:{record.SourceId}:{record.LineIndex}"));
                result.AddError($"Duplicate {fieldName} '{group.Key}' used by {sources}.");
            }
        }

        private static bool ContainsNote(VoiceManifestRecord record, string note)
        {
            return (record.Notes ?? string.Empty)
                .Split(';')
                .Any(value => string.Equals(value, note, StringComparison.Ordinal));
        }

        private static bool IsUnsupportedPunctuationOnly(string text)
        {
            string trimmed = text?.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed == "..." || trimmed == "…")
            {
                return false;
            }

            for (int index = 0; index < trimmed.Length; index++)
            {
                if (char.IsLetterOrDigit(trimmed[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
