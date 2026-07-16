using _Project.Scripts.Systems.MissionSystem;
using _Project.Scripts.Systems.ProgressionSystem;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class MissionSystemPhaseCTests
    {
        [Test]
        public void DefaultCatalog_HasFullPlanMissionChain()
        {
            MissionCatalog catalog = MissionCatalog.CreateRuntimeDefault();

            try
            {
                Assert.That(catalog.Count, Is.EqualTo(47));
                Assert.That(catalog.GetMissionAt(0).Id, Is.EqualTo("boot_finish_tutorial"));
                Assert.That(catalog.GetMissionAt(37).Id, Is.EqualTo("break_final_choice"));
                Assert.That(catalog.GetMissionAt(46).Id, Is.EqualTo("terminal_250000_total_kills"));
                Assert.That(catalog.GetMissionById("observe_dmg_lv2").UpgradeType, Is.EqualTo(PlayerMetaUpgradeType.Damage));
                Assert.That(
                    catalog.GetMissionById("memory_three_upgrades_lv2").ObjectiveParameterValue,
                    Is.EqualTo(2f));

                AssertMission(
                    catalog,
                    "boot_survive_30",
                    MissionObjectiveType.SingleRunSurvivalTime,
                    MissionProgressMode.BestSingleRun,
                    30,
                    1000);
                AssertMission(
                    catalog,
                    "observe_100_total_kills",
                    MissionObjectiveType.TotalEnemyKills,
                    MissionProgressMode.AbsoluteLifetime,
                    100,
                    2500);
                AssertMission(
                    catalog,
                    "break_45_loops",
                    MissionObjectiveType.TotalRunsCompleted,
                    MissionProgressMode.AbsoluteLifetime,
                    45,
                    18000);
                AssertMission(
                    catalog,
                    "break_50_loops",
                    MissionObjectiveType.TotalRunsCompleted,
                    MissionProgressMode.AbsoluteLifetime,
                    50,
                    20000);
                AssertMission(
                    catalog,
                    "terminal_1000_kills_run",
                    MissionObjectiveType.SingleRunEnemyKills,
                    MissionProgressMode.BestSingleRun,
                    1000,
                    10000);
                AssertMission(
                    catalog,
                    "terminal_10000_kills_run",
                    MissionObjectiveType.SingleRunEnemyKills,
                    MissionProgressMode.BestSingleRun,
                    10000,
                    30000);
                AssertMission(
                    catalog,
                    "terminal_250000_total_kills",
                    MissionObjectiveType.TotalEnemyKills,
                    MissionProgressMode.AbsoluteLifetime,
                    250000,
                    40000);

                string[] stableMissionIds =
                {
                    "boot_finish_tutorial",
                    "boot_first_loop",
                    "boot_purchase_upgrade",
                    "boot_select_3_gates",
                    "observe_3_loops",
                    "observe_100_kills_run",
                    "observe_dmg_lv2",
                    "observe_survive_120",
                    "memory_select_10_gates",
                    "memory_10_loops",
                    "memory_survive_180",
                    "memory_three_upgrades_lv2",
                    "command_1000_total_kills",
                    "command_20_loops",
                    "command_survive_300",
                    "command_squad_3",
                    "fatigue_major_5",
                    "fatigue_35_loops",
                    "fatigue_survive_360",
                    "fatigue_max_one_upgrade",
                    "break_max_all_upgrades",
                    "break_3000_total_kills",
                    "break_survive_420",
                    "break_final_choice"
                };
                for (int index = 0; index < stableMissionIds.Length; index++)
                {
                    Assert.That(catalog.GetMissionById(stableMissionIds[index]), Is.Not.Null, stableMissionIds[index]);
                }

                var seenIds = new HashSet<string>();
                int rewardTotal = 0;
                for (int index = 0; index < catalog.Count; index++)
                {
                    MissionDefinition mission = catalog.GetMissionAt(index);
                    Assert.That(seenIds.Add(mission.Id), Is.True, $"Duplicate mission id: {mission.Id}");
                    Assert.That(mission.RewardCoins, Is.GreaterThanOrEqualTo(0), mission.Id);
                    rewardTotal += mission.RewardCoins;
                }

                Assert.That(rewardTotal, Is.EqualTo(485500));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void DeltaSinceUnlock_UsesCapturedBaseline()
        {
            MissionDefinition mission = new MissionDefinition(
                "boot_purchase_upgrade",
                "BOOT",
                "PURCHASE ANY UPGRADE",
                MissionObjectiveType.AnyCoreUpgradePurchased,
                MissionProgressMode.DeltaSinceUnlock,
                1);
            MissionProgressSnapshot unlockSnapshot = new MissionProgressSnapshot(damageLevel: 1);
            float baseline = MissionProgressEvaluator.CaptureBaseline(mission, unlockSnapshot);

            MissionProgressResult before = MissionProgressEvaluator.Evaluate(
                mission,
                new MissionProgressSnapshot(damageLevel: 1),
                baseline);
            MissionProgressResult after = MissionProgressEvaluator.Evaluate(
                mission,
                new MissionProgressSnapshot(damageLevel: 2),
                baseline);

            Assert.That(before.IsComplete, Is.False);
            Assert.That(before.ProgressValue, Is.EqualTo(0f));
            Assert.That(after.IsComplete, Is.True);
            Assert.That(after.ProgressValue, Is.EqualTo(1f));
        }

        [Test]
        public void GateDelta_AndMajorGateObjectives_UseSeparateCounters()
        {
            MissionDefinition gateMission = new MissionDefinition(
                "memory_select_10_gates",
                "MEMORY LEAK",
                "PASS THROUGH 10 GATES",
                MissionObjectiveType.GatesSelected,
                MissionProgressMode.DeltaSinceUnlock,
                10);
            MissionDefinition majorMission = new MissionDefinition(
                "fatigue_major_5",
                "SYSTEM FATIGUE",
                "TRIGGER 5 MAJOR GATES",
                MissionObjectiveType.MajorGatesSelected,
                MissionProgressMode.DeltaSinceUnlock,
                5);
            MissionProgressSnapshot baselineSnapshot = new MissionProgressSnapshot(
                lifetimeGatesSelected: 8,
                lifetimeMajorGatesSelected: 1);
            float gateBaseline = MissionProgressEvaluator.CaptureBaseline(gateMission, baselineSnapshot);
            float majorBaseline = MissionProgressEvaluator.CaptureBaseline(majorMission, baselineSnapshot);
            MissionProgressSnapshot progressSnapshot = new MissionProgressSnapshot(
                lifetimeGatesSelected: 18,
                lifetimeMajorGatesSelected: 4);

            Assert.That(
                MissionProgressEvaluator.Evaluate(gateMission, progressSnapshot, gateBaseline).IsComplete,
                Is.True);
            Assert.That(
                MissionProgressEvaluator.Evaluate(majorMission, progressSnapshot, majorBaseline).ProgressValue,
                Is.EqualTo(3f));
            Assert.That(
                MissionProgressEvaluator.Evaluate(majorMission, progressSnapshot, majorBaseline).IsComplete,
                Is.False);
        }

        [Test]
        public void BestSingleRun_KeepsBestStoredProgress()
        {
            MissionDefinition mission = new MissionDefinition(
                "observe_100_kills_run",
                "OBSERVE",
                "DEFEAT 100 ENEMIES IN ONE RUN",
                MissionObjectiveType.SingleRunEnemyKills,
                MissionProgressMode.BestSingleRun,
                100);

            MissionProgressResult lowerRun = MissionProgressEvaluator.Evaluate(
                mission,
                new MissionProgressSnapshot(currentRunEnemyKills: 40),
                storedProgressOrBaseline: 75);
            MissionProgressResult betterRun = MissionProgressEvaluator.Evaluate(
                mission,
                new MissionProgressSnapshot(currentRunEnemyKills: 120),
                storedProgressOrBaseline: lowerRun.ProgressValue);

            Assert.That(lowerRun.ProgressValue, Is.EqualTo(75f));
            Assert.That(lowerRun.IsComplete, Is.False);
            Assert.That(betterRun.ProgressValue, Is.EqualTo(120f));
            Assert.That(betterRun.IsComplete, Is.True);
        }

        [Test]
        public void CoreUpgradeObjectives_IgnoreMoveSpeedAndUsePlanCaps()
        {
            MissionDefinition threeAtLevelTwo = new MissionDefinition(
                "memory_three_upgrades_lv2",
                "MEMORY LEAK",
                "RAISE 3 CORE UPGRADES TO LV.2",
                MissionObjectiveType.CoreUpgradesAtLevel,
                MissionProgressMode.AbsoluteLifetime,
                3,
                objectiveParameterValue: 2);
            MissionDefinition maxAll = new MissionDefinition(
                "break_max_all_upgrades",
                "BREAK THE CYCLE",
                "MAX ALL 5 CORE UPGRADES",
                MissionObjectiveType.MaxedCoreUpgrades,
                MissionProgressMode.AbsoluteLifetime,
                5);
            MissionProgressSnapshot partial = new MissionProgressSnapshot(
                damageLevel: 2,
                fireRateLevel: 2,
                maxHpLevel: 1,
                projectileCountLevel: 2,
                squadSizeLevel: 1);
            MissionProgressSnapshot maxed = new MissionProgressSnapshot(
                damageLevel: 5,
                fireRateLevel: 5,
                maxHpLevel: 5,
                projectileCountLevel: 2,
                squadSizeLevel: 3);

            Assert.That(MissionProgressEvaluator.Evaluate(threeAtLevelTwo, partial).IsComplete, Is.True);
            Assert.That(MissionProgressEvaluator.Evaluate(maxAll, maxed).ProgressValue, Is.EqualTo(5f));
            Assert.That(MissionProgressEvaluator.Evaluate(maxAll, maxed).IsComplete, Is.True);
        }

        [Test]
        public void MissionSystem_CompletesActiveAndUnlocksExactlyNextMission()
        {
            MissionCatalog catalog = MissionCatalog.CreateRuntimeDefault();
            var missionSystem = new _Project.Scripts.Systems.MissionSystem.MissionSystem(catalog);

            try
            {
                Assert.That(missionSystem.StartFirstMission(new MissionProgressSnapshot()), Is.True);
                Assert.That(missionSystem.ActiveMissionId, Is.EqualTo("boot_finish_tutorial"));

                bool completed = missionSystem.TryCompleteActiveMission(
                    new MissionProgressSnapshot(gameplayTutorialCompleted: true),
                    out MissionDefinition completedMission,
                    out MissionDefinition unlockedMission);

                Assert.That(completed, Is.True);
                Assert.That(completedMission.Id, Is.EqualTo("boot_finish_tutorial"));
                Assert.That(unlockedMission.Id, Is.EqualTo("boot_survive_30"));
                Assert.That(missionSystem.ActiveMissionId, Is.EqualTo("boot_survive_30"));
                Assert.That(missionSystem.CompletedMissionIds, Does.Contain("boot_finish_tutorial"));
                Assert.That(missionSystem.MissionNotificationUnread, Is.True);

                missionSystem.MarkNotificationRead();
                Assert.That(missionSystem.MissionNotificationUnread, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void FinalChoiceObjective_CompletesWhenEitherBranchIsResolved()
        {
            MissionDefinition mission = new MissionDefinition(
                "break_final_choice",
                "BREAK THE CYCLE",
                "MAKE THE FINAL CHOICE",
                MissionObjectiveType.FinalChoiceResolved,
                MissionProgressMode.AbsoluteLifetime,
                1);

            Assert.That(
                MissionProgressEvaluator.Evaluate(mission, new MissionProgressSnapshot()).IsComplete,
                Is.False);
            Assert.That(
                MissionProgressEvaluator.Evaluate(
                    mission,
                    new MissionProgressSnapshot(finalChoiceResolved: true)).IsComplete,
                Is.True);
        }

        private static void AssertMission(
            MissionCatalog catalog,
            string id,
            MissionObjectiveType objectiveType,
            MissionProgressMode progressMode,
            float targetValue,
            int rewardCoins)
        {
            MissionDefinition mission = catalog.GetMissionById(id);
            Assert.That(mission, Is.Not.Null, id);
            Assert.That(mission.ObjectiveType, Is.EqualTo(objectiveType), id);
            Assert.That(mission.ProgressMode, Is.EqualTo(progressMode), id);
            Assert.That(mission.TargetValue, Is.EqualTo(targetValue), id);
            Assert.That(mission.RewardCoins, Is.EqualTo(rewardCoins), id);
        }
    }
}
