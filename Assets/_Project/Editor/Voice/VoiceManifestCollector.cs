using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _Project.Cutscenes;
using _Project.Scripts.Gameplay.Dialogue;
using _Project.Scripts.Systems.AudioSystem;
using _Project.Scripts.Systems.TutorialSystem;

namespace _Project.Editor.Voice
{
    internal static class VoiceManifestCollector
    {
        internal const string GameplayCsvPath =
            "Assets/_Project/Data/Dialogue/GameplayDialogueContent_v0.1.csv";

        internal const string StorySourceType = "STORY";
        internal const string TutorialSourceType = "TUTORIAL";
        internal const string GameplaySourceType = "GAMEPLAY";

        public static IReadOnlyList<VoiceManifestRecord> CollectAll()
        {
            var records = new List<VoiceManifestRecord>();
            CollectDefinitions(records, StoryCutsceneLibrary.GetAll(), StorySourceType);
            CollectDefinitions(records, TutorialCutsceneDefinitions.GetAll(), TutorialSourceType);
            CollectGameplay(records, LoadGameplayEntries());
            MarkDuplicateCandidates(records);
            return records.AsReadOnly();
        }

        internal static VoiceManifestRecord CreateGameplayRecord(GameplayDialogueEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            string emotion = GameplayVoiceEmotionResolver.Resolve(entry.PsychologyPhase, entry.Tag);
            var record = new VoiceManifestRecord
            {
                VoiceId = VoiceIdUtility.ForGameplay(entry.DialogueId),
                SourceType = GameplaySourceType,
                SourceId = entry.DialogueId,
                LineIndex = 0,
                Speaker = "UNIT-07",
                Emotion = emotion,
                PsychologyPhase = entry.PsychologyPhase.ToString().ToUpperInvariant(),
                Tag = entry.Tag,
                Text = entry.Text
            };

            if (string.Equals(emotion, "unknown", StringComparison.Ordinal))
            {
                AppendNote(record, "UNMAPPED_EMOTION");
            }

            FinalizeRecord(record);
            return record;
        }

        internal static string GetSpeakerFolder(string speaker)
        {
            string normalized = NormalizeSpeaker(speaker);
            if (string.Equals(normalized, "SYSTEM", StringComparison.Ordinal))
            {
                return "System";
            }

            if (string.Equals(normalized, "UNIT-07", StringComparison.Ordinal))
            {
                return "Unit07";
            }

            if (string.Equals(normalized, "HUMAN COMMAND", StringComparison.Ordinal))
            {
                return "HumanCommand";
            }

            string safeSegment = VoiceIdUtility.NormalizeIdSegment(normalized);
            if (string.IsNullOrEmpty(safeSegment))
            {
                return "Unknown";
            }

            var folder = new StringBuilder(safeSegment.Length);
            string[] words = safeSegment.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words)
            {
                if (word.Length == 0)
                {
                    continue;
                }

                folder.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                {
                    folder.Append(word.Substring(1).ToLowerInvariant());
                }
            }

            return folder.Length > 0 ? folder.ToString() : "Unknown";
        }

        internal static bool HasOfficialSpeakerFolder(string speaker)
        {
            string normalized = NormalizeSpeaker(speaker);
            return string.Equals(normalized, "SYSTEM", StringComparison.Ordinal)
                || string.Equals(normalized, "UNIT-07", StringComparison.Ordinal)
                || string.Equals(normalized, "HUMAN COMMAND", StringComparison.Ordinal);
        }

        private static IReadOnlyList<GameplayDialogueEntry> LoadGameplayEntries()
        {
            if (!File.Exists(GameplayCsvPath))
            {
                throw new FileNotFoundException("Gameplay dialogue CSV was not found.", GameplayCsvPath);
            }

            string csv = File.ReadAllText(GameplayCsvPath, Encoding.UTF8);
            return GameplayDialogueCsvParser.Parse(csv, GameplayCsvPath);
        }

