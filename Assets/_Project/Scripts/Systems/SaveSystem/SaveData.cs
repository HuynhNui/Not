using System;
using System.Collections.Generic;
using _Project.Scripts.Systems.ProgressionSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.SaveSystem
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 4;

        public int schemaVersion = CurrentSchemaVersion;
        public string balanceVersionLastPlayed = _Project.Scripts.Data.Balance.CombatScalingConfig.DefaultConfigVersion;
        public long revision;
        public long lastUpdatedUnixMs;
        public float bestSurvivalTime;
        public int bestKillCount;
        public int bestCoinsEarned;
        public int bestScore;
        public int totalEnemyKills;
        public int walletCoins;
        public int totalRunsCompleted;
        public int storyStage;
        public List<UpgradeLevelSaveEntry> upgradeLevels = new List<UpgradeLevelSaveEntry>();
        public List<string> seenCutsceneIds = new List<string>();

        public static SaveData CreateNew(long timestampUnixMs)
        {
            var saveData = new SaveData
            {
                schemaVersion = CurrentSchemaVersion,
                revision = 0,
                lastUpdatedUnixMs = timestampUnixMs
            };

            saveData.Normalize(timestampUnixMs);
            return saveData;
        }

        public void Normalize(long fallbackTimestampUnixMs)
        {
            schemaVersion = CurrentSchemaVersion;
            balanceVersionLastPlayed = string.IsNullOrWhiteSpace(balanceVersionLastPlayed)
                ? _Project.Scripts.Data.Balance.CombatScalingConfig.DefaultConfigVersion
                : balanceVersionLastPlayed.Trim();
            revision = Math.Max(0, revision);
            lastUpdatedUnixMs = lastUpdatedUnixMs > 0 ? lastUpdatedUnixMs : fallbackTimestampUnixMs;
            bestSurvivalTime = Mathf.Max(0f, bestSurvivalTime);
            bestKillCount = Mathf.Max(0, bestKillCount);
            bestCoinsEarned = Mathf.Max(0, bestCoinsEarned);
            bestScore = Mathf.Max(0, bestScore);
            totalEnemyKills = Mathf.Max(Mathf.Max(0, totalEnemyKills), bestKillCount);
            walletCoins = Mathf.Max(0, walletCoins);
            totalRunsCompleted = Mathf.Max(0, totalRunsCompleted);
            storyStage = Mathf.Max(0, storyStage);

            if (upgradeLevels == null)
            {
                upgradeLevels = new List<UpgradeLevelSaveEntry>();
            }

            if (seenCutsceneIds == null)
            {
                seenCutsceneIds = new List<string>();
            }

            RemoveDuplicateOrInvalidUpgradeEntries();
            EnsureAllUpgradeEntries();
            NormalizeSeenCutsceneIds();
        }

        public int GetUpgradeLevel(PlayerMetaUpgradeType type)
        {
            UpgradeLevelSaveEntry entry = FindUpgradeEntry(type);
            return entry != null ? Mathf.Max(0, entry.level) : 0;
        }

        public void SetUpgradeLevel(PlayerMetaUpgradeType type, int level)
        {
            UpgradeLevelSaveEntry entry = FindUpgradeEntry(type);
            if (entry == null)
            {
                entry = new UpgradeLevelSaveEntry(type, 0);
                upgradeLevels.Add(entry);
            }

            entry.level = Mathf.Max(0, level);
        }

        public bool HasSeenCutscene(string cutsceneId)
        {
            string safeId = NormalizeCutsceneId(cutsceneId);
            if (string.IsNullOrEmpty(safeId) || seenCutsceneIds == null)
            {
                return false;
            }

            for (int index = 0; index < seenCutsceneIds.Count; index++)
            {
                if (string.Equals(seenCutsceneIds[index], safeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool MarkCutsceneSeen(string cutsceneId)
        {
            string safeId = NormalizeCutsceneId(cutsceneId);
            if (string.IsNullOrEmpty(safeId))
            {
                return false;
            }

            seenCutsceneIds ??= new List<string>();

            if (HasSeenCutscene(safeId))
            {
                return false;
            }

            seenCutsceneIds.Add(safeId);
            storyStage = Mathf.Max(storyStage, seenCutsceneIds.Count);
            return true;
        }

        public SaveData Clone()
        {
            var clone = new SaveData
            {
                schemaVersion = schemaVersion,
                balanceVersionLastPlayed = balanceVersionLastPlayed,
                revision = revision,
                lastUpdatedUnixMs = lastUpdatedUnixMs,
                bestSurvivalTime = bestSurvivalTime,
                bestKillCount = bestKillCount,
                bestCoinsEarned = bestCoinsEarned,
                bestScore = bestScore,
                totalEnemyKills = totalEnemyKills,
                walletCoins = walletCoins,
                totalRunsCompleted = totalRunsCompleted,
                storyStage = storyStage,
                upgradeLevels = new List<UpgradeLevelSaveEntry>(),
                seenCutsceneIds = new List<string>()
            };

            if (upgradeLevels != null)
            {
                for (int index = 0; index < upgradeLevels.Count; index++)
                {
                    UpgradeLevelSaveEntry entry = upgradeLevels[index];
                    if (entry == null)
                    {
                        continue;
                    }

                    clone.upgradeLevels.Add(new UpgradeLevelSaveEntry(entry.upgradeType, entry.level));
                }
            }

            if (seenCutsceneIds != null)
            {
                for (int index = 0; index < seenCutsceneIds.Count; index++)
                {
                    string safeId = NormalizeCutsceneId(seenCutsceneIds[index]);
                    if (!string.IsNullOrEmpty(safeId))
                    {
                        clone.seenCutsceneIds.Add(safeId);
                    }
                }
            }

            return clone;
        }

        private void EnsureAllUpgradeEntries()
        {
            PlayerMetaUpgradeType[] upgradeTypes =
                (PlayerMetaUpgradeType[])Enum.GetValues(typeof(PlayerMetaUpgradeType));

            for (int index = 0; index < upgradeTypes.Length; index++)
            {
                PlayerMetaUpgradeType type = upgradeTypes[index];
                if (FindUpgradeEntry(type) == null)
                {
                    upgradeLevels.Add(new UpgradeLevelSaveEntry(type, 0));
                }
            }
        }

        private void RemoveDuplicateOrInvalidUpgradeEntries()
        {
            var seenTypes = new HashSet<string>();

            for (int index = upgradeLevels.Count - 1; index >= 0; index--)
            {
                UpgradeLevelSaveEntry entry = upgradeLevels[index];
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.upgradeType)
                    || !Enum.TryParse(entry.upgradeType, out PlayerMetaUpgradeType _)
                    || !seenTypes.Add(entry.upgradeType))
                {
                    upgradeLevels.RemoveAt(index);
                    continue;
                }

                entry.level = Mathf.Clamp(entry.level, 0, PlayerMetaUpgradeService.MaxUpgradeLevel);
            }
        }

        private void NormalizeSeenCutsceneIds()
        {
            var cleanedIds = new List<string>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < seenCutsceneIds.Count; index++)
            {
                string safeId = NormalizeCutsceneId(seenCutsceneIds[index]);
                if (string.IsNullOrEmpty(safeId) || !seenIds.Add(safeId))
                {
                    continue;
                }

                cleanedIds.Add(safeId);
            }

            seenCutsceneIds = cleanedIds;
            storyStage = Mathf.Max(storyStage, seenCutsceneIds.Count);
        }

        private static string NormalizeCutsceneId(string cutsceneId)
        {
            return string.IsNullOrWhiteSpace(cutsceneId)
                ? string.Empty
                : cutsceneId.Trim();
        }

        private UpgradeLevelSaveEntry FindUpgradeEntry(PlayerMetaUpgradeType type)
        {
            string key = type.ToString();
            if (upgradeLevels == null)
            {
                return null;
            }

            for (int index = 0; index < upgradeLevels.Count; index++)
            {
                UpgradeLevelSaveEntry entry = upgradeLevels[index];
                if (entry != null && string.Equals(entry.upgradeType, key, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class UpgradeLevelSaveEntry
    {
        public string upgradeType;
        public int level;

        public UpgradeLevelSaveEntry()
        {
        }

        public UpgradeLevelSaveEntry(string upgradeType, int level)
        {
            this.upgradeType = upgradeType;
            this.level = level;
        }

        public UpgradeLevelSaveEntry(PlayerMetaUpgradeType upgradeType, int level)
            : this(upgradeType.ToString(), level)
        {
        }
    }
}
