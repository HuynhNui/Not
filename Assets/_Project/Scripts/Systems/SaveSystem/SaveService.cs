using System;
using System.Threading.Tasks;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem.Cloud;
using UnityEngine;

namespace _Project.Scripts.Systems.SaveSystem
{
    public sealed class SaveService
    {
        public const string CloudSnapshotName = "true_gate_save_v1";
        private const string LegacyUpgradeLevelKeyPrefix = "MetaUpgrade.Level.";

        private static SaveService _instance;

        private readonly LocalSaveRepository _localRepository;
        private ICloudSaveProvider _cloudSaveProvider;
        private SaveData _data;
        private bool _isLoaded;
        private bool _isCloudUploadQueued;

        private SaveService(LocalSaveRepository localRepository, ICloudSaveProvider cloudSaveProvider)
        {
            _localRepository = localRepository ?? throw new ArgumentNullException(nameof(localRepository));
            _cloudSaveProvider = cloudSaveProvider ?? new NoOpCloudSaveProvider();
        }

        public static SaveService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = CreateDefault();
                }

                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        public SaveData Data
        {
            get
            {
                EnsureLoaded();
                return _data;
            }
        }

        public SaveConflict PendingConflict { get; private set; }
        public event Action DataChanged;
        public event Action<UpgradePurchaseTelemetry> UpgradePurchased;

        public static SaveService CreateDefault()
        {
            return new SaveService(
                new LocalSaveRepository(Application.persistentDataPath),
                new NoOpCloudSaveProvider());
        }

        public static SaveService CreateForTests(string directoryPath)
        {
            return new SaveService(
                new LocalSaveRepository(directoryPath),
                new NoOpCloudSaveProvider());
        }

        public static void SetInstanceForTests(SaveService saveService)
        {
            _instance = saveService;
        }

        public void SetCloudProvider(ICloudSaveProvider cloudSaveProvider)
        {
            _cloudSaveProvider = cloudSaveProvider ?? new NoOpCloudSaveProvider();
        }

        public void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            long now = GetCurrentUnixMs();

            if (_localRepository.TryLoad(out SaveData loadedData))
            {
                _data = loadedData;
                _data.Normalize(now);
            }
            else
            {
                _data = CreateInitialSaveData(now);
                SaveLocal();
            }

