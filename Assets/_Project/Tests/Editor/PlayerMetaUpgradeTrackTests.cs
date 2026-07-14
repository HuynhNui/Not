using System;
using System.IO;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.SaveSystem;
using NUnit.Framework;

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
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(3));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.SquadSize), Is.EqualTo(5));
            Assert.That(PlayerMetaUpgradeService.GetMaxLevel(PlayerMetaUpgradeType.MoveSpeed), Is.EqualTo(0));
        }

        [Test]
        public void ProjectileValues_PlateauAtSixAfterLevelThree()
        {
            int[] expected = { 1, 2, 4, 6, 6, 6 };
            for (int level = 0; level < expected.Length; level++)
            {
                Assert.That(
                    PlayerMetaUpgradeService.GetValueForLevel(
                        PlayerMetaUpgradeType.ProjectileCount,
                        level),
                    Is.EqualTo(expected[level]));
            }

            Assert.That(
                PlayerMetaUpgradeService.CalculateMaxValue(PlayerMetaUpgradeType.ProjectileCount),
                Is.EqualTo(6));
        }

        [Test]
        public void ProjectilePurchase_StopsAtLevelThree()
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
                Assert.That(PlayerMetaUpgradeService.TryPurchase(PlayerMetaUpgradeType.ProjectileCount), Is.True);
                Assert.That(service.GetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(3));
                Assert.That(PlayerMetaUpgradeService.IsMaxLevel(PlayerMetaUpgradeType.ProjectileCount), Is.True);
                Assert.That(PlayerMetaUpgradeService.GetCost(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(0));
                Assert.That(PlayerMetaUpgradeService.TryPurchase(PlayerMetaUpgradeType.ProjectileCount), Is.False);
                Assert.That(service.GetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(3));
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
        public void SchemaSixProjectileLevelFive_MigratesToLevelThreeWithoutResettingProgress()
        {
            var saveData = SaveData.CreateNew(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            saveData.schemaVersion = 6;
            saveData.walletCoins = 1234;
            saveData.storyStage = 2;
            saveData.gameplayTutorialCompleted = true;
            saveData.SetUpgradeLevel(PlayerMetaUpgradeType.Damage, 5);
            saveData.SetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount, 5);

            saveData.Normalize(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            Assert.That(saveData.schemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(saveData.GetUpgradeLevel(PlayerMetaUpgradeType.ProjectileCount), Is.EqualTo(3));
            Assert.That(saveData.GetUpgradeLevel(PlayerMetaUpgradeType.Damage), Is.EqualTo(5));
            Assert.That(saveData.walletCoins, Is.EqualTo(1234));
            Assert.That(saveData.storyStage, Is.EqualTo(2));
            Assert.That(saveData.gameplayTutorialCompleted, Is.True);
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
    }
}
