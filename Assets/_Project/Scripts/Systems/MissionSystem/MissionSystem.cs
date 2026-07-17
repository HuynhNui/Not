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
        private readonly HashSet<string> _grantedMissionRewardIds = new HashSet<string>();
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
        public event System.Action<MissionDefinition> MissionRewardClaimed;

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
        public bool ShouldShowMissionAttention => MissionNotificationUnread || HasAnyUnclaimedMissionRewards;
        public bool HasAnyUnclaimedMissionRewards
        {
            get
            {
                foreach (string missionId in _completedMissionIds)
                {
                    if (IsMissionRewardClaimable(missionId))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

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

        public bool IsMissionUnlocked(string missionId)
        {
            string safeMissionId = NormalizeMissionId(missionId);
            MissionDefinition mission = _catalog != null
                ? _catalog.GetMissionById(safeMissionId)
                : null;
            return mission != null && IsMissionUnlocked(mission);
        }

        public MissionProgressResult EvaluateMission(string missionId)
        {
            MissionDefinition mission = _catalog != null
                ? _catalog.GetMissionById(NormalizeMissionId(missionId))
                : null;
            MissionProgressSnapshot snapshot = _saveService != null
                ? BuildSnapshot(default, hasRunSnapshot: false)
                : default;
            return EvaluateMission(mission, snapshot);
        }

        public MissionProgressResult EvaluateMission(MissionDefinition mission, MissionProgressSnapshot snapshot)
        {
            if (mission == null)
            {
                return new MissionProgressResult(0f, 0f);
            }

            MissionProgressSaveEntry entry = FindMissionProgressEntry(_saveService?.Data, mission.Id);
            float storedProgressOrBaseline = entry != null
                ? GetStoredProgressOrBaseline(mission, entry)
                : GetLegacyStoredProgressOrBaseline(mission);
            return MissionProgressEvaluator.Evaluate(mission, snapshot, storedProgressOrBaseline);
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
            return EvaluateMission(ActiveMission, snapshot);
        }

        public bool IsMissionCompleted(string missionId)
        {
            string safeMissionId = NormalizeMissionId(missionId);
            return !string.IsNullOrEmpty(safeMissionId)
                && _completedMissionIds.Contains(safeMissionId);
        }

        public bool IsMissionRewardClaimed(string missionId)
        {
            string safeMissionId = NormalizeMissionId(missionId);
            return !string.IsNullOrEmpty(safeMissionId)
                && _grantedMissionRewardIds.Contains(safeMissionId);
        }

        public bool IsMissionRewardClaimable(string missionId)
        {
            string safeMissionId = NormalizeMissionId(missionId);
            return !string.IsNullOrEmpty(safeMissionId)
                && _catalog != null
                && _catalog.GetMissionById(safeMissionId) != null
                && _completedMissionIds.Contains(safeMissionId)
                && !_grantedMissionRewardIds.Contains(safeMissionId);
        }

        public bool TryClaimMissionReward(string missionId)
        {
            if (_saveService == null)
            {
                return false;
            }

            string safeMissionId = NormalizeMissionId(missionId);
            if (string.IsNullOrEmpty(safeMissionId))
            {
                return false;
            }

            MissionDefinition mission = _catalog != null
                ? _catalog.GetMissionById(safeMissionId)
                : null;
            if (mission == null || !IsMissionRewardClaimable(safeMissionId))
            {
                return false;
            }

            if (!_saveService.GrantMissionRewardOnce(mission.Id, mission.RewardCoins))
            {
                return false;
            }

            SyncMissionListsFromSave();
            MissionNotificationUnread = _saveService.Data.missionNotificationUnread;
            MissionRewardClaimed?.Invoke(mission);
            return true;
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

            unlockedMission = FindNextUnlockAfterCompletion(activeMission);
            MissionDefinition nextFocusMission = unlockedMission ?? FindFirstUnlockedIncompleteMission();
            if (nextFocusMission != null)
            {
                SetActiveMission(nextFocusMission, snapshot, unread: true);
            }
            else
            {
                _activeMissionId = null;
                _activeMissionStoredProgressOrBaseline = 0f;
                MissionNotificationUnread = true;
            }

            return true;
        }

        public void MarkNotificationRead()
        {
            MissionNotificationUnread = false;
        }

        private void SyncFromSave(MissionProgressSnapshot snapshot)
        {
            SaveData data = _saveService.Data;
            SyncMissionListsFromSave();
            EnsureUnlockedMissionProgressEntries(data, snapshot);

            MissionDefinition activeMission = _catalog.GetMissionById(data.activeMissionId);
            if (activeMission == null
                || IsMissionCompleted(activeMission.Id)
                || !IsMissionUnlocked(activeMission))
            {
                MissionDefinition focusMission = FindFirstUnlockedIncompleteMission();
                if (focusMission == null)
                {
                    data.activeMissionId = string.Empty;
                    data.activeMissionBaseline = 0f;
                    data.activeMissionProgress = 0f;
                    _activeMissionId = null;
                    _activeMissionStoredProgressOrBaseline = 0f;
                    MissionNotificationUnread = data.missionNotificationUnread;
                    return;
                }

                MissionProgressSaveEntry focusEntry = EnsureMissionProgressEntry(data, focusMission, snapshot);
                data.activeMissionId = focusMission.Id;
                data.activeMissionBaseline = focusEntry.baseline;
                data.activeMissionProgress = focusEntry.progress;
                data.missionNotificationUnread = true;
                _saveService.CommitMissionState();
                activeMission = focusMission;
            }

            _activeMissionId = activeMission.Id;
            MissionProgressSaveEntry activeEntry = EnsureMissionProgressEntry(data, activeMission, snapshot);
            _activeMissionStoredProgressOrBaseline = GetStoredProgressOrBaseline(activeMission, activeEntry);
            data.activeMissionProgress = activeEntry.progress;
            data.activeMissionBaseline = activeEntry.baseline;
            MissionNotificationUnread = data.missionNotificationUnread;
        }

        private void EvaluateAndPersist(MissionProgressSnapshot snapshot)
        {
            if (_catalog == null || _catalog.Count == 0)
            {
                return;
            }

            SyncFromSave(snapshot);
            SaveData data = _saveService.Data;
            List<MissionDefinition> completedThisPass = new List<MissionDefinition>();
            List<MissionDefinition> activeMissions = GetUnlockedIncompleteMissions();
            for (int index = 0; index < activeMissions.Count; index++)
            {
                MissionDefinition mission = activeMissions[index];
                MissionProgressSaveEntry entry = EnsureMissionProgressEntry(data, mission, snapshot);
                MissionProgressResult result = MissionProgressEvaluator.Evaluate(
                    mission,
                    snapshot,
                    GetStoredProgressOrBaseline(mission, entry));

                entry.progress = result.ProgressValue;
                if (mission.ProgressMode == MissionProgressMode.DeltaSinceUnlock)
                {
                    entry.baseline = GetStoredProgressOrBaseline(mission, entry);
                }

                if (result.IsComplete)
                {
                    completedThisPass.Add(mission);
                }
            }

            if (completedThisPass.Count <= 0)
            {
                SyncActiveMissionFieldsFromFocus(data, snapshot);
                _saveService.CommitMissionState();
                return;
            }

            for (int index = 0; index < completedThisPass.Count; index++)
            {
                CompleteMissionInSave(completedThisPass[index], snapshot);
            }

            EnsureUnlockedMissionProgressEntries(data, snapshot);
            SyncActiveMissionFieldsFromFocus(data, snapshot);
            _saveService.CommitMissionState();
        }

        private void CompleteMissionInSave(
            MissionDefinition completedMission,
            MissionProgressSnapshot snapshot)
        {
            SaveData data = _saveService.Data;
            EnsureListContains(data.completedMissionIds, completedMission.Id);
            _completedMissionIds.Add(completedMission.Id);
            RemoveMissionProgressEntry(data, completedMission.Id);

            MissionDefinition unlockedMission = FindNextUnlockAfterCompletion(completedMission);
            if (unlockedMission != null)
            {
                EnsureMissionProgressEntry(data, unlockedMission, snapshot);
            }

            data.missionNotificationUnread = true;
            MissionNotificationUnread = true;
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

        private List<MissionDefinition> GetUnlockedIncompleteMissions()
        {
            List<MissionDefinition> missions = new List<MissionDefinition>();
            if (_catalog == null)
            {
                return missions;
            }

            for (int index = 0; index < _catalog.Count; index++)
            {
                MissionDefinition mission = _catalog.GetMissionAt(index);
                if (mission != null
                    && !IsMissionCompleted(mission.Id)
                    && IsMissionUnlocked(mission))
                {
                    missions.Add(mission);
                }
            }

            return missions;
        }

        private MissionDefinition FindFirstUnlockedIncompleteMission()
        {
            if (_catalog == null)
            {
                return null;
            }

            for (int index = 0; index < _catalog.Count; index++)
            {
                MissionDefinition mission = _catalog.GetMissionAt(index);
                if (mission != null
                    && !IsMissionCompleted(mission.Id)
                    && IsMissionUnlocked(mission))
                {
                    return mission;
                }
            }

            return null;
        }

        private bool IsMissionUnlocked(MissionDefinition mission)
        {
            if (mission == null || _catalog == null)
            {
                return false;
            }

            int missionIndex = _catalog.IndexOf(mission.Id);
            if (missionIndex < 0)
            {
                return false;
            }

            if (IsBootMission(mission))
            {
                if (missionIndex == 0)
                {
                    return true;
                }

                MissionDefinition previousMission = _catalog.GetMissionAt(missionIndex - 1);
                return previousMission != null && IsMissionCompleted(previousMission.Id);
            }

            if (!AreBootMissionsCompleted())
            {
                return false;
            }

            MissionDefinition previousSameCategory = FindPreviousSameCategoryMission(missionIndex);
            return previousSameCategory == null || IsMissionCompleted(previousSameCategory.Id);
        }

        private bool AreBootMissionsCompleted()
        {
            if (_catalog == null)
            {
                return false;
            }

            for (int index = 0; index < _catalog.Count; index++)
            {
                MissionDefinition mission = _catalog.GetMissionAt(index);
                if (mission == null || !IsBootMission(mission))
                {
                    continue;
                }

                if (!IsMissionCompleted(mission.Id))
                {
                    return false;
                }
            }

            return true;
        }

        private MissionDefinition FindNextUnlockAfterCompletion(MissionDefinition completedMission)
        {
            if (completedMission == null || _catalog == null)
            {
                return null;
            }

            int completedIndex = _catalog.IndexOf(completedMission.Id);
            if (completedIndex < 0)
            {
                return null;
            }

            if (IsBootMission(completedMission))
            {
                MissionDefinition nextMission = _catalog.GetMissionAt(completedIndex + 1);
                if (nextMission != null && IsBootMission(nextMission))
                {
                    return nextMission;
                }

                return FindFirstUnlockedIncompleteMission();
            }

            string categoryKey = GetMissionCategoryKey(completedMission);
            for (int index = completedIndex + 1; index < _catalog.Count; index++)
            {
                MissionDefinition mission = _catalog.GetMissionAt(index);
                if (mission == null || IsBootMission(mission))
                {
                    continue;
                }

                if (GetMissionCategoryKey(mission) == categoryKey)
                {
                    return mission;
                }
            }

            return null;
        }

        private MissionDefinition FindPreviousSameCategoryMission(int missionIndex)
        {
            MissionDefinition mission = _catalog.GetMissionAt(missionIndex);
            string categoryKey = GetMissionCategoryKey(mission);
            for (int index = missionIndex - 1; index >= 0; index--)
            {
                MissionDefinition candidate = _catalog.GetMissionAt(index);
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

        private void EnsureUnlockedMissionProgressEntries(SaveData data, MissionProgressSnapshot snapshot)
        {
            if (data == null || _catalog == null)
            {
                return;
            }

            data.missionProgressEntries ??= new List<MissionProgressSaveEntry>();
            for (int index = data.missionProgressEntries.Count - 1; index >= 0; index--)
            {
                MissionProgressSaveEntry entry = data.missionProgressEntries[index];
                MissionDefinition mission = entry != null ? _catalog.GetMissionById(entry.missionId) : null;
                if (mission == null || IsMissionCompleted(mission.Id) || !IsMissionUnlocked(mission))
                {
                    data.missionProgressEntries.RemoveAt(index);
                }
            }

            for (int index = 0; index < _catalog.Count; index++)
            {
                MissionDefinition mission = _catalog.GetMissionAt(index);
                if (mission != null
                    && !IsMissionCompleted(mission.Id)
                    && IsMissionUnlocked(mission))
                {
                    EnsureMissionProgressEntry(data, mission, snapshot);
                }
            }
        }

        private MissionProgressSaveEntry EnsureMissionProgressEntry(
            SaveData data,
            MissionDefinition mission,
            MissionProgressSnapshot snapshot)
        {
            data.missionProgressEntries ??= new List<MissionProgressSaveEntry>();
            MissionProgressSaveEntry entry = FindMissionProgressEntry(data, mission.Id);
            if (entry != null)
            {
                return entry;
            }

            float legacyProgress = _activeMissionId == mission.Id
                || data.activeMissionId == mission.Id
                    ? data.activeMissionProgress
                    : 0f;
            float baseline = _activeMissionId == mission.Id
                || data.activeMissionId == mission.Id
                    ? data.activeMissionBaseline
                    : MissionProgressEvaluator.CaptureBaseline(mission, snapshot);
            entry = new MissionProgressSaveEntry(mission.Id, Mathf.Max(0f, legacyProgress), Mathf.Max(0f, baseline));
            data.missionProgressEntries.Add(entry);
            return entry;
        }

        private static MissionProgressSaveEntry FindMissionProgressEntry(SaveData data, string missionId)
        {
            string safeMissionId = NormalizeMissionId(missionId);
            if (data == null || data.missionProgressEntries == null || string.IsNullOrEmpty(safeMissionId))
            {
                return null;
            }

            for (int index = 0; index < data.missionProgressEntries.Count; index++)
            {
                MissionProgressSaveEntry entry = data.missionProgressEntries[index];
                if (entry != null && entry.missionId == safeMissionId)
                {
                    return entry;
                }
            }

            return null;
        }

        private static void RemoveMissionProgressEntry(SaveData data, string missionId)
        {
            string safeMissionId = NormalizeMissionId(missionId);
            if (data == null || data.missionProgressEntries == null || string.IsNullOrEmpty(safeMissionId))
            {
                return;
            }

            for (int index = data.missionProgressEntries.Count - 1; index >= 0; index--)
            {
                MissionProgressSaveEntry entry = data.missionProgressEntries[index];
                if (entry == null || entry.missionId == safeMissionId)
                {
                    data.missionProgressEntries.RemoveAt(index);
                }
            }
        }

        private void SyncActiveMissionFieldsFromFocus(SaveData data, MissionProgressSnapshot snapshot)
        {
            MissionDefinition focusMission = FindFirstUnlockedIncompleteMission();
            if (focusMission == null)
            {
                data.activeMissionId = string.Empty;
                data.activeMissionProgress = 0f;
                data.activeMissionBaseline = 0f;
                _activeMissionId = null;
                _activeMissionStoredProgressOrBaseline = 0f;
                MissionNotificationUnread = data.missionNotificationUnread;
                return;
            }

            MissionProgressSaveEntry entry = EnsureMissionProgressEntry(data, focusMission, snapshot);
            data.activeMissionId = focusMission.Id;
            data.activeMissionProgress = entry.progress;
            data.activeMissionBaseline = entry.baseline;
            _activeMissionId = focusMission.Id;
            _activeMissionStoredProgressOrBaseline = GetStoredProgressOrBaseline(focusMission, entry);
            MissionNotificationUnread = data.missionNotificationUnread;
        }

        private static float GetStoredProgressOrBaseline(
            MissionDefinition mission,
            MissionProgressSaveEntry entry)
        {
            if (mission == null || entry == null)
            {
                return 0f;
            }

            return mission.ProgressMode == MissionProgressMode.BestSingleRun
                ? entry.progress
                : entry.baseline;
        }

        private float GetLegacyStoredProgressOrBaseline(MissionDefinition mission)
        {
            if (mission == null || mission.Id != _activeMissionId)
            {
                return 0f;
            }

            return mission.ProgressMode == MissionProgressMode.BestSingleRun
                ? _activeMissionStoredProgressOrBaseline
                : _activeMissionStoredProgressOrBaseline;
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

        private void SyncMissionListsFromSave()
        {
            _completedMissionIds.Clear();
            _grantedMissionRewardIds.Clear();
            if (_saveService == null)
            {
                return;
            }

            SaveData data = _saveService.Data;
            AddMissionIdsToSet(data.completedMissionIds, _completedMissionIds);
            AddMissionIdsToSet(data.grantedMissionRewardIds, _grantedMissionRewardIds);
        }

        private static void AddMissionIdsToSet(List<string> missionIds, HashSet<string> target)
        {
            if (missionIds == null || target == null)
            {
                return;
            }

            for (int index = 0; index < missionIds.Count; index++)
            {
                string safeMissionId = NormalizeMissionId(missionIds[index]);
                if (!string.IsNullOrEmpty(safeMissionId))
                {
                    target.Add(safeMissionId);
                }
            }
        }

        private static string NormalizeMissionId(string missionId)
        {
            return string.IsNullOrWhiteSpace(missionId)
                ? string.Empty
                : missionId.Trim();
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
