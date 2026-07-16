using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _Project.Scripts.Core.GameLoop;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Systems.Balance;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.ProgressionSystem;
using UnityEditor;
using UnityEngine;

internal static class BalanceConfigExporter
{
    private const string OutputPath = "Tools/Balance/output/true_gate_balance_v1.json";
    private const string BenchmarkBootstrapPath =
        "Assets/_Project/Data/Balance/V1_3_3_EliteSquad/BalanceBootstrapConfig_v1_3_3_EliteSquad.asset";

    [MenuItem("Tools/Balance/Export True Gate V1 Config")]
    private static void Export()
    {
        CombatScalingConfig combat = LoadOrCreateDefault<CombatScalingConfig>(
            "Assets/_Project/Data/Balance/V1/CombatScalingConfig_v1.asset");
        PlayerMetaBalanceConfig meta = LoadOrCreateDefault<PlayerMetaBalanceConfig>(
            "Assets/_Project/Data/Balance/V1/PlayerMetaBalanceConfig_v1.asset");
        PlayerMetaEconomyConfig metaEconomy = LoadOrCreateDefault<PlayerMetaEconomyConfig>(
            "Assets/_Project/Data/Balance/V1_4_Economy/PlayerMetaEconomyConfig_v1_4_Run45.asset");
        RunPressureConfig pressure = LoadOrCreateDefault<RunPressureConfig>(
            "Assets/_Project/Data/Balance/V1/RunPressureConfig_v1.asset");
        GatePoolConfig gates = LoadOrCreateDefault<GatePoolConfig>(
            "Assets/_Project/Data/Balance/V1/GatePoolConfig_v1.asset");
        EconomyConfig economy = LoadOrCreateDefault<EconomyConfig>(
            "Assets/_Project/Data/Balance/V1/EconomyConfig_v1.asset");

        combat.ValidateValues();
        meta.ValidateValues();
        metaEconomy.ValidateValues();
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
            metaTracks = BuildMetaTracks(meta, metaEconomy, combat),
            pressureSamples = BuildPressureSamples(pressure),
            progressionCheckpoints = BuildProgressionCheckpoints(combat),
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

    [MenuItem("Tools/Balance/Export Active Bootstrap Config")]
    private static void ExportActiveBootstrapConfig()
    {
        BalanceBootstrapConfig bootstrap = ResolveActiveBootstrap();
        if (bootstrap == null)
        {
            Debug.LogError("No BalanceBootstrapConfig found in selection, active GameManager, or benchmark fallback path.");
            return;
        }

        bootstrap.ValidateValues();
        CombatScalingConfig combat = bootstrap.CombatScalingConfig;
        PlayerMetaBalanceConfig meta = bootstrap.PlayerMetaBalanceConfig;
        PlayerMetaEconomyConfig metaEconomy = bootstrap.PlayerMetaEconomyConfig;
        RunPressureConfig pressure = bootstrap.RunPressureConfig;
        GatePoolConfig gates = bootstrap.GatePoolConfig;
        EconomyConfig economy = bootstrap.EconomyConfig;
        GateScalingProfile gateScaling = bootstrap.GateScalingProfile;
        BalanceBenchmarkProfile benchmark = bootstrap.ActiveBenchmarkProfile;

        combat?.ValidateValues();
        meta?.ValidateValues();
        metaEconomy?.ValidateValues();
        pressure?.ValidateValues();
        gates?.ValidateValues();
        economy?.ValidateValues();
        gateScaling?.ValidateValues();

        string safeVersion = MakeSafeFileName(bootstrap.ActiveBalanceVersion);
        string outputDirectory = Path.GetFullPath(
            Path.Combine("Tools/Balance/output", safeVersion));
        Directory.CreateDirectory(outputDirectory);

        var export = new BalanceExport
        {
            schemaVersion = 2,
            exportedUtc = DateTime.UtcNow.ToString("O"),
            balanceVersion = bootstrap.ActiveBalanceVersion,
            combat = combat != null ? BuildCombat(combat) : null,
            metaLevels = meta != null && combat != null ? BuildMetaLevels(meta, combat) : new List<MetaLevelExport>(),
            metaTracks = meta != null ? BuildMetaTracks(meta, metaEconomy, combat) : new List<MetaTrackExport>(),
            pressureSamples = pressure != null ? BuildPressureSamples(pressure) : new List<PressureSampleExport>(),
            progressionCheckpoints = BuildProgressionCheckpoints(combat),
            enemyRoles = BuildEnemyRoles(bootstrap),
            gateSchedule = gates != null ? BuildGateSchedule(gates, gateScaling) : new List<GateScheduleExport>(),
            gateEntries = gates != null ? BuildGateEntries(gates) : new List<GateEntryExport>(),
            gateScaling = gateScaling != null ? BuildGateScaling(gateScaling) : null,
            benchmarkPreset = bootstrap.BenchmarkPreset.ToString(),
            benchmark = benchmark != null ? BuildBenchmark(benchmark, combat) : null,
            oldRunCapBenchmark = bootstrap.RunCapBenchmarkProfile != null
                ? BuildBenchmark(bootstrap.RunCapBenchmarkProfile, combat)
                : null,
            damageForwardCapBenchmark = bootstrap.DamageForwardCapBenchmarkProfile != null
                ? BuildBenchmark(bootstrap.DamageForwardCapBenchmarkProfile, combat)
                : null,
            economy = economy != null ? BuildEconomy(economy, metaEconomy) : null
        };

        string jsonPath = Path.Combine(outputDirectory, $"true_gate_{safeVersion}.json");
        string phasePath = Path.Combine(outputDirectory, "gate_phase_values.csv");
        string curvePath = Path.Combine(outputDirectory, "benchmark_target_curve.csv");
        string progressionPath = Path.Combine(outputDirectory, "progression_checkpoints.csv");
        File.WriteAllText(jsonPath, JsonUtility.ToJson(export, prettyPrint: true));
        File.WriteAllText(phasePath, BuildGatePhaseCsv(gateScaling));
        File.WriteAllText(curvePath, BuildBenchmarkCurveCsv(benchmark, gateScaling, combat));
        File.WriteAllText(progressionPath, BuildProgressionCheckpointCsv(combat));
        Debug.Log($"Exported active bootstrap config to {outputDirectory}");
    }

    private static T LoadOrCreateDefault<T>(string assetPath)
        where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        return asset != null ? asset : ScriptableObject.CreateInstance<T>();
    }

