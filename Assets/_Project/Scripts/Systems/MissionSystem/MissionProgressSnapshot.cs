using _Project.Scripts.Systems.ProgressionSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.MissionSystem
{
    public readonly struct MissionProgressSnapshot
    {
        public readonly bool GameplayTutorialCompleted;
        public readonly int TotalRunsCompleted;
        public readonly int LifetimeGatesSelected;
        public readonly int LifetimeMajorGatesSelected;
        public readonly int TotalEnemyKills;
        public readonly int CurrentRunEnemyKills;
        public readonly float CurrentRunSurvivalTime;
        public readonly bool FinalChoiceResolved;
        public readonly int DamageLevel;
        public readonly int FireRateLevel;
        public readonly int MaxHpLevel;
        public readonly int ProjectileCountLevel;
        public readonly int SquadSizeLevel;
        public readonly int SquadSizeValue;

        public MissionProgressSnapshot(
            bool gameplayTutorialCompleted = false,
            int totalRunsCompleted = 0,
            int lifetimeGatesSelected = 0,
            int lifetimeMajorGatesSelected = 0,
            int totalEnemyKills = 0,
            int currentRunEnemyKills = 0,
            float currentRunSurvivalTime = 0f,
            bool finalChoiceResolved = false,
            int damageLevel = 0,
            int fireRateLevel = 0,
            int maxHpLevel = 0,
            int projectileCountLevel = 0,
            int squadSizeLevel = 0,
            int squadSizeValue = 1)
        {
            GameplayTutorialCompleted = gameplayTutorialCompleted;
            TotalRunsCompleted = Mathf.Max(0, totalRunsCompleted);
            LifetimeGatesSelected = Mathf.Max(0, lifetimeGatesSelected);
            LifetimeMajorGatesSelected = Mathf.Max(0, lifetimeMajorGatesSelected);
            TotalEnemyKills = Mathf.Max(0, totalEnemyKills);
            CurrentRunEnemyKills = Mathf.Max(0, currentRunEnemyKills);
            CurrentRunSurvivalTime = Mathf.Max(0f, currentRunSurvivalTime);
            FinalChoiceResolved = finalChoiceResolved;
            DamageLevel = Mathf.Clamp(damageLevel, 0, PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.Damage));
            FireRateLevel = Mathf.Clamp(fireRateLevel, 0, PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.FireRate));
            MaxHpLevel = Mathf.Clamp(maxHpLevel, 0, PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.MaxHp));
            ProjectileCountLevel = Mathf.Clamp(
                projectileCountLevel,
                0,
                PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.ProjectileCount));
            SquadSizeLevel = Mathf.Clamp(squadSizeLevel, 0, PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.SquadSize));
            SquadSizeValue = Mathf.Max(1, squadSizeValue);
        }

        public int CoreUpgradeLevelTotal =>
            DamageLevel + FireRateLevel + MaxHpLevel + ProjectileCountLevel + SquadSizeLevel;

        public int GetUpgradeLevel(PlayerMetaUpgradeType upgradeType)
        {
            return upgradeType switch
            {
                PlayerMetaUpgradeType.Damage => DamageLevel,
                PlayerMetaUpgradeType.FireRate => FireRateLevel,
                PlayerMetaUpgradeType.MaxHp => MaxHpLevel,
                PlayerMetaUpgradeType.ProjectileCount => ProjectileCountLevel,
                PlayerMetaUpgradeType.SquadSize => SquadSizeLevel,
                _ => 0
            };
        }

        public int CountCoreUpgradesAtOrAboveLevel(int level)
        {
            int safeLevel = Mathf.Max(0, level);
            int count = 0;
            count += DamageLevel >= safeLevel ? 1 : 0;
            count += FireRateLevel >= safeLevel ? 1 : 0;
            count += MaxHpLevel >= safeLevel ? 1 : 0;
            count += ProjectileCountLevel >= safeLevel ? 1 : 0;
            count += SquadSizeLevel >= safeLevel ? 1 : 0;
            return count;
        }

        public int CountMaxedCoreUpgrades()
        {
            int count = 0;
            count += IsMaxed(PlayerMetaUpgradeType.Damage, DamageLevel) ? 1 : 0;
            count += IsMaxed(PlayerMetaUpgradeType.FireRate, FireRateLevel) ? 1 : 0;
            count += IsMaxed(PlayerMetaUpgradeType.MaxHp, MaxHpLevel) ? 1 : 0;
            count += IsMaxed(PlayerMetaUpgradeType.ProjectileCount, ProjectileCountLevel) ? 1 : 0;
            count += IsMaxed(PlayerMetaUpgradeType.SquadSize, SquadSizeLevel) ? 1 : 0;
            return count;
        }

        private static bool IsMaxed(PlayerMetaUpgradeType upgradeType, int level)
        {
            int maxLevel = PlayerMetaUpgradeService.GetMaxLevel(upgradeType);
            return maxLevel > 0 && level >= maxLevel;
        }
    }

    public readonly struct MissionProgressResult
    {
        public readonly float ProgressValue;
        public readonly float TargetValue;
        public readonly float NormalizedProgress;
        public readonly bool IsComplete;

        public MissionProgressResult(float progressValue, float targetValue)
        {
            ProgressValue = Mathf.Max(0f, progressValue);
            TargetValue = Mathf.Max(0f, targetValue);
            NormalizedProgress = TargetValue > 0f
                ? Mathf.Clamp01(ProgressValue / TargetValue)
                : 1f;
            IsComplete = ProgressValue >= TargetValue;
        }
    }
}