        private static void CollectDefinitions(
            List<VoiceManifestRecord> records,
            IReadOnlyList<StoryCutsceneDefinition> definitions,
            string sourceType)
        {
            if (definitions == null)
            {
                throw new InvalidOperationException($"Could not enumerate {sourceType} definitions.");
            }

            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                StoryCutsceneDefinition definition = definitions[definitionIndex];
                if (definition == null || definition.Lines == null)
                {
                    throw new InvalidOperationException(
                        $"{sourceType} definition at index {definitionIndex} is null or has no line collection.");
                }

                for (int lineIndex = 0; lineIndex < definition.Lines.Count; lineIndex++)
                {
                    StoryDialogueLine line = definition.Lines[lineIndex];
                    var record = new VoiceManifestRecord
                    {
                        VoiceId = sourceType == StorySourceType
                            ? VoiceIdUtility.ForStory(definition.CutsceneId, lineIndex)
                            : VoiceIdUtility.ForTutorial(definition.CutsceneId, lineIndex),
                        SourceType = sourceType,
                        SourceId = definition.CutsceneId,
                        LineIndex = lineIndex,
                        Speaker = line?.Speaker,
                        Emotion = line?.Emotion,
                        PsychologyPhase = string.Empty,
                        Tag = string.Empty,
                        Text = line?.Text
                    };

                    FinalizeRecord(record);
                    records.Add(record);
                }
            }
        }

        private static void CollectGameplay(
            List<VoiceManifestRecord> records,
            IReadOnlyList<GameplayDialogueEntry> entries)
        {
            if (entries == null)
            {
                throw new InvalidOperationException("Could not enumerate gameplay dialogue entries.");
            }

            for (int index = 0; index < entries.Count; index++)
            {
                records.Add(CreateGameplayRecord(entries[index]));
            }
        }

        private static void FinalizeRecord(VoiceManifestRecord record)
        {
            record.Speaker = NormalizeSpeaker(record.Speaker);
            record.Emotion = record.Emotion?.Trim() ?? string.Empty;
            record.PsychologyPhase = record.PsychologyPhase?.Trim() ?? string.Empty;
            record.Tag = record.Tag?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(record.Speaker))
            {
                AppendNote(record, "MISSING_SPEAKER");
            }

            if (string.IsNullOrWhiteSpace(record.Text))
            {
                AppendNote(record, "MISSING_TEXT");
            }

            string trimmedText = record.Text?.Trim();
            record.Status = trimmedText == "..." || trimmedText == "…" ? "SKIP" : "PENDING";
            record.DuplicateOf = record.DuplicateOf ?? string.Empty;
            record.Notes = record.Notes ?? string.Empty;
            record.OutputPath =
                $"Assets/_Project/Audio/Voice/{GetSpeakerFolder(record.Speaker)}/{record.VoiceId}.wav";
        }

        private static string NormalizeSpeaker(string speaker)
        {
            string trimmed = speaker?.Trim() ?? string.Empty;
            if (string.Equals(trimmed, "SYSTEM", StringComparison.OrdinalIgnoreCase))
            {
                return "SYSTEM";
            }

            if (string.Equals(trimmed, "UNIT-07", StringComparison.OrdinalIgnoreCase))
            {
                return "UNIT-07";
            }

            if (string.Equals(trimmed, "HUMAN COMMAND", StringComparison.OrdinalIgnoreCase))
            {
                return "HUMAN COMMAND";
            }

            return trimmed;
        }

        private static void MarkDuplicateCandidates(IReadOnlyList<VoiceManifestRecord> records)
        {
            var firstByKey = new Dictionary<string, VoiceManifestRecord>(StringComparer.Ordinal);
            for (int index = 0; index < records.Count; index++)
            {
                VoiceManifestRecord record = records[index];
                string key = string.Concat(
                    record.Speaker?.Trim().ToUpperInvariant(), "\u001F",
                    record.Emotion?.Trim().ToUpperInvariant(), "\u001F",
                    record.Text ?? string.Empty);

                if (firstByKey.TryGetValue(key, out VoiceManifestRecord first))
                {
                    record.DuplicateOf = first.VoiceId;
                }
                else
                {
                    firstByKey.Add(key, record);
                }
            }
        }

        private static void AppendNote(VoiceManifestRecord record, string note)
        {
            if (string.IsNullOrEmpty(record.Notes))
            {
                record.Notes = note;
                return;
            }

            string[] existing = record.Notes.Split(';');
            for (int index = 0; index < existing.Length; index++)
            {
                if (string.Equals(existing[index], note, StringComparison.Ordinal))
                {
                    return;
                }
            }

            record.Notes += ";" + note;
        }
    }
}
