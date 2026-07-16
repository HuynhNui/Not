using System.Collections.Generic;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.MissionSystem
{
    public sealed class MissionSystem
    {
        private readonly MissionCatalog _catalog;
        private readonly SaveService _saveService;
        private readonly HashSet<string> _completedMissionIds = new HashSet<string>();
        private string _activeMissionId;
        private float _activeMissionStoredProgressOrBaseline;
        private bool _progressionSuppressed;

        public MissionSystem(MissionCatalog catalog)
        {
            _catalog = catalog;
        }

        public MissionSystem(MissionCatalog catalog, SaveService saveService)
            : this(catalog)
        {
            _saveService = saveService;
            ActiveInstance = this;
        }

        public static MissionSystem ActiveInstance { get; private set; }

        public event System.Action<MissionDefinition, MissionDefinition> MissionCompleted;

        public MissionDefinition ActiveMission => _catalog != null
            ? _catalog.GetMissionById(_activeMissionId)
            : null;

        public IReadOnlyList<MissionDefinition> Missions => _catalog != null
            ? _catalog.Missions
            : null;

        public string ActiveMissionId => _activeMissionId;
        public int ActiveMissionIndex => _catalog != null
            ? _catalog.IndexOf(_activeMissionId)
            : -1;
        public float ActiveMissionStoredProgressOrBaseline => _activeMissionStoredProgressOrBaseline;
        public bool MissionNotificationUnread { get; private set; }
        public IReadOnlyCollection<string> CompletedMissionIds => _completedMissionIds;

        public void Dispose()
        {
            if (ReferenceEquals(ActiveInstance, this))
            {
                ActiveInstance = null;
            }
        }

        public void SetProgressionSuppressed(bool suppressed)
        {
            _progressionSuppressed = suppressed;
        }

        public void InitializeFromSave()
        {
            if (_saveService == null)
            {
                return;
            }

            _saveService.EnsureLoaded();
            SyncFromSave(BuildSnapshot(default, hasRunSnapshot: false));
        }

        public bool StartFirstMission(MissionProgressSnapshot snapshot)
        {
            if (_catalog == null || _catalog.Count == 0)
            {
                return false;
            }

            MissionDefinition firstMission = _catalog.GetMissionAt(0);
            return SetActiveMission(firstMission, snapshot, unread: true);
        }

        public bool SetActiveMission(
            string missionId,
            MissionProgressSnapshot snapshot,
            float storedProgressOrBaseline = 0f,
            bool unread = false)
        {
            MissionDefinition mission = _catalog != null
                ? _catalog.GetMissionById(missionId)
                : null;
            if (mission == null)
            {
                return false;
            }

            _activeMissionId = mission.Id;
            _activeMissionStoredProgressOrBaseline = storedProgressOrBaseline;
            MissionNotificationUnread = unread;
            return true;
        }

        public void NotifyGateSelected(GateConfig gateConfig, bool isTutorialGate)
        {
            if (_saveService == null || _progressionSuppressed || isTutorialGate || gateConfig == null)
            {
                return;
            }

            SaveData data = _saveService.Data;
            data.lifetimeGatesSelected = Mathf.Max(0, data.lifetimeGatesSelected) + 1;
            if (gateConfig.Category == BalanceGateCategory.Major)
            {
                data.lifetimeMajorGatesSelected = Mathf.Max(0, data.lifetimeMajorGatesSelected) + 1;
            }

            EvaluateAndPersist(BuildSnapshot(default, hasRunSnapshot: false));
        }

        public void EndRun(RunStatsSnapshot snapshot)
        {
            if (_saveService == null || _progressionSuppressed)
            {
                return;
            }

            EvaluateAndPersist(BuildSnapshot(snapshot, hasRunSnapshot: true));
        }

        public void NotifyUpgradePurchased()
        {
            if (_saveService == null || _progressionSuppressed)
            {
                return;
            }

            EvaluateAndPersist(BuildSnapshot(default, hasRunSnapshot: false));
        }

        public void NotifyGameplayTutorialCompleted()
        {
            if (_saveService == null || _progressionSuppressed)
            {
                return;
            }

            EvaluateAndPersist(BuildSnapshot(default, hasRunSnapshot: false));
        }

        public void NotifyFinalChoiceResolved(string branchId)
        {
            if (_saveService == null || _progressionSuppressed)
            {
                return;
            }

            _saveService.MarkFinalChoiceResolved();
            EvaluateAndPersist(BuildSnapshot(default, hasRunSnapshot: false));
        }

        public MissionProgressResult EvaluateActiveMission(MissionProgressSnapshot snapshot)
        {
            return MissionProgressEvaluator.Evaluate(
                ActiveMission,
                snapshot,
                _activeMissionStoredProgressOrBaseline);
        }

        public bool TryCompleteActiveMission(
            MissionProgressSnapshot snapshot,
            out MissionDefinition completedMission,
            out MissionDefinition unlockedMission)
        {
            completedMission = null;
            unlockedMission = null;

            MissionDefinition activeMission = ActiveMission;
            if (activeMission == null)
            {
                return false;
            }

            MissionProgressResult result = EvaluateActiveMission(snapshot);
            if (!result.IsComplete)
            {
                if (activeMission.ProgressMode == MissionProgressMode.BestSingleRun)
                {
                    _activeMissionStoredProgressOrBaseline = result.ProgressValue;
                }

                return false;
            }

            completedMission = activeMission;
            _completedMissionIds.Add(activeMission.Id);

            int nextIndex = _catalog.IndexOf(activeMission.Id) + 1;
            unlockedMission = _catalog.GetMissionAt(nextIndex);
            if (unlockedMission == null)
            {
                _activeMissionId = null;
                _activeMissionStoredProgressOrBaseline = 0f;
                MissionNotificationUnread = false;
                return true;
            }

            SetActiveMission(unlockedMission, snapshot, unread: true);
            return true;
        }

        public void MarkNotificationRead()
        {
            MissionNotificationUnread = false;
        }

        private void SyncFromSave(MissionProgressSnapshot snapshot)
        {
            SaveData data = _saveService.Data;
            _completedMissionIds.Clear();
            if (data.completedMissionIds != null)
            {
                for (int index = 0; index < data.completedMissionIds.Count; index++)
                {
                    string missionId = data.completedMissionIds[index];
                    if (!string.IsNullOrWhiteSpace(missionId))
                    {
                        _completedMissionIds.Add(missionId.Trim());
                    }
                }
            }

            MissionDefinition activeMission = _catalog.GetMissionById(data.activeMissionId);
            if (activeMission == null)
            {
                MissionDefinition firstMission = _catalog.GetMissionAt(0);
                if (firstMission == null)
                {
                    return;
                }

                data.activeMissionId = firstMission.Id;
                data.activeMissionBaseline = MissionProgressEvaluator.CaptureBaseline(firstMission, snapshot);
                data.activeMissionProgress = 0f;
                data.missionNotificationUnread = true;
                _saveService.CommitMissionState();
                activeMission = firstMission;
            }

            _activeMissionId = activeMission.Id;
            _activeMissionStoredProgressOrBaseline = activeMission.ProgressMode == MissionProgressMode.BestSingleRun
                ? data.activeMissionProgress
                : data.activeMissionBaseline;
            MissionNotificationUnread = data.missionNotificationUnread;
        }

        private void EvaluateAndPersist(MissionProgressSnapshot snapshot)
        {
            if (_catalog == null || _catalog.Count == 0)
            {
                return;
            }

            SyncFromSave(snapshot);

            MissionDefinition activeMission = ActiveMission;
            if (activeMission == null)
            {
                return;
            }

            MissionProgressResult result = EvaluateActiveMission(snapshot);
            SaveData data = _saveService.Data;
            data.activeMissionProgress = result.ProgressValue;
            data.activeMissionBaseline = activeMission.ProgressMode == MissionProgressMode.DeltaSinceUnlock
                ? _activeMissionStoredProgressOrBaseline
                : 0f;

            if (!result.IsComplete)
            {
                if (activeMission.ProgressMode == MissionProgressMode.BestSingleRun)
                {
                    _activeMissionStoredProgressOrBaseline = result.ProgressValue;
                }

                _saveService.CommitMissionState();
                return;
            }

            CompleteActiveMissionInSave(activeMission, snapshot);
        }

        private void CompleteActiveMissionInSave(
            MissionDefinition completedMission,
            MissionProgressSnapshot snapshot)
        {
            SaveData data = _saveService.Data;
            EnsureListContains(data.completedMissionIds, completedMission.Id);
            _completedMissionIds.Add(completedMission.Id);

            int nextIndex = _catalog.IndexOf(completedMission.Id) + 1;
            MissionDefinition unlockedMission = _catalog.GetMissionAt(nextIndex);
            if (unlockedMission != null)
            {
                data.activeMissionId = unlockedMission.Id;
                data.activeMissionBaseline = MissionProgressEvaluator.CaptureBaseline(unlockedMission, snapshot);
                data.activeMissionProgress = 0f;
                data.missionNotificationUnread = true;
                _activeMissionId = unlockedMission.Id;
                _activeMissionStoredProgressOrBaseline = data.activeMissionBaseline;
                MissionNotificationUnread = true;
            }
            else
            {
                data.activeMissionId = string.Empty;
                data.activeMissionBaseline = 0f;
                data.activeMissionProgress = 0f;
                data.missionNotificationUnread = false;
                _activeMissionId = null;
                _activeMissionStoredProgressOrBaseline = 0f;
                MissionNotificationUnread = false;
            }

            _saveService.CommitMissionState();
            _saveService.GrantMissionRewardOnce(completedMission.Id, completedMission.RewardCoins);
            MissionCompleted?.Invoke(completedMission, unlockedMission);
        }

        private MissionProgressSnapshot BuildSnapshot(RunStatsSnapshot runSnapshot, bool hasRunSnapshot)
        {
            SaveData data = _saveService.Data;
            int squadSizeLevel = data.GetUpgradeLevel(PlayerMetaUpgradeType.SquadSize);
            int squadSizeValue = Mathf.RoundToInt(
                PlayerMetaUpgradeService.GetValueForLevel(
                    PlayerMetaUpgradeType.SquadSize,
                    squadSizeLevel));

            return new MissionProgressSnapshot(
                gameplayTutorialCompleted: data.gameplayTutorialCompleted,
                totalRunsCompleted: data.totalRunsCompleted,
                lifetimeGatesSelected: data.lifetimeGatesSelected,
                lifetimeMajorGatesSelected: data.lifetimeMajorGatesSelected,
                totalEnemyKills: data.totalEnemyKills,
                currentRunEnemyKills: hasRunSnapshot ? runSnapshot.EnemyKills : 0,
                currentRunSurvivalTime: hasRunSnapshot ? runSnapshot.SurvivalTime : 0f,
                finalChoiceResolved: data.finalChoiceResolved,
                damageLevel: data.GetUpgradeLevel(PlayerMetaUpgradeType.Damage),
                fireRateLevel: data.GetUpgradeLevel(PlayerMetaUpgradeType.FireRate),
                maxHpLevel: data.GetUpgradeLevel(PlayerMetaUpgradeType.MaxHp),
                projectileCountLevel: data.GetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount),
                squadSizeLevel: squadSizeLevel,
                squadSizeValue: squadSizeValue);
        }

        private static void EnsureListContains(List<string> values, string value)
        {
            if (values == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string safeValue = value.Trim();
            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], safeValue, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            values.Add(safeValue);
        }

        private bool SetActiveMission(
            MissionDefinition mission,
            MissionProgressSnapshot snapshot,
            bool unread)
        {
            if (mission == null)
            {
                return false;
            }

            _activeMissionId = mission.Id;
            _activeMissionStoredProgressOrBaseline =
                MissionProgressEvaluator.CaptureBaseline(mission, snapshot);
            MissionNotificationUnread = unread;
            return true;
        }
    }
}
