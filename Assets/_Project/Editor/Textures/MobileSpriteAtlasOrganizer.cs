#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace _Project.Editor.Textures
{
    public static class MobileSpriteAtlasOrganizer
    {
        private const string AtlasDirectory = "Assets/_Project/Atlases";
        private const string ReportPath =
            "Assets/_Project/Documentation/MobileReadiness/07_SpriteAtlas_Audit_After.md";

        private static readonly AtlasGroup[] Groups =
        {
            new AtlasGroup(
                "UI_Common",
                true,
                path => ContainsAny(path, "/Art/UI/PausePanel/", "/Art/UI/SettingPanel/")),
            new AtlasGroup(
                "UI_MainMenu",
                true,
                path => path.Contains("/Art/UI/MainMenu/")),
            new AtlasGroup(
                "UI_Gameplay",
                true,
                path => ContainsAny(
                    path,
                    "/Art/UI/GameplayHudPanel/",
                    "/Art/UI/MissionSystem/",
                    "/Art/UI/Misson/")),
            new AtlasGroup(
                "UI_Upgrade",
                true,
                path => path.Contains("/Art/UI/Generated/")),
            new AtlasGroup(
                "UI_Dialogue",
                false,
                path => ContainsAny(
                    path,
                    "/Art/UI/GameplayDialogue/",
                    "/Art/UI/Tutorial/")),
            new AtlasGroup(
                "Gameplay_Characters",
                true,
                path => path.Contains("/Art/Maincharacter/")),
            new AtlasGroup(
                "Gameplay_Enemies",
                true,
                path => path.Contains("/Prefabs/Enemies/")),
            new AtlasGroup(
                "Gameplay_Gates",
                true,
                path => path.Contains("/Art/Gate/")),
            new AtlasGroup(
                "Gameplay_ProjectilesVFX",
                true,
                path => ContainsAny(
                    path,
                    "/Art/Sprites/Bullet/",
                    "/Prefabs/VFX/",
                    "/Prefabs/Bullets/"))
        };

        [MenuItem("Tools/True Gate/Mobile/Create Sprite Atlases")]
        public static void CreateOrUpdateAtlases()
        {
            EnsureFolder(AtlasDirectory);
            HashSet<string> buildDependencies = GetBuildDependencies();
            var assignedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<AtlasResult>();

            foreach (AtlasGroup group in Groups)
            {
                List<string> paths = buildDependencies
                    .Where(path => IsEligible(path, group, assignedPaths))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToList();

                if (paths.Count < 2)
                {
                    DeleteAtlasIfPresent(group.Name);
                    results.Add(new AtlasResult(group.Name, paths, created: false));
                    continue;
                }

                UnityEngine.Object[] packables = paths
                    .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                    .Cast<UnityEngine.Object>()
                    .ToArray();
                if (packables.Length < 2)
                {
                    DeleteAtlasIfPresent(group.Name);
                    results.Add(new AtlasResult(group.Name, paths, created: false));
                    continue;
                }

                SpriteAtlas atlas = GetOrCreateAtlas(group.Name);
                UnityEngine.Object[] oldPackables = SpriteAtlasExtensions.GetPackables(atlas);
                if (oldPackables.Length > 0)
                {
                    SpriteAtlasExtensions.Remove(atlas, oldPackables);
                }

                SpriteAtlasExtensions.Add(atlas, packables);
                ConfigureAtlas(atlas, group.PointFilter);
                EditorUtility.SetDirty(atlas);

                foreach (string path in paths)
                {
                    assignedPaths.Add(path);
                }

                results.Add(new AtlasResult(group.Name, paths, created: true));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport(results, buildDependencies);
            Debug.Log(
                $"[MobileSpriteAtlasOrganizer] Created or updated " +
                $"{results.Count(result => result.Created)} atlases. Report: {ReportPath}");
        }

        private static bool IsEligible(
            string assetPath,
            AtlasGroup group,
            ISet<string> assignedPaths)
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith("Assets/_Project/", StringComparison.Ordinal)
                || assignedPaths.Contains(path)
                || !group.Matches(path)
                || IsExcluded(path))
            {
                return false;
            }

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .ToArray();
            if (sprites.Length == 0)
            {
                return false;
            }

            return sprites.All(sprite => sprite.rect.width <= 1024f && sprite.rect.height <= 1024f);
        }

        private static bool IsExcluded(string assetPath)
        {
            string path = assetPath.ToLowerInvariant();
            return path.Contains("/background/")
                || path.Contains("/backgrounds/")
                || path.Contains("preview")
                || path.Contains("contact_sheet")
                || path.Contains("layout_preview")
                || path.Contains("_source")
                || path.EndsWith(".psd", StringComparison.Ordinal);
        }

        private static HashSet<string> GetBuildDependencies()
        {
            var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
            {
                foreach (string dependency in AssetDatabase.GetDependencies(scene.path, true))
                {
                    dependencies.Add(dependency.Replace('\\', '/'));
                }
            }

            return dependencies;
        }

        private static SpriteAtlas GetOrCreateAtlas(string name)
        {
            string path = $"{AtlasDirectory}/{name}.spriteatlas";
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas != null)
            {
                return atlas;
            }

            atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, path);
            return atlas;
        }

        private static void ConfigureAtlas(SpriteAtlas atlas, bool pointFilter)
        {
            SpriteAtlasPackingSettings packing = SpriteAtlasExtensions.GetPackingSettings(atlas);
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.padding = 4;
            SpriteAtlasExtensions.SetPackingSettings(atlas, packing);

            SpriteAtlasTextureSettings texture = SpriteAtlasExtensions.GetTextureSettings(atlas);
            texture.readable = false;
            texture.generateMipMaps = false;
            texture.sRGB = true;
            texture.filterMode = pointFilter ? FilterMode.Point : FilterMode.Bilinear;
            SpriteAtlasExtensions.SetTextureSettings(atlas, texture);

            var android = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 2048,
                format = TextureImporterFormat.ETC2_RGBA8,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
                crunchedCompression = false
            };
            SpriteAtlasExtensions.SetPlatformSettings(atlas, android);
            SpriteAtlasExtensions.SetIncludeInBuild(atlas, true);
        }

        private static void DeleteAtlasIfPresent(string name)
        {
            string path = $"{AtlasDirectory}/{name}.spriteatlas";
            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void WriteReport(
            IReadOnlyCollection<AtlasResult> results,
            IReadOnlyCollection<string> buildDependencies)
        {
            var assigned = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (AtlasResult result in results.Where(result => result.Created))
            {
                foreach (string path in result.AssetPaths)
                {
                    if (!assigned.TryGetValue(path, out List<string> atlases))
                    {
                        atlases = new List<string>();
                        assigned.Add(path, atlases);
                    }

                    atlases.Add(result.Name);
                }
            }

            List<string> buildSpritePaths = buildDependencies
                .Where(path => path.StartsWith("Assets/_Project/", StringComparison.Ordinal))
                .Where(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any())
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            List<string> unassigned = buildSpritePaths
                .Where(path => !assigned.ContainsKey(path))
                .ToList();

            var report = new StringBuilder();
            report.AppendLine("# Sprite Atlas Audit After");
            report.AppendLine();
            report.AppendLine(
                "Atlases are generated from sprite dependencies of enabled build scenes. " +
                "Large backgrounds, previews, PSD sources, and textures over 1024 px per sprite are excluded.");
            report.AppendLine();
            report.AppendLine("| Atlas | Created | Source textures | Lifecycle |");
            report.AppendLine("|---|---:|---:|---|");
            foreach (AtlasResult result in results)
            {
                report.AppendLine(
                    $"| {result.Name} | {(result.Created ? "Yes" : "No")} | " +
                    $"{result.AssetPaths.Count} | {DescribeLifecycle(result.Name)} |");
            }

            report.AppendLine();
            report.AppendLine($"- Atlas count: {results.Count(result => result.Created)}");
            report.AppendLine($"- Unique source textures: {assigned.Count}");
            report.AppendLine($"- Build sprite source textures: {buildSpritePaths.Count}");
            report.AppendLine($"- Source textures not atlased: {unassigned.Count}");
            report.AppendLine(
                $"- Duplicate source textures across atlases: " +
                $"{assigned.Count(pair => pair.Value.Count > 1)}");
            report.AppendLine(
                $"- Large backgrounds included in atlases: " +
                $"{assigned.Keys.Count(path => IsExcluded(path))}");
            report.AppendLine("- Max atlas size: 2048");
            report.AppendLine("- Android format: ETC2 RGBA8");
            report.AppendLine("- Rotation: Off");
            report.AppendLine("- Tight packing: Off");
            report.AppendLine("- Mipmaps: Off");
            report.AppendLine();
            report.AppendLine("## Assets");
            report.AppendLine();
            foreach (AtlasResult result in results.Where(result => result.Created))
            {
                report.AppendLine($"### {result.Name}");
                report.AppendLine();
                foreach (string path in result.AssetPaths)
                {
                    report.AppendLine($"- `{path}`");
                }

                report.AppendLine();
            }

            report.AppendLine("## Not Atlased");
            report.AppendLine();
            foreach (string path in unassigned)
            {
                report.AppendLine($"- `{path}` - {GetExclusionReason(path)}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? AtlasDirectory);
            File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ReportPath);
        }

        private static string GetExclusionReason(string assetPath)
        {
            if (IsExcluded(assetPath))
            {
                return "large/background/preview/source asset excluded by policy";
            }

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .ToArray();
            if (sprites.Any(sprite => sprite.rect.width > 1024f || sprite.rect.height > 1024f))
            {
                return "sprite dimensions exceed 1024 px";
            }

            if (!Groups.Any(group => group.Matches(assetPath)))
            {
                return "no matching lifecycle group";
            }

            return "group has fewer than two packable sprites";
        }

        private static string DescribeLifecycle(string atlasName)
        {
            if (atlasName.StartsWith("UI_", StringComparison.Ordinal))
            {
                return atlasName.Substring(3).Replace('_', ' ');
            }

            return atlasName.Substring("Gameplay_".Length).Replace('_', ' ');
        }

        private static bool ContainsAny(string value, params string[] fragments)
        {
            return fragments.Any(fragment =>
                value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private sealed class AtlasGroup
        {
            public readonly string Name;
            public readonly bool PointFilter;
            public readonly Func<string, bool> Matches;

            public AtlasGroup(string name, bool pointFilter, Func<string, bool> matches)
            {
                Name = name;
                PointFilter = pointFilter;
                Matches = matches;
            }
        }

        private sealed class AtlasResult
        {
            public readonly string Name;
            public readonly List<string> AssetPaths;
            public readonly bool Created;

            public AtlasResult(string name, List<string> assetPaths, bool created)
            {
                Name = name;
                AssetPaths = assetPaths;
                Created = created;
            }
        }
    }
}
#endif
