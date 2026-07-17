using System;
using System.Linq;
using System.Reflection;
using _Project.Cutscenes;
using _Project.Scripts.Core.GameLoop;
using _Project.Scripts.Gameplay.Combat;
using _Project.Scripts.Gameplay.Dialogue;
using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Systems.AudioSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using TrueGateAudioSystem = _Project.Scripts.Systems.AudioSystem.AudioSystem;
using RuntimeEnemySpawnerSystem = _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimeUISystem = _Project.Scripts.Systems.UISystem.UISystem;

namespace _Project.Tests.Editor
{
    public sealed class AudioSystemAssetTests
    {
        private const string CatalogPath = "Assets/_Project/Audio/AudioCatalog.asset";
        private const string MixerPath = "Assets/_Project/Audio/TrueGateAudioMixer.mixer";

        [Test]
        public void Catalog_HasAllRequiredCuesAndThirtyAssignedClips()
        {
            AudioCatalog catalog = LoadCatalog();

            Assert.That(catalog.Cues.Count, Is.EqualTo(26));
            Assert.That(catalog.CountAssignedClips(), Is.EqualTo(30));

            var expectedCueIds = Enum.GetValues(typeof(AudioCueId))
                .Cast<AudioCueId>()
                .Where(id => id != AudioCueId.None)
                .ToArray();
            CollectionAssert.AreEquivalent(expectedCueIds, catalog.Cues.Select(cue => cue.Id));
            Assert.That(catalog.Cues.Select(cue => cue.Id).Distinct().Count(), Is.EqualTo(catalog.Cues.Count));
            Assert.That(catalog.Cues.All(cue => cue.HasClips), Is.True);
            Assert.That(catalog.Cues.All(cue => cue.MixerGroup != null), Is.True);
        }

        [Test]
        public void Catalog_UsesExpectedShotAndHitVariantCounts()
        {
            AudioCatalog catalog = LoadCatalog();

            Assert.That(catalog.TryGet(AudioCueId.PlayerShot, out AudioCueEntry shot), Is.True);
            Assert.That(shot.Clips, Has.Length.EqualTo(3));
            CollectionAssert.AreEquivalent(
                new[] { "player_shot_01", "player_shot_02", "player_shot_03" },
                shot.Clips.Select(clip => clip.name));

            Assert.That(catalog.TryGet(AudioCueId.BulletHitEnemy, out AudioCueEntry hit), Is.True);
            Assert.That(hit.Clips, Has.Length.EqualTo(3));
            CollectionAssert.AreEquivalent(
                new[] { "bullet_hit_enemy_01", "bullet_hit_enemy_02", "bullet_hit_enemy_03" },
                hit.Clips.Select(clip => clip.name));
        }

        [Test]
        public void Mixer_HasRequiredGroupsAndExposedVolumeParameters()
        {
            AudioMixer mixer = LoadMixer();
            string[] groups = mixer.FindMatchingGroups(string.Empty).Select(group => group.name).ToArray();

            CollectionAssert.IsSubsetOf(
                new[] { "Master", "Music", "Ambience", "SFX", "UI", "Dialogue" },
                groups);

            AssertParameterExists(mixer, TrueGateAudioSystem.MusicVolumeParameter);
            AssertParameterExists(mixer, TrueGateAudioSystem.AmbienceVolumeParameter);
            AssertParameterExists(mixer, TrueGateAudioSystem.SfxVolumeParameter);
            AssertParameterExists(mixer, TrueGateAudioSystem.UiVolumeParameter);
            AssertParameterExists(mixer, TrueGateAudioSystem.DialogueVolumeParameter);
        }

        [Test]
        public void ImportSettings_MatchRuntimeUsage()
        {
            AssertImporter("Assets/Sound/Music/bgm_gameplay_normal.ogg", AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, false, true);
            AssertImporter("Assets/Sound/Music/amb_gameplay_planet.ogg", AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, false, true);
            AssertImporter("Assets/Sound/UI/ui_confirm.wav", AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM, true, false);
            AssertImporter("Assets/Sound/Combat/player_shot_01.ogg", AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM, true, false);
            AssertImporter("Assets/Sound/Combat/chomboom_explosion.ogg", AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis, true, false);
        }

