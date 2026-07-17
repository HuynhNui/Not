using _Project.Cutscenes;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Gameplay.Combat;
using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Gameplay.Gates;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.Balance;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.Telemetry;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;

namespace _Project.Tests.Editor
{
    public sealed class BalanceV1MathTests
    {
        [TestCase(0f, 0f)]
        [TestCase(4f, 4f)]
        [TestCase(6f, 6f)]
        [TestCase(18f, 12f)]
        public void EffectiveFireRate_ReturnsExpectedValue(float rawFireRate, float expected)
        {
            Assert.That(
                BalanceV1Math.EffectiveFireRate(rawFireRate),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void EffectiveFireRate_ApproachesButDoesNotExceedSoftMaximum()
        {
            float effective = BalanceV1Math.EffectiveFireRate(100000f);

            Assert.That(effective, Is.GreaterThan(17f));
            Assert.That(effective, Is.LessThan(BalanceV1Math.DefaultFireSoftCapMax));
        }

        [Test]
        public void ProjectileFactor_IsOneAtBaseProjectileCount()
        {
            Assert.That(BalanceV1Math.ProjectileFactor(5), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                BalanceV1Math.ProjectileFactor(16, 5, 0.20f),
                Is.EqualTo(1.3528f).Within(0.0001f));
        }

        [Test]
        public void ProjectileCount_IncreasesCoverageButReducesPerBulletDamage()
        {
            float baseBulletDamage = BalanceV1Math.DamagePerMainBullet(1f, 5);
            float upgradedBulletDamage = BalanceV1Math.DamagePerMainBullet(1f, 16);
            float upgradedVolleyDamage = upgradedBulletDamage * 16f;

            Assert.That(baseBulletDamage, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(upgradedBulletDamage, Is.LessThan(baseBulletDamage));
            Assert.That(upgradedVolleyDamage, Is.GreaterThan(5f));
            Assert.That(upgradedVolleyDamage, Is.LessThan(16f));
        }

        [Test]
        public void SquadFactor_UsesDiminishingReturns()
        {
            Assert.That(BalanceV1Math.SquadFactor(1), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BalanceV1Math.FollowerDamageScale(2), Is.EqualTo(0.55f).Within(0.0001f));
            Assert.That(BalanceV1Math.SquadFactor(12), Is.EqualTo(2.8241f).Within(0.0001f));
            Assert.That(BalanceV1Math.FollowerDamageScale(12), Is.EqualTo(0.1658f).Within(0.0001f));
        }

        [Test]
        public void EffectiveDps_IsMonotonicForPositiveUpgradeChanges()
        {
            float baseline = BalanceV1Math.EffectiveDps(1f, 4f, 5, 1);

            Assert.That(BalanceV1Math.EffectiveDps(1.1f, 4f, 5, 1), Is.GreaterThan(baseline));
            Assert.That(BalanceV1Math.EffectiveDps(1f, 4.4f, 5, 1), Is.GreaterThan(baseline));
            Assert.That(BalanceV1Math.EffectiveDps(1f, 4f, 6, 1), Is.GreaterThan(baseline));
            Assert.That(BalanceV1Math.EffectiveDps(1f, 4f, 5, 2), Is.GreaterThan(baseline));
        }

        [Test]
        public void FullMetaEffectiveDps_RemainsInsideTargetRange()
        {
            CombatScalingConfig combat = AssetDatabase.LoadAssetAtPath<CombatScalingConfig>(
                "Assets/_Project/Data/Balance/V1_3_3_EliteSquad/CombatScalingConfig_v1_3_3_EliteSquad.asset");
            float baseline = BalanceV1Math.EffectiveDps(1f, 4f, 1, 1, combat);
            float fullMeta = BalanceV1Math.EffectiveDps(3f, 6.4f, 3, 4, combat);
            float ratio = fullMeta / baseline;

            Assert.That(baseline, Is.EqualTo(15.06f).Within(0.01f));
            Assert.That(fullMeta, Is.EqualTo(344.6f).Within(0.2f));
            Assert.That(ratio, Is.InRange(22.5f, 23.5f));
            Assert.That(
                BalanceV1Math.DamagePerMainBullet(3f, 3, combat),
                Is.EqualTo(4.496f).Within(0.001f));
        }

        [Test]
        public void FullMetaDurability_RemainsInsideTargetRange()
        {
            CombatScalingConfig combat = AssetDatabase.LoadAssetAtPath<CombatScalingConfig>(
                "Assets/_Project/Data/Balance/V1_3_3_EliteSquad/CombatScalingConfig_v1_3_3_EliteSquad.asset");
            float hpMultiplier = 20f / 10f;
            float durabilityRatio = hpMultiplier * BalanceV1Math.SquadDurabilityFactor(4, combat);

            Assert.That(durabilityRatio, Is.EqualTo(8f).Within(0.0001f));
        }

        [Test]
        public void CombatScalingConfig_ValidationClampsInvalidValues()
        {
            CombatScalingConfig config = ScriptableObject.CreateInstance<CombatScalingConfig>();

            try
            {
                var serializedConfig = new SerializedObject(config);
                serializedConfig.FindProperty("fireSoftCapStart").floatValue = -5f;
                serializedConfig.FindProperty("fireSoftCapMax").floatValue = -10f;
                serializedConfig.FindProperty("baseProjectileCount").intValue = 0;
                serializedConfig.FindProperty("projectileCoverageCoefficient").floatValue = -1f;
                serializedConfig.FindProperty("squadCoverageCoefficient").floatValue = -1f;
                serializedConfig.FindProperty("followerHpRatio").floatValue = 2f;
                serializedConfig.FindProperty("recruitSpawnHpRatio").floatValue = -1f;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();

                config.ValidateValues();

                Assert.That(config.FireSoftCapStart, Is.EqualTo(0f));
                Assert.That(config.FireSoftCapMax, Is.EqualTo(0f));
                Assert.That(config.BaseProjectileCount, Is.EqualTo(1));
                Assert.That(config.ProjectileCoverageCoefficient, Is.EqualTo(0f));
                Assert.That(config.SquadCoverageCoefficient, Is.EqualTo(0f));
                Assert.That(config.FollowerHpRatio, Is.EqualTo(1f));
                Assert.That(config.RecruitSpawnHpRatio, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void RunPressureNode_ClampsMinimumVisibleToActiveCap()
        {
            var node = new RunPressureNode(
                0f,
                activeCap: 5,
                minimumVisible: 10,
                threatBudget: 2f,
                spawnPerSecond: 3f,
                hpMultiplier: 1f,
                damageMultiplier: 0.75f,
                speedMultiplier: 1f);

            Assert.That(node.MinimumVisible, Is.EqualTo(5));
            Assert.That(node.MinimumVisible, Is.LessThanOrEqualTo(node.ActiveCap));
        }

        [Test]
        public void DefaultRunPressure_InterpolatesWithoutBreakingVisibilityConstraint()
        {
            RunPressureConfig config = ScriptableObject.CreateInstance<RunPressureConfig>();

            try
            {
                RunPressureSnapshot snapshot = config.Evaluate(120f);

                Assert.That(snapshot.ActiveCap, Is.InRange(18, 28));
                Assert.That(snapshot.MinimumVisible, Is.LessThanOrEqualTo(snapshot.ActiveCap));
                Assert.That(snapshot.SpawnPerSecond, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultRunPressure_ProvidesExpectedLateRunPressure()
        {
            RunPressureSnapshot snapshot = RunPressureConfig.EvaluateDefault(420f);

            Assert.That(snapshot.ActiveCap, Is.EqualTo(48));
            Assert.That(snapshot.MinimumVisible, Is.EqualTo(34));
            Assert.That(snapshot.ThreatBudget, Is.EqualTo(13f).Within(0.0001f));
            Assert.That(snapshot.SpawnPerSecond, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void EnemyRoleDefaults_UseExpectedUnlocksAndThreatCosts()
        {
            Assert.That(
                EnemyRoleBalanceDefaults.GetUnlockTimeSeconds(BalanceEnemyRole.Chomboom),
                Is.EqualTo(30f));
            Assert.That(
                EnemyRoleBalanceDefaults.GetUnlockTimeSeconds(BalanceEnemyRole.Vomfy),
                Is.EqualTo(90f));
            Assert.That(
                EnemyRoleBalanceDefaults.GetThreatCost(BalanceEnemyRole.Chomboom),
                Is.EqualTo(1.5f));
            Assert.That(
                EnemyRoleBalanceDefaults.GetThreatCost(BalanceEnemyRole.Vomfy),
                Is.EqualTo(2f));
        }

        [Test]
        public void ThreatBudget_BlocksSpecialEnemyButAlwaysAllowsBasicDensity()
        {
            Assert.That(EnemyRoleBalanceDefaults.CanFitThreat(1.5f, 1.5f, 2f), Is.False);
            Assert.That(EnemyRoleBalanceDefaults.CanFitThreat(1.5f, 0f, 2f), Is.True);
        }

        [Test]
        public void ChomboomExplosion_DamagesEachNearbySquadUnitOnce()
        {
            var nearUnitAObject = new GameObject("NearUnitA");
            var nearUnitBObject = new GameObject("NearUnitB");
            var farUnitObject = new GameObject("FarUnit");
            var explosionObject = new GameObject("ChomboomExplosion");
            PlayerUnit nearUnitA = nearUnitAObject.AddComponent<PlayerUnit>();
            PlayerUnit nearUnitB = nearUnitBObject.AddComponent<PlayerUnit>();
            PlayerUnit farUnit = farUnitObject.AddComponent<PlayerUnit>();
            ChomboomBoomFx explosion = explosionObject.AddComponent<ChomboomBoomFx>();

            try
            {
                nearUnitA.SetMaxHp(10f);
                nearUnitA.RestoreFullHealth();
                nearUnitB.SetMaxHp(10f);
                nearUnitB.RestoreFullHealth();
                farUnit.SetMaxHp(10f);
                farUnit.RestoreFullHealth();

                nearUnitAObject.transform.position = Vector3.zero;
                nearUnitBObject.transform.position = new Vector3(1f, 0f, 0f);
                farUnitObject.transform.position = new Vector3(3f, 0f, 0f);
                explosionObject.transform.position = Vector3.zero;

                explosion.Init(null, 3f, 1.75f);
                explosion.Spawn();

                Assert.That(nearUnitA.CurrentHp, Is.EqualTo(7f).Within(0.0001f));
                Assert.That(nearUnitB.CurrentHp, Is.EqualTo(7f).Within(0.0001f));
                Assert.That(farUnit.CurrentHp, Is.EqualTo(10f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(nearUnitAObject);
                Object.DestroyImmediate(nearUnitBObject);
                Object.DestroyImmediate(farUnitObject);
                Object.DestroyImmediate(explosionObject);
            }
        }

        [Test]
        public void DefaultGatePool_ContainsRequiredCategoriesAndNoLegacyDoubleOperations()
        {
            var categoryCounts = new System.Collections.Generic.Dictionary<BalanceGateCategory, int>();
            var entriesById = new System.Collections.Generic.Dictionary<string, BalanceGateEntry>();
            string[] expectedGateIds =
            {
                "stable_damage",
                "stable_fire_rate",
                "stable_vitality",
                "utility_repair",
                "utility_barrier",
                "utility_freeze",
                "risky_glass_cannon",
                "risky_bullet_storm",
                "risky_reinforcement",
                "risky_bounty",
                "major_projectile",
                "major_recruit",
                "major_overclock"
            };

            foreach (BalanceGateEntry entry in GatePoolConfig.CreateDefaultEntries())
            {
                categoryCounts.TryGetValue(entry.Category, out int count);
                categoryCounts[entry.Category] = count + 1;
                Assert.That(entriesById.ContainsKey(entry.GateId), Is.False);
                entriesById.Add(entry.GateId, entry);

                Assert.That(entry.Magnitude, Is.Not.EqualTo(2f));
                Assert.That(entry.SecondaryMagnitude, Is.Not.EqualTo(2f));
                Assert.That(entry.DrawbackMagnitude, Is.Not.EqualTo(2f));
            }

            Assert.That(entriesById.Count, Is.EqualTo(13));
            CollectionAssert.AreEquivalent(expectedGateIds, entriesById.Keys);
            Assert.That(
                entriesById["risky_bullet_storm"].EffectType,
                Is.EqualTo(BalanceEffectType.ProjectileFlat));
            Assert.That(entriesById["risky_bullet_storm"].Magnitude, Is.EqualTo(1f));
            Assert.That(
                entriesById["major_projectile"].EffectType,
                Is.EqualTo(BalanceEffectType.ProjectileFlat));
            Assert.That(entriesById["major_projectile"].Magnitude, Is.EqualTo(1f));
            Assert.That(categoryCounts[BalanceGateCategory.Stable], Is.GreaterThanOrEqualTo(3));
            Assert.That(categoryCounts[BalanceGateCategory.Utility], Is.GreaterThanOrEqualTo(3));
            Assert.That(categoryCounts[BalanceGateCategory.Risky], Is.GreaterThanOrEqualTo(3));
            Assert.That(categoryCounts[BalanceGateCategory.Major], Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void GateCadence_MajorEligibilityOccursEveryFourthSet()
        {
            Assert.That(
                GateSystem.IsMajorEligibilitySet(1, 15f, 60f),
                Is.False);
            Assert.That(
                GateSystem.IsMajorEligibilitySet(4, 15f, 60f),
                Is.True);
            Assert.That(
                GateSystem.IsMajorEligibilitySet(8, 15f, 60f),
                Is.True);
        }

        [Test]
        public void MajorChance_UsesExpectedRunPhases()
        {
            Assert.That(GateSystem.GetMajorChance(59f), Is.EqualTo(0f));
            Assert.That(GateSystem.GetMajorChance(60f), Is.EqualTo(0.25f));
            Assert.That(GateSystem.GetMajorChance(180f), Is.EqualTo(0.4f));
            Assert.That(GateSystem.GetMajorChance(300f), Is.EqualTo(0.6f));
            Assert.That(
                GateSystem.ShouldSpawnMajor(4, 60f, 15f, 60f, 0.1f),
                Is.True);
            Assert.That(
                GateSystem.ShouldSpawnMajor(4, 60f, 15f, 60f, 0.2f),
                Is.True);
        }

        [Test]
        public void TimedGateModifier_DoesNotAdvanceWhilePausedAndExpiresAfterDuration()
        {
            var modifiers = new GateTimedModifierSet();
            modifiers.Add(BalanceEffectType.EnemySpeedMultiplier, 0.75f, 20f);

            modifiers.Tick(0f);
            Assert.That(
                modifiers.GetCombinedMultiplier(BalanceEffectType.EnemySpeedMultiplier),
                Is.EqualTo(0.75f).Within(0.0001f));

            modifiers.Tick(19f);
            Assert.That(
                modifiers.GetCombinedMultiplier(BalanceEffectType.EnemySpeedMultiplier),
                Is.EqualTo(0.75f).Within(0.0001f));

            modifiers.Tick(1.01f);
            Assert.That(
                modifiers.GetCombinedMultiplier(BalanceEffectType.EnemySpeedMultiplier),
                Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Barrier_BlocksOneHitAndExpiresWithoutConsumingHealth()
        {
            var unitObject = new GameObject("BarrierUnit");
            PlayerUnit unit = unitObject.AddComponent<PlayerUnit>();

            try
            {
                unit.SetMaxHp(10f);
                unit.RestoreFullHealth();
                unit.AddBarrierHits(1, 15f);

                unit.TakeDamage(4f);
                Assert.That(unit.CurrentHp, Is.EqualTo(10f).Within(0.0001f));
                Assert.That(unit.BarrierHits, Is.EqualTo(0));

                unit.TakeDamage(4f);
                Assert.That(unit.CurrentHp, Is.EqualTo(6f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(unitObject);
            }
        }

        [Test]
        public void EnemyRoleRewards_UsePerKillCoinValues()
        {
            Assert.That(
                EnemyRoleBalanceDefaults.GetRewardPoints(BalanceEnemyRole.Basic),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                EnemyRoleBalanceDefaults.GetRewardPoints(BalanceEnemyRole.Chomboom),
                Is.EqualTo(3f).Within(0.0001f));
            Assert.That(
                EnemyRoleBalanceDefaults.GetRewardPoints(BalanceEnemyRole.Vomfy),
                Is.EqualTo(5f).Within(0.0001f));
            Assert.That(
                EnemyRoleBalanceDefaults.GetRewardPoints(BalanceEnemyRole.Elite),
                Is.InRange(12f, 18f));
        }

        [Test]
        public void Economy_CoinsComeFromRewardPointsOnly()
        {
            Assert.That(EconomyConfig.CalculateDefaultFinalCoins(0f, 3600f), Is.EqualTo(0));
            Assert.That(EconomyConfig.CalculateDefaultFinalCoins(1f, 0f), Is.EqualTo(1));
            Assert.That(EconomyConfig.CalculateDefaultFinalCoins(3f, 120f), Is.EqualTo(3));
            Assert.That(EconomyConfig.CalculateDefaultFinalCoins(5f, 240f), Is.EqualTo(5));
        }

        [Test]
        public void Economy_TimeScoreUsesFloorOfHalfSurvivalSeconds()
        {
            Assert.That(EconomyConfig.CalculateDefaultTimeScore(0f), Is.EqualTo(0));
            Assert.That(EconomyConfig.CalculateDefaultTimeScore(119.9f), Is.EqualTo(59));
            Assert.That(EconomyConfig.CalculateDefaultTimeScore(120f), Is.EqualTo(60));
        }

        [Test]
        public void SaveData_V1JsonMigratesToV2WithoutLosingProgression()
        {
            const string legacyJson =
                "{\"schemaVersion\":1,\"revision\":7,\"lastUpdatedUnixMs\":1234,"
                + "\"bestSurvivalTime\":210.5,\"bestKillCount\":42,"
                + "\"bestCoinsEarned\":123,\"bestScore\":456,\"walletCoins\":789,"
                + "\"upgradeLevels\":[{\"upgradeType\":\"Damage\",\"level\":3}]}";
            SaveData saveData = JsonUtility.FromJson<SaveData>(legacyJson);

            saveData.Normalize(9999);

            Assert.That(saveData.schemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(saveData.balanceVersionLastPlayed, Is.EqualTo(CombatScalingConfig.DefaultConfigVersion));
            Assert.That(saveData.walletCoins, Is.EqualTo(789));
            Assert.That(saveData.bestSurvivalTime, Is.EqualTo(210.5f).Within(0.0001f));
            Assert.That(saveData.bestKillCount, Is.EqualTo(42));
            Assert.That(saveData.bestCoinsEarned, Is.EqualTo(123));
            Assert.That(saveData.bestScore, Is.EqualTo(456));
            Assert.That(saveData.totalEnemyKills, Is.EqualTo(42));
            Assert.That(saveData.GetUpgradeLevel(PlayerMetaUpgradeType.Damage), Is.EqualTo(3));
        }

        [Test]
        public void SaveData_V2JsonMigratesToCurrentSchemaWithStoryDefaults()
        {
            const string legacyJson =
                "{\"schemaVersion\":2,\"revision\":3,\"lastUpdatedUnixMs\":1234,"
                + "\"bestSurvivalTime\":99,\"bestKillCount\":5,"
                + "\"bestCoinsEarned\":10,\"bestScore\":20,\"walletCoins\":30,"
                + "\"upgradeLevels\":[{\"upgradeType\":\"Damage\",\"level\":1}]}";
            SaveData saveData = JsonUtility.FromJson<SaveData>(legacyJson);

            saveData.Normalize(9999);

            Assert.That(saveData.schemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(saveData.totalRunsCompleted, Is.EqualTo(0));
            Assert.That(saveData.totalEnemyKills, Is.EqualTo(5));
            Assert.That(saveData.storyStage, Is.EqualTo(0));
            Assert.That(saveData.seenCutsceneIds, Is.Not.Null);
            Assert.That(saveData.seenCutsceneIds, Is.Empty);
            Assert.That(saveData.walletCoins, Is.EqualTo(30));
            Assert.That(saveData.GetUpgradeLevel(PlayerMetaUpgradeType.Damage), Is.EqualTo(1));
        }

        [Test]
        public void SaveData_NormalizesSeenCutsceneIds()
        {
            SaveData saveData = SaveData.CreateNew(1000);
            saveData.seenCutsceneIds = new System.Collections.Generic.List<string>
            {
                " CS_BOOT_001 ",
                "",
                null,
                "CS_BOOT_001",
                "CS_RECYCLE_001"
            };

            saveData.Normalize(2000);

            Assert.That(saveData.seenCutsceneIds, Has.Count.EqualTo(2));
            Assert.That(saveData.seenCutsceneIds[0], Is.EqualTo("CS_BOOT_001"));
            Assert.That(saveData.seenCutsceneIds[1], Is.EqualTo("CS_RECYCLE_001"));
            Assert.That(saveData.storyStage, Is.EqualTo(2));
        }

        [Test]
        public void RecordRunResult_IncrementsCompletedRunsWithoutNewBest()
        {
            string directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-run-count-test-{System.Guid.NewGuid():N}");
            SaveService service = SaveService.CreateForTests(directoryPath);

            try
            {
                service.EnsureLoaded();
                int initialTotalEnemyKills = service.Data.totalEnemyKills;
                service.RecordRunResult(120f, 10, 2, 30);
                int runCountAfterFirstRun = service.Data.totalRunsCompleted;

                service.RecordRunResult(1f, 0, 0, 0);

                Assert.That(runCountAfterFirstRun, Is.EqualTo(1));
                Assert.That(service.Data.totalRunsCompleted, Is.EqualTo(2));
                Assert.That(service.Data.totalEnemyKills, Is.EqualTo(initialTotalEnemyKills + 10));
                Assert.That(service.Data.bestSurvivalTime, Is.EqualTo(120f).Within(0.0001f));
            }
            finally
            {
                SaveService.SetInstanceForTests(null);
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
        }

        [Test]
        public void ResetPlayerProgression_ClearsProgressionButKeepsSettings()
        {
            string directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-reset-progress-test-{System.Guid.NewGuid():N}");
            SaveService service = SaveService.CreateForTests(directoryPath);

            const string musicEnabledKey = "Settings.MusicEnabled";
            const string sfxEnabledKey = "Settings.SfxEnabled";
            const string vibrationKey = "Settings.Vibration";
            const string damageTextKey = "Settings.DamageText";

            bool hadLegacyBestSurvivalTime = PlayerPrefs.HasKey(RunStatsTracker.BestSurvivalTimePrefsKey);
            bool hadLegacyBestKillCount = PlayerPrefs.HasKey(RunStatsTracker.BestKillCountPrefsKey);
            bool hadLegacyBestCoinsEarned = PlayerPrefs.HasKey(RunStatsTracker.BestCoinsEarnedPrefsKey);
            bool hadLegacyBestScore = PlayerPrefs.HasKey(RunStatsTracker.BestScorePrefsKey);
            bool hadLegacyWalletCoins = PlayerPrefs.HasKey(RunStatsTracker.WalletCoinsPrefsKey);
            float legacyBestSurvivalTime = PlayerPrefs.GetFloat(RunStatsTracker.BestSurvivalTimePrefsKey);
            int legacyBestKillCount = PlayerPrefs.GetInt(RunStatsTracker.BestKillCountPrefsKey);
            int legacyBestCoinsEarned = PlayerPrefs.GetInt(RunStatsTracker.BestCoinsEarnedPrefsKey);
            int legacyBestScore = PlayerPrefs.GetInt(RunStatsTracker.BestScorePrefsKey);
            int legacyWalletCoins = PlayerPrefs.GetInt(RunStatsTracker.WalletCoinsPrefsKey);
            var hadLegacyUpgradeLevels = new System.Collections.Generic.Dictionary<PlayerMetaUpgradeType, bool>();
            var legacyUpgradeLevels = new System.Collections.Generic.Dictionary<PlayerMetaUpgradeType, int>();

            foreach (PlayerMetaUpgradeType upgradeType in
                (PlayerMetaUpgradeType[])System.Enum.GetValues(typeof(PlayerMetaUpgradeType)))
            {
                string key = "MetaUpgrade.Level." + upgradeType;
                hadLegacyUpgradeLevels[upgradeType] = PlayerPrefs.HasKey(key);
                legacyUpgradeLevels[upgradeType] = PlayerPrefs.GetInt(key);
            }

            try
            {
                PlayerPrefs.SetInt(musicEnabledKey, 0);
                PlayerPrefs.SetInt(sfxEnabledKey, 1);
                PlayerPrefs.SetInt(vibrationKey, 0);
                PlayerPrefs.SetInt(damageTextKey, 1);
                PlayerPrefs.Save();

                service.EnsureLoaded();
                int initialWalletCoins = service.Data.walletCoins;
                service.RecordRunResult(120f, 10, 25, 300);
                Assert.That(service.Data.walletCoins, Is.EqualTo(initialWalletCoins + 25));
                Assert.That(service.Data.totalRunsCompleted, Is.EqualTo(1));

                service.ResetPlayerProgression();

                Assert.That(service.Data.walletCoins, Is.EqualTo(0));
                Assert.That(service.Data.totalRunsCompleted, Is.EqualTo(0));
                Assert.That(service.Data.bestSurvivalTime, Is.EqualTo(0f));
                Assert.That(service.Data.bestKillCount, Is.EqualTo(0));
                Assert.That(PlayerPrefs.GetInt(musicEnabledKey), Is.EqualTo(0));
                Assert.That(PlayerPrefs.GetInt(sfxEnabledKey), Is.EqualTo(1));
                Assert.That(PlayerPrefs.GetInt(vibrationKey), Is.EqualTo(0));
                Assert.That(PlayerPrefs.GetInt(damageTextKey), Is.EqualTo(1));
            }
            finally
            {
                PlayerPrefs.DeleteKey(musicEnabledKey);
                PlayerPrefs.DeleteKey(sfxEnabledKey);
                PlayerPrefs.DeleteKey(vibrationKey);
                PlayerPrefs.DeleteKey(damageTextKey);
                RestoreFloatPrefsKey(RunStatsTracker.BestSurvivalTimePrefsKey, hadLegacyBestSurvivalTime, legacyBestSurvivalTime);
                RestoreIntPrefsKey(RunStatsTracker.BestKillCountPrefsKey, hadLegacyBestKillCount, legacyBestKillCount);
                RestoreIntPrefsKey(RunStatsTracker.BestCoinsEarnedPrefsKey, hadLegacyBestCoinsEarned, legacyBestCoinsEarned);
                RestoreIntPrefsKey(RunStatsTracker.BestScorePrefsKey, hadLegacyBestScore, legacyBestScore);
                RestoreIntPrefsKey(RunStatsTracker.WalletCoinsPrefsKey, hadLegacyWalletCoins, legacyWalletCoins);

                foreach (PlayerMetaUpgradeType upgradeType in
                    (PlayerMetaUpgradeType[])System.Enum.GetValues(typeof(PlayerMetaUpgradeType)))
                {
                    string key = "MetaUpgrade.Level." + upgradeType;
                    RestoreIntPrefsKey(key, hadLegacyUpgradeLevels[upgradeType], legacyUpgradeLevels[upgradeType]);
                }

                PlayerPrefs.Save();
                SaveService.SetInstanceForTests(null);
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
        }

        private static void RestoreFloatPrefsKey(string key, bool hadValue, float value)
        {
            if (hadValue)
            {
                PlayerPrefs.SetFloat(key, value);
                return;
            }

            PlayerPrefs.DeleteKey(key);
        }

        private static void RestoreIntPrefsKey(string key, bool hadValue, int value)
        {
            if (hadValue)
            {
                PlayerPrefs.SetInt(key, value);
                return;
            }

            PlayerPrefs.DeleteKey(key);
        }

        [Test]
        public void StoryCutsceneUnlockRules_RequirePreviousCutscenesAndExactThresholds()
        {
            SaveData saveData = SaveData.CreateNew(1000);
            var context = new StoryCutsceneProgressContext(3, 30f, 100, 100);

            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.EnemyDoesNotCharge,
                    saveData,
                    context),
                Is.False);

            saveData.MarkCutsceneSeen(StoryCutsceneIds.BootSequence);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.FirstDeathRecovery);

            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.EnemyDoesNotCharge,
                    saveData,
                    context),
                Is.True);

            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.GateMemoryLeak,
                    saveData,
                    new StoryCutsceneProgressContext(10, 179.9f, 0, 100)),
                Is.False);

            saveData.MarkCutsceneSeen(StoryCutsceneIds.EnemyDoesNotCharge);

            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.GateMemoryLeak,
                    saveData,
                    new StoryCutsceneProgressContext(10, 180f, 0, 100)),
                Is.True);
        }

        [Test]
        public void StoryCutsceneUnlockRules_ReturnFirstEligibleCutsceneInStoryOrder()
        {
            SaveData saveData = SaveData.CreateNew(1000);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.BootSequence);

            var context = new StoryCutsceneProgressContext(80, 999f, 999, 9999);

            Assert.That(
                StoryCutsceneUnlockRules.TryGetFirstEligible(saveData, context, out string cutsceneId),
                Is.True);
            Assert.That(cutsceneId, Is.EqualTo(StoryCutsceneIds.FirstDeathRecovery));
        }

        [Test]
        public void StoryCutsceneUnlockRules_HumanCommandUsesLifetimeKills()
        {
            SaveData saveData = SaveData.CreateNew(1000);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.BootSequence);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.FirstDeathRecovery);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.EnemyDoesNotCharge);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.GateMemoryLeak);
            saveData.bestKillCount = 5000;

            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.HumanCommand,
                    saveData,
                    new StoryCutsceneProgressContext(20, 300f, 0, 999)),
                Is.False);
            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.HumanCommand,
                    saveData,
                    new StoryCutsceneProgressContext(20, 300f, 0, 1000)),
                Is.True);
        }

        [Test]
        public void StoryCutsceneUnlockRules_UseUpdatedLoopThresholds()
        {
            SaveData saveData = SaveData.CreateNew(1000);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.BootSequence);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.FirstDeathRecovery);
            saveData.MarkCutsceneSeen(StoryCutsceneIds.EnemyDoesNotCharge);

            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.GateMemoryLeak,
                    saveData,
                    new StoryCutsceneProgressContext(9, 180f, 0, 1000)),
                Is.False);
            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.GateMemoryLeak,
                    saveData,
                    new StoryCutsceneProgressContext(10, 180f, 0, 1000)),
                Is.True);

