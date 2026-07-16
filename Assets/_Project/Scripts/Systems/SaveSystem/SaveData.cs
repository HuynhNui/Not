using System;
using System.Collections.Generic;
using _Project.Scripts.Systems.ProgressionSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.SaveSystem
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 10;
        public const string FirstMissionId = "boot_finish_tutorial";
        public const string FinalMissionId = "terminal_250000_total_kills";

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
        public int lifetimeCoinsEarned;
        public int lifetimeCoinsSpent;
        public int totalRunsCompleted;
        public int storyStage;
        public bool gameplayTutorialCompleted;
        public bool upgradeTutorialCompleted;
        public bool tutorialFirstRunBonusGranted;
        public int tutorialVersion;
        public string activeMissionId = FirstMissionId;
        public float activeMissionProgress;
        public float activeMissionBaseline;
        public List<string> completedMissionIds = new List<string>();
        public List<string> grantedMissionRewardIds = new List<string>();
        public int lifetimeGatesSelected;
        public int lifetimeMajorGatesSelected;
        public bool missionNotificationUnread = true;
        public bool finalChoiceResolved;
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
            int sourceSchemaVersion = schemaVersion;
            bool shouldPreserveLegacyTutorialCompletion =
                sourceSchemaVersion < 6
                && HasLegacyProgressEvidence();

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
            lifetimeCoinsEarned = Mathf.Max(Mathf.Max(0, lifetimeCoinsEarned), walletCoins);
            lifetimeCoinsSpent = Mathf.Max(0, lifetimeCoinsSpent);
            totalRunsCompleted = Mathf.Max(0, totalRunsCompleted);
            storyStage = Mathf.Max(0, storyStage);
            tutorialVersion = Mathf.Max(0, tutorialVersion);
            if (completedMissionIds == null)
            {
                completedMissionIds = new List<string>();
            }

            if (grantedMissionRewardIds == null)
            {
                grantedMissionRewardIds = new List<string>();
            }

            activeMissionId = NormalizeMissionId(activeMissionId);
            if (string.IsNullOrEmpty(activeMissionId))
            {
                if (ContainsMissionId(completedMissionIds, FinalMissionId))
                {
                    missionNotificationUnread = false;
                }
                else
                {
                    activeMissionId = FirstMissionId;
                    if (sourceSchemaVersion < CurrentSchemaVersion)
                    {
                        missionNotificationUnread = true;
                    }
                }
            }

            activeMissionProgress = Mathf.Max(0f, activeMissionProgress);
            activeMissionBaseline = Mathf.Max(0f, activeMissionBaseline);
            lifetimeGatesSelected = Mathf.Max(0, lifetimeGatesSelected);
            lifetimeMajorGatesSelected = Mathf.Clamp(
                lifetimeMajorGatesSelected,
                0,
                lifetimeGatesSelected);
            if (shouldPreserveLegacyTutorialCompletion)
            {
                gameplayTutorialCompleted = true;
                tutorialVersion = Mathf.Max(tutorialVersion, 1);
            }

            if (upgradeLevels == null)
            {
                upgradeLevels = new List<UpgradeLevelSaveEntry>();
            }

            if (seenCutsceneIds == null)
            {
                seenCutsceneIds = new List<string>();
            }

            RemoveDuplicateOrInvalidUpgradeEntries(sourceSchemaVersion);
            EnsureAllUpgradeEntries();
            NormalizeSeenCutsceneIds();
            NormalizeMissionIds();
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

        public bool HasGrantedMissionReward(string missionId)
        {
            string safeId = NormalizeMissionId(missionId);
            if (string.IsNullOrEmpty(safeId) || grantedMissionRewardIds == null)
            {
                return false;
            }

            for (int index = 0; index < grantedMissionRewardIds.Count; index++)
            {
                if (string.Equals(grantedMissionRewardIds[index], safeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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
                lifetimeCoinsEarned = lifetimeCoinsEarned,
                lifetimeCoinsSpent = lifetimeCoinsSpent,
                totalRunsCompleted = totalRunsCompleted,
                storyStage = storyStage,
                gameplayTutorialCompleted = gameplayTutorialCompleted,
                upgradeTutorialCompleted = upgradeTutorialCompleted,
                tutorialFirstRunBonusGranted = tutorialFirstRunBonusGranted,
                tutorialVersion = tutorialVersion,
                activeMissionId = activeMissionId,
                activeMissionProgress = activeMissionProgress,
                activeMissionBaseline = activeMissionBaseline,
                completedMissionIds = new List<string>(),
                grantedMissionRewardIds = new List<string>(),
                lifetimeGatesSelected = lifetimeGatesSelected,
                lifetimeMajorGatesSelected = lifetimeMajorGatesSelected,
                missionNotificationUnread = missionNotificationUnread,
                finalChoiceResolved = finalChoiceResolved,
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

            CopyNormalizedMissionIds(completedMissionIds, clone.completedMissionIds);
            CopyNormalizedMissionIds(grantedMissionRewardIds, clone.grantedMissionRewardIds);

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

        private void RemoveDuplicateOrInvalidUpgradeEntries(int sourceSchemaVersion)
        {
            var seenTypes = new HashSet<string>();
            bool loggedProjectileMigration = false;
            bool loggedSquadMigration = false;

            for (int index = upgradeLevels.Count - 1; index >= 0; index--)
            {
                UpgradeLevelSaveEntry entry = upgradeLevels[index];
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.upgradeType)
                    || !Enum.TryParse(entry.upgradeType, out PlayerMetaUpgradeType upgradeType)
                    || !seenTypes.Add(entry.upgradeType))
                {
                    upgradeLevels.RemoveAt(index);
                    continue;
                }

                if (upgradeType == PlayerMetaUpgradeType.ProjectileCount
                    && entry.level > PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.ProjectileCount))
                {
                    entry.level = PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.ProjectileCount);
                    if (!loggedProjectileMigration
                        && (Application.isEditor || Debug.isDebugBuild))
                    {
                        loggedProjectileMigration = true;
                        Debug.Log(
                            "[META MIGRATION] ProjectileCount level 3-5 -> 2; max projectile value now 3.");
                    }
                }

                if (upgradeType == PlayerMetaUpgradeType.SquadSize
                    && entry.level > PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.SquadSize))
                {
                    entry.level = PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.SquadSize);
                    if (!loggedSquadMigration
                        && (Application.isEditor || Debug.isDebugBuild))
                    {
                        loggedSquadMigration = true;
                        Debug.Log(
                            "[META MIGRATION] SquadSize level 4-5 -> 3; max squad value now 4.");
                    }
                }

                entry.level = Mathf.Clamp(entry.level, 0, PlayerMetaUpgradeService.GetMaxLevel(upgradeType));
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

        private void NormalizeMissionIds()
        {
            completedMissionIds = BuildNormalizedMissionIdList(completedMissionIds);
            grantedMissionRewardIds = BuildNormalizedMissionIdList(grantedMissionRewardIds);
        }

        private bool HasLegacyProgressEvidence()
        {
            if (revision > 0
                || bestSurvivalTime > 0f
                || bestKillCount > 0
                || bestCoinsEarned > 0
                || bestScore > 0
                || totalEnemyKills > 0
                || walletCoins > 0
                || lifetimeCoinsEarned > 0
                || lifetimeCoinsSpent > 0
                || totalRunsCompleted > 0
                || storyStage > 0
                || lifetimeGatesSelected > 0
                || lifetimeMajorGatesSelected > 0
                || finalChoiceResolved
                || (completedMissionIds != null && completedMissionIds.Count > 0)
                || (grantedMissionRewardIds != null && grantedMissionRewardIds.Count > 0)
                || upgradeTutorialCompleted
                || tutorialFirstRunBonusGranted
                || (seenCutsceneIds != null && seenCutsceneIds.Count > 0))
            {
                return true;
            }

            if (upgradeLevels == null)
            {
                return false;
            }

            for (int index = 0; index < upgradeLevels.Count; index++)
            {
                UpgradeLevelSaveEntry entry = upgradeLevels[index];
                if (entry != null && entry.level > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeCutsceneId(string cutsceneId)
        {
            return string.IsNullOrWhiteSpace(cutsceneId)
                ? string.Empty
                : cutsceneId.Trim();
        }

        private static string NormalizeMissionId(string missionId)
        {
            return string.IsNullOrWhiteSpace(missionId)
                ? string.Empty
                : missionId.Trim();
        }

        private static List<string> BuildNormalizedMissionIdList(List<string> missionIds)
        {
            var cleanedIds = new List<string>();
            if (missionIds == null)
            {
                return cleanedIds;
            }

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < missionIds.Count; index++)
            {
                string safeId = NormalizeMissionId(missionIds[index]);
                if (string.IsNullOrEmpty(safeId) || !seenIds.Add(safeId))
                {
                    continue;
                }

                cleanedIds.Add(safeId);
            }

            return cleanedIds;
        }

        private static bool ContainsMissionId(List<string> missionIds, string missionId)
        {
            string safeMissionId = NormalizeMissionId(missionId);
            if (string.IsNullOrEmpty(safeMissionId) || missionIds == null)
            {
                return false;
            }

            for (int index = 0; index < missionIds.Count; index++)
            {
                if (string.Equals(
                    NormalizeMissionId(missionIds[index]),
                    safeMissionId,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CopyNormalizedMissionIds(List<string> source, List<string> destination)
        {
            if (destination == null)
            {
                return;
            }

            List<string> cleanedIds = BuildNormalizedMissionIdList(source);
            for (int index = 0; index < cleanedIds.Count; index++)
            {
                destination.Add(cleanedIds[index]);
            }
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
