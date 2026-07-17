using System;
using System.IO;
using _Project.Scripts.Systems.MissionSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.UISystem;
using NUnit.Framework;
using TMPro;
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
            Assert.That(rowPrefab.transform.Find("ClaimButton")?.GetComponent<Button>(), Is.Not.Null);
            Assert.That(rowPrefab.transform.Find("RewardCoin")?.GetComponent<Image>(), Is.Not.Null);
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
        public void ShowMissionLog_KeepsBadgeVisibleWhileRewardIsUnclaimed()
        {
            UISystem uiSystem = UnityEngine.Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            GameObject badge = FindSceneObjectByPath(MissionButtonPath + "/MissionBadge");

            _saveService.Data.completedMissionIds.Add("boot_finish_tutorial");
            _saveService.Data.activeMissionId = "boot_first_loop";
            _saveService.Data.missionNotificationUnread = true;
            _saveService.CommitMissionState();
            _missionSystem.InitializeFromSave();

            uiSystem.Init();
            uiSystem.ShowMissionLog();

            Assert.That(_saveService.Data.missionNotificationUnread, Is.False);
            Assert.That(_missionSystem.HasAnyUnclaimedMissionRewards, Is.True);
            Assert.That(badge.activeSelf, Is.True);
        }

        [Test]
        public void RefreshMissionLog_RendersFullMissionListWithLockedRows()
        {
            SaveData data = _saveService.Data;
            data.completedMissionIds.Clear();
            data.completedMissionIds.Add("boot_finish_tutorial");
            data.completedMissionIds.Add("boot_survive_30");
            data.completedMissionIds.Add("boot_10_kills_run");
            data.completedMissionIds.Add("boot_first_loop");
            data.activeMissionId = "boot_purchase_upgrade";
            data.activeMissionProgress = 0.5f;
            _saveService.CommitMissionState();
            _missionSystem.InitializeFromSave();

            GameObject missionPanel = FindSceneObjectByPath(MissionLogPanelPath);
            MissionLogPanelUI missionLogPanelUI = missionPanel.GetComponent<MissionLogPanelUI>();
            Transform content = missionPanel.transform.Find("PanelCard/MissionScrollView/Content");

            missionLogPanelUI.Refresh(_missionSystem, data);

            string visibleText = GetVisibleMissionRowText(content);
            int visibleRowCount = CountVisibleMissionRows(content);

            Assert.That(visibleRowCount, Is.EqualTo(_catalog.Missions.Count));
            Assert.That(visibleText, Does.Contain("FINISH TUTORIAL"));
            Assert.That(visibleText, Does.Contain("COMPLETE FIRST LOOP"));
            Assert.That(visibleText, Does.Contain("PURCHASE ANY UPGRADE"));
            Assert.That(visibleText, Does.Contain("05 / BOOT - UPGRADE"));
            Assert.That(visibleText, Does.Contain("06 / BOOT - GATE"));
            Assert.That(visibleText, Does.Contain("UNLOCK: COMPLETE 05 / BOOT"));
            Assert.That(visibleText, Does.Contain("ENCRYPTED OBJECTIVE"));
            Assert.That(visibleText, Does.Not.Contain("PASS THROUGH 3 GATES"));
            Assert.That(visibleText, Does.Not.Contain("COMPLETE 3 LOOPS"));
            Assert.That(visibleText, Does.Not.Contain("DEFEAT 100 ENEMIES IN ONE RUN"));
        }

        [Test]
        public void ClaimButton_GrantsCompletedMissionRewardAndRefreshesRow()
        {
            SaveData data = _saveService.Data;
            data.completedMissionIds.Clear();
            data.grantedMissionRewardIds.Clear();
            data.completedMissionIds.Add("boot_finish_tutorial");
            data.activeMissionId = "boot_first_loop";
            data.activeMissionProgress = 0f;
            _saveService.CommitMissionState();
            _missionSystem.InitializeFromSave();

            GameObject missionPanel = FindSceneObjectByPath(MissionLogPanelPath);
            MissionLogPanelUI missionLogPanelUI = missionPanel.GetComponent<MissionLogPanelUI>();
            Transform content = missionPanel.transform.Find("PanelCard/MissionScrollView/Content");

            missionLogPanelUI.Refresh(_missionSystem, data);
            string beforeText = GetVisibleMissionRowText(content);
            Button claimButton = FindVisibleClaimButton(content);

            Assert.That(beforeText, Does.Contain("CLAIM"));
            Assert.That(beforeText, Does.Contain("+1000"));
            Assert.That(claimButton, Is.Not.Null);

            claimButton.onClick.Invoke();

            string afterText = GetVisibleMissionRowText(content);
            Assert.That(_saveService.Data.walletCoins, Is.EqualTo(1000));
            Assert.That(_saveService.Data.lifetimeCoinsEarned, Is.EqualTo(1000));
            Assert.That(_saveService.Data.grantedMissionRewardIds, Does.Contain("boot_finish_tutorial"));
            Assert.That(afterText, Does.Not.Contain("CLAIM"));
        }

        [Test]
        public void ClaimButton_SyncsMissionSystemBeforeClaim()
        {
            SaveData data = _saveService.Data;
            data.completedMissionIds.Clear();
            data.grantedMissionRewardIds.Clear();
            data.completedMissionIds.Add("boot_finish_tutorial");
            data.completedMissionIds.Add("boot_survive_30");
            data.completedMissionIds.Add("boot_10_kills_run");
            data.completedMissionIds.Add("boot_first_loop");
            data.activeMissionId = "boot_survive_30";
            data.activeMissionProgress = 0f;
            _saveService.CommitMissionState();

            GameObject missionPanel = FindSceneObjectByPath(MissionLogPanelPath);
            MissionLogPanelUI missionLogPanelUI = missionPanel.GetComponent<MissionLogPanelUI>();
            Transform content = missionPanel.transform.Find("PanelCard/MissionScrollView/Content");

            missionLogPanelUI.Refresh(_missionSystem, data);
            Button claimButton = FindVisibleClaimButton(content);

            Assert.That(claimButton, Is.Not.Null);
            Assert.That(_missionSystem.IsMissionRewardClaimable("boot_finish_tutorial"), Is.False);

            claimButton.onClick.Invoke();

            Assert.That(_saveService.Data.walletCoins, Is.EqualTo(1000));
            Assert.That(_saveService.Data.grantedMissionRewardIds, Does.Contain("boot_finish_tutorial"));
            Assert.That(_missionSystem.IsMissionRewardClaimed("boot_finish_tutorial"), Is.True);
        }

        [Test]
        public void ResetPlayerProgression_RefreshesOpenMissionLog()
        {
            UISystem uiSystem = UnityEngine.Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            Assert.That(uiSystem, Is.Not.Null);
            uiSystem.Init();

            SaveData data = _saveService.Data;
            data.completedMissionIds.Clear();
            data.grantedMissionRewardIds.Clear();
            data.completedMissionIds.Add("boot_finish_tutorial");
            data.completedMissionIds.Add("boot_survive_30");
            data.completedMissionIds.Add("boot_10_kills_run");
            data.completedMissionIds.Add("boot_first_loop");
            data.grantedMissionRewardIds.Add("boot_finish_tutorial");
            data.activeMissionId = "boot_purchase_upgrade";
            data.activeMissionProgress = 0.5f;
            data.missionNotificationUnread = false;
            _saveService.CommitMissionState();
            _missionSystem.InitializeFromSave();

            GameObject missionPanel = FindSceneObjectByPath(MissionLogPanelPath);
            MissionLogPanelUI missionLogPanelUI = missionPanel.GetComponent<MissionLogPanelUI>();
            Transform content = missionPanel.transform.Find("PanelCard/MissionScrollView/Content");
            TextMeshProUGUI summaryText = missionPanel.transform.Find("PanelCard/SummaryText")
                ?.GetComponent<TextMeshProUGUI>();

            uiSystem.ShowMissionLog();
            Assert.That(summaryText.text, Does.Contain("04 / 47 COMPLETE"));
            Assert.That(GetVisibleMissionRowText(content), Does.Contain("PURCHASE ANY UPGRADE"));

            _saveService.ResetPlayerProgression();

            Assert.That(_missionSystem.ActiveMissionId, Is.EqualTo(SaveData.FirstMissionId));
            Assert.That(_saveService.Data.completedMissionIds, Is.Empty);
            Assert.That(summaryText.text, Does.Contain("00 / 47 COMPLETE"));

            string visibleText = GetVisibleMissionRowText(content);
            Assert.That(visibleText, Does.Contain("FINISH TUTORIAL"));
            Assert.That(visibleText, Does.Contain("01 / BOOT - TUTORIAL"));
            Assert.That(visibleText, Does.Contain("02 / BOOT - SURVIVAL"));
            Assert.That(visibleText, Does.Contain("UNLOCK: COMPLETE 01 / BOOT"));
            Assert.That(visibleText, Does.Contain("ENCRYPTED OBJECTIVE"));
            Assert.That(visibleText, Does.Not.Contain("PURCHASE ANY UPGRADE"));
            Assert.That(missionLogPanelUI, Is.Not.Null);
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

        private static int CountVisibleMissionRows(Transform content)
        {
            int count = 0;
            for (int index = 0; index < content.childCount; index++)
            {
                Transform child = content.GetChild(index);
                if (child.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetVisibleMissionRowText(Transform content)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int index = 0; index < content.childCount; index++)
            {
                Transform child = content.GetChild(index);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                TextMeshProUGUI[] texts = child.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: false);
                for (int textIndex = 0; textIndex < texts.Length; textIndex++)
                {
                    builder.AppendLine(texts[textIndex].text);
                }
            }

            return builder.ToString();
        }

        private static Button FindVisibleClaimButton(Transform content)
        {
            for (int index = 0; index < content.childCount; index++)
            {
                Transform child = content.GetChild(index);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                Transform claimTransform = child.Find("ClaimButton");
                Button button = claimTransform != null
                    ? claimTransform.GetComponent<Button>()
                    : null;
                if (button != null && button.gameObject.activeSelf)
                {
                    return button;
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
