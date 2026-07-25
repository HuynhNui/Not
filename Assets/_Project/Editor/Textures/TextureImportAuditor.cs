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
using UnityEngine.U2D;

namespace _Project.Editor.Textures
{
    public sealed class TextureImportAuditor : EditorWindow
    {
        public enum UsageCategory
        {
            All,
            PixelGameplay,
            PixelUi,
            SmoothUiIllustration,
            Background,
            Other
        }

        public enum AuditorMode
        {
            AuditOnly,
            ApplyRecommendedSettings
        }

        private const string DefaultReportDirectory =
            "Assets/_Project/Documentation/MobileReadiness";
        private const string AndroidPlatform = "Android";

        private AuditorMode _mode;
        private UsageCategory _category = UsageCategory.All;
        private string _folderFilter = "Assets";
        private Vector2 _scroll;
        private List<AuditRow> _preview = new List<AuditRow>();

        [MenuItem("Tools/True Gate/Audit/Textures")]
        public static void Open()
        {
            GetWindow<TextureImportAuditor>("Texture Audit");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("True Gate Mobile Texture Auditor", EditorStyles.boldLabel);
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
                        $"{DefaultReportDirectory}/Texture_Audit_{suffix}.csv",
                        _folderFilter,
                        _category);
                }
            }

            using (new EditorGUI.DisabledScope(
                       _mode != AuditorMode.ApplyRecommendedSettings || _preview.Count == 0))
            {
                int changeCount = _preview.Count(row => row.NeedsChange);
                if (GUILayout.Button($"Apply {changeCount} Recommended Changes"))
                {
                    if (EditorUtility.DisplayDialog(
                            "Apply Texture Import Settings",
                            $"Reimport {changeCount} textures matching the current filter?",
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
                    EditorGUILayout.LabelField(row.SuggestedAction, EditorStyles.wordWrappedMiniLabel);
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
                "AssetPath,Dimensions,TextureType,SpriteMode,SourceFileSize," +
                "EstimatedRuntimeMemory,HasAlpha,ReadWriteEnabled,MipMaps,FilterMode," +
                "WrapMode,PixelsPerUnit,MaxSize,DefaultCompression,AndroidOverride," +
                "AndroidFormat,CompressionQuality,CrunchCompression,SpriteAtlas," +
                "SuggestedAction,Warnings");

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
                        "Applying Mobile Texture Settings",
                        row.AssetPath,
                        rows.Count == 0 ? 1f : (float)index / rows.Count);

                    TextureImporter importer =
                        AssetImporter.GetAtPath(row.AssetPath) as TextureImporter;
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

            string[] paths = AssetDatabase.FindAssets("t:Texture2D", searchFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Dictionary<string, string> duplicateHashes = FindDuplicateHashes(paths);
            SpriteAtlas[] atlases = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SpriteAtlas>)
                .Where(atlas => atlas != null)
                .ToArray();

            var rows = new List<AuditRow>(paths.Length);
            foreach (string path in paths)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (texture == null || importer == null)
                {
                    continue;
                }

                UsageCategory usage = Classify(path, importer);
                if (category != UsageCategory.All && usage != category)
                {
                    continue;
                }

                int width = Math.Max(1, texture.width);
                int height = Math.Max(1, texture.height);
                bool hasAlpha = importer.DoesSourceTextureHaveAlpha();
                bool scrolling = IsScrollingTexture(path, importer);
                bool hasNineSliceBorder = HasNineSliceBorder(path);
                TextureImporterPlatformSettings android =
                    importer.GetPlatformTextureSettings(AndroidPlatform);
                Recommendation recommendation = Recommend(
                    usage,
                    width,
                    height,
                    hasAlpha,
                    scrolling,
                    importer);

                string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), path);
                long sourceSize = File.Exists(absolutePath)
                    ? new FileInfo(absolutePath).Length
                    : 0L;
                string formatName = android.overridden
                    ? android.format.ToString()
                    : texture.format.ToString();
                long estimatedMemory = EstimateRuntimeMemory(
                    width,
                    height,
                    importer.mipmapEnabled,
                    formatName,
                    hasAlpha);

                var warnings = new List<string>();
                bool pixel = usage == UsageCategory.PixelGameplay
                    || usage == UsageCategory.PixelUi;
                bool ui = usage == UsageCategory.PixelUi
                    || usage == UsageCategory.SmoothUiIllustration;

                if ((pixel || ui) && importer.mipmapEnabled)
                {
                    warnings.Add(pixel ? "Pixel sprite has mipmaps" : "UI texture has mipmaps");
                }

                if (pixel && importer.filterMode != FilterMode.Point)
                {
                    warnings.Add("Pixel art is not using Point filtering");
                }

                if (importer.isReadable)
                {
                    warnings.Add("Read/Write is enabled");
                }

                if (ui && Math.Max(width, height) >= 2048)
                {
                    warnings.Add("Large UI texture needs visual size review");
                }

                if (!android.overridden && Math.Max(width, height) >= 512)
                {
                    warnings.Add("Large texture has no Android override");
                }

                if (Math.Max(width, height) >= 1024
                    && importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    warnings.Add("Large texture is uncompressed");
                }

                int sourceMaxSize = Mathf.Max(
                    32,
                    Mathf.NextPowerOfTwo(Math.Max(width, height)));
                if (importer.maxTextureSize > sourceMaxSize)
                {
                    warnings.Add("Max Size is larger than source dimensions");
                }

                if (scrolling && importer.wrapMode != TextureWrapMode.Repeat)
                {
                    warnings.Add("Scrolling texture is not using Repeat");
                }

                if (!scrolling
                    && importer.textureType == TextureImporterType.Sprite
                    && importer.wrapMode != TextureWrapMode.Clamp)
                {
                    warnings.Add("Sprite is not using Clamp");
                }

                if (hasNineSliceBorder)
                {
                    warnings.Add("9-slice border present and must be preserved");
                }

                if (path.IndexOf("/test", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("/debug", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    warnings.Add("Debug, test, or preview texture needs reference review");
                }

                if (duplicateHashes.TryGetValue(path, out string duplicateGroup))
                {
                    warnings.Add($"Duplicate source content: {duplicateGroup}");
                }

                string atlasNames = FindAtlasNames(path, atlases);
                if (usage == UsageCategory.Background && !string.IsNullOrEmpty(atlasNames))
                {
                    warnings.Add("Background is included in a Sprite Atlas");
                }

                rows.Add(new AuditRow
                {
                    AssetPath = path,
                    Dimensions = $"{width}x{height}",
                    TextureType = importer.textureType,
                    SpriteMode = importer.spriteImportMode,
                    SourceFileSize = sourceSize,
                    EstimatedRuntimeMemory = estimatedMemory,
                    HasAlpha = hasAlpha,
                    ReadWriteEnabled = importer.isReadable,
                    MipMaps = importer.mipmapEnabled,
                    FilterMode = importer.filterMode,
                    WrapMode = importer.wrapMode,
                    PixelsPerUnit = importer.spritePixelsPerUnit,
                    MaxSize = importer.maxTextureSize,
                    DefaultCompression = importer.textureCompression,
                    AndroidOverride = android.overridden,
                    AndroidFormat = android.format,
                    CompressionQuality = android.overridden
                        ? android.compressionQuality
                        : importer.compressionQuality,
                    CrunchCompression = android.overridden
                        ? android.crunchedCompression
                        : importer.crunchedCompression,
                    SpriteAtlas = atlasNames,
                    Category = usage,
                    Recommendation = recommendation,
                    NeedsChange = NeedsChange(importer, android, recommendation),
                    SuggestedAction = recommendation.ToString(),
                    Warnings = string.Join("; ", warnings)
                });
            }

            return rows;
        }

        private static Recommendation Recommend(
            UsageCategory category,
            int width,
            int height,
            bool hasAlpha,
            bool scrolling,
            TextureImporter importer)
        {
            bool sprite = importer.textureType == TextureImporterType.Sprite;
            bool managedGroup = category != UsageCategory.Other;
            bool point = category == UsageCategory.PixelGameplay
                || category == UsageCategory.PixelUi;
            int sourceMax = Math.Max(width, height);
            int recommendedMax = Mathf.Clamp(Mathf.NextPowerOfTwo(sourceMax), 32, 2048);
            bool androidOverride = managedGroup && sourceMax >= 512;

            return new Recommendation
            {
                ApplyCommonSpriteRules = sprite && managedGroup,
                MipMaps = sprite ? false : importer.mipmapEnabled,
                Readable = managedGroup ? false : importer.isReadable,
                FilterMode = point
                    ? FilterMode.Point
                    : managedGroup
                        ? FilterMode.Bilinear
                        : importer.filterMode,
                WrapMode = scrolling
                    ? TextureWrapMode.Repeat
                    : sprite && managedGroup
                        ? TextureWrapMode.Clamp
                        : importer.wrapMode,
                MaxSize = managedGroup ? recommendedMax : importer.maxTextureSize,
                Compression = managedGroup
                    ? TextureImporterCompression.Compressed
                    : importer.textureCompression,
                CompressionQuality = managedGroup ? 50 : importer.compressionQuality,
                CrunchCompression = managedGroup ? false : importer.crunchedCompression,
                AndroidOverride = androidOverride,
                AndroidFormat = hasAlpha
                    ? TextureImporterFormat.ETC2_RGBA8
                    : TextureImporterFormat.ETC2_RGB4
            };
        }

        private static bool NeedsChange(
            TextureImporter importer,
            TextureImporterPlatformSettings android,
            Recommendation recommendation)
        {
            if (importer.mipmapEnabled != recommendation.MipMaps
                || importer.isReadable != recommendation.Readable
                || importer.filterMode != recommendation.FilterMode
                || importer.wrapMode != recommendation.WrapMode
                || importer.maxTextureSize != recommendation.MaxSize
                || importer.textureCompression != recommendation.Compression
                || importer.compressionQuality != recommendation.CompressionQuality
                || importer.crunchedCompression != recommendation.CrunchCompression)
            {
                return true;
            }

            if (recommendation.AndroidOverride)
            {
                return !android.overridden
                    || android.maxTextureSize != recommendation.MaxSize
                    || android.format != recommendation.AndroidFormat
                    || android.compressionQuality != recommendation.CompressionQuality
                    || android.crunchedCompression;
            }

            return false;
        }

        private static void ApplyRecommendation(
            TextureImporter importer,
            Recommendation recommendation)
        {
            importer.mipmapEnabled = recommendation.MipMaps;
            importer.isReadable = recommendation.Readable;
            importer.filterMode = recommendation.FilterMode;
            importer.wrapMode = recommendation.WrapMode;
            importer.maxTextureSize = recommendation.MaxSize;
            importer.textureCompression = recommendation.Compression;
            importer.compressionQuality = recommendation.CompressionQuality;
            importer.crunchedCompression = recommendation.CrunchCompression;

            if (recommendation.ApplyCommonSpriteRules)
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
            }

            if (!recommendation.AndroidOverride)
            {
                return;
            }

            TextureImporterPlatformSettings android =
                importer.GetPlatformTextureSettings(AndroidPlatform);
            android.name = AndroidPlatform;
            android.overridden = true;
            android.maxTextureSize = recommendation.MaxSize;
            android.format = recommendation.AndroidFormat;
            android.textureCompression = TextureImporterCompression.Compressed;
            android.compressionQuality = recommendation.CompressionQuality;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
        }

        private static UsageCategory Classify(string assetPath, TextureImporter importer)
        {
            string path = assetPath.Replace('\\', '/').ToLowerInvariant();
            if (path.Contains("/background/")
                || path.Contains("/backgrounds/")
                || Path.GetFileNameWithoutExtension(path).StartsWith("bg_")
                || Path.GetFileNameWithoutExtension(path).StartsWith("background"))
            {
                return UsageCategory.Background;
            }

            if (path.Contains("/cutscenes/")
                || path.Contains("/dialogue/")
                || path.Contains("portrait")
                || path.Contains("illustration"))
            {
                return UsageCategory.SmoothUiIllustration;
            }

            if (path.Contains("/art/ui/"))
            {
                return path.Contains("pixel")
                    || path.Contains("/generated/")
                    || importer.filterMode == FilterMode.Point
                    ? UsageCategory.PixelUi
                    : UsageCategory.SmoothUiIllustration;
            }

            if (path.Contains("/maincharacter/")
                || path.Contains("/gate/")
                || path.Contains("/sprites/bullet/")
                || path.Contains("/enemies/")
                || path.Contains("/units/")
                || path.Contains("/vfx/")
                || path.Contains("pixel"))
            {
                return UsageCategory.PixelGameplay;
            }

            return UsageCategory.Other;
        }

        private static bool IsScrollingTexture(string assetPath, TextureImporter importer)
        {
            string path = assetPath.Replace('\\', '/').ToLowerInvariant();
            return importer.wrapMode == TextureWrapMode.Repeat
                || path.Contains("scroll")
                || path.Contains("tileable")
                || path.Contains("seamless");
        }

        private static bool HasNineSliceBorder(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .Any(sprite => sprite.border.sqrMagnitude > 0f);
        }

        private static string FindAtlasNames(string assetPath, IEnumerable<SpriteAtlas> atlases)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .ToArray();
            if (sprites.Length == 0)
            {
                return string.Empty;
            }

            return string.Join(
                "; ",
                atlases
                    .Where(atlas => sprites.Any(atlas.CanBindTo))
                    .Select(atlas => atlas.name)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal));
        }

        private static Dictionary<string, string> FindDuplicateHashes(IEnumerable<string> paths)
        {
            var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
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
                        if (!groups.TryGetValue(hash, out List<string> group))
                        {
                            group = new List<string>();
                            groups.Add(hash, group);
                        }

                        group.Add(path);
                    }
                }
            }

            var duplicates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (List<string> group in groups.Values.Where(group => group.Count > 1))
            {
                string label = string.Join(" | ", group.Select(Path.GetFileName));
                foreach (string path in group)
                {
                    duplicates[path] = label;
                }
            }

            return duplicates;
        }

        private static long EstimateRuntimeMemory(
            int width,
            int height,
            bool mipMaps,
            string formatName,
            bool hasAlpha)
        {
            string format = formatName ?? string.Empty;
            double bytesPerPixel;
            if (format.IndexOf("ETC2_RGB", StringComparison.OrdinalIgnoreCase) >= 0
                || format.IndexOf("DXT1", StringComparison.OrdinalIgnoreCase) >= 0
                || format.IndexOf("BC1", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bytesPerPixel = 0.5d;
            }
            else if (format.IndexOf("ETC2_RGBA", StringComparison.OrdinalIgnoreCase) >= 0
                || format.IndexOf("DXT5", StringComparison.OrdinalIgnoreCase) >= 0
                || format.IndexOf("BC3", StringComparison.OrdinalIgnoreCase) >= 0
                || format.IndexOf("ASTC", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bytesPerPixel = 1d;
            }
            else if (format.IndexOf("RGB24", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bytesPerPixel = 3d;
            }
            else
            {
                bytesPerPixel = hasAlpha ? 4d : 3d;
            }

            double mipMultiplier = mipMaps ? 4d / 3d : 1d;
            return (long)Math.Ceiling(width * (double)height * bytesPerPixel * mipMultiplier);
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
                EstimatedRuntimeMemory = rows.Sum(row => row.EstimatedRuntimeMemory),
                Readable = rows.Count(row => row.ReadWriteEnabled),
                MipMapped = rows.Count(row => row.MipMaps),
                AndroidOverrides = rows.Count(row => row.AndroidOverride),
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
            public long EstimatedRuntimeMemory;
            public int Readable;
            public int MipMapped;
            public int AndroidOverrides;
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
            public string Dimensions;
            public TextureImporterType TextureType;
            public SpriteImportMode SpriteMode;
            public long SourceFileSize;
            public long EstimatedRuntimeMemory;
            public bool HasAlpha;
            public bool ReadWriteEnabled;
            public bool MipMaps;
            public FilterMode FilterMode;
            public TextureWrapMode WrapMode;
            public float PixelsPerUnit;
            public int MaxSize;
            public TextureImporterCompression DefaultCompression;
            public bool AndroidOverride;
            public TextureImporterFormat AndroidFormat;
            public int CompressionQuality;
            public bool CrunchCompression;
            public string SpriteAtlas;
            public UsageCategory Category;
            public Recommendation Recommendation;
            public bool NeedsChange;
            public string SuggestedAction;
            public string Warnings;

            public string ToCsv()
            {
                return string.Join(",", new[]
                {
                    Csv(AssetPath),
                    Csv(Dimensions),
                    Csv(TextureType.ToString()),
                    Csv(SpriteMode.ToString()),
                    SourceFileSize.ToString(CultureInfo.InvariantCulture),
                    EstimatedRuntimeMemory.ToString(CultureInfo.InvariantCulture),
                    HasAlpha.ToString(),
                    ReadWriteEnabled.ToString(),
                    MipMaps.ToString(),
                    Csv(FilterMode.ToString()),
                    Csv(WrapMode.ToString()),
                    PixelsPerUnit.ToString("0.###", CultureInfo.InvariantCulture),
                    MaxSize.ToString(CultureInfo.InvariantCulture),
                    Csv(DefaultCompression.ToString()),
                    AndroidOverride.ToString(),
                    Csv(AndroidFormat.ToString()),
                    CompressionQuality.ToString(CultureInfo.InvariantCulture),
                    CrunchCompression.ToString(),
                    Csv(SpriteAtlas),
                    Csv(SuggestedAction),
                    Csv(Warnings)
                });
            }
        }

        public struct Recommendation
        {
            public bool ApplyCommonSpriteRules;
            public bool MipMaps;
            public bool Readable;
            public FilterMode FilterMode;
            public TextureWrapMode WrapMode;
            public int MaxSize;
            public TextureImporterCompression Compression;
            public int CompressionQuality;
            public bool CrunchCompression;
            public bool AndroidOverride;
            public TextureImporterFormat AndroidFormat;

            public override string ToString()
            {
                string android = AndroidOverride
                    ? $"; Android={AndroidFormat}@{MaxSize}"
                    : "; Android=default ETC2";
                return $"filter={FilterMode}; mipmaps={MipMaps}; readWrite={Readable}; " +
                    $"wrap={WrapMode}; maxSize={MaxSize}; compression={Compression}" +
                    android;
            }
        }
    }
}
#endif