        [Test]
        public void Scene_HasWiredAudioSystemObject()
        {
            OpenMainScene();
            GameObject audioObject = GameObject.Find("Systems/AudioSystem");

            Assert.That(audioObject, Is.Not.Null, "Main scene should contain Systems/AudioSystem.");
            Assert.That(audioObject.GetComponent<TrueGateAudioSystem>(), Is.Not.Null);
            Assert.That(audioObject.GetComponent<AudioEventRouter>(), Is.Not.Null);
        }

        [Test]
        public void SceneAudioSystem_ReferencesCatalogAndRouter()
        {
            OpenMainScene();
            AudioCatalog catalog = LoadCatalog();
            GameObject audioObject = GameObject.Find("Systems/AudioSystem");
            TrueGateAudioSystem audioSystem = audioObject.GetComponent<TrueGateAudioSystem>();
            AudioEventRouter router = audioObject.GetComponent<AudioEventRouter>();

            SerializedObject serializedAudio = new SerializedObject(audioSystem);
            SerializedObject serializedRouter = new SerializedObject(router);

            Assert.That(serializedAudio.FindProperty("catalog").objectReferenceValue, Is.EqualTo(catalog));
            Assert.That(serializedAudio.FindProperty("sfxSourceCount").intValue, Is.EqualTo(10));
            Assert.That(serializedAudio.FindProperty("playMenuMusicOnStart").boolValue, Is.True);
            Assert.That(serializedRouter.FindProperty("audioSystem").objectReferenceValue, Is.EqualTo(audioSystem));
            Assert.That(serializedRouter.FindProperty("pressureMusicStartSeconds").floatValue, Is.EqualTo(180f).Within(0.001f));
        }

        [Test]
        public void GameplaySystems_ExposeAudioRouterEvents()
        {
            AssertEventExists<BulletSpawner>("VolleyFired");
            AssertEventExists<RuntimeEnemySpawnerSystem>("EnemyDamaged");
            AssertEventExists<RuntimeEnemySpawnerSystem>("ChomboomExploded");
            AssertEventExists<ChomboomController>("Exploded");
            AssertEventExists<GameplayDialogueController>("DialogueShown");
            AssertEventExists<StoryCutsceneDirector>("OnDialogueAdvanceRequested");
            AssertEventExists<StoryCutsceneDirector>("OnDialogueLineShown");
            AssertEventExists<StoryCutsceneDirector>("OnFinalChoiceSelected");
            AssertEventExists<RuntimeUISystem>("UiCueRequested");
            AssertEventExists<RuntimeUISystem>("MusicSettingChanged");
            AssertEventExists<RuntimeUISystem>("SfxSettingChanged");
            AssertEventExists<GameManager>("RunBecamePlayable");
            AssertEventExists<GameManager>("RunEnded");
            AssertEventExists<GameManager>("ReturnedToMenu");
        }

        private static AudioCatalog LoadCatalog()
        {
            AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, $"Missing audio catalog at {CatalogPath}.");
            return catalog;
        }

        private static AudioMixer LoadMixer()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.That(mixer, Is.Not.Null, $"Missing audio mixer at {MixerPath}.");
            return mixer;
        }

        private static void AssertParameterExists(AudioMixer mixer, string parameterName)
        {
            Assert.That(mixer.GetFloat(parameterName, out _), Is.True, $"Missing exposed mixer parameter {parameterName}.");
        }

        private static void AssertImporter(
            string path,
            AudioClipLoadType expectedLoadType,
            AudioCompressionFormat expectedCompression,
            bool expectedPreload,
            bool expectedLoadInBackground)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            Assert.That(importer, Is.Not.Null, $"Missing importer for {path}.");
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            Assert.That(settings.loadType, Is.EqualTo(expectedLoadType), path);
            Assert.That(settings.compressionFormat, Is.EqualTo(expectedCompression), path);
            Assert.That(settings.preloadAudioData, Is.EqualTo(expectedPreload), path);
            Assert.That(importer.loadInBackground, Is.EqualTo(expectedLoadInBackground), path);
        }

        private static void AssertEventExists<T>(string eventName)
        {
            EventInfo eventInfo = typeof(T).GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(eventInfo, Is.Not.Null, $"{typeof(T).Name} should expose event {eventName}.");
        }

        private static void OpenMainScene()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project" });
            string scenePath = sceneGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => path.EndsWith("Main.unity", StringComparison.OrdinalIgnoreCase))
                ?? sceneGuids.Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();

            Assert.That(scenePath, Is.Not.Null, "Could not find a project scene under Assets/_Project.");
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }
}
