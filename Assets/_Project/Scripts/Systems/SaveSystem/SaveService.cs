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

        public void RecordRunResult(float survivalTime, int enemyKills, int coinsEarned, int score)
        {
            EnsureLoaded();

            bool changed = true;
            float safeSurvivalTime = Mathf.Max(0f, survivalTime);
            int safeEnemyKills = Mathf.Max(0, enemyKills);
            int safeCoinsEarned = Mathf.Max(0, coinsEarned);
            int safeScore = Mathf.Max(0, score);

            _data.totalRunsCompleted = Mathf.Max(0, _data.totalRunsCompleted) + 1;

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

        public int GetUpgradeLevel(PlayerMetaUpgradeType type)
        {
            EnsureLoaded();
            return _data.GetUpgradeLevel(type);
        }

        public bool TryPurchaseUpgrade(PlayerMetaUpgradeType type, int cost)
        {
            EnsureLoaded();

            if (!PlayerMetaUpgradeService.IsSupportedUpgrade(type)
                || _data.GetUpgradeLevel(type) >= PlayerMetaUpgradeService.MaxUpgradeLevel)
            {
                return false;
            }

            int safeCost = Mathf.Max(0, cost);
            if (_data.walletCoins < safeCost)
            {
                return false;
            }

            _data.walletCoins -= safeCost;
            _data.SetUpgradeLevel(type, _data.GetUpgradeLevel(type) + 1);
            CommitAndQueueCloudUpload();
            return true;
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
            saveData.walletCoins = PlayerPrefs.GetInt(RunStatsTracker.WalletCoinsPrefsKey, 0);

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
                || saveData.totalRunsCompleted > 0
                || saveData.storyStage > 0
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
}
