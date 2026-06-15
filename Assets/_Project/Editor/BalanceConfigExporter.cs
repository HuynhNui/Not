using System;
using System.Collections.Generic;
using System.IO;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Systems.Balance;
using _Project.Scripts.Systems.GateSystem;
using UnityEditor;
using UnityEngine;

internal static class BalanceConfigExporter
{
    private const string OutputPath = "Tools/Balance/output/true_gate_balance_v1.json";

    [MenuItem("Tools/Balance/Export True Gate V1 Config")]
    private static void Export()
    {
        CombatScalingConfig combat = LoadOrCreateDefault<CombatScalingConfig>(
            "Assets/_Project/Data/Balance/V1/CombatScalingConfig_v1.asset");
        PlayerMetaBalanceConfig meta = LoadOrCreateDefault<PlayerMetaBalanceConfig>(
            "Assets/_Project/Data/Balance/V1/PlayerMetaBalanceConfig_v1.asset");
        RunPressureConfig pressure = LoadOrCreateDefault<RunPressureConfig>(
            "Assets/_Project/Data/Balance/V1/RunPressureConfig_v1.asset");
        GatePoolConfig gates = LoadOrCreateDefault<GatePoolConfig>(
            "Assets/_Project/Data/Balance/V1/GatePoolConfig_v1.asset");
        EconomyConfig economy = LoadOrCreateDefault<EconomyConfig>(
            "Assets/_Project/Data/Balance/V1/EconomyConfig_v1.asset");

        combat.ValidateValues();
        meta.ValidateValues();
        pressure.ValidateValues();
        gates.ValidateValues();
        economy.ValidateValues();

        var export = new BalanceExport
        {
            schemaVersion = 1,
            exportedUtc = DateTime.UtcNow.ToString("O"),
            balanceVersion = CombatScalingConfig.DefaultConfigVersion,
            combat = BuildCombat(combat),
            metaLevels = BuildMetaLevels(meta, combat),
            pressureSamples = BuildPressureSamples(pressure),
            enemyRoles = BuildEnemyRoles(),
            gateSchedule = BuildGateSchedule(gates),
            gateEntries = BuildGateEntries(gates),
            economy = BuildEconomy(economy)
        };

        string absolutePath = Path.GetFullPath(OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        File.WriteAllText(absolutePath, JsonUtility.ToJson(export, prettyPrint: true));
        Debug.Log($"Exported balance config to {absolutePath}");
    }

    private static T LoadOrCreateDefault<T>(string assetPath)
        where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        return asset != null ? asset : ScriptableObject.CreateInstance<T>();
    }

    private static CombatExport BuildCombat(CombatScalingConfig config)
    {
        return new CombatExport
        {
            fireSoftCapStart = config.FireSoftCapStart,
            fireSoftCapMax = config.FireSoftCapMax,
            baseProjectileCount = config.BaseProjectileCount,
            projectileCoverageCoefficient = config.ProjectileCoverageCoefficient,
            squadCoverageCoefficient = config.SquadCoverageCoefficient,
            followerHpRatio = config.FollowerHpRatio,
            recruitSpawnHpRatio = config.RecruitSpawnHpRatio
        };
    }

    private static List<MetaLevelExport> BuildMetaLevels(
        PlayerMetaBalanceConfig meta,
        CombatScalingConfig combat)
    {
        var result = new List<MetaLevelExport>();

        for (int level = 0; level <= meta.MaxLevel; level++)
        {
            PlayerMetaLevelData data = meta.GetLevelData(level);
            result.Add(new MetaLevelExport
            {
                level = data.Level,
                damage = data.Damage,
                fireRate = data.FireRate,
                maxHp = data.MaxHp,
                projectileCount = data.ProjectileCount,
                squadSize = data.SquadSize,
                cost = data.Cost,
                effectiveDps = BalanceV1Math.EffectiveDps(
                    data.Damage,
                    data.FireRate,
                    data.ProjectileCount,
                    data.SquadSize,
                    combat),
                durability = data.MaxHp * BalanceV1Math.SquadDurabilityFactor(
                    data.SquadSize,
                    combat.FollowerHpRatio)
            });
        }

        return result;
    }

    private static List<PressureSampleExport> BuildPressureSamples(
        RunPressureConfig pressure)
    {
        var result = new List<PressureSampleExport>();

        for (int seconds = 0; seconds <= 720; seconds += 15)
        {
            RunPressureSnapshot sample = pressure.Evaluate(seconds);
            result.Add(new PressureSampleExport
            {
                seconds = seconds,
                activeCap = sample.ActiveCap,
                minimumVisible = sample.MinimumVisible,
                threatBudget = sample.ThreatBudget,
                spawnPerSecond = sample.SpawnPerSecond,
                hpMultiplier = sample.HpMultiplier,
                damageMultiplier = sample.DamageMultiplier,
                speedMultiplier = sample.SpeedMultiplier
            });
        }

        return result;
    }

