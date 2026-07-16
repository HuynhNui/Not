using System;
using System.Collections.Generic;
using System.IO;
using _Project.Scripts.Systems.SaveSystem;
using NUnit.Framework;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class MissionSavePhaseDTests
    {
        private string _directoryPath;
        private SaveService _service;

        [SetUp]
        public void SetUp()
        {
            _directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-mission-save-test-{Guid.NewGuid():N}");
            _service = SaveService.CreateForTests(_directoryPath);
            SaveService.SetInstanceForTests(_service);
            _service.EnsureLoaded();
        }

        [TearDown]
        public void TearDown()
        {
            SaveService.SetInstanceForTests(null);

            if (Directory.Exists(_directoryPath))
            {
                Directory.Delete(_directoryPath, recursive: true);
            }
        }

        [Test]
        public void NewSave_InitializesMissionStateAtFirstMission()
        {
            SaveData data = _service.Data;

            Assert.That(data.schemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(data.activeMissionId, Is.EqualTo(SaveData.FirstMissionId));
            Assert.That(data.activeMissionProgress, Is.EqualTo(0f));
            Assert.That(data.activeMissionBaseline, Is.EqualTo(0f));
            Assert.That(data.completedMissionIds, Is.Not.Null.And.Empty);
            Assert.That(data.grantedMissionRewardIds, Is.Not.Null.And.Empty);
            Assert.That(data.lifetimeGatesSelected, Is.EqualTo(0));
            Assert.That(data.lifetimeMajorGatesSelected, Is.EqualTo(0));
            Assert.That(data.missionNotificationUnread, Is.True);
            Assert.That(data.finalChoiceResolved, Is.False);
        }

        [Test]
        public void Normalize_MigratesLegacySaveAndCleansMissionLists()
        {
            var saveData = SaveData.CreateNew(1000);
            saveData.schemaVersion = 9;
            saveData.activeMissionId = " memory_select_10_gates ";
            saveData.activeMissionProgress = -10f;
            saveData.activeMissionBaseline = -5f;
            saveData.completedMissionIds = new List<string>
            {
                " boot_finish_tutorial ",
                "",
                null,
                "boot_finish_tutorial",
                "boot_first_loop"
            };
            saveData.grantedMissionRewardIds = new List<string>
            {
                " boot_finish_tutorial ",
                "boot_finish_tutorial",
                "boot_first_loop"
            };
            saveData.lifetimeGatesSelected = 3;
            saveData.lifetimeMajorGatesSelected = 7;

            saveData.Normalize(2000);

            Assert.That(saveData.schemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(saveData.activeMissionId, Is.EqualTo("memory_select_10_gates"));
            Assert.That(saveData.activeMissionProgress, Is.EqualTo(0f));
            Assert.That(saveData.activeMissionBaseline, Is.EqualTo(0f));
            Assert.That(saveData.completedMissionIds, Is.EqualTo(new[] { "boot_finish_tutorial", "boot_first_loop" }));
            Assert.That(saveData.grantedMissionRewardIds, Is.EqualTo(new[] { "boot_finish_tutorial", "boot_first_loop" }));
            Assert.That(saveData.lifetimeMajorGatesSelected, Is.EqualTo(3));
        }

        [Test]
        public void OldSchemaJson_DefaultsToFirstMission()
        {
            const string legacyJson =
                "{\"schemaVersion\":9,\"revision\":2,\"lastUpdatedUnixMs\":1234,"
                + "\"walletCoins\":50,\"completedMissionIds\":null,\"grantedMissionRewardIds\":null}";
            SaveData saveData = JsonUtility.FromJson<SaveData>(legacyJson);

            saveData.Normalize(9999);

            Assert.That(saveData.schemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(saveData.activeMissionId, Is.EqualTo(SaveData.FirstMissionId));
            Assert.That(saveData.missionNotificationUnread, Is.True);
            Assert.That(saveData.completedMissionIds, Is.Not.Null.And.Empty);
            Assert.That(saveData.grantedMissionRewardIds, Is.Not.Null.And.Empty);
            Assert.That(saveData.walletCoins, Is.EqualTo(50));
        }

        [Test]
        public void Clone_CopiesMissionStateWithoutSharingLists()
        {
            SaveData data = _service.Data;
            data.activeMissionId = "boot_select_3_gates";
            data.activeMissionProgress = 2f;
            data.activeMissionBaseline = 4f;
            data.completedMissionIds.Add("boot_finish_tutorial");
            data.grantedMissionRewardIds.Add("boot_finish_tutorial");
            data.lifetimeGatesSelected = 6;
            data.lifetimeMajorGatesSelected = 2;
            data.missionNotificationUnread = false;
            data.finalChoiceResolved = true;

            SaveData clone = data.Clone();
            clone.completedMissionIds.Add("boot_first_loop");
            clone.grantedMissionRewardIds.Add("boot_first_loop");

            Assert.That(clone.activeMissionId, Is.EqualTo("boot_select_3_gates"));
            Assert.That(clone.activeMissionProgress, Is.EqualTo(2f));
            Assert.That(clone.activeMissionBaseline, Is.EqualTo(4f));
            Assert.That(clone.lifetimeGatesSelected, Is.EqualTo(6));
            Assert.That(clone.lifetimeMajorGatesSelected, Is.EqualTo(2));
            Assert.That(clone.missionNotificationUnread, Is.False);
            Assert.That(clone.finalChoiceResolved, Is.True);
            Assert.That(data.completedMissionIds, Is.EqualTo(new[] { "boot_finish_tutorial" }));
            Assert.That(data.grantedMissionRewardIds, Is.EqualTo(new[] { "boot_finish_tutorial" }));
        }

        [Test]
        public void GrantMissionRewardOnce_MarksMissionOnlyOnceWithoutCoins()
        {
            bool firstGrant = _service.GrantMissionRewardOnce(" boot_finish_tutorial ", 250);
            bool secondGrant = _service.GrantMissionRewardOnce("boot_finish_tutorial", 250);

            Assert.That(firstGrant, Is.True);
            Assert.That(secondGrant, Is.False);
            Assert.That(_service.Data.walletCoins, Is.EqualTo(0));
            Assert.That(_service.Data.lifetimeCoinsEarned, Is.EqualTo(0));
            Assert.That(_service.Data.grantedMissionRewardIds, Is.EqualTo(new[] { "boot_finish_tutorial" }));
        }

        [Test]
        public void ResetPlayerProgression_ResetsMissionState()
        {
            _service.Data.activeMissionId = "memory_select_10_gates";
            _service.Data.completedMissionIds.Add("boot_finish_tutorial");
            _service.Data.grantedMissionRewardIds.Add("boot_finish_tutorial");
            _service.Data.lifetimeGatesSelected = 10;
            _service.Data.lifetimeMajorGatesSelected = 2;
            _service.Data.missionNotificationUnread = false;
            _service.Data.finalChoiceResolved = true;

            _service.ResetPlayerProgression();

            Assert.That(_service.Data.activeMissionId, Is.EqualTo(SaveData.FirstMissionId));
            Assert.That(_service.Data.completedMissionIds, Is.Empty);
            Assert.That(_service.Data.grantedMissionRewardIds, Is.Empty);
            Assert.That(_service.Data.lifetimeGatesSelected, Is.EqualTo(0));
            Assert.That(_service.Data.lifetimeMajorGatesSelected, Is.EqualTo(0));
            Assert.That(_service.Data.missionNotificationUnread, Is.True);
            Assert.That(_service.Data.finalChoiceResolved, Is.False);
        }
    }
}
