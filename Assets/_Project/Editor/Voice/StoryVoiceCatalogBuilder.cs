using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Project.Scripts.Systems.AudioSystem;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Voice
{
    public sealed class StoryVoiceCatalogValidationResult
    {
        public readonly List<string> Errors = new List<string>();

        public int StoryRecordCount { get; internal set; }
        public int ClipRecordCount { get; internal set; }
        public int SkipRecordCount { get; internal set; }
        public int CatalogEntryCount { get; internal set; }
        public bool IsValid => Errors.Count == 0;
    }

    public static class StoryVoiceCatalogBuilder
    {
        public const string ManifestPath = "Assets/_Project/Data/Voice/VoiceManifest.csv";
        public const string CatalogPath = "Assets/_Project/Audio/Voice/VoiceLineCatalog.asset";
        public const int ExpectedStoryRecords = 211;
        public const int ExpectedClipRecords = 205;
        public const int ExpectedSkipRecords = 6;

        [MenuItem("Tools/True Gate/Voice/Build Story Voice Catalog")]
        public static void BuildFromMenu()
        {
            try
            {
                VoiceLineCatalog catalog = Build();
                StoryVoiceCatalogValidationResult result = Validate(catalog);
                if (!result.IsValid)
                {
                    throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));
                }

                Debug.Log(
                    $"Story voice catalog built: {result.StoryRecordCount} STORY records, " +
                    $"{result.CatalogEntryCount} clips, {result.SkipRecordCount} SKIP.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Story voice catalog build failed: {exception.Message}");
                throw;
            }
        }

        [MenuItem("Tools/True Gate/Voice/Validate Story Voice Catalog")]
        public static void ValidateFromMenu()
        {
            VoiceLineCatalog catalog = AssetDatabase.LoadAssetAtPath<VoiceLineCatalog>(CatalogPath);
            StoryVoiceCatalogValidationResult result = Validate(catalog);
            if (result.IsValid)
            {
                Debug.Log(
                    $"Story voice catalog validation PASS: {result.StoryRecordCount} STORY records, " +
                    $"{result.CatalogEntryCount} clips, {result.SkipRecordCount} SKIP, 0 missing, 0 duplicates.");
                return;
            }

            Debug.LogError(
                $"Story voice catalog validation FAILED ({result.Errors.Count} errors):{Environment.NewLine}" +
                string.Join(Environment.NewLine, result.Errors));
        }

        public static VoiceLineCatalog Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            IReadOnlyList<VoiceManifestRecord> storyRecords = ReadStoryRecords();
            ValidateManifestShape(storyRecords);

            var entries = new List<VoiceLineCatalogEntry>(ExpectedClipRecords);
            var skippedLines = new List<VoiceLineCatalogSkip>(ExpectedSkipRecords);
            foreach (VoiceManifestRecord record in storyRecords)
            {
                if (IsSkip(record))
                {
                    skippedLines.Add(new VoiceLineCatalogSkip(record.SourceId, record.LineIndex));
                    continue;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(record.OutputPath);
                if (clip == null)
                {
                    throw new InvalidOperationException(
                        $"Missing or unreadable STORY AudioClip '{record.OutputPath}' for {record.VoiceId}.");
                }

                entries.Add(new VoiceLineCatalogEntry(
                    record.VoiceId,
                    record.SourceType,
                    record.SourceId,
                    record.LineIndex,
                    record.Speaker,
                    record.Emotion,
                    clip));
            }

            string catalogDirectory = Path.GetDirectoryName(CatalogPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(catalogDirectory) && !AssetDatabase.IsValidFolder(catalogDirectory))
            {
                throw new DirectoryNotFoundException($"Catalog directory does not exist: {catalogDirectory}");
            }

            VoiceLineCatalog catalog = AssetDatabase.LoadAssetAtPath<VoiceLineCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<VoiceLineCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetEntries(entries, skippedLines);
            if (!catalog.TryRebuildLookup(out string lookupError))
            {
                throw new InvalidOperationException(lookupError);
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        public static StoryVoiceCatalogValidationResult Validate(VoiceLineCatalog catalog = null)
        {
            var result = new StoryVoiceCatalogValidationResult();
            IReadOnlyList<VoiceManifestRecord> storyRecords;
            try
            {
                storyRecords = ReadStoryRecords();
            }
            catch (Exception exception)
            {
                result.Errors.Add(exception.Message);
                return result;
            }

            result.StoryRecordCount = storyRecords.Count;
            result.ClipRecordCount = storyRecords.Count(record => !IsSkip(record));
            result.SkipRecordCount = storyRecords.Count(IsSkip);
            ValidateExpectedCounts(result);
            ValidateManifestDuplicates(storyRecords, result.Errors);

            catalog ??= AssetDatabase.LoadAssetAtPath<VoiceLineCatalog>(CatalogPath);
            if (catalog == null)
            {
                result.Errors.Add($"Missing VoiceLineCatalog asset at {CatalogPath}.");
                return result;
            }

            result.CatalogEntryCount = catalog.Entries.Count;
            if (!catalog.TryRebuildLookup(out string lookupError))
            {
                result.Errors.Add(lookupError);
            }

            var manifestByVoiceId = storyRecords
                .Where(record => !IsSkip(record))
                .GroupBy(record => record.VoiceId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            foreach (VoiceLineCatalogEntry entry in catalog.Entries)
            {
                if (entry == null)
                {
                    result.Errors.Add("Catalog contains a null entry.");
                    continue;
                }

                if (!manifestByVoiceId.TryGetValue(entry.VoiceId, out VoiceManifestRecord record))
                {
                    result.Errors.Add($"Catalog VoiceId '{entry.VoiceId}' is not a non-SKIP STORY manifest record.");
                    continue;
                }

                if (entry.Clip == null)
                {
                    result.Errors.Add($"Catalog entry '{entry.VoiceId}' has no AudioClip.");
                    continue;
                }

                string actualPath = AssetDatabase.GetAssetPath(entry.Clip);
                if (!string.Equals(actualPath, record.OutputPath, StringComparison.Ordinal))
                {
                    result.Errors.Add(
                        $"Catalog path mismatch for {entry.VoiceId}: '{actualPath}' != '{record.OutputPath}'.");
                }

                if (!string.Equals(entry.SourceId, record.SourceId, StringComparison.Ordinal)
                    || entry.LineIndex != record.LineIndex)
                {
                    result.Errors.Add($"Catalog line key mismatch for {entry.VoiceId}.");
                }
            }

            foreach (VoiceManifestRecord record in storyRecords.Where(record => !IsSkip(record)))
            {
                if (!catalog.TryGet(record.SourceId, record.LineIndex, out VoiceLineCatalogEntry entry)
                    || entry == null
                    || entry.Clip == null)
                {
                    result.Errors.Add($"Missing catalog clip for {record.SourceId} line {record.LineIndex}.");
                }
            }

            foreach (VoiceManifestRecord record in storyRecords.Where(IsSkip))
            {
                if (!catalog.IsSkipped(record.SourceId, record.LineIndex))
                {
                    result.Errors.Add($"Missing SKIP marker for {record.SourceId} line {record.LineIndex}.");
                }
            }

            if (catalog.Entries.Count != ExpectedClipRecords)
            {
                result.Errors.Add(
                    $"Catalog has {catalog.Entries.Count} entries; expected {ExpectedClipRecords}.");
            }

            if (catalog.SkippedLines.Count != ExpectedSkipRecords)
            {
                result.Errors.Add(
                    $"Catalog has {catalog.SkippedLines.Count} SKIP markers; expected {ExpectedSkipRecords}.");
            }

            return result;
        }

        private static IReadOnlyList<VoiceManifestRecord> ReadStoryRecords()
        {
            return VoiceManifestCsvReader.Read(ManifestPath)
                .Where(record => string.Equals(record.SourceType, "STORY", StringComparison.Ordinal))
                .ToArray();
        }

        private static void ValidateManifestShape(IReadOnlyList<VoiceManifestRecord> storyRecords)
        {
            var result = new StoryVoiceCatalogValidationResult
            {
                StoryRecordCount = storyRecords.Count,
                ClipRecordCount = storyRecords.Count(record => !IsSkip(record)),
                SkipRecordCount = storyRecords.Count(IsSkip)
            };
            ValidateExpectedCounts(result);
            ValidateManifestDuplicates(storyRecords, result.Errors);
            if (result.Errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));
            }
        }

        private static void ValidateExpectedCounts(StoryVoiceCatalogValidationResult result)
        {
            if (result.StoryRecordCount != ExpectedStoryRecords)
            {
                result.Errors.Add(
                    $"Manifest has {result.StoryRecordCount} STORY records; expected {ExpectedStoryRecords}.");
            }

            if (result.ClipRecordCount != ExpectedClipRecords)
            {
                result.Errors.Add(
                    $"Manifest has {result.ClipRecordCount} STORY clip records; expected {ExpectedClipRecords}.");
            }

            if (result.SkipRecordCount != ExpectedSkipRecords)
            {
                result.Errors.Add(
                    $"Manifest has {result.SkipRecordCount} STORY SKIP records; expected {ExpectedSkipRecords}.");
            }
        }

        private static void ValidateManifestDuplicates(
            IReadOnlyList<VoiceManifestRecord> storyRecords,
            ICollection<string> errors)
        {
            AddDuplicateErrors(storyRecords, record => record.VoiceId, "VoiceId", errors);
            AddDuplicateErrors(storyRecords, record => record.OutputPath, "OutputPath", errors);
            AddDuplicateErrors(
                storyRecords,
                record => $"{record.SourceId}\u001F{record.LineIndex}",
                "SourceId + LineIndex",
                errors);
        }

        private static void AddDuplicateErrors(
            IEnumerable<VoiceManifestRecord> records,
            Func<VoiceManifestRecord, string> keySelector,
            string label,
            ICollection<string> errors)
        {
            foreach (IGrouping<string, VoiceManifestRecord> duplicate in records
                         .GroupBy(keySelector, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                errors.Add($"Duplicate STORY {label}: '{duplicate.Key}'.");
            }
        }

        private static bool IsSkip(VoiceManifestRecord record)
        {
            return string.Equals(record.Status, "SKIP", StringComparison.OrdinalIgnoreCase);
        }
    }
}
