#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Audio
{
    public sealed class AudioImportAuditor : EditorWindow
    {
        public enum UsageCategory
        {
            All,
            UiShortSfx,
            GameplayFrequentSfx,
            GameplayOneShot,
            VoiceDialogue,
            MusicAmbience
        }

        public enum AuditorMode
        {
            AuditOnly,
            ApplyRecommendedSettings
        }

        private const string DefaultReportDirectory =
            "Assets/_Project/Documentation/MobileReadiness";
        private const string AudioCatalogPath =
            "Assets/_Project/Audio/AudioCatalog.asset";
        private const string VoiceCatalogPath =
            "Assets/_Project/Audio/Voice/VoiceLineCatalog.asset";

        private AuditorMode _mode;
        private UsageCategory _category = UsageCategory.All;
        private string _folderFilter = "Assets";
        private Vector2 _scroll;
        private List<AuditRow> _preview = new List<AuditRow>();

        [MenuItem("Tools/True Gate/Audit/Audio")]
        public static void Open()
        {
            GetWindow<AudioImportAuditor>("Audio Audit");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("True Gate Mobile Audio Auditor", EditorStyles.boldLabel);
            _mode = (AuditorMode)EditorGUILayout.EnumPopup("Mode", _mode);
            _category = (UsageCategory)EditorGUILayout.EnumPopup("Category", _category);
            _folderFilter = EditorGUILayout.TextField("Folder Filter", _folderFilter);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan"))
                {
                    _preview = Scan(_folderFilter, _category);
                }

                if (GUILayout.Button("Export Audit"))
                {
                    string suffix = _mode == AuditorMode.AuditOnly ? "Before" : "After";
                    ExportAudit(
                        $"{DefaultReportDirectory}/Audio_Audit_{suffix}.csv",
                        _folderFilter,
                        _category);
                }
            }

            using (new EditorGUI.DisabledScope(
                       _mode != AuditorMode.ApplyRecommendedSettings || _preview.Count == 0))
            {
                if (GUILayout.Button($"Apply {_preview.Count(row => row.NeedsChange)} Recommended Changes"))
                {
                    int changeCount = _preview.Count(row => row.NeedsChange);
                    if (EditorUtility.DisplayDialog(
                            "Apply Audio Import Settings",
                            $"Reimport {changeCount} AudioClip assets matching the current filter?",
                            "Apply",
                            "Cancel"))
                    {
                        ApplyRecommendedSettings(_folderFilter, _category);
                        _preview = Scan(_folderFilter, _category);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                $"Scanned: {_preview.Count} | Changes: {_preview.Count(row => row.NeedsChange)}");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (AuditRow row in _preview.Take(250))
            {
                EditorGUILayout.LabelField(
                    $"{(row.NeedsChange ? "CHANGE" : "OK")}: {row.AssetPath}",
                    row.NeedsChange ? EditorStyles.boldLabel : EditorStyles.label);
                if (row.NeedsChange)
                {
                    EditorGUILayout.LabelField(row.SuggestedSettings, EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        public static AuditSummary ExportAudit(
            string reportPath,
            string folderFilter = "Assets",
            UsageCategory category = UsageCategory.All)
        {
            List<AuditRow> rows = Scan(folderFilter, category);
            string directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var csv = new StringBuilder();
            csv.AppendLine(
                "AssetPath,FileName,Duration,Channels,Frequency,SourceFileSize,LoadType," +
                "CompressionFormat,Quality,PreloadAudioData,LoadInBackground,ForceToMono," +
                "Ambisonic,UsageCategory,ReferenceStatus,EstimatedRuntimeMemory," +
                "SuggestedSettings,Warnings");

            foreach (AuditRow row in rows)
            {
                csv.AppendLine(row.ToCsv());
            }

            File.WriteAllText(reportPath, csv.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            return BuildSummary(rows);
        }

        public static ApplySummary ApplyRecommendedSettings(
            string folderFilter = "Assets",
            UsageCategory category = UsageCategory.All)
        {
            List<AuditRow> rows = Scan(folderFilter, category);
            int changed = 0;
            int unchanged = 0;

            try
            {
                for (int index = 0; index < rows.Count; index++)
                {
                    AuditRow row = rows[index];
                    if (!row.NeedsChange)
                    {
                        unchanged++;
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Applying Mobile Audio Settings",
                        row.AssetPath,
                        rows.Count == 0 ? 1f : (float)index / rows.Count);

                    AudioImporter importer = AssetImporter.GetAtPath(row.AssetPath) as AudioImporter;
                    if (importer == null)
                    {
                        continue;
                    }

                    ApplyRecommendation(importer, row.Recommendation);
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            return new ApplySummary
            {
                Scanned = rows.Count,
                Changed = changed,
                Unchanged = unchanged
            };
        }

        public static List<AuditRow> Scan(
            string folderFilter = "Assets",
            UsageCategory category = UsageCategory.All)
        {
            string normalizedFolder = NormalizeFolder(folderFilter);
            string[] searchFolders = AssetDatabase.IsValidFolder(normalizedFolder)
                ? new[] { normalizedFolder }
                : new[] { "Assets" };

            string[] paths = AssetDatabase.FindAssets("t:AudioClip", searchFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            HashSet<string> buildDependencies = BuildDependencySet(
                EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));
            HashSet<string> prefabDependencies = BuildDependencySet(
                AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                    .Select(AssetDatabase.GUIDToAssetPath));
            HashSet<string> audioCatalogDependencies = File.Exists(AudioCatalogPath)
                ? BuildDependencySet(new[] { AudioCatalogPath })
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> voiceCatalogDependencies = File.Exists(VoiceCatalogPath)
                ? BuildDependencySet(new[] { VoiceCatalogPath })
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            HashSet<string> duplicateNames = paths
                .GroupBy(
                    path => Path.GetFileNameWithoutExtension(path),
                    StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> duplicateHashes = FindDuplicateHashes(paths);

            var rows = new List<AuditRow>(paths.Length);
            foreach (string path in paths)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (clip == null || importer == null)
                {
                    continue;
                }

                UsageCategory usage = Classify(path, clip.length);
                if (category != UsageCategory.All && usage != category)
                {
                    continue;
                }

                Recommendation recommendation = Recommend(usage, clip.length, clip.channels);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), path);
                long sourceSize = File.Exists(absolutePath)
                    ? new FileInfo(absolutePath).Length
                    : 0L;
                long estimatedMemory = EstimateRuntimeMemory(clip, settings, sourceSize);
                string referenceStatus = audioCatalogDependencies.Contains(path)
                    ? "AudioCatalog"
                    : voiceCatalogDependencies.Contains(path)
                        ? "VoiceLineCatalog"
                        : buildDependencies.Contains(path)
                            ? "BuildScene"
                            : prefabDependencies.Contains(path)
                                ? "PrefabOnly"
                                : "NoDirectReference";

                var warnings = new List<string>();
                if (clip.length > 10f && settings.loadType == AudioClipLoadType.DecompressOnLoad)
                {
                    warnings.Add("Long clip uses DecompressOnLoad");
                }

                if (usage == UsageCategory.MusicAmbience && settings.preloadAudioData)
                {
                    warnings.Add("Music or ambience is preloaded");
                }

                if (usage == UsageCategory.VoiceDialogue
                    && clip.length > 3f
                    && settings.preloadAudioData)
                {
                    warnings.Add("Long voice is preloaded");
                }

                if (clip.length < 1f && settings.loadType == AudioClipLoadType.Streaming)
                {
                    warnings.Add("Very short clip uses Streaming");
                }

                if (usage != UsageCategory.MusicAmbience && clip.channels > 1)
                {
                    warnings.Add("Potential mono content is stored as stereo");
                }

                if (clip.frequency > 44100 && usage != UsageCategory.MusicAmbience)
                {
                    warnings.Add("High sample rate for non-music");
                }

                if (importer.ambisonic)
                {
                    warnings.Add("Ambisonic enabled");
                }

                if (referenceStatus == "NoDirectReference")
                {
                    warnings.Add("No direct scene, prefab, or AudioCatalog dependency");
                }

                if (usage == UsageCategory.VoiceDialogue
                    && path.StartsWith("Assets/_Project/Audio/Voice/", StringComparison.Ordinal)
                    && !voiceCatalogDependencies.Contains(path))
                {
                    warnings.Add("Clip is missing from VoiceLineCatalog");
                }

                if (usage != UsageCategory.VoiceDialogue
                    && path.StartsWith("Assets/_Project/Audio/", StringComparison.Ordinal)
                    && !audioCatalogDependencies.Contains(path))
                {
                    warnings.Add("Clip is missing from AudioCatalog");
                }

                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                if (duplicateNames.Contains(fileNameWithoutExtension))
                {
                    warnings.Add("Duplicate filename");
                }

                if (duplicateHashes.Contains(path))
                {
                    warnings.Add("Duplicate source content");
                }

                if (importer.loadInBackground != recommendation.LoadInBackground)
                {
                    warnings.Add("Load In Background differs from category policy");
                }

                rows.Add(new AuditRow
                {
                    AssetPath = path,
                    FileName = Path.GetFileName(path),
                    Duration = clip.length,
                    Channels = clip.channels,
                    Frequency = clip.frequency,
                    SourceFileSize = sourceSize,
                    LoadType = settings.loadType,
                    CompressionFormat = settings.compressionFormat,
                    Quality = settings.quality,
                    PreloadAudioData = settings.preloadAudioData,
                    LoadInBackground = importer.loadInBackground,
                    ForceToMono = importer.forceToMono,
                    Ambisonic = importer.ambisonic,
                    Category = usage,
                    ReferenceStatus = referenceStatus,
                    EstimatedRuntimeMemory = estimatedMemory,
                    Recommendation = recommendation,
                    NeedsChange = NeedsChange(importer, settings, recommendation),
                    SuggestedSettings = recommendation.ToString(),
                    Warnings = string.Join("; ", warnings)
                });
            }

            return rows;
        }

        private static Recommendation Recommend(
            UsageCategory category,
            float duration,
            int channels)
        {
            switch (category)
            {
                case UsageCategory.MusicAmbience:
                    return new Recommendation(
                        AudioClipLoadType.Streaming,
                        AudioCompressionFormat.Vorbis,
                        0.70f,
                        false,
                        true,
                        false);
                case UsageCategory.VoiceDialogue:
                    return new Recommendation(
                        duration > 8f
                            ? AudioClipLoadType.Streaming
                            : AudioClipLoadType.CompressedInMemory,
                        AudioCompressionFormat.Vorbis,
                        0.70f,
                        false,
                        true,
                        true);
                case UsageCategory.UiShortSfx:
                case UsageCategory.GameplayFrequentSfx:
                    return new Recommendation(
                        duration <= 0.75f
                            ? AudioClipLoadType.DecompressOnLoad
                            : AudioClipLoadType.CompressedInMemory,
                        duration <= 0.75f
                            ? AudioCompressionFormat.ADPCM
                            : AudioCompressionFormat.Vorbis,
                        duration <= 0.75f ? 1f : 0.75f,
                        true,
                        false,
                        true);
                default:
                    return new Recommendation(
                        AudioClipLoadType.CompressedInMemory,
                        AudioCompressionFormat.Vorbis,
                        0.75f,
                        true,
                        false,
                        channels == 1);
            }
        }

        private static UsageCategory Classify(string assetPath, float duration)
        {
            string path = assetPath.Replace('\\', '/').ToLowerInvariant();
            string file = Path.GetFileNameWithoutExtension(path);

            if (path.Contains("/music/")
                || file.StartsWith("bgm_", StringComparison.Ordinal)
                || file.StartsWith("amb_", StringComparison.Ordinal))
            {
                return UsageCategory.MusicAmbience;
            }

            if (path.Contains("/voice/")
                || file.StartsWith("vo_", StringComparison.Ordinal)
                || file.Contains("dialogue")
                || file.Contains("voice"))
            {
                return UsageCategory.VoiceDialogue;
            }

            if (path.Contains("/ui/")
                || file.Contains("button")
                || file.Contains("click")
                || file.StartsWith("ui_", StringComparison.Ordinal))
            {
                return UsageCategory.UiShortSfx;
            }

            if (file.Contains("shot")
                || file.Contains("hit")
                || file.Contains("coin")
                || file.Contains("tick")
                || file.Contains("gate_"))
            {
                return UsageCategory.GameplayFrequentSfx;
            }

            return duration <= 0.75f
                ? UsageCategory.GameplayFrequentSfx
                : UsageCategory.GameplayOneShot;
        }

        private static bool NeedsChange(
            AudioImporter importer,
            AudioImporterSampleSettings current,
            Recommendation recommendation)
        {
            return current.loadType != recommendation.LoadType
                || current.compressionFormat != recommendation.Compression
                || !Mathf.Approximately(current.quality, recommendation.Quality)
                || current.preloadAudioData != recommendation.Preload
                || current.sampleRateSetting != AudioSampleRateSetting.OptimizeSampleRate
                || importer.loadInBackground != recommendation.LoadInBackground
                || importer.forceToMono != recommendation.ForceToMono
                || importer.ambisonic;
        }

        private static void ApplyRecommendation(
            AudioImporter importer,
            Recommendation recommendation)
        {
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = recommendation.LoadType;
            settings.compressionFormat = recommendation.Compression;
            settings.quality = recommendation.Quality;
            settings.preloadAudioData = recommendation.Preload;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = recommendation.LoadInBackground;
            importer.forceToMono = recommendation.ForceToMono;
            importer.ambisonic = false;
        }

        private static long EstimateRuntimeMemory(
            AudioClip clip,
            AudioImporterSampleSettings settings,
            long sourceSize)
        {
            long pcmBytes = (long)clip.samples * Math.Max(1, clip.channels) * sizeof(float);
            switch (settings.loadType)
            {
                case AudioClipLoadType.DecompressOnLoad:
                    return pcmBytes;
                case AudioClipLoadType.Streaming:
                    return Math.Min(sourceSize, 200L * 1024L);
                default:
                    return sourceSize;
            }
        }

        private static HashSet<string> BuildDependencySet(IEnumerable<string> roots)
        {
            var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in roots.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                foreach (string dependency in AssetDatabase.GetDependencies(root, true))
                {
                    dependencies.Add(dependency);
                }
            }

            return dependencies;
        }

        private static HashSet<string> FindDuplicateHashes(IEnumerable<string> paths)
        {
            var hashGroups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            using (SHA256 sha = SHA256.Create())
            {
                foreach (string path in paths)
                {
                    string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), path);
                    if (!File.Exists(absolutePath))
                    {
                        continue;
                    }

                    using (FileStream stream = File.OpenRead(absolutePath))
                    {
                        string hash = BitConverter.ToString(sha.ComputeHash(stream));
                        if (!hashGroups.TryGetValue(hash, out List<string> group))
                        {
                            group = new List<string>();
                            hashGroups.Add(hash, group);
                        }

                        group.Add(path);
                    }
                }
            }

            return hashGroups.Values
                .Where(group => group.Count > 1)
                .SelectMany(group => group)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return "Assets";
            }

            return folder.Replace('\\', '/').TrimEnd('/');
        }

        private static AuditSummary BuildSummary(IReadOnlyCollection<AuditRow> rows)
        {
            return new AuditSummary
            {
                Count = rows.Count,
                DecompressOnLoad = rows.Count(row => row.LoadType == AudioClipLoadType.DecompressOnLoad),
                CompressedInMemory = rows.Count(row => row.LoadType == AudioClipLoadType.CompressedInMemory),
                Streaming = rows.Count(row => row.LoadType == AudioClipLoadType.Streaming),
                Preloaded = rows.Count(row => row.PreloadAudioData),
                EstimatedRuntimeMemory = rows.Sum(row => row.EstimatedRuntimeMemory),
                Warnings = rows.Count(row => !string.IsNullOrEmpty(row.Warnings)),
                RecommendedChanges = rows.Count(row => row.NeedsChange)
            };
        }

        private static string Csv(string value)
        {
            return $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        }

        [Serializable]
        public sealed class AuditSummary
        {
            public int Count;
            public int DecompressOnLoad;
            public int CompressedInMemory;
            public int Streaming;
            public int Preloaded;
            public long EstimatedRuntimeMemory;
            public int Warnings;
            public int RecommendedChanges;
        }

        [Serializable]
        public sealed class ApplySummary
        {
            public int Scanned;
            public int Changed;
            public int Unchanged;
        }

        public sealed class AuditRow
        {
            public string AssetPath;
            public string FileName;
            public float Duration;
            public int Channels;
            public int Frequency;
            public long SourceFileSize;
            public AudioClipLoadType LoadType;
            public AudioCompressionFormat CompressionFormat;
            public float Quality;
            public bool PreloadAudioData;
            public bool LoadInBackground;
            public bool ForceToMono;
            public bool Ambisonic;
            public UsageCategory Category;
            public string ReferenceStatus;
            public long EstimatedRuntimeMemory;
            public Recommendation Recommendation;
            public bool NeedsChange;
            public string SuggestedSettings;
            public string Warnings;

            public string ToCsv()
            {
                return string.Join(",", new[]
                {
                    Csv(AssetPath),
                    Csv(FileName),
                    Duration.ToString("0.###", CultureInfo.InvariantCulture),
                    Channels.ToString(CultureInfo.InvariantCulture),
                    Frequency.ToString(CultureInfo.InvariantCulture),
                    SourceFileSize.ToString(CultureInfo.InvariantCulture),
                    Csv(LoadType.ToString()),
                    Csv(CompressionFormat.ToString()),
                    Quality.ToString("0.00", CultureInfo.InvariantCulture),
                    PreloadAudioData.ToString(),
                    LoadInBackground.ToString(),
                    ForceToMono.ToString(),
                    Ambisonic.ToString(),
                    Csv(Category.ToString()),
                    Csv(ReferenceStatus),
                    EstimatedRuntimeMemory.ToString(CultureInfo.InvariantCulture),
                    Csv(SuggestedSettings),
                    Csv(Warnings)
                });
            }
        }

        public struct Recommendation
        {
            public readonly AudioClipLoadType LoadType;
            public readonly AudioCompressionFormat Compression;
            public readonly float Quality;
            public readonly bool Preload;
            public readonly bool LoadInBackground;
            public readonly bool ForceToMono;

            public Recommendation(
                AudioClipLoadType loadType,
                AudioCompressionFormat compression,
                float quality,
                bool preload,
                bool loadInBackground,
                bool forceToMono)
            {
                LoadType = loadType;
                Compression = compression;
                Quality = quality;
                Preload = preload;
                LoadInBackground = loadInBackground;
                ForceToMono = forceToMono;
            }

            public override string ToString()
            {
                return $"{LoadType}; {Compression}; q={Quality:0.00}; " +
                    $"preload={Preload}; background={LoadInBackground}; mono={ForceToMono}; " +
                    "sampleRate=Optimize";
            }
        }
    }
}
#endif
