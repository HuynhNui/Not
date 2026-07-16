using System;
using System.IO;
using _Project.Scripts.Systems.MissionSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.UISystem;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Tests.Editor
{
    public sealed class MissionUiPhaseHTests
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string MissionButtonPath =
            "GameCanvas/UIRoot/SafeAreaRoot/MainMenuPanel/MenuSafeAreaRoot/MissionButton";
        private const string MissionLogPanelPath =
            "GameCanvas/UIRoot/SafeAreaRoot/MissionLogPanel";
        private const string MissionRowPrefabPath =
            "Assets/_Project/Prefabs/UI/MissionRow.prefab";
        private const string MissionLogPanelPrefabPath =
            "Assets/_Project/Prefabs/UI/MissionLogPanel.prefab";

        private string _saveDirectoryPath;
        private SaveService _saveService;
        private MissionCatalog _catalog;
        private MissionSystem _missionSystem;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            _saveDirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-mission-ui-test-{Guid.NewGuid():N}");
            _saveService = SaveService.CreateForTests(_saveDirectoryPath);
            SaveService.SetInstanceForTests(_saveService);
            _saveService.EnsureLoaded();
            _catalog = MissionCatalog.CreateRuntimeDefault();
            _missionSystem = new MissionSystem(_catalog, _saveService);
            _missionSystem.InitializeFromSave();
        }

        [TearDown]
        public void TearDown()
        {
            _missionSystem?.Dispose();
            if (_catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(_catalog);
            }

            SaveService.SetInstanceForTests(null);
            Time.timeScale = 1f;

            if (Directory.Exists(_saveDirectoryPath))
            {
                Directory.Delete(_saveDirectoryPath, recursive: true);
            }
        }

        [Test]
        public void Scene_HasExactlyOneMissionButtonAndNoDailyRewardObject()
        {
            GameObject missionButton = FindSceneObjectByPath(MissionButtonPath);

            Assert.That(missionButton, Is.Not.Null);
            Assert.That(CountSceneObjectsNamed("MissionButton"), Is.EqualTo(1));
            Assert.That(CountObjectsContaining("DailyReward"), Is.EqualTo(0));
            Assert.That(CountObjectsContaining("DailyCoin"), Is.EqualTo(0));
            Assert.That(CountObjectsContaining("DailyDisabled"), Is.EqualTo(0));
            Assert.That(missionButton.GetComponent<Button>().interactable, Is.True);
            Assert.That(missionButton.GetComponent<Button>().onClick.GetPersistentEventCount(), Is.EqualTo(0));
        }

        [Test]
        public void Scene_MissionButtonKeepsFormerDailyRewardRect()
        {
            RectTransform rectTransform = FindSceneObjectByPath(MissionButtonPath).GetComponent<RectTransform>();

            Assert.That(rectTransform.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(rectTransform.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(rectTransform.anchoredPosition, Is.EqualTo(new Vector2(-86f, 796f)));
            Assert.That(rectTransform.sizeDelta, Is.EqualTo(new Vector2(138f, 120f)));
            Assert.That(rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(rectTransform.GetSiblingIndex(), Is.EqualTo(4));
        }

        [Test]
        public void Scene_HasExactlyOneMissionLogPanelAndRequiredPrefabs()
        {
            GameObject panel = FindSceneObjectByPath(MissionLogPanelPath);
            MissionLogPanelUI panelPrefab = AssetDatabase.LoadAssetAtPath<MissionLogPanelUI>(MissionLogPanelPrefabPath);
            MissionRowUI rowPrefab = AssetDatabase.LoadAssetAtPath<MissionRowUI>(MissionRowPrefabPath);

            Assert.That(panel, Is.Not.Null);
            Assert.That(CountSceneObjectsNamed("MissionLogPanel"), Is.EqualTo(1));
            Assert.That(panel.activeSelf, Is.False);
            Assert.That(panel.GetComponent<MissionLogPanelUI>(), Is.Not.Null);
            Assert.That(panel.transform.Find("PanelCard/Header/BackButton")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(panel.transform.Find("PanelCard/MissionScrollView/Content/MissionRowTemplate"), Is.Not.Null);
            Assert.That(panelPrefab, Is.Not.Null);
            Assert.That(rowPrefab, Is.Not.Null);
        }

        [Test]
        public void ShowMissionLog_ClearsUnreadBadgeAndBackReturnsMainMenu()
        {
            UISystem uiSystem = UnityEngine.Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            GameObject missionPanel = FindSceneObjectByPath(MissionLogPanelPath);
            GameObject mainMenuPanel = FindSceneObjectByPath("GameCanvas/UIRoot/SafeAreaRoot/MainMenuPanel");
            GameObject badge = FindSceneObjectByPath(MissionButtonPath + "/MissionBadge");
            Button backButton = missionPanel.transform.Find("PanelCard/Header/BackButton").GetComponent<Button>();

            _saveService.Data.missionNotificationUnread = true;
            _saveService.CommitMissionState();

            uiSystem.Init();
            uiSystem.ShowMissionLog();

            Assert.That(uiSystem.CurrentScreen, Is.EqualTo(UISystem.UIScreen.Mission));
            Assert.That(_saveService.Data.missionNotificationUnread, Is.False);
            Assert.That(missionPanel.activeSelf, Is.True);
            Assert.That(mainMenuPanel.activeSelf, Is.False);
            Assert.That(badge.activeSelf, Is.False);

            backButton.onClick.Invoke();

            Assert.That(uiSystem.CurrentScreen, Is.EqualTo(UISystem.UIScreen.MainMenu));
            Assert.That(mainMenuPanel.activeSelf, Is.True);
            Assert.That(missionPanel.activeSelf, Is.False);
        }

        [Test]
        public void ExistingPrimaryNavigationStillHasRequiredBindings()
        {
            UISystem uiSystem = UnityEngine.Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);

            Assert.That(uiSystem, Is.Not.Null);
            Assert.That(FindSceneObjectNamed("StartRunButton")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(FindSceneObjectNamed("UPDATEButton")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(FindSceneObjectNamed("SETTINGButton")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(FindSceneObjectByPath("GameCanvas/UIRoot/SafeAreaRoot/GameplayHUDPanel"), Is.Not.Null);
            Assert.That(FindSceneObjectByPath("GameCanvas/UIRoot/SafeAreaRoot/GameOverPanel"), Is.Not.Null);
        }

        private static int CountSceneObjectsNamed(string objectName)
        {
            int count = 0;
            foreach (GameObject gameObject in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (IsMainSceneObject(gameObject) && gameObject.name == objectName)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountObjectsContaining(string namePart)
        {
            int count = 0;
            foreach (GameObject gameObject in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (IsMainSceneObject(gameObject) && gameObject.name.Contains(namePart))
                {
                    count++;
                }
            }

            return count;
        }

        private static GameObject FindSceneObjectByPath(string path)
        {
            string[] parts = path.Split('/');
            GameObject current = GameObject.Find(parts[0]);
            for (int index = 1; index < parts.Length && current != null; index++)
            {
                Transform child = current.transform.Find(parts[index]);
                current = child != null ? child.gameObject : null;
            }

            return current;
        }

        private static GameObject FindSceneObjectNamed(string objectName)
        {
            foreach (GameObject gameObject in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (IsMainSceneObject(gameObject) && gameObject.name == objectName)
                {
                    return gameObject;
                }
            }

            return null;
        }

        private static bool IsMainSceneObject(GameObject gameObject)
        {
            return gameObject != null
                && gameObject.scene.IsValid()
                && gameObject.scene.path == MainScenePath;
        }
    }
}
