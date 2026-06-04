using System;
using System.IO;
using System.Linq;
using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Systems.EnemySpawnerSystem;
using _Project.Scripts.Systems.PoolSystem;
using _Project.Scripts.Systems.UISystem;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Editor
{
    [InitializeOnLoad]
    public static class ChomboomPrefabRepair
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string ChomboomPrefabPath = "Assets/_Project/Prefabs/Enemies/Chomboom.prefab";
        private const string ChomboomFxPrefabPath = "Assets/_Project/Prefabs/Enemies/ChomboomBoomFx.prefab";
        private const string ChomAsepritePath = "Assets/_Project/Prefabs/Enemies/ChomBombs (full) v1.1/ChomBombs/ChomForGame.aseprite";
        private const string ChomboomControllerPath = "Assets/_Project/Prefabs/Enemies/ChomBombs (full) v1.1/ChomBombs/Chomboom.controller";
        private const string ChomboomFxControllerPath = "Assets/_Project/Prefabs/Enemies/ChomBombs (full) v1.1/ChomBombs/ChomboomBoomFx.controller";
        private const string HealthBarPrefabPath = "Assets/_Project/Prefabs/UI/WorldHealthBar_New.prefab";

        private static readonly DirectionalStateBinding[] ChomboomStates =
        {
            new DirectionalStateBinding("walk d", "walk"),
            new DirectionalStateBinding("walk s1", "walk"),
            new DirectionalStateBinding("walk s2", "walk"),
            new DirectionalStateBinding("hurt d", "hurt"),
            new DirectionalStateBinding("hurt s1", "hurt"),
            new DirectionalStateBinding("hurt s2", "hurt"),
            new DirectionalStateBinding("boom d", "boom"),
            new DirectionalStateBinding("boom s1", "boom"),
            new DirectionalStateBinding("boom s2", "boom")
        };

        static ChomboomPrefabRepair()
        {
            EditorApplication.delayCall -= RepairPrefabsIfNeeded;
            EditorApplication.delayCall += RepairPrefabsIfNeeded;
        }

        [MenuItem("Tools/True Gate/Repair Chomboom Prefabs")]
        private static void RepairPrefabsFromMenu()
        {
            RepairPrefabs(force: true);
        }

        [MenuItem("Tools/True Gate/Wire Chomboom In Main Scene")]
        private static void WireMainSceneFromMenu()
        {
            RepairPrefabs(force: true);
            WireMainScene();
        }

        public static void BuildAllFromCommandLine()
        {
            RepairPrefabs(force: true);
            WireMainScene();
        }

        internal static void RepairFromAssetPostprocessor(string[] importedAssets)
        {
            if (importedAssets == null)
            {
                return;
            }

            if (importedAssets.Contains(ChomAsepritePath)
                || importedAssets.Contains(ChomboomPrefabPath)
                || importedAssets.Contains(ChomboomFxPrefabPath)
                || importedAssets.Contains(ChomboomControllerPath)
                || importedAssets.Contains(ChomboomFxControllerPath))
            {
                EditorApplication.delayCall -= RepairPrefabsIfNeeded;
                EditorApplication.delayCall += RepairPrefabsIfNeeded;
            }
        }

        private static void RepairPrefabsIfNeeded()
        {
            RepairPrefabs(force: false);
        }

        private static void RepairPrefabs(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(ChomAsepritePath).OfType<Sprite>().ToArray();
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ChomAsepritePath).OfType<AnimationClip>().ToArray();

            if (sprites.Length == 0 && clips.Length == 0)
            {
                return;
            }

            EnsureDirectory(ChomboomPrefabPath);
            EnsureDirectory(ChomboomControllerPath);

            AnimatorController chomboomController = LoadOrCreateController(ChomboomControllerPath);
            AnimatorController fxController = LoadOrCreateController(ChomboomFxControllerPath);

            EnsureChomboomController(chomboomController, clips);
            EnsureFxController(fxController, clips);
            CreateOrRepairFxPrefab(sprites, fxController, force);
            CreateOrRepairChomboomPrefab(sprites, chomboomController, force);
            AssetDatabase.SaveAssets();
        }

        private static void CreateOrRepairChomboomPrefab(Sprite[] sprites, AnimatorController controller, bool force)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChomboomPrefabPath);
            bool loadedExisting = existingPrefab != null;
            GameObject prefabRoot = loadedExisting
                ? PrefabUtility.LoadPrefabContents(ChomboomPrefabPath)
                : new GameObject("Chomboom");

            try
            {
                prefabRoot.name = "Chomboom";
                prefabRoot.layer = 8;
                prefabRoot.tag = "Untagged";
                prefabRoot.transform.localPosition = new Vector3(0f, 5.57f, 0f);
                prefabRoot.transform.localRotation = Quaternion.identity;
                prefabRoot.transform.localScale = Vector3.one;

                Rigidbody2D body = EnsureComponent<Rigidbody2D>(prefabRoot);
                body.gravityScale = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;

                BoxCollider2D collider = EnsureComponent<BoxCollider2D>(prefabRoot);
                collider.isTrigger = true;
                collider.offset = new Vector2(0f, 0.35f);
                collider.size = new Vector2(0.78f, 0.78f);

                Transform visual = EnsureChild(prefabRoot.transform, "Visual");
                visual.gameObject.layer = 8;
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one * 2.6f;

                SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(visual.gameObject);
                renderer.sortingOrder = 1;
                Sprite defaultSprite = FindSprite(sprites, "Frame_0") ?? sprites.FirstOrDefault();

                if (defaultSprite != null && (force || renderer.sprite == null))
                {
                    renderer.sprite = defaultSprite;
                }

                Animator animator = EnsureComponent<Animator>(visual.gameObject);
                animator.runtimeAnimatorController = controller;

                EnemyController enemyController = EnsureComponent<EnemyController>(prefabRoot);
                ConfigureEnemyController(enemyController);

                ChomboomController chomboomController = EnsureComponent<ChomboomController>(prefabRoot);
                ConfigureChomboomController(chomboomController, enemyController, animator);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, ChomboomPrefabPath);
            }
            finally
            {
                if (loadedExisting)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(prefabRoot);
                }
            }
        }

        private static void CreateOrRepairFxPrefab(Sprite[] sprites, AnimatorController controller, bool force)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChomboomFxPrefabPath);
            bool loadedExisting = existingPrefab != null;
            GameObject prefabRoot = loadedExisting
                ? PrefabUtility.LoadPrefabContents(ChomboomFxPrefabPath)
                : new GameObject("ChomboomBoomFx");

            try
            {
                prefabRoot.name = "ChomboomBoomFx";
                prefabRoot.layer = 8;
                prefabRoot.transform.localPosition = Vector3.zero;
                prefabRoot.transform.localRotation = Quaternion.identity;
                prefabRoot.transform.localScale = Vector3.one;

                Transform visual = EnsureChild(prefabRoot.transform, "Visual");
                visual.gameObject.layer = 8;
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one * 5f;

                SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(visual.gameObject);
                renderer.sortingOrder = 3;
                Sprite defaultSprite = FindSprite(sprites, "Frame_51") ?? sprites.LastOrDefault();

                if (defaultSprite != null && (force || renderer.sprite == null))
                {
                    renderer.sprite = defaultSprite;
                }

                Animator animator = EnsureComponent<Animator>(visual.gameObject);
                animator.runtimeAnimatorController = controller;

                ChomboomBoomFx boomFx = EnsureComponent<ChomboomBoomFx>(prefabRoot);
                ConfigureBoomFx(boomFx, animator);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, ChomboomFxPrefabPath);
            }
            finally
            {
                if (loadedExisting)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(prefabRoot);
                }
            }
        }

        private static void ConfigureEnemyController(EnemyController enemyController)
        {
            GameObject healthBarObject = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
            WorldHealthBarView healthBar = healthBarObject != null
                ? healthBarObject.GetComponent<WorldHealthBarView>()
                : null;

            SerializedObject serializedObject = new SerializedObject(enemyController);
            serializedObject.FindProperty("unitData").objectReferenceValue = null;
            serializedObject.FindProperty("currentHealth").floatValue = 3f;
            serializedObject.FindProperty("scoreValue").intValue = 1;
            serializedObject.FindProperty("fallbackMoveSpeed").floatValue = 3f;
            serializedObject.FindProperty("fallbackMaxHealth").floatValue = 3f;
            serializedObject.FindProperty("fallbackContactDamage").floatValue = 0f;
            serializedObject.FindProperty("coinReward").intValue = 1;
            serializedObject.FindProperty("destroyOnPlayerHit").boolValue = false;
            serializedObject.FindProperty("despawnImmediatelyOnDeath").boolValue = false;
            serializedObject.FindProperty("movementMode").enumValueIndex = (int)EnemyMovementMode.ChaseTarget;
            serializedObject.FindProperty("clampInsideCameraWidth").boolValue = true;
            serializedObject.FindProperty("horizontalPadding").floatValue = 0.25f;
            serializedObject.FindProperty("despawnBelowCameraOffset").floatValue = 1.5f;
            serializedObject.FindProperty("healthBarPrefab").objectReferenceValue = healthBar;
            serializedObject.FindProperty("healthBarAnchor").objectReferenceValue = null;
            serializedObject.FindProperty("healthBarOffset").vector3Value = new Vector3(0f, 0.95f, 0f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureChomboomController(
            ChomboomController chomboomController,
            EnemyController enemyController,
            Animator animator)
        {
            GameObject fxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChomboomFxPrefabPath);
            ChomboomBoomFx boomFx = fxPrefab != null ? fxPrefab.GetComponent<ChomboomBoomFx>() : null;

            SerializedObject serializedObject = new SerializedObject(chomboomController);
            serializedObject.FindProperty("enemyController").objectReferenceValue = enemyController;
            serializedObject.FindProperty("animator").objectReferenceValue = animator;
            serializedObject.FindProperty("boomFxPrefab").objectReferenceValue = boomFx;
            serializedObject.FindProperty("triggerRadius").floatValue = 0.45f;
            serializedObject.FindProperty("armingDuration").floatValue = 2f;
            serializedObject.FindProperty("hurtDuration").floatValue = 0.4f;
            serializedObject.FindProperty("explosionDamage").floatValue = 2f;
            serializedObject.FindProperty("explosionRadius").floatValue = 1.75f;
            serializedObject.FindProperty("walkDownStateName").stringValue = "walk d";
            serializedObject.FindProperty("walkLeftStateName").stringValue = "walk s1";
            serializedObject.FindProperty("walkRightStateName").stringValue = "walk s2";
            serializedObject.FindProperty("hurtDownStateName").stringValue = "hurt d";
            serializedObject.FindProperty("hurtLeftStateName").stringValue = "hurt s1";
            serializedObject.FindProperty("hurtRightStateName").stringValue = "hurt s2";
            serializedObject.FindProperty("boomDownStateName").stringValue = "boom d";
            serializedObject.FindProperty("boomLeftStateName").stringValue = "boom s1";
            serializedObject.FindProperty("boomRightStateName").stringValue = "boom s2";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBoomFx(ChomboomBoomFx boomFx, Animator animator)
        {
            SerializedObject serializedObject = new SerializedObject(boomFx);
            serializedObject.FindProperty("animator").objectReferenceValue = animator;
            serializedObject.FindProperty("damage").floatValue = 2f;
            serializedObject.FindProperty("radius").floatValue = 1.75f;
            serializedObject.FindProperty("lifetime").floatValue = 0.6f;
            serializedObject.FindProperty("boomStateName").stringValue = "boom fx";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireMainScene()
        {
            GameObject chomboomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChomboomPrefabPath);
            GameObject boomFxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChomboomFxPrefabPath);

            if (chomboomPrefab == null || boomFxPrefab == null)
            {
                Debug.LogWarning("Chomboom prefabs are missing. Repair prefabs before wiring the scene.");
                return;
            }

            Scene activeScene = EditorSceneManager.GetActiveScene();
            Scene scene = activeScene.path == MainScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            EnemySpawnerSystem spawnerSystem = UnityEngine.Object.FindAnyObjectByType<EnemySpawnerSystem>();
            PoolSystem poolSystem = UnityEngine.Object.FindAnyObjectByType<PoolSystem>();

            if (spawnerSystem == null || poolSystem == null)
            {
                Debug.LogWarning("Could not find EnemySpawnerSystem or PoolSystem in Main scene.");
                return;
            }

            EnemyController chomboomEnemy = chomboomPrefab.GetComponent<EnemyController>();
            bool changed = false;
            changed |= UpsertSpawnEntry(spawnerSystem, chomboomEnemy, 1f, 0f);
            changed |= UpsertPoolDefinition(poolSystem, "Chomboom", chomboomPrefab, 8);
            changed |= UpsertPoolDefinition(poolSystem, "ChomboomBoomFx", boomFxPrefab, 8);

            if (changed)
            {
                EditorUtility.SetDirty(spawnerSystem);
                EditorUtility.SetDirty(poolSystem);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static bool UpsertSpawnEntry(
            EnemySpawnerSystem spawnerSystem,
            EnemyController prefab,
            float spawnWeight,
            float unlockAfterSeconds)
        {
            if (spawnerSystem == null || prefab == null)
            {
                return false;
            }

            SerializedObject serializedObject = new SerializedObject(spawnerSystem);
            SerializedProperty entries = serializedObject.FindProperty("spawnEntries");
            bool changed = false;

            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                SerializedProperty prefabProperty = entry.FindPropertyRelative("prefab");

                if (prefabProperty.objectReferenceValue != prefab)
                {
                    continue;
                }

                changed |= SetFloatIfDifferent(entry.FindPropertyRelative("spawnWeight"), spawnWeight);
                changed |= SetFloatIfDifferent(entry.FindPropertyRelative("unlockAfterSeconds"), unlockAfterSeconds);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                return changed;
            }

            entries.arraySize++;
            SerializedProperty newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            newEntry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            newEntry.FindPropertyRelative("spawnWeight").floatValue = spawnWeight;
            newEntry.FindPropertyRelative("unlockAfterSeconds").floatValue = unlockAfterSeconds;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static bool UpsertPoolDefinition(
            PoolSystem poolSystem,
            string poolId,
            GameObject prefab,
            int initialSize)
        {
            if (poolSystem == null || prefab == null)
            {
                return false;
            }

            SerializedObject serializedObject = new SerializedObject(poolSystem);
            SerializedProperty definitions = serializedObject.FindProperty("poolDefinitions");
            bool changed = false;

            for (int index = 0; index < definitions.arraySize; index++)
            {
                SerializedProperty definition = definitions.GetArrayElementAtIndex(index);

                if (definition.FindPropertyRelative("poolId").stringValue != poolId)
                {
                    continue;
                }

                changed |= SetObjectIfDifferent(definition.FindPropertyRelative("prefab"), prefab);
                changed |= SetIntIfDifferent(definition.FindPropertyRelative("initialSize"), initialSize);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                return changed;
            }

            definitions.arraySize++;
            SerializedProperty newDefinition = definitions.GetArrayElementAtIndex(definitions.arraySize - 1);
            newDefinition.FindPropertyRelative("poolId").stringValue = poolId;
            newDefinition.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            newDefinition.FindPropertyRelative("initialSize").intValue = initialSize;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static bool SetFloatIfDifferent(SerializedProperty property, float value)
        {
            if (Mathf.Approximately(property.floatValue, value))
            {
                return false;
            }

            property.floatValue = value;
            return true;
        }

        private static bool SetIntIfDifferent(SerializedProperty property, int value)
        {
            if (property.intValue == value)
            {
                return false;
            }

            property.intValue = value;
            return true;
        }

        private static bool SetObjectIfDifferent(SerializedProperty property, UnityEngine.Object value)
        {
            if (property.objectReferenceValue == value)
            {
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        private static AnimatorController LoadOrCreateController(string path)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            if (controller != null)
            {
                return controller;
            }

            EnsureDirectory(path);
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AssetDatabase.ImportAsset(path);
            return controller;
        }

        private static void EnsureChomboomController(AnimatorController controller, AnimationClip[] clips)
        {
            if (controller == null)
            {
                return;
            }

            EnsureBaseLayer(controller);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            for (int index = 0; index < ChomboomStates.Length; index++)
            {
                DirectionalStateBinding binding = ChomboomStates[index];
                AnimationClip clip = FindClip(clips, binding.StateName)
                    ?? FindClip(clips, binding.FallbackClipName)
                    ?? clips.FirstOrDefault();
                AnimatorState state = EnsureState(stateMachine, binding.StateName, clip);

                if (index == 0)
                {
                    stateMachine.defaultState = state;
                }
            }

            RemoveTransitions(stateMachine);
        }

        private static void EnsureFxController(AnimatorController controller, AnimationClip[] clips)
        {
            if (controller == null)
            {
                return;
            }

            EnsureBaseLayer(controller);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimationClip clip = FindClip(clips, "boom fx") ?? clips.LastOrDefault();
            AnimatorState state = EnsureState(stateMachine, "boom fx", clip);
            stateMachine.defaultState = state;
            RemoveTransitions(stateMachine);
        }

        private static void EnsureBaseLayer(AnimatorController controller)
        {
            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }
        }

        private static AnimatorState EnsureState(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip)
        {
            AnimatorState state = stateMachine.states
                .Select(childState => childState.state)
                .FirstOrDefault(existingState => existingState.name == stateName);

            if (state == null)
            {
                state = stateMachine.AddState(stateName);
            }

            if (clip != null)
            {
                state.motion = clip;
            }

            return state;
        }

        private static void RemoveTransitions(AnimatorStateMachine stateMachine)
        {
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                childState.state.transitions = Array.Empty<AnimatorStateTransition>();
            }
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string name)
        {
            return clips.FirstOrDefault(clip => string.Equals(clip.name, name, StringComparison.OrdinalIgnoreCase))
                ?? clips.FirstOrDefault(clip => clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static Sprite FindSprite(Sprite[] sprites, string name)
        {
            return sprites.FirstOrDefault(sprite => string.Equals(sprite.name, name, StringComparison.OrdinalIgnoreCase))
                ?? sprites.FirstOrDefault(sprite => sprite.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();

            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);

            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void EnsureDirectory(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);

            if (string.IsNullOrEmpty(directory) || AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        private readonly struct DirectionalStateBinding
        {
            public readonly string StateName;
            public readonly string FallbackClipName;

            public DirectionalStateBinding(string stateName, string fallbackClipName)
            {
                StateName = stateName;
                FallbackClipName = fallbackClipName;
            }
        }
    }

    internal sealed class ChomboomAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            ChomboomPrefabRepair.RepairFromAssetPostprocessor(importedAssets);
        }
    }
}
