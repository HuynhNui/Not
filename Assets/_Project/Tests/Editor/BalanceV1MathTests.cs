using _Project.Scripts.Data.Balance;
using _Project.Scripts.Gameplay.Combat;
using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Gameplay.Gates;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.Balance;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.Telemetry;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.IO;

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
            Assert.That(BalanceV1Math.FollowerDamageScale(2), Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(BalanceV1Math.SquadFactor(12), Is.LessThan(2.5f));
            Assert.That(BalanceV1Math.FollowerDamageScale(12), Is.LessThan(0.15f));
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
            float baseline = BalanceV1Math.EffectiveDps(1f, 4f, 5, 1);
            float fullMeta = BalanceV1Math.EffectiveDps(1.55f, 6.4f, 16, 12);
            float ratio = fullMeta / baseline;

            Assert.That(ratio, Is.InRange(7f, 8f));
        }

        [Test]
        public void FullMetaDurability_RemainsInsideTargetRange()
        {
            float hpMultiplier = 20f / 10f;
            float durabilityRatio = hpMultiplier * BalanceV1Math.SquadDurabilityFactor(12, 0.25f);

            Assert.That(durabilityRatio, Is.EqualTo(7.5f).Within(0.0001f));
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
            Assert.That(GateSystem.GetMajorChance(60f), Is.EqualTo(0.15f));
            Assert.That(GateSystem.GetMajorChance(180f), Is.EqualTo(0.2f));
            Assert.That(GateSystem.GetMajorChance(300f), Is.EqualTo(0.25f));
            Assert.That(
                GateSystem.ShouldSpawnMajor(4, 60f, 15f, 60f, 0.1f),
                Is.True);
            Assert.That(
                GateSystem.ShouldSpawnMajor(4, 60f, 15f, 60f, 0.2f),
                Is.False);
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
        public void EnemyRoleRewards_UseFractionalEconomyValues()
        {
            Assert.That(
                EnemyRoleBalanceDefaults.GetRewardPoints(BalanceEnemyRole.Basic),
                Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(
                EnemyRoleBalanceDefaults.GetRewardPoints(BalanceEnemyRole.Chomboom),
                Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(
                EnemyRoleBalanceDefaults.GetRewardPoints(BalanceEnemyRole.Vomfy),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                EnemyRoleBalanceDefaults.GetRewardPoints(BalanceEnemyRole.Elite),
                Is.InRange(12f, 18f));
        }

        [Test]
        public void Economy_RoundsRewardPointsOnceAtRunLevel()
        {
            Assert.That(
                EconomyConfig.CalculateDefaultFinalCoins(0.6f, 0f),
                Is.EqualTo(1));
            Assert.That(
                EconomyConfig.CalculateDefaultFinalCoins(1.4f, 0f),
                Is.EqualTo(1));
            Assert.That(
                EconomyConfig.CalculateDefaultFinalCoins(1.6f, 0f),
                Is.EqualTo(2));
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
            Assert.That(saveData.GetUpgradeLevel(PlayerMetaUpgradeType.Damage), Is.EqualTo(3));
        }

        [Test]
        public void SaveData_V2JsonMigratesToV3WithStoryDefaults()
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
                service.RecordRunResult(120f, 10, 2, 30);
                int runCountAfterFirstRun = service.Data.totalRunsCompleted;

                service.RecordRunResult(1f, 0, 0, 0);

                Assert.That(runCountAfterFirstRun, Is.EqualTo(1));
                Assert.That(service.Data.totalRunsCompleted, Is.EqualTo(2));
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
                Assert.That(fullMeta.Damage, Is.EqualTo(1.55f).Within(0.0001f));
                Assert.That(fullMeta.FireRate, Is.EqualTo(6.4f).Within(0.0001f));
                Assert.That(fullMeta.ProjectileCount, Is.EqualTo(16));
                Assert.That(fullMeta.SquadSize, Is.EqualTo(12));
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
                Is.EqualTo(1.55f).Within(0.0001f));
            Assert.That(
                PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.FireRate, 5),
                Is.EqualTo(6.4f).Within(0.0001f));
            Assert.That(
                PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.MaxHp, 5),
                Is.EqualTo(20f).Within(0.0001f));
            Assert.That(
                PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.ProjectileCount, 5),
                Is.EqualTo(16f));
            Assert.That(
                PlayerMetaUpgradeService.GetValueForLevel(PlayerMetaUpgradeType.SquadSize, 5),
                Is.EqualTo(12f));
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
                    Is.EqualTo(0.45f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(squadObject);
            }
        }
    }
}
