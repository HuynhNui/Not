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
            string activeMissionId = saveData.activeMissionId;
            int completedCount = saveData.completedMissionIds != null
                ? saveData.completedMissionIds.Count
                : 0;
            _missionSystem = missionSystem;
            _saveData = saveData;
            _focusRowIndex = -1;

            List<MissionDisplayEntry> displayEntries = BuildDisplayEntries(missions, activeMissionId, saveData);
            EnsureRowCount(displayEntries.Count);

            for (int index = 0; index < displayEntries.Count; index++)
            {
                MissionDisplayEntry entry = displayEntries[index];
                if (entry.IsLockedPhaseCard)
                {
                    _rows[index].ConfigureLockedPhaseCard();
                    continue;
                }

                if (entry.State == MissionRowState.CompletedUnclaimed)
                {
                    _focusRowIndex = index;
                }
                else if (entry.State == MissionRowState.Active && _focusRowIndex < 0)
                {
                    _focusRowIndex = index;
                }

                float progressValue = entry.State == MissionRowState.Active
                    ? saveData.activeMissionProgress
                    : 0f;

                _rows[index].Configure(
                    entry.Mission,
                    entry.MissionNumber,
                    entry.State,
                    progressValue,
                    entry.Mission.TargetValue,
                    HandleClaimRequested);
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
            string activeMissionId,
            SaveData saveData)
        {
            List<MissionDisplayEntry> entries = new List<MissionDisplayEntry>();
            if (missions == null || missions.Count == 0)
            {
                return entries;
            }

            int activeIndex = FindMissionIndex(missions, activeMissionId);
            if (activeIndex < 0)
            {
                activeIndex = FindFirstIncompleteIndex(missions, saveData);
            }

            MissionDefinition activeMission = GetMissionAt(missions, activeIndex);
            string currentPhase = activeMission != null
                ? activeMission.Phase
                : FindLastCompletedPhase(missions, saveData);

            AddRecentCompletedFromCurrentPhase(entries, missions, saveData, currentPhase, activeIndex);

            if (activeMission != null)
            {
                entries.Add(new MissionDisplayEntry(activeMission, activeIndex + 1, MissionRowState.Active));
            }

            int nextLockedIndex = FindNextLockedIndex(missions, activeIndex, saveData);
            if (nextLockedIndex >= 0)
            {
                entries.Add(new MissionDisplayEntry(
                    missions[nextLockedIndex],
                    nextLockedIndex + 1,
                    MissionRowState.Locked));
            }

            AddFuturePhaseCards(entries, missions, currentPhase, nextLockedIndex);
            return entries;
        }

        private static void AddRecentCompletedFromCurrentPhase(
            List<MissionDisplayEntry> entries,
            IReadOnlyList<MissionDefinition> missions,
            SaveData saveData,
            string currentPhase,
            int activeIndex)
        {
            if (string.IsNullOrWhiteSpace(currentPhase))
            {
                return;
            }

            List<MissionDisplayEntry> completedRows = new List<MissionDisplayEntry>();
            int scanStart = activeIndex >= 0 ? activeIndex - 1 : missions.Count - 1;
            for (int index = scanStart; index >= 0 && completedRows.Count < 3; index--)
            {
                MissionDefinition mission = missions[index];
                if (mission == null || mission.Phase != currentPhase)
                {
                    continue;
                }

                if (ContainsMissionId(saveData.completedMissionIds, mission.Id))
                {
                    completedRows.Add(new MissionDisplayEntry(
                        mission,
                        index + 1,
                        ResolveCompletedState(saveData, mission.Id)));
                }
            }

            for (int index = completedRows.Count - 1; index >= 0; index--)
            {
                entries.Add(completedRows[index]);
            }
        }

        private static void AddFuturePhaseCards(
            List<MissionDisplayEntry> entries,
            IReadOnlyList<MissionDefinition> missions,
            string currentPhase,
            int nextLockedIndex)
        {
            HashSet<string> lockedPhases = new HashSet<string>();
            string nextLockedPhase = GetMissionAt(missions, nextLockedIndex)?.Phase;
            int scanStart = nextLockedIndex >= 0 ? nextLockedIndex + 1 : 0;

            for (int index = scanStart; index < missions.Count; index++)
            {
                MissionDefinition mission = missions[index];
                if (mission == null
                    || mission.Phase == currentPhase
                    || mission.Phase == nextLockedPhase
                    || string.IsNullOrWhiteSpace(mission.Phase)
                    || !lockedPhases.Add(mission.Phase))
                {
                    continue;
                }

                entries.Add(MissionDisplayEntry.LockedPhaseCard());
            }
        }

        private static int FindMissionIndex(IReadOnlyList<MissionDefinition> missions, string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return -1;
            }

            string safeMissionId = missionId.Trim();
            for (int index = 0; index < missions.Count; index++)
            {
                MissionDefinition mission = missions[index];
                if (mission != null && mission.Id == safeMissionId)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindFirstIncompleteIndex(IReadOnlyList<MissionDefinition> missions, SaveData saveData)
        {
            for (int index = 0; index < missions.Count; index++)
            {
                MissionDefinition mission = missions[index];
                if (mission != null && !ContainsMissionId(saveData.completedMissionIds, mission.Id))
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindNextLockedIndex(
            IReadOnlyList<MissionDefinition> missions,
            int activeIndex,
            SaveData saveData)
        {
            int scanStart = Mathf.Max(0, activeIndex + 1);
            for (int index = scanStart; index < missions.Count; index++)
            {
                MissionDefinition mission = missions[index];
                if (mission != null && !ContainsMissionId(saveData.completedMissionIds, mission.Id))
                {
                    return index;
                }
            }

            return -1;
        }

        private static string FindLastCompletedPhase(IReadOnlyList<MissionDefinition> missions, SaveData saveData)
        {
            for (int index = missions.Count - 1; index >= 0; index--)
            {
                MissionDefinition mission = missions[index];
                if (mission != null && ContainsMissionId(saveData.completedMissionIds, mission.Id))
                {
                    return mission.Phase;
                }
            }

            return string.Empty;
        }

        private static MissionDefinition GetMissionAt(IReadOnlyList<MissionDefinition> missions, int index)
        {
            return missions != null && index >= 0 && index < missions.Count
                ? missions[index]
                : null;
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
            private MissionDisplayEntry(bool isLockedPhaseCard)
            {
                Mission = null;
                MissionNumber = 0;
                State = MissionRowState.Locked;
                IsLockedPhaseCard = isLockedPhaseCard;
            }

            public MissionDisplayEntry(
                MissionDefinition mission,
                int missionNumber,
                MissionRowState state)
            {
                Mission = mission;
                MissionNumber = missionNumber;
                State = state;
                IsLockedPhaseCard = false;
            }

            public MissionDefinition Mission { get; }
            public int MissionNumber { get; }
            public MissionRowState State { get; }
            public bool IsLockedPhaseCard { get; }

            public static MissionDisplayEntry LockedPhaseCard()
            {
                return new MissionDisplayEntry(true);
            }
        }
    }
}
