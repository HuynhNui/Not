using System.IO;
using _Project.Cutscenes;
using _Project.Scripts.Systems.SaveSystem;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class TutorialSaveTests
    {
        private string _directoryPath;
        private SaveService _service;

        [SetUp]
        public void SetUp()
        {
            _directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-tutorial-save-test-{System.Guid.NewGuid():N}");
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
        public void NewSave_TutorialFlags_DefaultFalse()
        {
            Assert.That(_service.Data.gameplayTutorialCompleted, Is.False);
            Assert.That(_service.Data.upgradeTutorialCompleted, Is.False);
            Assert.That(_service.Data.tutorialFirstRunBonusGranted, Is.False);
            Assert.That(_service.Data.tutorialVersion, Is.EqualTo(0));
        }

        [Test]
        public void MarkGameplayTutorialCompleted_PersistsFlag()
        {
            _service.MarkGameplayTutorialCompleted();

            Assert.That(_service.IsGameplayTutorialCompleted(), Is.True);
            Assert.That(_service.Data.tutorialVersion, Is.EqualTo(1));
        }

        [Test]
        public void MarkUpgradeTutorialCompleted_PersistsFlag()
        {
            _service.MarkUpgradeTutorialCompleted();

            Assert.That(_service.IsUpgradeTutorialCompleted(), Is.True);
            Assert.That(_service.Data.tutorialVersion, Is.EqualTo(1));
        }

        [Test]
        public void GrantTutorialFirstRunBonus_OnlyOnce()
        {
            bool firstGrant = _service.GrantTutorialFirstRunBonusIfNeeded(25);
            bool secondGrant = _service.GrantTutorialFirstRunBonusIfNeeded(25);

            Assert.That(firstGrant, Is.True);
            Assert.That(secondGrant, Is.False);
            Assert.That(_service.Data.walletCoins, Is.EqualTo(25));
            Assert.That(_service.HasGrantedTutorialFirstRunBonus(), Is.True);
        }

        [Test]
        public void ResetPlayerProgression_ResetsTutorialFlags()
        {
            _service.MarkGameplayTutorialCompleted();
            _service.MarkUpgradeTutorialCompleted();
            _service.GrantTutorialFirstRunBonusIfNeeded(25);

            _service.ResetPlayerProgression();

            Assert.That(_service.Data.gameplayTutorialCompleted, Is.False);
            Assert.That(_service.Data.upgradeTutorialCompleted, Is.False);
            Assert.That(_service.Data.tutorialFirstRunBonusGranted, Is.False);
            Assert.That(_service.Data.tutorialVersion, Is.EqualTo(0));
        }

        [Test]
        public void ResetPlayerProgression_ReopensFirstCutsceneAndGameplayTutorial()
        {
            _service.RecordCutsceneSeen(StoryCutsceneIds.BootSequence);
            _service.MarkGameplayTutorialCompleted();
            _service.RecordRunResult(10f, 1, 0, 10);

            _service.ResetPlayerProgression();

            Assert.That(_service.HasSeenCutscene(StoryCutsceneIds.BootSequence), Is.False);
            Assert.That(_service.Data.seenCutsceneIds, Is.Empty);
            Assert.That(_service.Data.storyStage, Is.EqualTo(0));
            Assert.That(_service.Data.totalRunsCompleted, Is.EqualTo(0));
            Assert.That(_service.IsGameplayTutorialCompleted(), Is.False);
        }
    }
}