            saveData.MarkCutsceneSeen(StoryCutsceneIds.GateMemoryLeak);
            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.HumanCommand,
                    saveData,
                    new StoryCutsceneProgressContext(20, 300f, 0, 1000)),
                Is.True);

            saveData.MarkCutsceneSeen(StoryCutsceneIds.HumanCommand);
            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.SystemFatigue,
                    saveData,
                    new StoryCutsceneProgressContext(35, 360f, 0, 1000)),
                Is.True);

            saveData.MarkCutsceneSeen(StoryCutsceneIds.SystemFatigue);
            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.FinalChoicePreChoice,
                    saveData,
                    new StoryCutsceneProgressContext(50, 420f, 0, 1000)),
                Is.True);
            Assert.That(
                StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.FinalChoicePreChoice,
                    saveData,
                    new StoryCutsceneProgressContext(51, 420f, 0, 1000)),
                Is.False);
        }

        [Test]
        public void StoryCutsceneUnlockRules_FinalChoiceMapsAliasAndExposesBranches()
        {
            Assert.That(
                StoryCutsceneUnlockRules.NormalizePlayableCutsceneId(StoryCutsceneIds.FinalChoice),
                Is.EqualTo(StoryCutsceneIds.FinalChoicePreChoice));
            Assert.That(
                StoryCutsceneUnlockRules.FinalChoiceBranchIds,
                Is.EquivalentTo(new[]
                {
                    StoryCutsceneIds.FinalChoiceContinueProtocol,
                    StoryCutsceneIds.FinalChoiceShutDownCore
                }));
        }

        [Test]
        public void WalletCoins_CommitOnlyWhenRunResultIsRecorded()
        {
            string directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-save-test-{System.Guid.NewGuid():N}");
            SaveService service = SaveService.CreateForTests(directoryPath);

            try
            {
                service.EnsureLoaded();
                int initialWalletCoins = service.Data.walletCoins;
                int initialBestCoinsEarned = service.Data.bestCoinsEarned;

                int pendingRunCoins = EconomyConfig.CalculateDefaultFinalCoins(10.6f, 120f);
                Assert.That(service.Data.walletCoins, Is.EqualTo(initialWalletCoins));

                service.RecordRunResult(120f, 10, pendingRunCoins, 15);
                Assert.That(service.Data.walletCoins, Is.EqualTo(initialWalletCoins + 11));
                Assert.That(service.Data.bestCoinsEarned, Is.EqualTo(Mathf.Max(initialBestCoinsEarned, 11)));
            }
            finally
            {
                SaveService.SetInstanceForTests(null);
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
        }

        [Test]
        public void TelemetryWriter_BuffersUntilFlushAndWritesSingleSummaryRow()
        {
            string directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-telemetry-test-{System.Guid.NewGuid():N}");
            var writer = new BalanceTelemetryWriter(directoryPath);

            try
            {
                writer.BufferSummary(new BalanceRunSummaryRow
                {
                    runId = "run-1",
                    runStartedUtc = "2026-06-15T00:00:00Z",
                    runEndedUtc = "2026-06-15T00:02:00Z",
                    buildVersion = "test",
                    balanceVersion = CombatScalingConfig.DefaultConfigVersion,
                    survivalSeconds = 120f,
                    enemyKills = 25,
                    coinRewardPoints = 10.6f,
                    coinsEarned = 11,
                    score = 85
                });
                writer.BufferSnapshot(new BalanceRunSnapshotRow
                {
                    runId = "run-1",
                    elapsedSeconds = 15f,
                    enemyKills = 3,
                    squadCount = 2
                });
                writer.BufferEvent(new BalanceTelemetryEvent
                {
                    eventName = "run_end",
                    runId = "run-1"
                });

                Assert.That(File.Exists(writer.SummaryPath), Is.False);
                Assert.That(writer.BufferedSummaryCount, Is.EqualTo(1));

                writer.Flush();

                string[] summaryLines = File.ReadAllLines(writer.SummaryPath);
                Assert.That(summaryLines, Has.Length.EqualTo(2));
                Assert.That(summaryLines[0], Does.StartWith("run_id,"));
                Assert.That(summaryLines[1], Does.StartWith("run-1,"));
                Assert.That(File.ReadAllLines(writer.SnapshotPath), Has.Length.EqualTo(2));
                Assert.That(File.ReadAllLines(writer.EventPath), Has.Length.EqualTo(1));
                Assert.That(writer.BufferedSummaryCount, Is.EqualTo(0));
            }
            finally
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
        }

        [Test]
        public void TelemetryWriter_EscapesCsvText()
        {
            Assert.That(
                BalanceTelemetryWriter.EscapeCsv("Gate, \"Risky\""),
                Is.EqualTo("\"Gate, \"\"Risky\"\"\""));
        }

        [Test]
        public void TelemetryWriter_FileFailureDoesNotThrow()
        {
            string rootPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-telemetry-failure-{System.Guid.NewGuid():N}");
            File.WriteAllText(rootPath, "not a directory");
            var writer = new BalanceTelemetryWriter(Path.Combine(rootPath, "blocked"));
            writer.BufferEvent(new BalanceTelemetryEvent
            {
                eventName = "run_end",
                runId = "run-1"
            });

            try
            {
                Assert.DoesNotThrow(writer.Flush);
            }
            finally
            {
                File.Delete(rootPath);
            }
        }

        [Test]
        public void TelemetryConfig_DefaultsUseFifteenSecondCappedSnapshots()
        {
            BalanceTelemetryConfig telemetryConfig =
                ScriptableObject.CreateInstance<BalanceTelemetryConfig>();

            try
            {
                telemetryConfig.ValidateValues();
                Assert.That(telemetryConfig.SnapshotIntervalSeconds, Is.EqualTo(15f));
                Assert.That(telemetryConfig.MaxSnapshotsPerRun, Is.EqualTo(80));
                Assert.That(telemetryConfig.DevelopmentBuildOnly, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(telemetryConfig);
            }
        }

        [Test]
        public void DefaultPlayerMetaConfig_ContainsSixBalancedLevels()
        {
            PlayerMetaBalanceConfig config = ScriptableObject.CreateInstance<PlayerMetaBalanceConfig>();

            try
            {
                config.ValidateValues();
                PlayerMetaLevelData fullMeta = config.GetLevelData(5);

                Assert.That(config.Levels.Count, Is.EqualTo(6));
                Assert.That(fullMeta.Damage, Is.EqualTo(5.0f).Within(0.0001f));
                Assert.That(fullMeta.FireRate, Is.EqualTo(6.4f).Within(0.0001f));
                Assert.That(fullMeta.MaxHp, Is.EqualTo(20f).Within(0.0001f));
                Assert.That(fullMeta.ProjectileCount, Is.EqualTo(3));
                Assert.That(fullMeta.SquadSize, Is.EqualTo(4));
                Assert.That(fullMeta.Cost, Is.EqualTo(2200));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PlayerMetaUpgradeService_UsesExplicitV1LevelTable()
        {
            Assert.That(
                PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.Damage, 5),
                Is.EqualTo(5.0f).Within(0.0001f));
            Assert.That(
                PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.FireRate, 5),
                Is.EqualTo(6.4f).Within(0.0001f));
            Assert.That(
                PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.MaxHp, 5),
                Is.EqualTo(20f).Within(0.0001f));
            Assert.That(
                PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.ProjectileCount, 5),
                Is.EqualTo(3f));
            Assert.That(
                PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.SquadSize, 5),
                Is.EqualTo(4f));

            int[] expectedProjectiles = { 1, 2, 3, 3, 3, 3 };
            for (int level = 0; level < expectedProjectiles.Length; level++)
            {
                Assert.That(
                    PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.ProjectileCount, level),
                    Is.EqualTo(expectedProjectiles[level]));
            }

            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(2));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.SquadSize), Is.EqualTo(3));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.Damage), Is.EqualTo(5));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.MoveSpeed), Is.EqualTo(0));
            Assert.That(PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.ProjectileCount, 4), Is.EqualTo(3f));
        }

        [Test]
        public void PlayerMetaUpgradeService_UsesRun45ProgressionCosts()
        {
            string directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-projectile-cost-test-{System.Guid.NewGuid():N}");
            SaveService service = SaveService.CreateForTests(directoryPath);
            PlayerMetaEconomyConfig economyConfig = PlayerMetaEconomyConfig.CreateRun45RuntimeConfig();

            try
            {
                SaveService.SetInstanceForTests(service);
                PlayerMetaUpgradeService.Configure(null, economyConfig, null);
                service.EnsureLoaded();
                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(15000));

                service.Data.SetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount, 1);
                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(55000));

                service.Data.SetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount, 2);
                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(0));

                service.Data.SetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount, 3);
                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(0));

                service.Data.SetUpgradeLevel(PlayerMetaUpgradeType.Damage, 1);
                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.Damage), Is.EqualTo(12000));
                Assert.That(PlayerMetaUpgradeService.GetFullTreeTotalCost(), Is.EqualTo(850000));
                Assert.That(PlayerMetaEconomyConfig.Run45ConfigVersion, Is.EqualTo(PlayerProgressionMilestones.ConfigVersion));
            }
            finally
            {
                Object.DestroyImmediate(economyConfig);
                PlayerMetaUpgradeService.Configure(null, null, null);
                SaveService.SetInstanceForTests(null);
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
        }

        [Test]
        public void PlayerProgressionMilestones_MatchRun45ReferencePlan()
        {
            Assert.That(PlayerProgressionMilestones.ConfigVersion, Is.EqualTo("economy-v1.4.1-run45-progression"));
            Assert.That(PlayerProgressionMilestones.FullTreeCost, Is.EqualTo(850000));
            Assert.That(PlayerProgressionMilestones.ReferenceCheckpoints.Count, Is.EqualTo(9));

            Assert.That(PlayerProgressionMilestones.TryGetCheckpoint(10, out PlayerProgressionCheckpoint run10), Is.True);
            Assert.That(run10.DamageValue, Is.EqualTo(3.5f).Within(0.0001f));
            Assert.That(run10.FireRateValue, Is.EqualTo(4.4f).Within(0.0001f));
            Assert.That(run10.MaxHpValue, Is.EqualTo(11.5f).Within(0.0001f));
            Assert.That(run10.ProjectileCountValue, Is.EqualTo(2));
            Assert.That(run10.SquadSizeValue, Is.EqualTo(2));
            Assert.That(run10.TargetPurchases, Is.EqualTo(5));
            Assert.That(run10.TargetSpent, Is.EqualTo(38000));

            Assert.That(PlayerProgressionMilestones.TryGetCheckpoint(45, out PlayerProgressionCheckpoint run45), Is.True);
            Assert.That(run45.DamageValue, Is.EqualTo(4.75f).Within(0.0001f));
            Assert.That(run45.FireRateValue, Is.EqualTo(6.4f).Within(0.0001f));
            Assert.That(run45.MaxHpValue, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(run45.ProjectileCountValue, Is.EqualTo(3));
            Assert.That(run45.SquadSizeValue, Is.EqualTo(4));
            Assert.That(run45.TargetPurchases, Is.EqualTo(19));
            Assert.That(run45.TargetCumulativeIncome, Is.EqualTo(793000));
            Assert.That(run45.TargetSpent, Is.EqualTo(711000));
            Assert.That(run45.TargetWalletReserve, Is.EqualTo(82000));
        }

        [Test]
        public void BulletSpawner_ExposesEffectiveFireRateAndNormalizedDamage()
        {
            var gameObject = new GameObject("BulletSpawnerTest");
            BulletSpawner spawner = gameObject.AddComponent<BulletSpawner>();

            try
            {
                spawner.Initialize(1f, 18f);
                spawner.SetProjectileCount(16);

                Assert.That(spawner.EffectiveFireRate, Is.EqualTo(12f).Within(0.0001f));
                Assert.That(spawner.DamagePerProjectile, Is.LessThan(1f));

                float mainDamage = spawner.DamagePerProjectile;
                spawner.SetShooterDamageScale(0.25f);

                Assert.That(
                    spawner.DamagePerProjectile,
                    Is.EqualTo(mainDamage * 0.25f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void BulletSpawner_ResolveVisualTierIndex_UsesDamageThresholds()
        {
            var gameObject = new GameObject("BulletVisualTierTest");
            BulletSpawner spawner = gameObject.AddComponent<BulletSpawner>();
            var bulletObjects = new GameObject[5];

            try
            {
                var serializedSpawner = new SerializedObject(spawner);
                SerializedProperty tiers = serializedSpawner.FindProperty("visualTiers");
                tiers.arraySize = 5;
                float[] thresholds = { 0f, 1.3f, 1.6f, 1.9f, 2.5f };
                string[] names =
                {
                    "Bullet_Tier_00",
                    "Bullet_Tier_10",
                    "Bullet_Tier_20",
                    "Bullet_Tier_50",
                    "Bullet_Tier_100"
                };

                for (int index = 0; index < thresholds.Length; index++)
                {
                    bulletObjects[index] = new GameObject(names[index]);
                    Bullet bullet = bulletObjects[index].AddComponent<Bullet>();
                    SerializedProperty tier = tiers.GetArrayElementAtIndex(index);
                    tier.FindPropertyRelative("minDamage").floatValue = thresholds[index];
                    tier.FindPropertyRelative("bulletPrefab").objectReferenceValue = bullet;
                }

                serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(spawner.ResolveVisualTierIndex(0f), Is.EqualTo(0));
                Assert.That(spawner.ResolveVisualTierIndex(1f), Is.EqualTo(0));
                Assert.That(spawner.ResolveVisualTierIndex(1.29f), Is.EqualTo(0));
                Assert.That(spawner.ResolveVisualTierIndex(1.3f), Is.EqualTo(1));
                Assert.That(spawner.ResolveVisualTierIndex(1.59f), Is.EqualTo(1));
                Assert.That(spawner.ResolveVisualTierIndex(1.6f), Is.EqualTo(2));
                Assert.That(spawner.ResolveVisualTierIndex(1.89f), Is.EqualTo(2));
                Assert.That(spawner.ResolveVisualTierIndex(1.9f), Is.EqualTo(3));
                Assert.That(spawner.ResolveVisualTierIndex(2.49f), Is.EqualTo(3));
                Assert.That(spawner.ResolveVisualTierIndex(2.5f), Is.EqualTo(4));

                spawner.SetVisualTierDamage(1.9f);
                int tierBeforeProjectileUpgrade = spawner.ResolveVisualTierIndex(spawner.VisualTierDamage);
                spawner.SetProjectileCount(16);
                Assert.That(
                    spawner.ResolveVisualTierIndex(spawner.VisualTierDamage),
                    Is.EqualTo(tierBeforeProjectileUpgrade));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                for (int index = 0; index < bulletObjects.Length; index++)
                {
                    Object.DestroyImmediate(bulletObjects[index]);
                }
            }
        }

        [Test]
        public void BulletSpawner_FallbackUsesLowestValidVisualTierBeforeSerializedFallback()
        {
            var spawnerObject = new GameObject("BulletVisualFallbackTest");
            BulletSpawner spawner = spawnerObject.AddComponent<BulletSpawner>();
            var placeholderObject = new GameObject("Bullet_Tier_10");
            var tierObject = new GameObject("Bullet_Tier_00");

            try
            {
                Bullet placeholder = placeholderObject.AddComponent<Bullet>();
                Bullet tierBullet = tierObject.AddComponent<Bullet>();
                var serializedSpawner = new SerializedObject(spawner);
                serializedSpawner.FindProperty("bulletPrefab").objectReferenceValue = placeholder;
                SerializedProperty tiers = serializedSpawner.FindProperty("visualTiers");
                tiers.arraySize = 1;
                SerializedProperty tier = tiers.GetArrayElementAtIndex(0);
                tier.FindPropertyRelative("minDamage").floatValue = 1.3f;
                tier.FindPropertyRelative("bulletPrefab").objectReferenceValue = tierBullet;
                serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

                MethodInfo getPrefab = typeof(BulletSpawner).GetMethod(
                    "GetBulletPrefabForCurrentTier",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(getPrefab, Is.Not.Null);

                Bullet resolved = (Bullet)getPrefab.Invoke(spawner, null);

                Assert.That(resolved, Is.EqualTo(tierBullet));
            }
            finally
            {
                Object.DestroyImmediate(spawnerObject);
                Object.DestroyImmediate(placeholderObject);
                Object.DestroyImmediate(tierObject);
            }
        }

        [Test]
        public void ConfigureRuntimeFrom_AppliesFollowerHpRatio()
        {
            var mainObject = new GameObject("MainUnitTest");
            var followerObject = new GameObject("FollowerUnitTest");
            PlayerUnit main = mainObject.AddComponent<PlayerUnit>();
            PlayerUnit follower = followerObject.AddComponent<PlayerUnit>();

            try
            {
                main.SetMaxHp(20f);
                main.RestoreFullHealth();
                follower.ConfigureRuntimeFrom(main, restoreFullHealth: true, maxHpMultiplier: 0.25f);

                Assert.That(follower.MaxHp, Is.EqualTo(5f).Within(0.0001f));
                Assert.That(follower.CurrentHp, Is.EqualTo(5f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(mainObject);
                Object.DestroyImmediate(followerObject);
            }
        }

        [Test]
        public void Promotion_PreservesMainMaxHpAndFollowerRemainingHp()
        {
            var mainObject = new GameObject("PromotionMainTest");
            var followerObject = new GameObject("PromotionFollowerTest");
            PlayerUnit main = mainObject.AddComponent<PlayerUnit>();
            PlayerUnit follower = followerObject.AddComponent<PlayerUnit>();

            try
            {
                main.SetMaxHp(20f);
                main.RestoreFullHealth();
                follower.SetMaxHp(5f);
                follower.SetCurrentHp(3f);

                main.ReviveWithStateFrom(follower);

                Assert.That(main.MaxHp, Is.EqualTo(20f).Within(0.0001f));
                Assert.That(main.CurrentHp, Is.EqualTo(3f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(mainObject);
                Object.DestroyImmediate(followerObject);
            }
        }

        [Test]
        public void SetSquadCount_RecruitUsesFollowerAndSpawnHpRatios()
        {
            var squadObject = new GameObject("SquadIntegrationTest");
            squadObject.AddComponent<BulletSpawner>();
            MainPlayerUnit main = squadObject.AddComponent<MainPlayerUnit>();
            PlayerController controller = squadObject.AddComponent<PlayerController>();

            try
            {
                controller.SetMainPlayerUnit(main);
                main.SetMaxHp(20f);
                main.RestoreFullHealth();

                controller.SetSquadCount(2, 0.5f);

                Assert.That(controller.CurrentSquadCount, Is.EqualTo(2));
                Assert.That(controller.Followers.Count, Is.EqualTo(1));

                FollowerUnit follower = controller.Followers[0];
                Assert.That(follower.MaxHp, Is.EqualTo(5f).Within(0.0001f));
                Assert.That(follower.CurrentHp, Is.EqualTo(2.5f).Within(0.0001f));
                Assert.That(
                    follower.BulletSpawner.ShooterDamageScale,
                    Is.EqualTo(0.55f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(squadObject);
            }
        }

        [Test]
        public void ConfigureSquadUnitPhysics_PreservesManualHurtbox()
        {
            var squadObject = new GameObject("RendererHurtboxSquadTest");
            Texture2D texture = null;
            Sprite sprite = null;

            try
            {
                texture = new Texture2D(8, 8);
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 8f, 8f),
                    new Vector2(0.5f, 0.5f),
                    8f);

                squadObject.AddComponent<SpriteRenderer>().sprite = sprite;
                CircleCollider2D manualCollider = squadObject.AddComponent<CircleCollider2D>();
                manualCollider.radius = 0.1f;
                MainPlayerUnit main = squadObject.AddComponent<MainPlayerUnit>();
                PlayerController controller = squadObject.AddComponent<PlayerController>();

                controller.SetMainPlayerUnit(main);

                CircleCollider2D circle = squadObject.GetComponent<CircleCollider2D>();
                Assert.That(circle, Is.Not.Null);
                Assert.That(circle.isTrigger, Is.True);
                Assert.That(circle.radius, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(squadObject.GetComponent<BoxCollider2D>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(squadObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void EnemyController_InitPreservesManualCombatCollider()
        {
            var enemyObject = new GameObject("RendererHurtboxEnemyTest");
            var visualObject = new GameObject("Visual");
            Texture2D texture = null;
            Sprite sprite = null;

            try
            {
                visualObject.transform.SetParent(enemyObject.transform, false);
                texture = new Texture2D(8, 8);
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 8f, 8f),
                    new Vector2(0.5f, 0.5f),
                    8f);

                visualObject.AddComponent<SpriteRenderer>().sprite = sprite;
                BoxCollider2D staleCollider = enemyObject.AddComponent<BoxCollider2D>();
                staleCollider.isTrigger = true;
                staleCollider.size = new Vector2(0.1f, 0.1f);
                staleCollider.offset = new Vector2(0.2f, 0.3f);
                EnemyController enemy = enemyObject.AddComponent<EnemyController>();

                enemy.Init(null, null);

                BoxCollider2D box = enemyObject.GetComponent<BoxCollider2D>();
                Assert.That(box, Is.Not.Null);
                Assert.That(box.isTrigger, Is.True);
                Assert.That(box.size.x, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(box.size.y, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(box.offset.x, Is.EqualTo(0.2f).Within(0.0001f));
                Assert.That(box.offset.y, Is.EqualTo(0.3f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void GateLogic_AllowsLivingFollowerToChooseGate()
        {
            var squadObject = new GameObject("GateFollowerSquadTest");
            var gateObject = new GameObject("GateFollowerGateTest");
            var gateSystemObject = new GameObject("GateFollowerSystemTest");

            try
            {
                squadObject.AddComponent<BulletSpawner>();
                MainPlayerUnit main = squadObject.AddComponent<MainPlayerUnit>();
                PlayerController controller = squadObject.AddComponent<PlayerController>();
                controller.SetMainPlayerUnit(main);
                main.SetMaxHp(20f);
                main.RestoreFullHealth();
                controller.SetSquadCount(2, 0.5f);

                FollowerUnit follower = controller.Followers[0];
                GateSystem gateSystem = gateSystemObject.AddComponent<GateSystem>();
                GateLogic gate = gateObject.AddComponent<GateLogic>();
                gate.Init(null, gateSystem, main, controller, null, null, 0f, 1f, 1f);
                SetPrivateField(gateSystem, "_isGateSetActive", true);
                SetPrivateField(gate, "consumeAfterUse", false);

                bool selected = false;
                gateSystem.GateSelected += (setIndex, config) => selected = true;

                gate.HandlePlayerTriggered(follower);

                Assert.That(selected, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gateSystemObject);
                Object.DestroyImmediate(gateObject);
                Object.DestroyImmediate(squadObject);
            }
        }

        [Test]
        public void GateLogic_IgnoresDeadFollower()
        {
            var squadObject = new GameObject("GateDeadFollowerSquadTest");
            var gateObject = new GameObject("GateDeadFollowerGateTest");
            var gateSystemObject = new GameObject("GateDeadFollowerSystemTest");

            try
            {
                squadObject.AddComponent<BulletSpawner>();
                MainPlayerUnit main = squadObject.AddComponent<MainPlayerUnit>();
                PlayerController controller = squadObject.AddComponent<PlayerController>();
                controller.SetMainPlayerUnit(main);
                main.SetMaxHp(20f);
                main.RestoreFullHealth();
                controller.SetSquadCount(2, 0.5f);

                FollowerUnit follower = controller.Followers[0];
                follower.TakeDamage(999f);

                GateSystem gateSystem = gateSystemObject.AddComponent<GateSystem>();
                GateLogic gate = gateObject.AddComponent<GateLogic>();
                gate.Init(null, gateSystem, main, controller, null, null, 0f, 1f, 1f);
                SetPrivateField(gateSystem, "_isGateSetActive", true);
                SetPrivateField(gate, "consumeAfterUse", false);

                bool selected = false;
                gateSystem.GateSelected += (setIndex, config) => selected = true;

                gate.HandlePlayerTriggered(follower);

                Assert.That(selected, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gateSystemObject);
                Object.DestroyImmediate(gateObject);
                Object.DestroyImmediate(squadObject);
            }
        }

        [Test]
        public void EnemyController_TargetPositionUsesClosestSquadUnit()
        {
            var squadObject = new GameObject("EnemyTargetSquadTest");
            var enemyObject = new GameObject("EnemyTargetTest");
            Texture2D texture = null;
            Sprite sprite = null;

            try
            {
                texture = new Texture2D(8, 8);
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 8f, 8f),
                    new Vector2(0.5f, 0.5f),
                    8f);

                squadObject.AddComponent<SpriteRenderer>().sprite = sprite;
                squadObject.AddComponent<BulletSpawner>();
                MainPlayerUnit main = squadObject.AddComponent<MainPlayerUnit>();
                PlayerController controller = squadObject.AddComponent<PlayerController>();
                controller.SetMainPlayerUnit(main);
                main.SetMaxHp(20f);
                main.RestoreFullHealth();
                controller.SetSquadCount(2, 0.5f);

                FollowerUnit follower = controller.Followers[0];
                main.transform.position = Vector3.zero;
                follower.transform.position = new Vector3(3f, 0f, 0f);
                enemyObject.transform.position = new Vector3(3.5f, 0f, 0f);

                EnemyController enemy = enemyObject.AddComponent<EnemyController>();
                enemy.Init(main.transform, main, null, controller);

                Vector3 targetPosition = enemy.GetCurrentTargetPosition();

                Assert.That(targetPosition.x, Is.GreaterThan(2.5f));
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(squadObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
