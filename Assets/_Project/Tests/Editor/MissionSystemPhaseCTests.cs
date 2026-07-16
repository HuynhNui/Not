using _Project.Scripts.Systems.MissionSystem;
using _Project.Scripts.Systems.ProgressionSystem;
using NUnit.Framework;
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
                Assert.That(catalog.Count, Is.EqualTo(24));
                Assert.That(catalog.GetMissionAt(0).Id, Is.EqualTo("boot_finish_tutorial"));
                Assert.That(catalog.GetMissionAt(23).Id, Is.EqualTo("break_final_choice"));
                Assert.That(catalog.GetMissionById("observe_dmg_lv2").UpgradeType, Is.EqualTo(PlayerMetaUpgradeType.Damage));
                Assert.That(
                    catalog.GetMissionById("memory_three_upgrades_lv2").ObjectiveParameterValue,
                    Is.EqualTo(2f));
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
                Assert.That(unlockedMission.Id, Is.EqualTo("boot_first_loop"));
                Assert.That(missionSystem.ActiveMissionId, Is.EqualTo("boot_first_loop"));
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
    }
}