            _isLoaded = true;
        }

        public async Task LoadAsync()
        {
            EnsureLoaded();
            await TryMergeCloudSaveAsync();
        }

        public async Task SaveAsync()
        {
            EnsureLoaded();
            Touch();
            SaveLocal();
            DataChanged?.Invoke();
            await TryUploadCloudSaveAsync();
        }

        public async Task FlushAsync()
        {
            EnsureLoaded();
            SaveLocal();
            await TryUploadCloudSaveAsync();
        }

        public void ResetPlayerProgression()
        {
            long now = GetCurrentUnixMs();
            _data = SaveData.CreateNew(now);
            _data.revision = 1;
            _data.lastUpdatedUnixMs = now;
            _data.Normalize(now);
            _isLoaded = true;
            PendingConflict = null;

            ClearLegacyProgressionPrefs();

            try
            {
                _localRepository.DeleteSaveFiles();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to delete old local save files: {exception.Message}");
            }

            SaveLocal();
            DataChanged?.Invoke();
            QueueCloudUpload();
        }

        public void RecordRunResult(float survivalTime, int enemyKills, int coinsEarned, int score)
        {
            EnsureLoaded();

            bool changed = true;
            float safeSurvivalTime = Mathf.Max(0f, survivalTime);
            int safeEnemyKills = Mathf.Max(0, enemyKills);
            int safeCoinsEarned = Mathf.Max(0, coinsEarned);
            int safeScore = Mathf.Max(0, score);

            _data.totalRunsCompleted = Mathf.Max(0, _data.totalRunsCompleted) + 1;
            _data.totalEnemyKills = Mathf.Max(0, _data.totalEnemyKills) + safeEnemyKills;

            if (safeSurvivalTime > _data.bestSurvivalTime)
            {
                _data.bestSurvivalTime = safeSurvivalTime;
                changed = true;
            }

            if (safeEnemyKills > _data.bestKillCount)
            {
                _data.bestKillCount = safeEnemyKills;
                changed = true;
            }

            if (safeCoinsEarned > _data.bestCoinsEarned)
            {
                _data.bestCoinsEarned = safeCoinsEarned;
                changed = true;
            }

            if (safeScore > _data.bestScore)
            {
                _data.bestScore = safeScore;
                changed = true;
            }

            if (safeCoinsEarned > 0)
            {
                _data.walletCoins = Mathf.Max(0, _data.walletCoins + safeCoinsEarned);
                _data.lifetimeCoinsEarned = Mathf.Max(0, _data.lifetimeCoinsEarned + safeCoinsEarned);
                changed = true;
            }

            if (changed)
            {
                CommitAndQueueCloudUpload();
            }
        }

        public bool HasSeenCutscene(string cutsceneId)
        {
            EnsureLoaded();
            return _data.HasSeenCutscene(cutsceneId);
        }

        public bool RecordCutsceneSeen(string cutsceneId)
        {
            EnsureLoaded();

            if (!_data.MarkCutsceneSeen(cutsceneId))
            {
                return false;
            }

            CommitAndQueueCloudUpload();
            return true;
        }

        public bool TrySpendWalletCoins(int amount)
        {
            EnsureLoaded();

            int safeAmount = Mathf.Max(0, amount);
            if (_data.walletCoins < safeAmount)
            {
                return false;
            }

            _data.walletCoins -= safeAmount;
            CommitAndQueueCloudUpload();
            return true;
        }

        public bool TryAddDebugWalletCoins(int amount)
        {
            EnsureLoaded();
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                return false;
            }

            int safeAmount = Mathf.Max(0, amount);
            if (safeAmount <= 0)
            {
                return false;
            }

            _data.walletCoins = Mathf.Max(0, _data.walletCoins + safeAmount);
            CommitAndQueueCloudUpload();
            return true;
        }

        public int GetUpgradeLevel(PlayerMetaUpgradeType type)
        {
            EnsureLoaded();
            return _data.GetUpgradeLevel(type);
        }

        public bool TryPurchaseUpgrade(PlayerMetaUpgradeType type, int cost)
        {
            EnsureLoaded();

            if (!PlayerMetaUpgradeService.IsSupportedUpgrade(type)
                || _data.GetUpgradeLevel(type) >= PlayerMetaUpgradeService.GetMaxLevel(type))
            {
                return false;
            }

            int safeCost = Mathf.Max(0, cost);
            if (_data.walletCoins < safeCost)
            {
                return false;
            }

            int fromLevel = _data.GetUpgradeLevel(type);
            int walletBefore = _data.walletCoins;
            _data.walletCoins -= safeCost;
            _data.lifetimeCoinsSpent = Mathf.Max(0, _data.lifetimeCoinsSpent + safeCost);
            _data.SetUpgradeLevel(type, fromLevel + 1);
            CommitAndQueueCloudUpload();
            UpgradePurchased?.Invoke(new UpgradePurchaseTelemetry(
                type,
                fromLevel,
                fromLevel + 1,
                safeCost,
                walletBefore,
                _data.walletCoins,
                _data.totalRunsCompleted));
            return true;
        }

        public bool IsGameplayTutorialCompleted()
        {
            EnsureLoaded();
            return _data.gameplayTutorialCompleted;
        }

        public void MarkGameplayTutorialCompleted()
        {
            EnsureLoaded();
            if (_data.gameplayTutorialCompleted)
            {
                return;
            }

            _data.gameplayTutorialCompleted = true;
            _data.tutorialVersion = Mathf.Max(_data.tutorialVersion, 1);
            CommitAndQueueCloudUpload();
        }

        public bool IsUpgradeTutorialCompleted()
        {
            EnsureLoaded();
            return _data.upgradeTutorialCompleted;
        }

        public void MarkUpgradeTutorialCompleted()
        {
            EnsureLoaded();
            if (_data.upgradeTutorialCompleted)
            {
                return;
            }

            _data.upgradeTutorialCompleted = true;
            _data.tutorialVersion = Mathf.Max(_data.tutorialVersion, 1);
            CommitAndQueueCloudUpload();
        }

        public bool HasGrantedTutorialFirstRunBonus()
        {
            EnsureLoaded();
            return _data.tutorialFirstRunBonusGranted;
        }

        public bool GrantTutorialFirstRunBonusIfNeeded(int amount)
        {
            EnsureLoaded();

            if (_data.tutorialFirstRunBonusGranted)
            {
                return false;
            }

            int safeAmount = Mathf.Max(0, amount);
            _data.walletCoins = Mathf.Max(0, _data.walletCoins + safeAmount);
            _data.tutorialFirstRunBonusGranted = true;
            CommitAndQueueCloudUpload();
            return true;
        }

        public bool GrantMissionRewardOnce(string missionId, int coinAmount)
        {
            EnsureLoaded();

            string safeMissionId = NormalizeMissionId(missionId);
            if (string.IsNullOrEmpty(safeMissionId)
                || _data.HasGrantedMissionReward(safeMissionId))
            {
                return false;
            }

            _data.grantedMissionRewardIds ??= new System.Collections.Generic.List<string>();
            _data.grantedMissionRewardIds.Add(safeMissionId);

            int safeAmount = Mathf.Max(0, coinAmount);
            if (safeAmount > 0)
            {
                _data.walletCoins = Mathf.Max(0, _data.walletCoins + safeAmount);
                _data.lifetimeCoinsEarned = Mathf.Max(0, _data.lifetimeCoinsEarned + safeAmount);
            }

            CommitAndQueueCloudUpload();
            return true;
        }

        public void CommitMissionState()
        {
            EnsureLoaded();
            CommitAndQueueCloudUpload();
        }

        public bool MarkFinalChoiceResolved()
        {
            EnsureLoaded();
            if (_data.finalChoiceResolved)
            {
                return false;
            }

            _data.finalChoiceResolved = true;
            CommitAndQueueCloudUpload();
            return true;
        }

        public bool IsUpdateOnboardingCompleted()
        {
            return IsUpgradeTutorialCompleted();
        }

        public void MarkUpdateOnboardingCompleted()
        {
            MarkUpgradeTutorialCompleted();
        }

        public void ResolvePendingConflict(SaveConflictResolution resolution)
        {
            EnsureLoaded();

            if (PendingConflict == null)
            {
                return;
            }

            if (resolution == SaveConflictResolution.UseCloud && PendingConflict.CloudData != null)
            {
                _data = PendingConflict.CloudData.Clone();
                _data.Normalize(GetCurrentUnixMs());
                PendingConflict = null;
                SaveLocal();
                DataChanged?.Invoke();
                return;
            }

            if (PendingConflict.LocalData != null)
            {
                _data = PendingConflict.LocalData.Clone();
                _data.Normalize(GetCurrentUnixMs());
            }

            PendingConflict = null;
            CommitAndQueueCloudUpload();
        }

        private static long GetCurrentUnixMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static SaveData CreateInitialSaveData(long now)
        {
            SaveData saveData = SaveData.CreateNew(now);

            if (!HasLegacyProgressionData())
            {
                return saveData;
            }

            saveData.bestSurvivalTime = PlayerPrefs.GetFloat(RunStatsTracker.BestSurvivalTimePrefsKey, 0f);
            saveData.bestKillCount = PlayerPrefs.GetInt(RunStatsTracker.BestKillCountPrefsKey, 0);
            saveData.bestCoinsEarned = PlayerPrefs.GetInt(RunStatsTracker.BestCoinsEarnedPrefsKey, 0);
            saveData.bestScore = PlayerPrefs.GetInt(RunStatsTracker.BestScorePrefsKey, 0);
            saveData.totalEnemyKills = saveData.bestKillCount;
            saveData.walletCoins = PlayerPrefs.GetInt(RunStatsTracker.WalletCoinsPrefsKey, 0);
            saveData.lifetimeCoinsEarned = saveData.walletCoins;

            PlayerMetaUpgradeType[] upgradeTypes =
                (PlayerMetaUpgradeType[])Enum.GetValues(typeof(PlayerMetaUpgradeType));

            for (int index = 0; index < upgradeTypes.Length; index++)
            {
                PlayerMetaUpgradeType upgradeType = upgradeTypes[index];
                saveData.SetUpgradeLevel(upgradeType, PlayerPrefs.GetInt(GetLegacyUpgradeLevelKey(upgradeType), 0));
            }

            saveData.revision = 1;
            saveData.lastUpdatedUnixMs = now;
            saveData.Normalize(now);
            Debug.Log("Migrated legacy PlayerPrefs progression into local SaveData.");
            return saveData;
        }

        private static bool HasLegacyProgressionData()
        {
            if (PlayerPrefs.HasKey(RunStatsTracker.BestSurvivalTimePrefsKey)
                || PlayerPrefs.HasKey(RunStatsTracker.BestKillCountPrefsKey)
                || PlayerPrefs.HasKey(RunStatsTracker.BestCoinsEarnedPrefsKey)
                || PlayerPrefs.HasKey(RunStatsTracker.BestScorePrefsKey)
                || PlayerPrefs.HasKey(RunStatsTracker.WalletCoinsPrefsKey))
            {
                return true;
            }

            PlayerMetaUpgradeType[] upgradeTypes =
                (PlayerMetaUpgradeType[])Enum.GetValues(typeof(PlayerMetaUpgradeType));

            for (int index = 0; index < upgradeTypes.Length; index++)
            {
                if (PlayerPrefs.HasKey(GetLegacyUpgradeLevelKey(upgradeTypes[index])))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetLegacyUpgradeLevelKey(PlayerMetaUpgradeType type)
        {
            return LegacyUpgradeLevelKeyPrefix + type;
        }

        private static void ClearLegacyProgressionPrefs()
        {
            PlayerPrefs.DeleteKey(RunStatsTracker.BestSurvivalTimePrefsKey);
            PlayerPrefs.DeleteKey(RunStatsTracker.BestKillCountPrefsKey);
            PlayerPrefs.DeleteKey(RunStatsTracker.BestCoinsEarnedPrefsKey);
            PlayerPrefs.DeleteKey(RunStatsTracker.BestScorePrefsKey);
            PlayerPrefs.DeleteKey(RunStatsTracker.WalletCoinsPrefsKey);

            PlayerMetaUpgradeType[] upgradeTypes =
                (PlayerMetaUpgradeType[])Enum.GetValues(typeof(PlayerMetaUpgradeType));

            for (int index = 0; index < upgradeTypes.Length; index++)
            {
                PlayerPrefs.DeleteKey(GetLegacyUpgradeLevelKey(upgradeTypes[index]));
            }

            PlayerPrefs.Save();
        }

        private async Task TryMergeCloudSaveAsync()
        {
            if (_cloudSaveProvider == null || !_cloudSaveProvider.IsAvailable)
            {
                return;
            }

            try
            {
                CloudSaveLoadResult result = await _cloudSaveProvider.TryLoadAsync();
                if (!result.HasData || result.Data == null)
                {
                    await TryUploadCloudSaveAsync();
                    return;
                }

                SaveData cloudData = result.Data.Clone();
                cloudData.Normalize(GetCurrentUnixMs());

                if (ShouldUseCloudWithoutConflict(_data, cloudData))
                {
                    _data = cloudData;
                    SaveLocal();
                    DataChanged?.Invoke();
                    return;
                }

                if (ShouldUploadLocalWithoutConflict(_data, cloudData))
                {
                    await TryUploadCloudSaveAsync();
                    return;
                }

                PendingConflict = new SaveConflict(_data, cloudData);
                Debug.LogWarning("Save conflict detected. Waiting for an explicit Use Local or Use Cloud resolution.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Cloud save load failed: {exception.Message}");
            }
        }

        private static bool ShouldUseCloudWithoutConflict(SaveData localData, SaveData cloudData)
        {
            if (cloudData == null)
            {
                return false;
            }

            if (localData == null || IsEmptyProgressionData(localData))
            {
                return !IsEmptyProgressionData(cloudData);
            }

            return localData.revision == cloudData.revision
                && cloudData.lastUpdatedUnixMs > localData.lastUpdatedUnixMs;
        }

        private static bool ShouldUploadLocalWithoutConflict(SaveData localData, SaveData cloudData)
        {
            if (localData == null)
            {
                return false;
            }

            if (cloudData == null || IsEmptyProgressionData(cloudData))
            {
                return !IsEmptyProgressionData(localData);
            }

            return localData.revision == cloudData.revision
                && localData.lastUpdatedUnixMs >= cloudData.lastUpdatedUnixMs;
        }

        private static bool IsEmptyProgressionData(SaveData saveData)
        {
            if (saveData == null)
            {
                return true;
            }

            if (saveData.revision > 0
                || saveData.walletCoins > 0
                || saveData.bestSurvivalTime > 0f
                || saveData.bestKillCount > 0
                || saveData.bestCoinsEarned > 0
                || saveData.bestScore > 0
                || saveData.totalEnemyKills > 0
                || saveData.totalRunsCompleted > 0
                || saveData.storyStage > 0
                || saveData.lifetimeGatesSelected > 0
                || saveData.lifetimeMajorGatesSelected > 0
                || saveData.activeMissionProgress > 0f
                || saveData.activeMissionBaseline > 0f
                || saveData.finalChoiceResolved
                || (saveData.completedMissionIds != null && saveData.completedMissionIds.Count > 0)
                || (saveData.grantedMissionRewardIds != null && saveData.grantedMissionRewardIds.Count > 0)
                || (saveData.seenCutsceneIds != null && saveData.seenCutsceneIds.Count > 0))
            {
                return false;
            }

            PlayerMetaUpgradeType[] upgradeTypes =
                (PlayerMetaUpgradeType[])Enum.GetValues(typeof(PlayerMetaUpgradeType));

            for (int index = 0; index < upgradeTypes.Length; index++)
            {
                if (saveData.GetUpgradeLevel(upgradeTypes[index]) > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeMissionId(string missionId)
        {
            return string.IsNullOrWhiteSpace(missionId)
                ? string.Empty
                : missionId.Trim();
        }

        private void CommitAndQueueCloudUpload()
        {
            Touch();
            SaveLocal();
            DataChanged?.Invoke();
            QueueCloudUpload();
        }

        private void Touch()
        {
            _data.revision = Math.Max(0, _data.revision) + 1;
            _data.lastUpdatedUnixMs = GetCurrentUnixMs();
            _data.balanceVersionLastPlayed =
                _Project.Scripts.Data.Balance.CombatScalingConfig.DefaultConfigVersion;
            _data.Normalize(_data.lastUpdatedUnixMs);
        }

        private void SaveLocal()
        {
            try
            {
                _localRepository.Save(_data);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Local save failed: {exception.Message}");
            }
        }

        private void QueueCloudUpload()
        {
            if (_isCloudUploadQueued
                || _cloudSaveProvider == null
                || !_cloudSaveProvider.IsAvailable)
            {
                return;
            }

            _isCloudUploadQueued = true;
            _ = UploadQueuedCloudSaveAsync();
        }

        private async Task UploadQueuedCloudSaveAsync()
        {
            try
            {
                await TryUploadCloudSaveAsync();
            }
            finally
            {
                _isCloudUploadQueued = false;
            }
        }

        private async Task TryUploadCloudSaveAsync()
        {
            if (_cloudSaveProvider == null || !_cloudSaveProvider.IsAvailable)
            {
                return;
            }

            try
            {
                await _cloudSaveProvider.TrySaveAsync(_data.Clone());
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Cloud save upload failed: {exception.Message}");
            }
        }
    }

    public readonly struct UpgradePurchaseTelemetry
    {
        public readonly PlayerMetaUpgradeType UpgradeType;
        public readonly int FromLevel;
        public readonly int ToLevel;
        public readonly int Cost;
        public readonly int WalletBefore;
        public readonly int WalletAfter;
        public readonly int LifetimeRunCount;

        public UpgradePurchaseTelemetry(
            PlayerMetaUpgradeType upgradeType,
            int fromLevel,
            int toLevel,
            int cost,
            int walletBefore,
            int walletAfter,
            int lifetimeRunCount)
        {
            UpgradeType = upgradeType;
            FromLevel = fromLevel;
            ToLevel = toLevel;
            Cost = cost;
            WalletBefore = walletBefore;
            WalletAfter = walletAfter;
            LifetimeRunCount = lifetimeRunCount;
        }
    }
}
