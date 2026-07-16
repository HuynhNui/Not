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
        private int _activeRowIndex = -1;

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
                SetText(activeMissionText, "MISSION SYSTEM OFFLINE");
                SetText(summaryText, "NO DATA");
                return;
            }

            IReadOnlyList<MissionDefinition> missions = missionSystem.Missions;
            EnsureRowCount(missions.Count);

            string activeMissionId = saveData.activeMissionId;
            int completedCount = saveData.completedMissionIds != null
                ? saveData.completedMissionIds.Count
                : 0;
            _activeRowIndex = -1;

            for (int index = 0; index < missions.Count; index++)
            {
                MissionDefinition mission = missions[index];
                MissionRowState state = GetState(mission, activeMissionId, saveData);
                if (state == MissionRowState.Active)
                {
                    _activeRowIndex = index;
                }

                float progressValue = state == MissionRowState.Active
                    ? saveData.activeMissionProgress
                    : state == MissionRowState.Completed
                        ? mission.TargetValue
                        : 0f;

                _rows[index].Configure(mission, index + 1, state, progressValue, mission.TargetValue);
            }

            for (int index = missions.Count; index < _rows.Count; index++)
            {
                _rows[index].gameObject.SetActive(false);
            }

            MissionDefinition activeMission = missionSystem.ActiveMission;
            SetText(activeMissionText, activeMission != null ? activeMission.Title : "ALL MISSIONS COMPLETE");
            SetText(summaryText, $"{completedCount:N0} / {missions.Count:N0} COMPLETE");
        }

        public void ScrollToActiveMission()
        {
            if (scrollRect == null || contentRoot == null || _activeRowIndex < 0 || _rows.Count <= 1)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float normalized = 1f - Mathf.Clamp01((float)_activeRowIndex / Mathf.Max(1, _rows.Count - 1));
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

        private static MissionRowState GetState(
            MissionDefinition mission,
            string activeMissionId,
            SaveData saveData)
        {
            if (mission == null)
            {
                return MissionRowState.Locked;
            }

            if (ContainsMissionId(saveData.completedMissionIds, mission.Id))
            {
                return MissionRowState.Completed;
            }

            return mission.Id == activeMissionId
                ? MissionRowState.Active
                : MissionRowState.Locked;
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

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
