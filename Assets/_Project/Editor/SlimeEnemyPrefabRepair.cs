using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace _Project.Editor
{
    [InitializeOnLoad]
    public static class SlimeEnemyPrefabRepair
    {
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemies/Enemy.prefab";
        private const string SlimeAsepritePath = "Assets/_Project/Prefabs/Enemies/Slime/Slime 1.aseprite";
        private const string SlimeControllerPath = "Assets/_Project/Prefabs/Enemies/Slime/Slime1.controller";

        private static readonly string[] RequiredStates =
        {
            "Idle",
            "Walk",
            "Run",
            "Attack",
            "Hurt",
            "Death"
        };

        static SlimeEnemyPrefabRepair()
        {
            EditorApplication.delayCall -= RepairIfNeeded;
            EditorApplication.delayCall += RepairIfNeeded;
        }

        [MenuItem("Tools/True Gate/Repair Slime Enemy Prefab")]
        private static void RepairFromMenu()
        {
            Repair(true);
        }

        public static void RepairFromCommandLine()
        {
            Repair(true);
        }

        internal static void RepairFromAssetPostprocessor(string[] importedAssets)
        {
            if (importedAssets == null)
            {
                return;
            }

            if (importedAssets.Contains(SlimeAsepritePath)
                || importedAssets.Contains(EnemyPrefabPath)
                || importedAssets.Contains(SlimeControllerPath))
            {
                EditorApplication.delayCall -= RepairIfNeeded;
                EditorApplication.delayCall += RepairIfNeeded;
            }
        }

        private static void RepairIfNeeded()
        {
            Repair(false);
        }

        private static void Repair(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SlimeAsepritePath).OfType<Sprite>().ToArray();
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SlimeAsepritePath).OfType<AnimationClip>().ToArray();

            if (sprites.Length == 0 && clips.Length == 0)
            {
                return;
            }

            bool changed = false;
            AnimatorController controller = LoadOrCreateController();

            if (controller != null)
            {
                changed |= EnsureAnimatorStates(controller, clips);
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

            try
            {
                Transform visual = prefabRoot.transform.Find("Visual");

                if (visual == null)
                {
                    GameObject visualObject = new GameObject("Visual");
                    visual = visualObject.transform;
                    visual.SetParent(prefabRoot.transform, false);
                    visual.localPosition = Vector3.zero;
                    visual.localRotation = Quaternion.identity;
                    visual.localScale = Vector3.one * 2.2f;
                    changed = true;
                }

                SpriteRenderer rootRenderer = prefabRoot.GetComponent<SpriteRenderer>();

                if (rootRenderer != null)
                {
                    UnityEngine.Object.DestroyImmediate(rootRenderer, true);
                    changed = true;
                }

                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();

                if (renderer == null)
                {
                    renderer = visual.gameObject.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 1;
                    changed = true;
                }

                Sprite defaultSprite = FindSprite(sprites, "Frame_0") ?? sprites.FirstOrDefault();

                if (defaultSprite != null && (force || renderer.sprite != defaultSprite))
                {
                    renderer.sprite = defaultSprite;
                    changed = true;
                }

                Animator animator = visual.GetComponent<Animator>();

                if (animator == null)
                {
                    animator = visual.gameObject.AddComponent<Animator>();
                    changed = true;
                }

                if (controller != null && (force || animator.runtimeAnimatorController != controller))
                {
                    animator.runtimeAnimatorController = controller;
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Repaired Enemy prefab to use Slime1 animation assets.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static AnimatorController LoadOrCreateController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(SlimeControllerPath);

            if (controller != null)
            {
                return controller;
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(SlimeControllerPath);
            AssetDatabase.ImportAsset(SlimeControllerPath);
            return controller;
        }

        private static bool EnsureAnimatorStates(AnimatorController controller, AnimationClip[] clips)
        {
            bool changed = false;

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
                changed = true;
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimationClip fallbackClip = clips.FirstOrDefault();

            foreach (string stateName in RequiredStates)
            {
                AnimatorState state = stateMachine.states
                    .Select(childState => childState.state)
                    .FirstOrDefault(existingState => existingState.name == stateName);

                if (state == null)
                {
                    state = stateMachine.AddState(stateName);
                    changed = true;
                }

                AnimationClip clip = FindClip(clips, stateName) ?? fallbackClip;

                if (clip != null && state.motion != clip)
                {
                    state.motion = clip;
                    changed = true;
                }

                if (stateName == "Idle" && stateMachine.defaultState != state)
                {
                    stateMachine.defaultState = state;
                    changed = true;
                }
            }

            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                childState.state.transitions = Array.Empty<AnimatorStateTransition>();
            }

            return changed;
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
    }

    internal sealed class SlimeEnemyAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            SlimeEnemyPrefabRepair.RepairFromAssetPostprocessor(importedAssets);
        }
    }
}