    private static BalanceBootstrapConfig ResolveActiveBootstrap()
    {
        if (Selection.activeObject is BalanceBootstrapConfig selected)
        {
            return selected;
        }

        GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>(
            FindObjectsInactive.Include);
        if (manager != null)
        {
            var serializedObject = new SerializedObject(manager);
            UnityEngine.Object reference = serializedObject.FindProperty("balanceConfig").objectReferenceValue;
            if (reference is BalanceBootstrapConfig sceneBootstrap)
            {
                return sceneBootstrap;
            }
        }

        return AssetDatabase.LoadAssetAtPath<BalanceBootstrapConfig>(BenchmarkBootstrapPath);
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
            recruitSpawnHpRatio = config.RecruitSpawnHpRatio,
            squadPowerModel = config.SquadPowerModel.ToString()
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
                    combat)
            });
        }

        return result;
    }

    private static List<MetaTrackExport> BuildMetaTracks(
        PlayerMetaBalanceConfig meta,
        PlayerMetaEconomyConfig metaEconomy,
        CombatScalingConfig combat)
    {
        var result = new List<MetaTrackExport>();

        for (int index = 0; index < PlayerMetaUpgradeService.Definitions.Length; index++)
        {
            UpgradeDefinition definition = PlayerMetaUpgradeService.Definitions[index];
            int maxLevel = Mathf.Clamp(definition.MaxLevel, 0, meta.MaxLevel);
            var values = new List<float>();
            var costs = new List<int>();
            var purchasableLevels = new List<MetaTrackLevelExport>();

            for (int level = 0; level <= maxLevel; level++)
            {
                PlayerMetaLevelData data = meta.GetLevelData(level);
                values.Add(GetMetaTrackValue(definition.Type, data));
                int purchaseCost = level == 0
                    ? 0
                    : metaEconomy != null
                        ? metaEconomy.GetPurchaseCost(definition.Type, level - 1)
                        : data.Cost;
                costs.Add(purchaseCost);

                if (level > 0)
                {
                    PlayerMetaLevelData previousData = meta.GetLevelData(level - 1);
                    purchasableLevels.Add(new MetaTrackLevelExport
                    {
                        level = level,
                        value = GetMetaTrackValue(definition.Type, data),
                        purchaseCost = purchaseCost,
                        dpsDelta = EstimateTrackDpsDelta(definition.Type, previousData, data, combat),
                        emissionDelta = EstimateTrackEmissionDelta(definition.Type, previousData, data, combat)
                    });
                }
            }

            result.Add(new MetaTrackExport
            {
                type = definition.Type.ToString(),
                maxLevel = maxLevel,
                maxPurchasableLevel = maxLevel,
                values = values,
                costs = costs,
                purchasableLevels = purchasableLevels,
                totalCost = metaEconomy != null
                    ? metaEconomy.GetTrackTotalCost(definition.Type)
                    : SumCosts(costs)
            });
        }

        return result;
    }

    private static List<ProgressionCheckpointExport> BuildProgressionCheckpoints(
        CombatScalingConfig combat)
    {
        var result = new List<ProgressionCheckpointExport>();
        IReadOnlyList<PlayerProgressionCheckpoint> checkpoints =
            PlayerProgressionMilestones.ReferenceCheckpoints;

        for (int index = 0; index < checkpoints.Count; index++)
        {
            PlayerProgressionCheckpoint checkpoint = checkpoints[index];
            result.Add(new ProgressionCheckpointExport
            {
                run = checkpoint.RunNumber,
                damageLevel = checkpoint.DamageLevel,
                fireRateLevel = checkpoint.FireRateLevel,
                maxHpLevel = checkpoint.MaxHpLevel,
                projectileCountLevel = checkpoint.ProjectileCountLevel,
                squadSizeLevel = checkpoint.SquadSizeLevel,
                damage = checkpoint.DamageValue,
                fireRate = checkpoint.FireRateValue,
                maxHp = checkpoint.MaxHpValue,
                projectileCount = checkpoint.ProjectileCountValue,
                squadSize = checkpoint.SquadSizeValue,
                purchases = checkpoint.TargetPurchases,
                targetCumulativeIncome = checkpoint.TargetCumulativeIncome,
                targetSpent = checkpoint.TargetSpent,
                targetWalletReserve = checkpoint.TargetWalletReserve,
                effectiveDps = checkpoint.EstimateDps(combat),
                estimatedBaseProjectileEmissionsPerSecond = checkpoint.EstimateEmissions(combat)
            });
        }

        return result;
    }

    private static float GetMetaTrackValue(
        PlayerMetaUpgradeType type,
        PlayerMetaLevelData data)
    {
        return type switch
        {
            PlayerMetaUpgradeType.Damage => data.Damage,
            PlayerMetaUpgradeType.FireRate => data.FireRate,
            PlayerMetaUpgradeType.MaxHp => data.MaxHp,
            PlayerMetaUpgradeType.ProjectileCount => data.ProjectileCount,
            PlayerMetaUpgradeType.SquadSize => data.SquadSize,
            _ => 0f
        };
    }

    private static float EstimateTrackDpsDelta(
        PlayerMetaUpgradeType type,
        PlayerMetaLevelData before,
        PlayerMetaLevelData after,
        CombatScalingConfig combat)
    {
        PlayerMetaLevelData baseline = PlayerMetaBalanceConfig.GetDefaultLevelData(0);
        (float beforeDamage, float beforeFire, int beforeProjectiles, int beforeSquad) =
            BuildTrackDeltaStats(type, before, baseline);
        (float afterDamage, float afterFire, int afterProjectiles, int afterSquad) =
            BuildTrackDeltaStats(type, after, baseline);

        return BalanceV1Math.EffectiveDps(afterDamage, afterFire, afterProjectiles, afterSquad, combat)
            - BalanceV1Math.EffectiveDps(beforeDamage, beforeFire, beforeProjectiles, beforeSquad, combat);
    }

    private static float EstimateTrackEmissionDelta(
        PlayerMetaUpgradeType type,
        PlayerMetaLevelData before,
        PlayerMetaLevelData after,
        CombatScalingConfig combat)
    {
        PlayerMetaLevelData baseline = PlayerMetaBalanceConfig.GetDefaultLevelData(0);
        (float beforeDamage, float beforeFire, int beforeProjectiles, int beforeSquad) =
            BuildTrackDeltaStats(type, before, baseline);
        (float afterDamage, float afterFire, int afterProjectiles, int afterSquad) =
            BuildTrackDeltaStats(type, after, baseline);

        return BalanceV1Math.EstimatedBaseProjectileEmissionsPerSecond(
                afterFire,
                afterProjectiles,
                afterSquad,
                combat)
            - BalanceV1Math.EstimatedBaseProjectileEmissionsPerSecond(
                beforeFire,
                beforeProjectiles,
                beforeSquad,
                combat);
    }

    private static (float damage, float fireRate, int projectileCount, int squadSize) BuildTrackDeltaStats(
        PlayerMetaUpgradeType type,
        PlayerMetaLevelData trackData,
        PlayerMetaLevelData baseline)
    {
        return type switch
        {
            PlayerMetaUpgradeType.Damage => (
                trackData.Damage,
                baseline.FireRate,
                baseline.ProjectileCount,
                baseline.SquadSize),
            PlayerMetaUpgradeType.FireRate => (
                baseline.Damage,
                trackData.FireRate,
                baseline.ProjectileCount,
                baseline.SquadSize),
            PlayerMetaUpgradeType.ProjectileCount => (
                baseline.Damage,
                baseline.FireRate,
                trackData.ProjectileCount,
                baseline.SquadSize),
            PlayerMetaUpgradeType.SquadSize => (
                baseline.Damage,
                baseline.FireRate,
                baseline.ProjectileCount,
                trackData.SquadSize),
            _ => (
                baseline.Damage,
                baseline.FireRate,
                baseline.ProjectileCount,
                baseline.SquadSize)
        };
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

    private static List<EnemyRoleExport> BuildEnemyRoles(BalanceBootstrapConfig bootstrap = null)
    {
        var result = new List<EnemyRoleExport>();

        foreach (BalanceEnemyRole role in Enum.GetValues(typeof(BalanceEnemyRole)))
        {
            EnemyRoleConfig roleConfig = bootstrap != null ? bootstrap.GetEnemyRoleConfig(role) : null;
            result.Add(new EnemyRoleExport
            {
                role = role.ToString(),
                unlockSeconds = EnemyRoleBalanceDefaults.GetUnlockTimeSeconds(role),
                threatCost = roleConfig != null ? roleConfig.ThreatCost : EnemyRoleBalanceDefaults.GetThreatCost(role),
                rewardPoints = roleConfig != null ? roleConfig.RewardPoints : EnemyRoleBalanceDefaults.GetRewardPoints(role)
            });
        }

        return result;
    }

    private static List<GateScheduleExport> BuildGateSchedule(GatePoolConfig gates)
    {
        return BuildGateSchedule(gates, null);
    }

    private static List<GateScheduleExport> BuildGateSchedule(
        GatePoolConfig gates,
        GateScalingProfile gateScaling)
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
                majorChance = gateScaling != null
                    ? gateScaling.GetMajorChance(elapsed)
                    : GateSystem.GetMajorChance(elapsed),
                phase = gateScaling != null
                    ? gateScaling.EvaluatePhase(elapsed).PhaseId
                    : string.Empty
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

    private static EconomyExport BuildEconomy(
        EconomyConfig economy,
        PlayerMetaEconomyConfig metaEconomy = null)
    {
        return new EconomyExport
        {
            rewardScale = economy.RewardScale,
            timeCoinPer30Seconds = economy.TimeCoinPer30Seconds,
            timeScorePerSecond = economy.TimeScorePerSecond,
            eliteCoinBonusMin = economy.EliteCoinBonusMin,
            eliteCoinBonusMax = economy.EliteCoinBonusMax,
            storyMilestones = new List<int>(economy.StoryMilestones),
            metaEconomyVersion = metaEconomy != null ? metaEconomy.ConfigVersion : string.Empty,
            upgradeCostScale = metaEconomy != null ? metaEconomy.UpgradeCostScale : 1f,
            fullUpgradeTreeCost = metaEconomy != null ? metaEconomy.GetFullTreeTotalCost() : 0
        };
    }

    private static int SumCosts(IReadOnlyList<int> costs)
    {
        int total = 0;
        for (int index = 0; index < costs.Count; index++)
        {
            total += costs[index];
        }

        return total;
    }

    private static GateScalingExport BuildGateScaling(GateScalingProfile profile)
    {
        var phases = new List<GatePhaseExport>();
        foreach (GateScalingPhase phase in profile.Phases)
        {
            var overrides = new List<GatePhaseOverrideExport>();
            foreach (GatePhaseOverride gateOverride in phase.Overrides)
            {
                overrides.Add(new GatePhaseOverrideExport
                {
                    gateId = gateOverride.GateId,
                    label = gateOverride.DisplayLabel,
                    magnitude = gateOverride.Magnitude,
                    durationSeconds = gateOverride.DurationSeconds,
                    secondaryMagnitude = gateOverride.SecondaryMagnitude,
                    secondaryDurationSeconds = gateOverride.SecondaryDurationSeconds,
                    drawbackMagnitude = gateOverride.DrawbackMagnitude,
                    drawbackDurationSeconds = gateOverride.DrawbackDurationSeconds,
                    offerWeightMultiplier = gateOverride.OfferWeightMultiplier
                });
            }

            phases.Add(new GatePhaseExport
            {
                phase = phase.PhaseId,
                startSeconds = phase.StartSeconds,
                overrides = overrides
            });
        }

        GateRunStatCaps caps = profile.RunStatCaps;
        MajorGateSettings major = profile.MajorSettings;
        return new GateScalingExport
        {
            profileVersion = profile.ProfileVersion,
            phases = phases,
            major = new MajorSettingsExport
            {
                unlockSeconds = major.UnlockSeconds,
                earlyChance = major.EarlyChance,
                midChance = major.MidChance,
                lateChance = major.LateChance,
                guaranteedAfterEligibleMisses = major.GuaranteedAfterEligibleMisses
            },
            caps = new RunCapExport
            {
                damage = caps.Damage,
                fireRate = caps.FireRate,
                maxHp = caps.MaxHp,
                projectileCount = caps.ProjectileCount,
                squadCount = caps.SquadCount,
                maxIncomingDamageMultiplier = caps.MaxIncomingDamageMultiplier,
                maxEnemyPressureMultiplier = caps.MaxEnemyPressureMultiplier,
                minEnemySpeedMultiplier = caps.MinEnemySpeedMultiplier
            }
        };
    }

    private static BenchmarkExport BuildBenchmark(
        BalanceBenchmarkProfile benchmark,
        CombatScalingConfig combat)
    {
        PlayerRunStartStats stats = benchmark.ToRunStartStats();
        return new BenchmarkExport
        {
            profileId = benchmark.ProfileId,
            enabled = benchmark.Enabled,
            editorOrDebugOnly = true,
            damage = stats.Damage,
            fireRate = stats.FireRate,
            maxHp = stats.MaxHp,
            projectileCount = stats.ProjectileCount,
            squadSize = stats.SquadSize,
            effectiveDps = combat != null
                ? BalanceV1Math.EffectiveDps(
                    stats.Damage,
                    stats.FireRate,
                    stats.ProjectileCount,
                    stats.SquadSize,
                    combat)
                : 0f,
            estimatedBaseProjectileEmissionsPerSecond = combat != null
                ? BalanceV1Math.EstimatedBaseProjectileEmissionsPerSecond(
                    stats.FireRate,
                    stats.ProjectileCount,
                    stats.SquadSize,
                    combat)
                : 0f,
            suppressSaveCommit = benchmark.SuppressSaveCommit,
            suppressWalletReward = benchmark.SuppressWalletReward,
            suppressStoryProgress = benchmark.SuppressStoryProgress,
            suppressTutorialProgress = benchmark.SuppressTutorialProgress
        };
    }

    private static string BuildGatePhaseCsv(GateScalingProfile profile)
    {
        var builder = new StringBuilder();
        builder.AppendLine("phase,start_seconds,gate_id,label,magnitude,duration_seconds,secondary_magnitude,secondary_duration_seconds,drawback_magnitude,drawback_duration_seconds");
        if (profile == null)
        {
            return builder.ToString();
        }

        foreach (GateScalingPhase phase in profile.Phases)
        {
            foreach (GatePhaseOverride gateOverride in phase.Overrides)
            {
                builder.AppendLine(string.Join(",",
                    EscapeCsv(phase.PhaseId),
                    F(phase.StartSeconds),
                    EscapeCsv(gateOverride.GateId),
                    EscapeCsv(gateOverride.DisplayLabel),
                    F(gateOverride.Magnitude),
                    F(gateOverride.DurationSeconds),
                    F(gateOverride.SecondaryMagnitude),
                    F(gateOverride.SecondaryDurationSeconds),
                    F(gateOverride.DrawbackMagnitude),
                    F(gateOverride.DrawbackDurationSeconds)));
            }
        }

        return builder.ToString();
    }

    private static string BuildBenchmarkCurveCsv(
        BalanceBenchmarkProfile benchmark,
        GateScalingProfile gateScaling,
        CombatScalingConfig combat)
    {
        var builder = new StringBuilder();
        builder.AppendLine("checkpoint,damage,fire_rate,max_hp,projectile_count,squad_count,effective_dps,estimated_base_projectile_emissions_per_second");
        if (benchmark == null || combat == null)
        {
            return builder.ToString();
        }

        PlayerRunStartStats start = benchmark.ToRunStartStats();
        builder.AppendLine(BuildBenchmarkCurveRow("start", start.Damage, start.FireRate, start.MaxHp, start.ProjectileCount, start.SquadSize, combat));

        GateRunStatCaps caps = gateScaling != null ? gateScaling.RunStatCaps : null;
        if (caps != null)
        {
            builder.AppendLine(BuildBenchmarkCurveRow("run_caps", caps.Damage, caps.FireRate, caps.MaxHp, caps.ProjectileCount, caps.SquadCount, combat));
        }

        return builder.ToString();
    }

    private static string BuildBenchmarkCurveRow(
        string label,
        float damage,
        float fireRate,
        float maxHp,
        int projectileCount,
        int squadCount,
        CombatScalingConfig combat)
    {
        return string.Join(",",
            EscapeCsv(label),
            F(damage),
            F(fireRate),
            F(maxHp),
            projectileCount,
            squadCount,
            F(BalanceV1Math.EffectiveDps(damage, fireRate, projectileCount, squadCount, combat)),
            F(BalanceV1Math.EstimatedBaseProjectileEmissionsPerSecond(
                fireRate,
                projectileCount,
                squadCount,
                combat)));
    }

    private static string BuildProgressionCheckpointCsv(CombatScalingConfig combat)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            "run,damage_level,fire_rate_level,max_hp_level,projectile_count_level,squad_size_level,"
            + "damage,fire_rate,max_hp,projectile_count,squad_size,purchases,"
            + "target_cumulative_income,target_spent,target_wallet_reserve,"
            + "effective_dps,estimated_base_projectile_emissions_per_second");

        IReadOnlyList<PlayerProgressionCheckpoint> checkpoints =
            PlayerProgressionMilestones.ReferenceCheckpoints;
        for (int index = 0; index < checkpoints.Count; index++)
        {
            PlayerProgressionCheckpoint checkpoint = checkpoints[index];
            builder.Append(checkpoint.RunNumber).Append(',')
                .Append(checkpoint.DamageLevel).Append(',')
                .Append(checkpoint.FireRateLevel).Append(',')
                .Append(checkpoint.MaxHpLevel).Append(',')
                .Append(checkpoint.ProjectileCountLevel).Append(',')
                .Append(checkpoint.SquadSizeLevel).Append(',')
                .Append(F(checkpoint.DamageValue)).Append(',')
                .Append(F(checkpoint.FireRateValue)).Append(',')
                .Append(F(checkpoint.MaxHpValue)).Append(',')
                .Append(checkpoint.ProjectileCountValue).Append(',')
                .Append(checkpoint.SquadSizeValue).Append(',')
                .Append(checkpoint.TargetPurchases).Append(',')
                .Append(checkpoint.TargetCumulativeIncome).Append(',')
                .Append(checkpoint.TargetSpent).Append(',')
                .Append(checkpoint.TargetWalletReserve).Append(',')
                .Append(F(checkpoint.EstimateDps(combat))).Append(',')
                .Append(F(checkpoint.EstimateEmissions(combat))).AppendLine();
        }

        return builder.ToString();
    }

    private static string MakeSafeFileName(string value)
    {
        string safe = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(invalid, '_');
        }

        return safe;
    }

    private static string EscapeCsv(string value)
    {
        string safe = value ?? string.Empty;
        return safe.Contains(",") || safe.Contains("\"") || safe.Contains("\n") || safe.Contains("\r")
            ? $"\"{safe.Replace("\"", "\"\"")}\""
            : safe;
    }

    private static string F(float value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    [Serializable]
    private sealed class BalanceExport
    {
        public int schemaVersion;
        public string exportedUtc;
        public string balanceVersion;
        public CombatExport combat;
        public List<MetaLevelExport> metaLevels;
        public List<MetaTrackExport> metaTracks;
        public List<PressureSampleExport> pressureSamples;
        public List<ProgressionCheckpointExport> progressionCheckpoints;
        public List<EnemyRoleExport> enemyRoles;
        public List<GateScheduleExport> gateSchedule;
        public List<GateEntryExport> gateEntries;
        public GateScalingExport gateScaling;
        public string benchmarkPreset;
        public BenchmarkExport benchmark;
        public BenchmarkExport oldRunCapBenchmark;
        public BenchmarkExport damageForwardCapBenchmark;
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
        public string squadPowerModel;
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
    private sealed class MetaTrackExport
    {
        public string type;
        public int maxLevel;
        public int maxPurchasableLevel;
        public List<float> values;
        public List<int> costs;
        public List<MetaTrackLevelExport> purchasableLevels;
        public int totalCost;
    }

    [Serializable]
    private sealed class MetaTrackLevelExport
    {
        public int level;
        public float value;
        public int purchaseCost;
        public float dpsDelta;
        public float emissionDelta;
    }

    [Serializable]
    private sealed class ProgressionCheckpointExport
    {
        public int run;
        public int damageLevel;
        public int fireRateLevel;
        public int maxHpLevel;
        public int projectileCountLevel;
        public int squadSizeLevel;
        public float damage;
        public float fireRate;
        public float maxHp;
        public int projectileCount;
        public int squadSize;
        public int purchases;
        public int targetCumulativeIncome;
        public int targetSpent;
        public int targetWalletReserve;
        public float effectiveDps;
        public float estimatedBaseProjectileEmissionsPerSecond;
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
        public string phase;
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
        public string metaEconomyVersion;
        public float upgradeCostScale;
        public int fullUpgradeTreeCost;
    }

    [Serializable]
    private sealed class GateScalingExport
    {
        public string profileVersion;
        public List<GatePhaseExport> phases;
        public MajorSettingsExport major;
        public RunCapExport caps;
    }

    [Serializable]
    private sealed class GatePhaseExport
    {
        public string phase;
        public float startSeconds;
        public List<GatePhaseOverrideExport> overrides;
    }

    [Serializable]
    private sealed class GatePhaseOverrideExport
    {
        public string gateId;
        public string label;
        public float magnitude;
        public float durationSeconds;
        public float secondaryMagnitude;
        public float secondaryDurationSeconds;
        public float drawbackMagnitude;
        public float drawbackDurationSeconds;
        public float offerWeightMultiplier;
    }

    [Serializable]
    private sealed class MajorSettingsExport
    {
        public float unlockSeconds;
        public float earlyChance;
        public float midChance;
        public float lateChance;
        public int guaranteedAfterEligibleMisses;
    }

    [Serializable]
    private sealed class RunCapExport
    {
        public float damage;
        public float fireRate;
        public float maxHp;
        public int projectileCount;
        public int squadCount;
        public float maxIncomingDamageMultiplier;
        public float maxEnemyPressureMultiplier;
        public float minEnemySpeedMultiplier;
    }

    [Serializable]
    private sealed class BenchmarkExport
    {
        public string profileId;
        public bool enabled;
        public bool editorOrDebugOnly;
        public float damage;
        public float fireRate;
        public float maxHp;
        public int projectileCount;
        public int squadSize;
        public float effectiveDps;
        public float estimatedBaseProjectileEmissionsPerSecond;
        public bool suppressSaveCommit;
        public bool suppressWalletReward;
        public bool suppressStoryProgress;
        public bool suppressTutorialProgress;
    }
}
