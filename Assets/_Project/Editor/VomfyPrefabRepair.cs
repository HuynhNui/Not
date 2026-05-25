using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace _Project.Editor
{
    [InitializeOnLoad]
    internal static class VomfyPrefabRepair
    {
        private const string VomfyPrefabPath = "Assets/_Project/Prefabs/Enemies/Vomfy.prefab";
        private const string VomfyAsepritePath = "Assets/_Project/Prefabs/Enemies/Vomfy/VomfyAll/VomyfyreForGame.aseprite";
        private const string VomfyControllerPath = "Assets/_Project/Prefabs/Enemies/Vomfy/VomfyAll/GameManager.controller";

        private static readonly string[] RequiredStates =
        {
            "Idle",
            "Hop",
            "Attackaction",
            "ow"
        };

        static VomfyPrefabRepair()
        {
            EditorApplication.delayCall -= RepairIfNeeded;
            EditorApplication.delayCall += RepairIfNeeded;
        }

        [MenuItem("Tools/True Gate/Repair Vomfy Prefab")]
        private static void RepairFromMenu()
        {
            Repair(true);
        }

        internal static void RepairFromAssetPostprocessor(string[] importedAssets)
        {
            if (importedAssets == null)
            {
                return;
            }

            if (importedAssets.Contains(VomfyAsepritePath)
                || importedAssets.Contains(VomfyPrefabPath)
                || importedAssets.Contains(VomfyControllerPath))
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

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(VomfyAsepritePath).OfType<Sprite>().ToArray();
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(VomfyAsepritePath).OfType<AnimationClip>().ToArray();

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

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(VomfyPrefabPath);

            try
            {
                SpriteRenderer renderer = prefabRoot.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(spriteRenderer => spriteRenderer.gameObject.name == "Visual")
                    ?? prefabRoot.GetComponentInChildren<SpriteRenderer>(true);

                Sprite defaultSprite = FindSprite(sprites, "Frame_0") ?? sprites.FirstOrDefault();

                if (renderer != null && defaultSprite != null && (force || renderer.sprite != defaultSprite))
                {
                    renderer.sprite = defaultSprite;
                    changed = true;
                }

                Animator animator = prefabRoot.GetComponentsInChildren<Animator>(true)
                    .FirstOrDefault(childAnimator => childAnimator.gameObject.name == "Visual")
                    ?? prefabRoot.GetComponentInChildren<Animator>(true);

                if (animator != null && controller != null && (force || animator.runtimeAnimatorController != controller))
                {
                    animator.runtimeAnimatorController = controller;
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, VomfyPrefabPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Repaired Vomfy prefab to use VomyfyreForGame.aseprite animation assets.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static AnimatorController LoadOrCreateController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(VomfyControllerPath);

            if (controller != null)
            {
                return controller;
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(VomfyControllerPath);
            AssetDatabase.ImportAsset(VomfyControllerPath);
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

                AnimationClip clip = FindClip(clips, stateName);

                if (clip == null && stateName == "ow")
                {
                    clip = clips.LastOrDefault();
                }

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

    internal sealed class VomfyAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            VomfyPrefabRepair.RepairFromAssetPostprocessor(importedAssets);
        }
    }
}
