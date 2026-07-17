#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using _Project.Cutscenes;
using _Project.Scripts.Core.GameLoop;
using _Project.Scripts.Gameplay.Dialogue;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.AudioSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using TrueGateAudioSystem = _Project.Scripts.Systems.AudioSystem.AudioSystem;
using RuntimeEnemySpawnerSystem = _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimeGateSystem = _Project.Scripts.Systems.GateSystem.GateSystem;
using RuntimeUISystem = _Project.Scripts.Systems.UISystem.UISystem;

namespace _Project.Editor
{
    public static class AudioSystemSetup
    {
        private const string AudioRoot = "Assets/Sound/";
        private const string MixerPath = "Assets/_Project/Audio/TrueGateAudioMixer.mixer";
        private const string CatalogPath = "Assets/_Project/Audio/AudioCatalog.asset";

        [MenuItem("Tools/True Gate/Audio/Rebuild Audio Setup")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Audio");
            AssetDatabase.Refresh();

            AudioMixer mixer = EnsureMixer(
                out AudioMixerGroup musicGroup,
                out AudioMixerGroup ambienceGroup,
                out AudioMixerGroup sfxGroup,
                out AudioMixerGroup uiGroup,
                out AudioMixerGroup dialogueGroup);

            AudioCatalog catalog = EnsureCatalog(
                mixer,
                musicGroup,
                ambienceGroup,
                sfxGroup,
                uiGroup,
                dialogueGroup);

            ConfigureImportSettings();
            WireScene(catalog);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"Audio setup rebuilt. Catalog clips={catalog.CountAssignedClips()}.");
        }

        private static AudioMixer EnsureMixer(
            out AudioMixerGroup musicGroup,
            out AudioMixerGroup ambienceGroup,
            out AudioMixerGroup sfxGroup,
            out AudioMixerGroup uiGroup,
            out AudioMixerGroup dialogueGroup)
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Type controllerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
            if (controllerType == null)
            {
                throw new InvalidOperationException("UnityEditor.Audio.AudioMixerController type was not found.");
            }

            object controller = mixer;
            if (mixer == null)
            {
                MethodInfo createMethod = controllerType.GetMethod(
                    "CreateMixerControllerAtPath",
                    BindingFlags.Public | BindingFlags.Static);
                controller = createMethod?.Invoke(null, new object[] { MixerPath });
                mixer = controller as AudioMixer;
            }

            if (mixer == null || controller == null)
            {
                throw new InvalidOperationException($"Failed to create or load audio mixer at {MixerPath}.");
            }

            string[] groupNames = { "Music", "Ambience", "SFX", "UI", "Dialogue" };
            for (int index = 0; index < groupNames.Length; index++)
            {
                if (!HasExactGroup(controller, mixer, groupNames[index]))
                {
                    CreateMixerGroup(controllerType, controller, mixer, groupNames[index]);
                }
            }

