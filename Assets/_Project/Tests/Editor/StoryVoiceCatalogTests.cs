using System.Linq;
using _Project.Editor.Voice;
using _Project.Scripts.Systems.AudioSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class StoryVoiceCatalogTests
    {
        [Test]
        public void Builder_CreatesCompleteValidStoryCatalog()
        {
            VoiceLineCatalog catalog = StoryVoiceCatalogBuilder.Build();
            StoryVoiceCatalogValidationResult result = StoryVoiceCatalogBuilder.Validate(catalog);

            Assert.That(result.IsValid, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.StoryRecordCount, Is.EqualTo(211));
            Assert.That(result.CatalogEntryCount, Is.EqualTo(205));
            Assert.That(result.SkipRecordCount, Is.EqualTo(6));
            Assert.That(catalog.Entries.All(entry => entry.Clip != null), Is.True);
        }

        [Test]
        public void Catalog_LooksUpByLineKeyAndVoiceId()
        {
            VoiceLineCatalog catalog = LoadCatalog();

            Assert.That(catalog.TryGet("CS_01_BootSequence", 0, out VoiceLineCatalogEntry byLine), Is.True);
            Assert.That(byLine.VoiceId, Is.EqualTo("VO_CS_CS_01_BOOTSEQUENCE_000"));
            Assert.That(catalog.TryGetByVoiceId(byLine.VoiceId, out VoiceLineCatalogEntry byId), Is.True);
            Assert.That(byId, Is.SameAs(byLine));
        }

        [Test]
        public void Catalog_RejectsDuplicateLineKey()
        {
            VoiceLineCatalog catalog = ScriptableObject.CreateInstance<VoiceLineCatalog>();
            AudioClip clip = AudioClip.Create("duplicate-test", 128, 1, 24000, false);
            try
            {
                catalog.SetEntries(new[]
                {
                    CreateEntry("VO_TEST_001", "TEST", 0, clip),
                    CreateEntry("VO_TEST_002", "TEST", 0, clip)
                });

                Assert.That(catalog.TryRebuildLookup(out string error), Is.False);
                StringAssert.Contains("Duplicate voice line key", error);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void Validation_DetectsMissingClip()
        {
            VoiceLineCatalog catalog = ScriptableObject.CreateInstance<VoiceLineCatalog>();
            try
            {
                catalog.SetEntries(new[]
                {
                    CreateEntry(
                        "VO_CS_CS_01_BOOTSEQUENCE_000",
                        "CS_01_BootSequence",
                        0,
                        null)
                });

                StoryVoiceCatalogValidationResult result = StoryVoiceCatalogBuilder.Validate(catalog);

                Assert.That(result.IsValid, Is.False);
                Assert.That(
                    result.Errors.Any(error => error.Contains("has no AudioClip")),
                    Is.True,
                    string.Join("\n", result.Errors));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void SkipRecords_DoNotRequireAudioClips()
        {
            VoiceLineCatalog catalog = LoadCatalog();

            Assert.That(catalog.SkippedLines.Count, Is.EqualTo(6));
            Assert.That(catalog.Entries.Count, Is.EqualTo(205));
            Assert.That(catalog.IsSkipped("CS_03_EnemyDoesNotCharge", 19), Is.True);
            Assert.That(catalog.TryGet("CS_03_EnemyDoesNotCharge", 19, out _), Is.False);
        }

        [Test]
        public void VoiceImporters_UseStoryVoiceSettingsOnly()
        {
            string[] clipGuids = AssetDatabase.FindAssets(
                "t:AudioClip",
                new[] { "Assets/_Project/Audio/Voice" });

            Assert.That(clipGuids, Has.Length.EqualTo(205));
            foreach (string guid in clipGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                Assert.That(importer, Is.Not.Null, path);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                Assert.That(importer.forceToMono, Is.True, path);
                Assert.That(importer.loadInBackground, Is.True, path);
                Assert.That(settings.preloadAudioData, Is.True, path);
                Assert.That(settings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad), path);
                Assert.That(settings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM), path);
                Assert.That(
                    settings.sampleRateSetting,
                    Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate),
                    path);
            }
        }

        private static VoiceLineCatalog LoadCatalog()
        {
            VoiceLineCatalog catalog =
                AssetDatabase.LoadAssetAtPath<VoiceLineCatalog>(StoryVoiceCatalogBuilder.CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            return catalog;
        }

        private static VoiceLineCatalogEntry CreateEntry(
            string voiceId,
            string sourceId,
            int lineIndex,
            AudioClip clip)
        {
            return new VoiceLineCatalogEntry(
                voiceId,
                "STORY",
                sourceId,
                lineIndex,
                "SYSTEM",
                "cold",
                clip);
        }
    }
}
