using System;
using System.IO;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Gameplay.Combat;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.SaveSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class PlayerMetaUpgradeTrackTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerMetaUpgradeService.Configure(null, null);
        }

        [TearDown]
        public void TearDown()
        {
            SaveService.SetInstanceForTests(null);
            PlayerMetaUpgradeService.Configure(null, null);
        }

        [Test]
        public void MaxLevels_ArePerUpgradeType()
        {
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.Damage), Is.EqualTo(5));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.FireRate), Is.EqualTo(5));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.MaxHp), Is.EqualTo(5));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(2));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.SquadSize), Is.EqualTo(3));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.MoveSpeed), Is.EqualTo(0));
        }

        [Test]
        public void ProjectileAndSquadValues_UseEliteSquadLowProjectileProgression()
        {
            int[] expectedProjectiles = { 1, 2, 3 };
            for (int level = 0; level < expectedProjectiles.Length; level++)
            {
                Assert.That(
                    PlayerMetaUpgradeService.GetValueForLevel(
                        PlayerMetaUpgradeType.ProjectileCount,
                        level),
                    Is.EqualTo(expectedProjectiles[level]));
            }

            int[] expectedSquad = { 1, 2, 3, 4 };
            for (int level = 0; level < expectedSquad.Length; level++)
            {
                Assert.That(
                    PlayerMetaUpgradeService.GetValueForLevel(
                        PlayerMetaUpgradeType.SquadSize,
                        level),
                    Is.EqualTo(expectedSquad[level]));
            }

            Assert.That(
                PlayerMetaUpgradeService.CalculateMaxValue(PlayerMetaUpgradeType.ProjectileCount),
                Is.EqualTo(3));
            Assert.That(
                PlayerMetaUpgradeService.CalculateMaxValue(PlayerMetaUpgradeType.SquadSize),
                Is.EqualTo(4));
        }

        [Test]
        public void DefaultPermanentDamageValues_UseDamageForwardMetaProgression()
        {
            PlayerMetaBalanceConfig config = ScriptableObject.CreateInstance<PlayerMetaBalanceConfig>();
            float[] expectedDamage = { 1.00f, 1.40f, 1.80f, 2.20f, 2.60f, 3.00f };

            for (int level = 0; level < expectedDamage.Length; level++)
            {
                Assert.That(config.GetLevelData(level).Damage, Is.EqualTo(expectedDamage[level]).Within(0.0001f));
            }

            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void ProjectilePurchase_StopsAtLevelTwo()
        {
            string directoryPath = CreateTempDirectoryPath("true-gate-bullet-purchase");
            SaveService service = SaveService.CreateForTests(directoryPath);
            SaveService.SetInstanceForTests(service);

            try
            {
                service.EnsureLoaded();
                service.RecordRunResult(1f, 0, 1000, 0);

                Assert.That(PlayerMetaUpgradeService.TryPurchase(PlayerMetaUpgradeType.ProjectileCount), Is.True);
                Assert.That(service.GetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(1));
                Assert.That(PlayerMetaUpgradeService.TryPurchase(PlayerMetaUpgradeType.ProjectileCount), Is.True);
                Assert.That(service.GetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(2));
                Assert.That(PlayerMetaUpgradeService.IsMaxLevel(PlayerMetaUpgradeType.ProjectileCount), Is.True);
                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(0));
                Assert.That(PlayerMetaUpgradeService.TryPurchase(PlayerMetaUpgradeType.ProjectileCount), Is.False);
                Assert.That(service.GetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(2));
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
        public void Run45EconomyConfig_HasExpectedTrackTotalsAndPurchaseCounts()
        {
            PlayerMetaEconomyConfig economy = PlayerMetaEconomyConfig.CreateRun45RuntimeConfig();

            try
            {
                Assert.That(economy.GetFullTreeTotalCost(), Is.EqualTo(850000));
                Assert.That(economy.GetTrackTotalCost(PlayerMetaUpgradeType.Damage), Is.EqualTo(250000));
                Assert.That(economy.GetTrackTotalCost(PlayerMetaUpgradeType.FireRate), Is.EqualTo(190000));
                Assert.That(economy.GetTrackTotalCost(PlayerMetaUpgradeType.MaxHp), Is.EqualTo(140000));
                Assert.That(economy.GetTrackTotalCost(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(70000));
                Assert.That(economy.GetTrackTotalCost(PlayerMetaUpgradeType.SquadSize), Is.EqualTo(200000));

                Assert.That(economy.GetPurchaseCount(PlayerMetaUpgradeType.Damage), Is.EqualTo(5));
                Assert.That(economy.GetPurchaseCount(PlayerMetaUpgradeType.FireRate), Is.EqualTo(5));
                Assert.That(economy.GetPurchaseCount(PlayerMetaUpgradeType.MaxHp), Is.EqualTo(5));
                Assert.That(economy.GetPurchaseCount(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(2));
                Assert.That(economy.GetPurchaseCount(PlayerMetaUpgradeType.SquadSize), Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(economy);
            }
        }

        [Test]
        public void Run45EconomyConfig_ProvidesPerTrackPurchaseCosts()
        {
            PlayerMetaEconomyConfig economy = PlayerMetaEconomyConfig.CreateRun45RuntimeConfig();
            string directoryPath = CreateTempDirectoryPath("true-gate-run45-costs");
            SaveService service = SaveService.CreateForTests(directoryPath);

            try
            {
                SaveService.SetInstanceForTests(service);
                PlayerMetaUpgradeService.Configure(null, economy, null);
                service.EnsureLoaded();

                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.Damage), Is.EqualTo(4000));

                service.Data.SetUpgradeLevel(PlayerMetaUpgradeType.SquadSize, 2);
                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.SquadSize), Is.EqualTo(140000));

                service.Data.SetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount, 1);
                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(55000));
            }
            finally
            {
                SaveService.SetInstanceForTests(null);
                PlayerMetaUpgradeService.Configure(null, null);
                UnityEngine.Object.DestroyImmediate(economy);
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
        }

        [Test]
        public void Run45RewardFormula_PreservesStrongestRunAnchor()
        {
            EconomyConfig economy = ScriptableObject.CreateInstance<EconomyConfig>();
            var serializedObject = new SerializedObject(economy);
            serializedObject.FindProperty("rewardScale").floatValue = 0.85f;
            serializedObject.FindProperty("timeCoinPer30Seconds").floatValue = 300f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            economy.ValidateValues();

            try
            {
                Assert.That(
                    economy.CalculateFinalCoins(34493f, 545f),
                    Is.EqualTo(34769).Within(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(economy);
            }
        }

        [Test]
        public void SchemaSevenQuantityLevels_MigrateToEliteSquadCapsWithoutResettingProgress()
        {
            var saveData = SaveData.CreateNew(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            saveData.schemaVersion = 7;
            saveData.walletCoins = 1234;
            saveData.storyStage = 2;
            saveData.gameplayTutorialCompleted = true;
            saveData.SetUpgradeLevel(PlayerMetaUpgradeType.Damage, 5);
            saveData.SetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount, 5);
            saveData.SetUpgradeLevel(PlayerMetaUpgradeType.SquadSize, 5);

            saveData.Normalize(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            Assert.That(saveData.schemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(saveData.GetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(2));
            Assert.That(saveData.GetUpgradeLevel(PlayerMetaUpgradeType.SquadSize), Is.EqualTo(3));
            Assert.That(saveData.GetUpgradeLevel(PlayerMetaUpgradeType.Damage), Is.EqualTo(5));
            Assert.That(saveData.walletCoins, Is.EqualTo(1234));
            Assert.That(saveData.storyStage, Is.EqualTo(2));
            Assert.That(saveData.gameplayTutorialCompleted, Is.True);
        }

        [Test]
        public void ApplyStatsToPlayer_EliteSquadFollowersMirrorMainStats()
        {
            GameObject root = new GameObject("elite-squad-test-root");
            GameObject mainObject = new GameObject("main");
            mainObject.transform.SetParent(root.transform);
            mainObject.AddComponent<BulletSpawner>();
            MainPlayerUnit main = mainObject.AddComponent<MainPlayerUnit>();
            main.Initialize();
            PlayerController controller = root.AddComponent<PlayerController>();
            SetPrivateObjectReference(controller, "mainPlayerUnit", main);

            CombatScalingConfig combat = ScriptableObject.CreateInstance<CombatScalingConfig>();
            SetSquadPowerModel(combat, SquadPowerModel.EqualStrengthUnits);
            PlayerMetaUpgradeService.Configure(null, combat);

            try
            {
                PlayerMetaUpgradeService.ApplyStatsToPlayer(
                    new PlayerRunStartStats(3.00f, 6.40f, 20f, 3, 4),
                    main,
                    controller);

                Assert.That(controller.CurrentSquadCount, Is.EqualTo(4));
                Assert.That(controller.Followers.Count, Is.EqualTo(3));
                for (int index = 0; index < controller.Followers.Count; index++)
                {
                    FollowerUnit follower = controller.Followers[index];
                    Assert.That(follower.Damage, Is.EqualTo(3.00f).Within(0.0001f));
                    Assert.That(follower.FireRate, Is.EqualTo(6.40f).Within(0.0001f));
                    Assert.That(follower.MaxHp, Is.EqualTo(20f).Within(0.0001f));
                    Assert.That(follower.BulletSpawner.ProjectileCount, Is.EqualTo(3));
                    Assert.That(follower.BulletSpawner.ShooterDamageScale, Is.EqualTo(1f).Within(0.0001f));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(combat);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DebugWalletCoins_AddsCoinsInEditor()
        {
            string directoryPath = CreateTempDirectoryPath("true-gate-debug-coins");
            SaveService service = SaveService.CreateForTests(directoryPath);
            SaveService.SetInstanceForTests(service);

            try
            {
                service.EnsureLoaded();

                Assert.That(service.TryAddDebugWalletCoins(10000), Is.True);
                Assert.That(service.Data.walletCoins, Is.EqualTo(10000));
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

        private static string CreateTempDirectoryPath(string prefix)
        {
            return Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        }

        private static void SetPrivateObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSquadPowerModel(
            CombatScalingConfig combat,
            SquadPowerModel model)
        {
            var serializedObject = new SerializedObject(combat);
            serializedObject.FindProperty("squadPowerModel").enumValueIndex = (int)model;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            combat.ValidateValues();
        }
    }
}
