using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace _Project.Tests.PlayMode
{
    public sealed class SafeAreaHierarchyPlayModeTests
    {
        private const string SafeAreaFitterTypeName =
            "_Project.Scripts.Systems.UISystem.SafeAreaFitter";
        private HashSet<int> _existingObjectIds;
        private Scene _mainScene;
        private bool _loadedByTest;

        [UnityTest]
        public IEnumerator MainScene_ImportantUiUsesSingleSafeAreaRoots()
        {
            _existingObjectIds = new HashSet<int>(
                Object.FindObjectsByType<GameObject>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Select(gameObject => gameObject.GetInstanceID()));

            _mainScene = SceneManager.GetSceneByName("Main");
            _loadedByTest = !_mainScene.IsValid() || !_mainScene.isLoaded;
            if (_loadedByTest)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
                while (!load.isDone)
                {
                    yield return null;
                }

                _mainScene = SceneManager.GetSceneByName("Main");
            }

            yield return null;

            MonoBehaviour[] fitters = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
                .Where(IsSafeAreaFitter)
                .ToArray();

            Assert.That(fitters, Has.Length.EqualTo(8));
            Assert.That(
                fitters.All(fitter => fitter
                    .GetComponentsInParent<MonoBehaviour>(true)
                    .Count(IsSafeAreaFitter) == 1),
                Is.True,
                "SafeAreaFitter roots must not be nested.");
            Assert.That(
                fitters.All(fitter =>
                {
                    Canvas canvas = fitter.GetComponentInParent<Canvas>();
                    return canvas != null && canvas.renderMode != RenderMode.WorldSpace;
                }),
                Is.True,
                "World-space canvases must not use screen safe-area anchors.");

            AssertDescendant(
                "GameCanvas/UIRoot/SafeAreaRoot/MainMenuPanel/MenuSafeAreaRoot",
                "GameCanvas/UIRoot/SafeAreaRoot/MainMenuPanel/MenuSafeAreaRoot/StartRunButton");
            AssertDescendant(
                "GameCanvas/UIRoot/SafeAreaRoot/GameplayHUDPanel",
                "GameCanvas/UIRoot/SafeAreaRoot/GameplayHUDPanel/HudContentRoot");
            AssertDescendant(
                "GameCanvas/UIRoot/SafeAreaRoot/GameplayDialogueLayer",
                "GameCanvas/UIRoot/SafeAreaRoot/GameplayDialogueLayer/GameplaySpeechBubble");
            AssertDescendant(
                "GameCanvas/UIRoot/SafeAreaRoot/SettingsPanel/SettingsSafeAreaRoot",
                "GameCanvas/UIRoot/SafeAreaRoot/SettingsPanel/SettingsSafeAreaRoot/MainPanel");
            AssertDescendant(
                "GameCanvas/UIRoot/SafeAreaRoot/PausePanel/PanelContainer/PauseFrame/PauseSafeAreaRoot",
                "GameCanvas/UIRoot/SafeAreaRoot/PausePanel/PanelContainer/PauseFrame/PauseSafeAreaRoot/ContentRoot");
            AssertDescendant(
                "GameCanvas/UIRoot/SafeAreaRoot/TutorialOverlayPanel/TutorialSafeAreaRoot",
                "GameCanvas/UIRoot/SafeAreaRoot/TutorialOverlayPanel/TutorialSafeAreaRoot/SkipButton");
            AssertDescendant(
                "GameCanvas/UIRoot/SafeAreaRoot/MissionLogPanel/MissionSafeAreaRoot",
                "GameCanvas/UIRoot/SafeAreaRoot/MissionLogPanel/MissionSafeAreaRoot/PanelCard");

            AssertOutsideSafeArea(
                "GameCanvas/UIRoot/SafeAreaRoot/MainMenuPanel/BackgroundLayer");
            AssertOutsideSafeArea(
                "GameCanvas/UIRoot/SafeAreaRoot/SettingsPanel/Background");
            AssertOutsideSafeArea(
                "GameCanvas/UIRoot/SafeAreaRoot/TutorialOverlayPanel/DimBackground");
            AssertOutsideSafeArea(
                "GameCanvas/UIRoot/SafeAreaRoot/MissionLogPanel/BackgroundLayer");

            int missingComponentCount = _mainScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Count(transform => transform.GetComponents<Component>().Any(component => component == null));

            Assert.That(missingComponentCount, Is.Zero);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;

            GameObject[] newPersistentRoots = Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(gameObject => gameObject.transform.parent == null
                    && gameObject.scene.name == "DontDestroyOnLoad"
                    && _existingObjectIds != null
                    && !_existingObjectIds.Contains(gameObject.GetInstanceID()))
                .ToArray();

            foreach (GameObject gameObject in newPersistentRoots)
            {
                gameObject.SetActive(false);
            }

            if (_loadedByTest && _mainScene.IsValid() && _mainScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_mainScene);
                while (unload != null && !unload.isDone)
                {
                    yield return null;
                }
            }

            foreach (GameObject gameObject in newPersistentRoots)
            {
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            }

            yield return null;
            _existingObjectIds = null;
            _mainScene = default;
            _loadedByTest = false;
        }

        private static void AssertDescendant(string safeRootPath, string controlPath)
        {
            Transform safeRoot = FindByPath(safeRootPath);
            Transform control = FindByPath(controlPath);

            Assert.That(safeRoot, Is.Not.Null, safeRootPath);
            Assert.That(
                safeRoot.GetComponents<MonoBehaviour>().Any(IsSafeAreaFitter),
                Is.True,
                safeRootPath);
            Assert.That(control, Is.Not.Null, controlPath);
            Assert.That(control.IsChildOf(safeRoot), Is.True, controlPath);
        }

        private static void AssertOutsideSafeArea(string path)
        {
            Transform target = FindByPath(path);
            Assert.That(target, Is.Not.Null, path);
            Assert.That(
                target.GetComponentsInParent<MonoBehaviour>(true).Any(IsSafeAreaFitter),
                Is.False,
                path);
        }

        private static bool IsSafeAreaFitter(MonoBehaviour component)
        {
            return component != null && component.GetType().FullName == SafeAreaFitterTypeName;
        }

        private static Transform FindByPath(string path)
        {
            string[] parts = path.Split('/');
            Scene mainScene = SceneManager.GetSceneByName("Main");
            GameObject root = mainScene
                .GetRootGameObjects()
                .FirstOrDefault(candidate => candidate.name == parts[0]);

            return root == null
                ? null
                : parts.Length == 1
                    ? root.transform
                    : root.transform.Find(string.Join("/", parts.Skip(1)));
        }
    }
}