            musicGroup = FindExactGroup(controller, mixer, "Music");
            ambienceGroup = FindExactGroup(controller, mixer, "Ambience");
            sfxGroup = FindExactGroup(controller, mixer, "SFX");
            uiGroup = FindExactGroup(controller, mixer, "UI");
            dialogueGroup = FindExactGroup(controller, mixer, "Dialogue");
            ExposeMixerParameters(controllerType, controller, musicGroup, ambienceGroup, sfxGroup, uiGroup, dialogueGroup);
            EditorUtility.SetDirty(mixer);
            return mixer;
        }

        private static void ExposeMixerParameters(
            Type controllerType,
            object controller,
            AudioMixerGroup musicGroup,
            AudioMixerGroup ambienceGroup,
            AudioMixerGroup sfxGroup,
            AudioMixerGroup uiGroup,
            AudioMixerGroup dialogueGroup)
        {
            Type exposedType = Type.GetType("UnityEditor.Audio.ExposedAudioParameter, UnityEditor");
            if (exposedType == null)
            {
                throw new InvalidOperationException("UnityEditor.Audio.ExposedAudioParameter type was not found.");
            }

            Array exposed = Array.CreateInstance(exposedType, 5);
            SetExposed(exposedType, exposed, 0, musicGroup, TrueGateAudioSystem.MusicVolumeParameter);
            SetExposed(exposedType, exposed, 1, ambienceGroup, TrueGateAudioSystem.AmbienceVolumeParameter);
            SetExposed(exposedType, exposed, 2, sfxGroup, TrueGateAudioSystem.SfxVolumeParameter);
            SetExposed(exposedType, exposed, 3, uiGroup, TrueGateAudioSystem.UiVolumeParameter);
            SetExposed(exposedType, exposed, 4, dialogueGroup, TrueGateAudioSystem.DialogueVolumeParameter);
            controllerType
                .GetProperty("exposedParameters", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(controller, exposed);
        }

        private static AudioMixerGroup CreateMixerGroup(
            Type controllerType,
            object controller,
            AudioMixer mixer,
            string groupName)
        {
            Type groupType = Type.GetType("UnityEditor.Audio.AudioMixerGroupController, UnityEditor");
            if (groupType == null)
            {
                throw new InvalidOperationException("UnityEditor.Audio.AudioMixerGroupController type was not found.");
            }

            object group = Activator.CreateInstance(
                groupType,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                args: new object[] { mixer },
                culture: null);
            if (group == null)
            {
                throw new InvalidOperationException($"Failed to create mixer group {groupName}.");
            }

            ((UnityEngine.Object)group).name = groupName;
            groupType
                .GetMethod("PreallocateGUIDs", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(group, null);

            object masterGroup = controllerType
                .GetProperty("masterGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(controller);
            if (masterGroup == null)
            {
                throw new InvalidOperationException("Audio mixer master group was not found.");
            }

            PropertyInfo childrenProperty = groupType.GetProperty(
                "children",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Array existingChildren = childrenProperty?.GetValue(masterGroup) as Array;
            int childCount = existingChildren?.Length ?? 0;
            Array newChildren = Array.CreateInstance(groupType, childCount + 1);
            for (int index = 0; index < childCount; index++)
            {
                newChildren.SetValue(existingChildren.GetValue(index), index);
            }

            newChildren.SetValue(group, childCount);
            childrenProperty?.SetValue(masterGroup, newChildren);

            AssetDatabase.AddObjectToAsset((UnityEngine.Object)group, mixer);
            EditorUtility.SetDirty((UnityEngine.Object)group);
            EditorUtility.SetDirty((UnityEngine.Object)masterGroup);
            return (AudioMixerGroup)group;
        }

        private static void SetExposed(Type exposedType, Array exposed, int index, AudioMixerGroup group, string parameterName)
        {
            object exposedParameter = Activator.CreateInstance(exposedType);
            FieldInfo guidField = exposedType.GetField("guid", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo nameField = exposedType.GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            object guid = group
                .GetType()
                .GetMethod("GetGUIDForVolume", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(group, null);
            guidField?.SetValue(exposedParameter, guid);
            nameField?.SetValue(exposedParameter, parameterName);
            exposed.SetValue(exposedParameter, index);
        }

        private static AudioCatalog EnsureCatalog(
            AudioMixer mixer,
            AudioMixerGroup musicGroup,
            AudioMixerGroup ambienceGroup,
            AudioMixerGroup sfxGroup,
            AudioMixerGroup uiGroup,
            AudioMixerGroup dialogueGroup)
        {
            AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AudioCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            CueDefinition[] definitions =
            {
                Music(AudioCueId.BgmMainMenu, "Music/bgm_main_menu.ogg", musicGroup, 0.82f),
                Music(AudioCueId.BgmGameplayNormal, "Music/bgm_gameplay_normal.ogg", musicGroup, 0.82f),
                Music(AudioCueId.BgmGameplayPressure, "Music/bgm_gameplay_pressure.ogg", musicGroup, 0.84f),
                Music(AudioCueId.BgmStoryCutscene, "Music/bgm_story_cutscene.ogg", musicGroup, 0.78f),
                Music(AudioCueId.BgmEnding, "Music/bgm_ending.ogg", musicGroup, 0.82f),
                new CueDefinition(AudioCueId.AmbGameplayPlanet, AudioCueCategory.Ambience, new[] { Clip("Music/amb_gameplay_planet.ogg") }, ambienceGroup, 0.52f, Vector2.one, 0f, 1, true, 80),
                Sfx(AudioCueId.StingerMissionComplete, new[] { "Music/stinger_mission_complete.ogg" }, sfxGroup, 0.9f, Vector2.one, 0.15f, 1, 80),
                Ui(AudioCueId.UiConfirm, "UI/ui_confirm.wav", uiGroup, 0.82f, 0.02f, 2),
                Ui(AudioCueId.UiBack, "UI/ui_back.wav", uiGroup, 0.78f, 0.02f, 2),
                Ui(AudioCueId.UiInvalidLocked, "UI/ui_invalid_locked.wav", uiGroup, 0.85f, 0.08f, 1),
                Ui(AudioCueId.UiPanelOpen, "UI/ui_panel_open.wav", uiGroup, 0.82f, 0.03f, 1),
                Ui(AudioCueId.UiPanelClose, "UI/ui_panel_close.wav", uiGroup, 0.82f, 0.03f, 1),
                Ui(AudioCueId.UiUpgradePurchase, "UI/ui_upgrade_purchase.wav", uiGroup, 0.88f, 0.05f, 1),
                Ui(AudioCueId.UiFocus, "UI/ui_focus.wav", uiGroup, 0.62f, 0.02f, 2),
                Ui(AudioCueId.UiDialogueAdvance, "UI/ui_dialogue_advance.wav", uiGroup, 0.72f, 0.03f, 1),
                Sfx(AudioCueId.PlayerShot, new[] { "Combat/player_shot_01.ogg", "Combat/player_shot_02.ogg", "Combat/player_shot_03.ogg" }, sfxGroup, 0.62f, new Vector2(0.96f, 1.04f), 0.075f, 2, 20),
                Sfx(AudioCueId.BulletHitEnemy, new[] { "Combat/bullet_hit_enemy_01.ogg", "Combat/bullet_hit_enemy_02.ogg", "Combat/bullet_hit_enemy_03.ogg" }, sfxGroup, 0.55f, new Vector2(0.96f, 1.04f), 0.04f, 3, 15),
                Sfx(AudioCueId.ChomboomExplosion, new[] { "Combat/chomboom_explosion.ogg" }, sfxGroup, 0.95f, new Vector2(0.98f, 1.02f), 0f, 3, 85),
                Sfx(AudioCueId.GateFreezeActivate, new[] { "Combat/gate_freeze_activate.wav" }, sfxGroup, 0.82f, Vector2.one, 0.05f, 1, 70),
                Sfx(AudioCueId.RunStartDeploy, new[] { "Combat/run_start_deploy.ogg" }, sfxGroup, 0.9f, Vector2.one, 0.5f, 1, 80),
                Sfx(AudioCueId.SquadDefeated, new[] { "Combat/squad_defeated.ogg" }, sfxGroup, 0.9f, Vector2.one, 0.5f, 1, 90),
                Sfx(AudioCueId.SystemWarningMemoryGlitch, new[] { "Combat/system_warning_memory_glitch.wav" }, sfxGroup, 0.9f, Vector2.one, 0.5f, 1, 90),
                new CueDefinition(AudioCueId.DialogueTypeSystem, AudioCueCategory.Dialogue, new[] { Clip("Combat/dialogue_type_system.ogg") }, dialogueGroup, 0.7f, Vector2.one, 0.01f, 1, false, 75),
                new CueDefinition(AudioCueId.DialogueTypeUnit07, AudioCueCategory.Dialogue, new[] { Clip("Combat/dialogue_type_unit07.ogg") }, dialogueGroup, 0.72f, Vector2.one, 0.01f, 1, false, 75),
                Sfx(AudioCueId.FinalChoiceConfirm, new[] { "Combat/final_choice_confirm.ogg" }, sfxGroup, 0.95f, Vector2.one, 0.5f, 1, 95),
                Sfx(AudioCueId.CoreShutdown, new[] { "Combat/core_shutdown.ogg" }, sfxGroup, 0.96f, Vector2.one, 0.5f, 1, 100)
            };

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            serializedCatalog.FindProperty("audioMixer").objectReferenceValue = mixer;
            SerializedProperty cuesProperty = serializedCatalog.FindProperty("cues");
            cuesProperty.arraySize = definitions.Length;
            for (int index = 0; index < definitions.Length; index++)
            {
                ApplyCue(cuesProperty.GetArrayElementAtIndex(index), definitions[index]);
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static CueDefinition Music(AudioCueId id, string path, AudioMixerGroup group, float volume)
        {
            return new CueDefinition(id, AudioCueCategory.Music, new[] { Clip(path) }, group, volume, Vector2.one, 0f, 1, true, 100);
        }

        private static CueDefinition Ui(AudioCueId id, string path, AudioMixerGroup group, float volume, float interval, int voices)
        {
            return new CueDefinition(id, AudioCueCategory.Ui, new[] { Clip(path) }, group, volume, Vector2.one, interval, voices, false, 60);
        }

        private static CueDefinition Sfx(
            AudioCueId id,
            string[] paths,
            AudioMixerGroup group,
            float volume,
            Vector2 pitch,
            float interval,
            int voices,
            int priority)
        {
            AudioClip[] clips = new AudioClip[paths.Length];
            for (int index = 0; index < paths.Length; index++)
            {
                clips[index] = Clip(paths[index]);
            }

            return new CueDefinition(id, AudioCueCategory.Sfx, clips, group, volume, pitch, interval, voices, false, priority);
        }

        private static void ApplyCue(SerializedProperty cueProperty, CueDefinition definition)
        {
            cueProperty.FindPropertyRelative("id").enumValueIndex = (int)definition.Id;
            cueProperty.FindPropertyRelative("category").enumValueIndex = (int)definition.Category;
            SerializedProperty clipsProperty = cueProperty.FindPropertyRelative("clips");
            clipsProperty.arraySize = definition.Clips.Length;
            for (int index = 0; index < definition.Clips.Length; index++)
            {
                clipsProperty.GetArrayElementAtIndex(index).objectReferenceValue = definition.Clips[index];
            }

            cueProperty.FindPropertyRelative("mixerGroup").objectReferenceValue = definition.MixerGroup;
            cueProperty.FindPropertyRelative("volume").floatValue = definition.Volume;
            cueProperty.FindPropertyRelative("pitchRange").vector2Value = definition.PitchRange;
            cueProperty.FindPropertyRelative("minimumInterval").floatValue = definition.MinimumInterval;
            cueProperty.FindPropertyRelative("maximumSimultaneousVoices").intValue = definition.MaximumSimultaneousVoices;
            cueProperty.FindPropertyRelative("loop").boolValue = definition.Loop;
            cueProperty.FindPropertyRelative("priority").intValue = definition.Priority;
        }

        private static void ConfigureImportSettings()
        {
            string[] musicPaths =
            {
                "Music/bgm_main_menu.ogg",
                "Music/bgm_gameplay_normal.ogg",
                "Music/bgm_gameplay_pressure.ogg",
                "Music/bgm_story_cutscene.ogg",
                "Music/bgm_ending.ogg",
                "Music/amb_gameplay_planet.ogg"
            };
            for (int index = 0; index < musicPaths.Length; index++)
            {
                ConfigureAudioImporter(musicPaths[index], AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, false, true, 0.7f);
            }

            string[] shortPaths =
            {
                "UI/ui_confirm.wav",
                "UI/ui_back.wav",
                "UI/ui_invalid_locked.wav",
                "UI/ui_panel_open.wav",
                "UI/ui_panel_close.wav",
                "UI/ui_upgrade_purchase.wav",
                "UI/ui_focus.wav",
                "UI/ui_dialogue_advance.wav",
                "Combat/player_shot_01.ogg",
                "Combat/player_shot_02.ogg",
                "Combat/player_shot_03.ogg",
                "Combat/bullet_hit_enemy_01.ogg",
                "Combat/bullet_hit_enemy_02.ogg",
                "Combat/bullet_hit_enemy_03.ogg",
                "Combat/gate_freeze_activate.wav"
            };
            for (int index = 0; index < shortPaths.Length; index++)
            {
                ConfigureAudioImporter(shortPaths[index], AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM, true, false, 1f);
            }

            string[] oneShotPaths =
            {
                "Music/stinger_mission_complete.ogg",
                "Combat/chomboom_explosion.ogg",
                "Combat/run_start_deploy.ogg",
                "Combat/squad_defeated.ogg",
                "Combat/system_warning_memory_glitch.wav",
                "Combat/dialogue_type_system.ogg",
                "Combat/dialogue_type_unit07.ogg",
                "Combat/final_choice_confirm.ogg",
                "Combat/core_shutdown.ogg"
            };
            for (int index = 0; index < oneShotPaths.Length; index++)
            {
                ConfigureAudioImporter(oneShotPaths[index], AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis, true, false, 0.8f);
            }
        }

        private static void ConfigureAudioImporter(
            string path,
            AudioClipLoadType loadType,
            AudioCompressionFormat compression,
            bool preload,
            bool loadInBackground,
            float quality)
        {
            AudioImporter importer = AssetImporter.GetAtPath(AudioRoot + path) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Missing audio importer at {AudioRoot}{path}");
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = loadType;
            settings.compressionFormat = compression;
            settings.quality = quality;
            settings.preloadAudioData = preload;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = loadInBackground;
            importer.SaveAndReimport();
        }

        private static void WireScene(AudioCatalog catalog)
        {
            GameObject systemsRoot = GameObject.Find("Systems") ?? new GameObject("Systems");
            Transform audioTransform = systemsRoot.transform.Find("AudioSystem");
            GameObject audioObject = audioTransform != null ? audioTransform.gameObject : new GameObject("AudioSystem");
            audioObject.transform.SetParent(systemsRoot.transform, false);

            TrueGateAudioSystem audioComponent = audioObject.GetComponent<TrueGateAudioSystem>();
            if (audioComponent == null)
            {
                audioComponent = audioObject.AddComponent<TrueGateAudioSystem>();
            }

            AudioEventRouter router = audioObject.GetComponent<AudioEventRouter>();
            if (router == null)
            {
                router = audioObject.AddComponent<AudioEventRouter>();
            }

            GameManager gameManager = UnityEngine.Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            RuntimeUISystem uiSystem = UnityEngine.Object.FindAnyObjectByType<RuntimeUISystem>(FindObjectsInactive.Include);
            RuntimeEnemySpawnerSystem enemySpawner = UnityEngine.Object.FindAnyObjectByType<RuntimeEnemySpawnerSystem>(FindObjectsInactive.Include);
            RuntimeGateSystem gateSystem = UnityEngine.Object.FindAnyObjectByType<RuntimeGateSystem>(FindObjectsInactive.Include);
            PlayerController playerController = UnityEngine.Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            StoryCutsceneDirector storyDirector = UnityEngine.Object.FindAnyObjectByType<StoryCutsceneDirector>(FindObjectsInactive.Include);
            GameplayDialogueController gameplayDialogue = UnityEngine.Object.FindAnyObjectByType<GameplayDialogueController>(FindObjectsInactive.Include);

            SerializedObject audioObjectSerialized = new SerializedObject(audioComponent);
            audioObjectSerialized.FindProperty("catalog").objectReferenceValue = catalog;
            audioObjectSerialized.FindProperty("sfxSourceCount").intValue = 10;
            audioObjectSerialized.FindProperty("playMenuMusicOnStart").boolValue = true;
            audioObjectSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject routerObject = new SerializedObject(router);
            routerObject.FindProperty("audioSystem").objectReferenceValue = audioComponent;
            routerObject.FindProperty("gameManager").objectReferenceValue = gameManager;
            routerObject.FindProperty("uiSystem").objectReferenceValue = uiSystem;
            routerObject.FindProperty("enemySpawnerSystem").objectReferenceValue = enemySpawner;
            routerObject.FindProperty("gateSystem").objectReferenceValue = gateSystem;
            routerObject.FindProperty("playerController").objectReferenceValue = playerController;
            routerObject.FindProperty("storyCutsceneDirector").objectReferenceValue = storyDirector;
            routerObject.FindProperty("gameplayDialogueController").objectReferenceValue = gameplayDialogue;
            routerObject.FindProperty("pressureMusicStartSeconds").floatValue = 180f;
            routerObject.ApplyModifiedPropertiesWithoutUndo();

            if (gameManager != null)
            {
                SerializedObject gameManagerObject = new SerializedObject(gameManager);
                SerializedProperty audioRouterProperty = gameManagerObject.FindProperty("audioEventRouter");
                if (audioRouterProperty != null)
                {
                    audioRouterProperty.objectReferenceValue = router;
                    gameManagerObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(audioObject);
            if (gameManager != null)
            {
                EditorUtility.SetDirty(gameManager);
            }
        }

        private static bool HasExactGroup(object controller, AudioMixer mixer, string groupName)
        {
            AudioMixerGroup[] groups = GetAllGroups(controller, mixer);
            for (int index = 0; index < groups.Length; index++)
            {
                if (groups[index] != null && groups[index].name == groupName)
                {
                    return true;
                }
            }

            return false;
        }

        private static AudioMixerGroup FindExactGroup(object controller, AudioMixer mixer, string groupName)
        {
            AudioMixerGroup[] groups = GetAllGroups(controller, mixer);
            for (int index = 0; index < groups.Length; index++)
            {
                if (groups[index] != null && groups[index].name == groupName)
                {
                    return groups[index];
                }
            }

            throw new InvalidOperationException($"Missing mixer group {groupName}.");
        }

        private static AudioMixerGroup[] GetAllGroups(object controller, AudioMixer mixer)
        {
            MethodInfo getAllGroups = controller
                .GetType()
                .GetMethod("GetAllAudioGroupsSlow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            object result = getAllGroups?.Invoke(controller, null);
            if (result is System.Collections.IEnumerable enumerable)
            {
                var groups = new System.Collections.Generic.List<AudioMixerGroup>();
                foreach (object group in enumerable)
                {
                    if (group is AudioMixerGroup mixerGroup)
                    {
                        groups.Add(mixerGroup);
                    }
                }

                return groups.ToArray();
            }

            return mixer.FindMatchingGroups(string.Empty);
        }

        private static AudioClip Clip(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + path);
            if (clip == null)
            {
                throw new InvalidOperationException($"Missing audio clip at {AudioRoot}{path}");
            }

            return clip;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private readonly struct CueDefinition
        {
            public CueDefinition(
                AudioCueId id,
                AudioCueCategory category,
                AudioClip[] clips,
                AudioMixerGroup mixerGroup,
                float volume,
                Vector2 pitchRange,
                float minimumInterval,
                int maximumSimultaneousVoices,
                bool loop,
                int priority)
            {
                Id = id;
                Category = category;
                Clips = clips;
                MixerGroup = mixerGroup;
                Volume = volume;
                PitchRange = pitchRange;
                MinimumInterval = minimumInterval;
                MaximumSimultaneousVoices = maximumSimultaneousVoices;
                Loop = loop;
                Priority = priority;
            }

            public readonly AudioCueId Id;
            public readonly AudioCueCategory Category;
            public readonly AudioClip[] Clips;
            public readonly AudioMixerGroup MixerGroup;
            public readonly float Volume;
            public readonly Vector2 PitchRange;
            public readonly float MinimumInterval;
            public readonly int MaximumSimultaneousVoices;
            public readonly bool Loop;
            public readonly int Priority;
        }
    }
}
#endif
