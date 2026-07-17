using System.Collections.Generic;
using _Project.Scripts.Systems.MissionSystem;
using _Project.Scripts.Systems.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RuntimeMissionSystem = _Project.Scripts.Systems.MissionSystem.MissionSystem;

namespace _Project.Scripts.Systems.UISystem
{
    public sealed class MissionLogPanelUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI activeMissionText;
        [SerializeField] private TextMeshProUGUI summaryText;

        [Header("List")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private MissionRowUI rowPrefab;

        [Header("Buttons")]
        [SerializeField] private Button backButton;

        private readonly List<MissionRowUI> _rows = new List<MissionRowUI>();
        private int _focusRowIndex = -1;
        private RuntimeMissionSystem _missionSystem;
        private SaveData _saveData;

        public Button BackButton => backButton;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }
        }

        public void Refresh(RuntimeMissionSystem missionSystem, SaveData saveData)
        {
            if (missionSystem == null || missionSystem.Missions == null || saveData == null)
            {
                SetText(activeMissionText, "MAIN OBJECTIVE");
                SetText(summaryText, "NO DATA");
                return;
            }

            IReadOnlyList<MissionDefinition> missions = missionSystem.Missions;
            int completedCount = saveData.completedMissionIds != null
                ? saveData.completedMissionIds.Count
                : 0;
            _missionSystem = missionSystem;
            _saveData = saveData;
            _focusRowIndex = -1;

            List<MissionDisplayEntry> displayEntries = BuildDisplayEntries(missions, missionSystem, saveData);
            EnsureRowCount(displayEntries.Count);

            for (int index = 0; index < displayEntries.Count; index++)
            {
                MissionDisplayEntry entry = displayEntries[index];
                if (entry.State == MissionRowState.CompletedUnclaimed)
                {
                    _focusRowIndex = index;
                }
                else if (entry.State == MissionRowState.Active && _focusRowIndex < 0)
                {
                    _focusRowIndex = index;
                }

                float progressValue = entry.State == MissionRowState.Active && _missionSystem != null
                    ? _missionSystem.EvaluateMission(entry.Mission.Id).ProgressValue
                    : 0f;

                _rows[index].Configure(
                    entry.Mission,
                    entry.MissionNumber,
                    entry.State,
                    progressValue,
                    entry.Mission.TargetValue,
                    HandleClaimRequested,
                    entry.ClassificationText,
                    entry.UnlockRequirementText);
            }

            for (int index = displayEntries.Count; index < _rows.Count; index++)
            {
                _rows[index].gameObject.SetActive(false);
            }

            SetText(activeMissionText, "MAIN OBJECTIVE");
            SetText(summaryText, $"COMPLETE ALL DIRECTIVES\n{completedCount:00} / {missions.Count:00} COMPLETE");
        }

        public void ScrollToActiveMission()
        {
            if (scrollRect == null || contentRoot == null || _focusRowIndex < 0 || _rows.Count <= 1)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float normalized = 1f - Mathf.Clamp01((float)_focusRowIndex / Mathf.Max(1, _rows.Count - 1));
            scrollRect.verticalNormalizedPosition = normalized;
        }

        private void EnsureRowCount(int count)
        {
            if (rowPrefab == null || contentRoot == null)
            {
                return;
            }

            while (_rows.Count < count)
            {
                MissionRowUI row = Instantiate(rowPrefab, contentRoot);
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }
        }

        private static List<MissionDisplayEntry> BuildDisplayEntries(
            IReadOnlyList<MissionDefinition> missions,
            RuntimeMissionSystem missionSystem,
            SaveData saveData)
        {
            List<MissionDisplayEntry> entries = new List<MissionDisplayEntry>();
            if (missions == null || missions.Count == 0)
            {
                return entries;
            }

            for (int index = 0; index < missions.Count; index++)
            {
                MissionDefinition mission = missions[index];
                if (mission == null)
                {
                    continue;
                }

                if (ContainsMissionId(saveData.completedMissionIds, mission.Id))
                {
                    entries.Add(new MissionDisplayEntry(
                        mission,
                        index + 1,
                        ResolveCompletedState(saveData, mission.Id),
                        BuildClassificationText(mission, index + 1),
                        "UNLOCKED"));
                    continue;
                }

                if (missionSystem != null && missionSystem.IsMissionUnlocked(mission.Id))
                {
                    entries.Add(new MissionDisplayEntry(
                        mission,
                        index + 1,
                        MissionRowState.Active,
                        BuildClassificationText(mission, index + 1),
                        "UNLOCKED - MAIN OBJECTIVE"));
                    continue;
                }

                entries.Add(new MissionDisplayEntry(
                    mission,
                    index + 1,
                    MissionRowState.Locked,
                    BuildClassificationText(mission, index + 1),
                    BuildUnlockRequirementText(missions, index)));
            }

            return entries;
        }

        private static string BuildClassificationText(MissionDefinition mission, int missionNumber)
        {
            if (mission == null)
            {
                return string.Empty;
            }

            return $"{missionNumber:00} / {mission.Phase} - {GetMissionCategoryLabel(mission)}";
        }

        private static string BuildUnlockRequirementText(IReadOnlyList<MissionDefinition> missions, int missionIndex)
        {
            if (missionIndex <= 0)
            {
                return "UNLOCK: START CAMPAIGN";
            }

            MissionDefinition mission = missions != null && missionIndex < missions.Count
                ? missions[missionIndex]
                : null;
            MissionDefinition previousMission = IsBootMission(mission)
                ? missions != null && missionIndex - 1 < missions.Count ? missions[missionIndex - 1] : null
                : FindPreviousSameCategoryMission(missions, missionIndex);
            if (previousMission == null)
            {
                return IsBootMission(mission)
                    ? "UNLOCK: COMPLETE PREVIOUS DIRECTIVE"
                    : "UNLOCK: COMPLETE BOOT";
            }

            int previousMissionNumber = FindMissionNumber(missions, previousMission.Id);
            return $"UNLOCK: COMPLETE {previousMissionNumber:00} / {previousMission.Phase}";
        }

        private static int FindMissionNumber(IReadOnlyList<MissionDefinition> missions, string missionId)
        {
            if (missions == null || string.IsNullOrWhiteSpace(missionId))
            {
                return 0;
            }

            for (int index = 0; index < missions.Count; index++)
            {
                MissionDefinition mission = missions[index];
                if (mission != null && mission.Id == missionId)
                {
                    return index + 1;
                }
            }

            return 0;
        }

        private static MissionDefinition FindPreviousSameCategoryMission(
            IReadOnlyList<MissionDefinition> missions,
            int missionIndex)
        {
            MissionDefinition mission = missions != null && missionIndex >= 0 && missionIndex < missions.Count
                ? missions[missionIndex]
                : null;
            string categoryKey = GetMissionCategoryKey(mission);
            for (int index = missionIndex - 1; index >= 0; index--)
            {
                MissionDefinition candidate = missions[index];
                if (candidate == null || IsBootMission(candidate))
                {
                    continue;
                }

                if (GetMissionCategoryKey(candidate) == categoryKey)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsBootMission(MissionDefinition mission)
        {
            return mission != null && mission.Phase == "BOOT";
        }

        private static string GetMissionCategoryLabel(MissionDefinition mission)
        {
            return mission.ObjectiveType switch
            {
                MissionObjectiveType.GameplayTutorialCompleted => "TUTORIAL",
                MissionObjectiveType.SingleRunSurvivalTime => "SURVIVAL",
                MissionObjectiveType.SingleRunEnemyKills => "COMBAT RUN",
                MissionObjectiveType.TotalEnemyKills => "COMBAT TOTAL",
                MissionObjectiveType.TotalRunsCompleted => "LOOP",
                MissionObjectiveType.GatesSelected => "GATE",
                MissionObjectiveType.MajorGatesSelected => "MAJOR GATE",
                MissionObjectiveType.AnyCoreUpgradePurchased => "UPGRADE",
                MissionObjectiveType.UpgradeLevel => "UPGRADE",
                MissionObjectiveType.CoreUpgradesAtLevel => "UPGRADE",
                MissionObjectiveType.MaxedCoreUpgrades => "UPGRADE",
                MissionObjectiveType.SquadSize => "SQUAD",
                MissionObjectiveType.FinalChoiceResolved => "STORY",
                _ => "OBJECTIVE"
            };
        }

        private static string GetMissionCategoryKey(MissionDefinition mission)
        {
            if (mission == null)
            {
                return string.Empty;
            }

            return mission.ObjectiveType switch
            {
                MissionObjectiveType.SingleRunSurvivalTime => "SURVIVAL",
                MissionObjectiveType.SingleRunEnemyKills => "COMBAT_RUN",
                MissionObjectiveType.TotalEnemyKills => "COMBAT_TOTAL",
                MissionObjectiveType.TotalRunsCompleted => "LOOP",
                MissionObjectiveType.GatesSelected => "GATE",
                MissionObjectiveType.MajorGatesSelected => "MAJOR_GATE",
                MissionObjectiveType.AnyCoreUpgradePurchased => "UPGRADE",
                MissionObjectiveType.UpgradeLevel => "UPGRADE",
                MissionObjectiveType.CoreUpgradesAtLevel => "UPGRADE",
                MissionObjectiveType.MaxedCoreUpgrades => "UPGRADE",
                MissionObjectiveType.SquadSize => "SQUAD",
                MissionObjectiveType.FinalChoiceResolved => "STORY",
                MissionObjectiveType.GameplayTutorialCompleted => "TUTORIAL",
                _ => mission.ObjectiveType.ToString()
            };
        }

        private static bool ContainsMissionId(List<string> values, string missionId)
        {
            if (values == null || string.IsNullOrWhiteSpace(missionId))
            {
                return false;
            }

            string safeMissionId = missionId.Trim();
            for (int index = 0; index < values.Count; index++)
            {
                if (values[index] == safeMissionId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HandleClaimRequested(string missionId)
        {
            _missionSystem?.InitializeFromSave();
            if (_missionSystem == null || !_missionSystem.TryClaimMissionReward(missionId))
            {
                return false;
            }

            _saveData = SaveService.Instance.Data;
            Refresh(_missionSystem, _saveData);
            return true;
        }

        private static MissionRowState ResolveCompletedState(SaveData saveData, string missionId)
        {
            return ContainsMissionId(saveData.grantedMissionRewardIds, missionId)
                ? MissionRowState.CompletedClaimed
                : MissionRowState.CompletedUnclaimed;
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private readonly struct MissionDisplayEntry
        {
            public MissionDisplayEntry(
                MissionDefinition mission,
                int missionNumber,
                MissionRowState state,
                string classificationText,
                string unlockRequirementText)
            {
                Mission = mission;
                MissionNumber = missionNumber;
                State = state;
                ClassificationText = classificationText;
                UnlockRequirementText = unlockRequirementText;
            }

            public MissionDefinition Mission { get; }
            public int MissionNumber { get; }
            public MissionRowState State { get; }
            public string ClassificationText { get; }
            public string UnlockRequirementText { get; }
        }
    }
}