    private static List<EnemyRoleExport> BuildEnemyRoles()
    {
        var result = new List<EnemyRoleExport>();

        foreach (BalanceEnemyRole role in Enum.GetValues(typeof(BalanceEnemyRole)))
        {
            result.Add(new EnemyRoleExport
            {
                role = role.ToString(),
                unlockSeconds = EnemyRoleBalanceDefaults.GetUnlockTimeSeconds(role),
                threatCost = EnemyRoleBalanceDefaults.GetThreatCost(role),
                rewardPoints = EnemyRoleBalanceDefaults.GetRewardPoints(role)
            });
        }

        return result;
    }

    private static List<GateScheduleExport> BuildGateSchedule(GatePoolConfig gates)
    {
        var result = new List<GateScheduleExport>();
        int totalSets = Mathf.CeilToInt(720f / gates.GateCadenceSeconds);

        for (int set = 1; set <= totalSets; set++)
        {
            float elapsed = set * gates.GateCadenceSeconds;
            result.Add(new GateScheduleExport
            {
                gateSet = set,
                elapsedSeconds = elapsed,
                majorEligible = GateSystem.IsMajorEligibilitySet(
                    set,
                    gates.GateCadenceSeconds,
                    gates.MajorGateCadenceSeconds),
                majorChance = GateSystem.GetMajorChance(elapsed)
            });
        }

        return result;
    }

    private static List<GateEntryExport> BuildGateEntries(GatePoolConfig gates)
    {
        IReadOnlyList<BalanceGateEntry> entries = gates.Entries.Count > 0
            ? gates.Entries
            : GatePoolConfig.CreateDefaultEntries();
        var result = new List<GateEntryExport>();

        for (int index = 0; index < entries.Count; index++)
        {
            BalanceGateEntry entry = entries[index];
            result.Add(new GateEntryExport
            {
                gateId = entry.GateId,
                label = entry.DisplayLabel,
                category = entry.Category.ToString(),
                weight = entry.Weight,
                minimumTimeSeconds = entry.MinTimeSeconds,
                effect = entry.EffectType.ToString(),
                magnitude = entry.Magnitude,
                durationSeconds = entry.DurationSeconds,
                secondaryEffect = entry.SecondaryEffectType.ToString(),
                secondaryMagnitude = entry.SecondaryMagnitude,
                drawback = entry.DrawbackType.ToString(),
                drawbackMagnitude = entry.DrawbackMagnitude
            });
        }

        return result;
    }

    private static EconomyExport BuildEconomy(EconomyConfig economy)
    {
        return new EconomyExport
        {
            rewardScale = economy.RewardScale,
            timeCoinPer30Seconds = economy.TimeCoinPer30Seconds,
            timeScorePerSecond = economy.TimeScorePerSecond,
            eliteCoinBonusMin = economy.EliteCoinBonusMin,
            eliteCoinBonusMax = economy.EliteCoinBonusMax,
            storyMilestones = new List<int>(economy.StoryMilestones)
        };
    }

    [Serializable]
    private sealed class BalanceExport
    {
        public int schemaVersion;
        public string exportedUtc;
        public string balanceVersion;
        public CombatExport combat;
        public List<MetaLevelExport> metaLevels;
        public List<PressureSampleExport> pressureSamples;
        public List<EnemyRoleExport> enemyRoles;
        public List<GateScheduleExport> gateSchedule;
        public List<GateEntryExport> gateEntries;
        public EconomyExport economy;
    }

    [Serializable]
    private sealed class CombatExport
    {
        public float fireSoftCapStart;
        public float fireSoftCapMax;
        public int baseProjectileCount;
        public float projectileCoverageCoefficient;
        public float squadCoverageCoefficient;
        public float followerHpRatio;
        public float recruitSpawnHpRatio;
    }

    [Serializable]
    private sealed class MetaLevelExport
    {
        public int level;
        public float damage;
        public float fireRate;
        public float maxHp;
        public int projectileCount;
        public int squadSize;
        public int cost;
        public float effectiveDps;
        public float durability;
    }

    [Serializable]
    private sealed class PressureSampleExport
    {
        public int seconds;
        public int activeCap;
        public int minimumVisible;
        public float threatBudget;
        public float spawnPerSecond;
        public float hpMultiplier;
        public float damageMultiplier;
        public float speedMultiplier;
    }

    [Serializable]
    private sealed class EnemyRoleExport
    {
        public string role;
        public float unlockSeconds;
        public float threatCost;
        public float rewardPoints;
    }

    [Serializable]
    private sealed class GateScheduleExport
    {
        public int gateSet;
        public float elapsedSeconds;
        public bool majorEligible;
        public float majorChance;
    }

    [Serializable]
    private sealed class GateEntryExport
    {
        public string gateId;
        public string label;
        public string category;
        public float weight;
        public float minimumTimeSeconds;
        public string effect;
        public float magnitude;
        public float durationSeconds;
        public string secondaryEffect;
        public float secondaryMagnitude;
        public string drawback;
        public float drawbackMagnitude;
    }

    [Serializable]
    private sealed class EconomyExport
    {
        public float rewardScale;
        public float timeCoinPer30Seconds;
        public float timeScorePerSecond;
        public float eliteCoinBonusMin;
        public float eliteCoinBonusMax;
        public List<int> storyMilestones;
    }
}
