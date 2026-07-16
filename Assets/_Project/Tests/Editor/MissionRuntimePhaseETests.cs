using System;
using System.IO;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Systems.MissionSystem;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem;
using NUnit.Framework;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class MissionRuntimePhaseETests
    {
        private string _directoryPath;
        private SaveService _service;
        private MissionCatalog _catalog;
        private _Project.Scripts.Systems.MissionSystem.MissionSystem _missionSystem;

        [SetUp]
        public void SetUp()
        {
            _directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-mission-runtime-test-{Guid.NewGuid():N}");
            _service = SaveService.CreateForTests(_directoryPath);
            SaveService.SetInstanceForTests(_service);
            _service.EnsureLoaded();
            _catalog = MissionCatalog.CreateRuntimeDefault();
            _missionSystem = new _Project.Scripts.Systems.MissionSystem.MissionSystem(_catalog, _service);
            _missionSystem.InitializeFromSave();
        }

        [TearDown]
        public void TearDown()
        {
            _missionSystem?.Dispose();
            if (_catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(_catalog);
            }

            SaveService.SetInstanceForTests(null);

            if (Directory.Exists(_directoryPath))
            {
                Directory.Delete(_directoryPath, recursive: true);
            }
        }

        [Test]
        public void TutorialCompletion_CompletesMissionOneAndUnlocksMissionTwo()
        {
            _service.MarkGameplayTutorialCompleted();
            _missionSystem.NotifyGameplayTutorialCompleted();

            Assert.That(_service.Data.completedMissionIds, Does.Contain("boot_finish_tutorial"));
            Assert.That(_service.Data.activeMissionId, Is.EqualTo("boot_first_loop"));
            Assert.That(_service.Data.missionNotificationUnread, Is.True);
            Assert.That(_service.Data.walletCoins, Is.EqualTo(0));
            Assert.That(_service.Data.lifetimeCoinsEarned, Is.EqualTo(0));
            Assert.That(_service.Data.grantedMissionRewardIds, Does.Not.Contain("boot_finish_tutorial"));
            Assert.That(_missionSystem.IsMissionRewardClaimable("boot_finish_tutorial"), Is.True);
        }

        [Test]
        public void EndRun_CompletesFirstLoopMissionAfterRunStatsPersist()
        {
            SetActiveMission("boot_first_loop");
            _service.RecordRunResult(42f, 3, 0, 10);

            _missionSystem.EndRun(new RunStatsSnapshot(42f, 3, 0, 10, 0, 42f, 3, 0, 10));

            Assert.That(_service.Data.completedMissionIds, Does.Contain("boot_first_loop"));
            Assert.That(_service.Data.activeMissionId, Is.EqualTo("boot_purchase_upgrade"));
        }

        [Test]
        public void GateSelected_IgnoresTutorialGatesAndCountsMajorSeparately()
        {
            SetActiveMission("fatigue_major_5");
            GateConfig stableGate = CreateGate(BalanceGateCategory.Stable);
            GateConfig majorGate = CreateGate(BalanceGateCategory.Major);

            try
            {
                _missionSystem.NotifyGateSelected(stableGate, isTutorialGate: true);
                _missionSystem.NotifyGateSelected(stableGate, isTutorialGate: false);
                _missionSystem.NotifyGateSelected(majorGate, isTutorialGate: false);

                Assert.That(_service.Data.lifetimeGatesSelected, Is.EqualTo(2));
                Assert.That(_service.Data.lifetimeMajorGatesSelected, Is.EqualTo(1));
                Assert.That(_service.Data.activeMissionId, Is.EqualTo("fatigue_major_5"));
                Assert.That(_service.Data.activeMissionProgress, Is.EqualTo(1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stableGate);
                UnityEngine.Object.DestroyImmediate(majorGate);
            }
        }

        [Test]
        public void UpgradePurchase_CompletesDeltaUpgradeMission()
        {
            SetActiveMission("boot_purchase_upgrade");

            Assert.That(_service.TryPurchaseUpgrade(PlayerMetaUpgradeType.Damage, 0), Is.True);
            _missionSystem.NotifyUpgradePurchased();

            Assert.That(_service.Data.completedMissionIds, Does.Contain("boot_purchase_upgrade"));
            Assert.That(_service.Data.activeMissionId, Is.EqualTo("boot_select_3_gates"));
        }

        [Test]
        public void FinalChoiceResolved_CompletesFinalMission()
        {
            SetActiveMission("break_final_choice");

            _missionSystem.NotifyFinalChoiceResolved("CS_07_FinalChoice_ContinueProtocol");

            Assert.That(_service.Data.finalChoiceResolved, Is.True);
            Assert.That(_service.Data.completedMissionIds, Does.Contain("break_final_choice"));
            Assert.That(_service.Data.activeMissionId, Is.Empty);
        }

        [Test]
        public void BenchmarkSuppression_PreventsMissionProgressAndRewards()
        {
            SetActiveMission("boot_first_loop");
            _service.RecordRunResult(10f, 1, 0, 1);

            _missionSystem.SetProgressionSuppressed(true);
            _missionSystem.EndRun(new RunStatsSnapshot(10f, 1, 0, 1, 0, 10f, 1, 0, 1));

            Assert.That(_service.Data.activeMissionId, Is.EqualTo("boot_first_loop"));
            Assert.That(_service.Data.completedMissionIds, Is.Empty);
            Assert.That(_service.Data.walletCoins, Is.EqualTo(0));
        }

        [Test]
        public void ClaimMissionReward_GrantsCoinsOnceAndMarksClaimed()
        {
            _service.MarkGameplayTutorialCompleted();
            _missionSystem.NotifyGameplayTutorialCompleted();

            MissionDefinition completedMission = _catalog.GetMissionById("boot_finish_tutorial");
            Assert.That(completedMission, Is.Not.Null);
            Assert.That(_missionSystem.HasAnyUnclaimedMissionRewards, Is.True);

            bool firstClaim = _missionSystem.TryClaimMissionReward("boot_finish_tutorial");
            bool secondClaim = _missionSystem.TryClaimMissionReward("boot_finish_tutorial");

            Assert.That(firstClaim, Is.True);
            Assert.That(secondClaim, Is.False);
            Assert.That(_service.Data.walletCoins, Is.EqualTo(completedMission.RewardCoins));
            Assert.That(_service.Data.lifetimeCoinsEarned, Is.EqualTo(completedMission.RewardCoins));
            Assert.That(_service.Data.grantedMissionRewardIds, Does.Contain("boot_finish_tutorial"));
            Assert.That(_missionSystem.IsMissionRewardClaimed("boot_finish_tutorial"), Is.True);
            Assert.That(_missionSystem.HasAnyUnclaimedMissionRewards, Is.False);
        }

        [Test]
        public void ClaimMissionReward_FailsForLockedOrActiveIncompleteMission()
        {
            Assert.That(_missionSystem.TryClaimMissionReward("boot_finish_tutorial"), Is.False);
            Assert.That(_missionSystem.TryClaimMissionReward("boot_first_loop"), Is.False);
            Assert.That(_missionSystem.TryClaimMissionReward("missing_mission"), Is.False);
            Assert.That(_service.Data.walletCoins, Is.EqualTo(0));
            Assert.That(_service.Data.grantedMissionRewardIds, Is.Empty);
        }

        private void SetActiveMission(string missionId)
        {
            MissionDefinition mission = _catalog.GetMissionById(missionId);
            Assert.That(mission, Is.Not.Null);
            _service.Data.activeMissionId = missionId;
            _service.Data.activeMissionProgress = 0f;
            _service.Data.activeMissionBaseline = MissionProgressEvaluator.CaptureBaseline(
                mission,
                new MissionProgressSnapshot(
                    gameplayTutorialCompleted: _service.Data.gameplayTutorialCompleted,
                    totalRunsCompleted: _service.Data.totalRunsCompleted,
                    lifetimeGatesSelected: _service.Data.lifetimeGatesSelected,
                    lifetimeMajorGatesSelected: _service.Data.lifetimeMajorGatesSelected,
                    totalEnemyKills: _service.Data.totalEnemyKills,
                    finalChoiceResolved: _service.Data.finalChoiceResolved,
                    damageLevel: _service.Data.GetUpgradeLevel(PlayerMetaUpgradeType.Damage),
                    fireRateLevel: _service.Data.GetUpgradeLevel(PlayerMetaUpgradeType.FireRate),
                    maxHpLevel: _service.Data.GetUpgradeLevel(PlayerMetaUpgradeType.MaxHp),
                    projectileCountLevel: _service.Data.GetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount),
                    squadSizeLevel: _service.Data.GetUpgradeLevel(PlayerMetaUpgradeType.SquadSize),
                    squadSizeValue: 1));
            _service.Data.missionNotificationUnread = false;
            _service.CommitMissionState();
            _missionSystem.InitializeFromSave();
        }

        private static GateConfig CreateGate(BalanceGateCategory category)
        {
            GateConfig gate = ScriptableObject.CreateInstance<GateConfig>();
            gate.ConfigureRuntime(new BalanceGateEntry(
                "test_gate",
                "TEST GATE",
                category,
                1f,
                0f,
                BalanceEffectType.DamageMultiplier,
                1.1f));
            return gate;
        }
    }
}
