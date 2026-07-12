using System.IO;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.TutorialSystem;
using NUnit.Framework;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class TutorialFlowDecisionTests
    {
        private string _directoryPath;
        private SaveService _service;
        private GameObject _managerObject;
        private TutorialManager _manager;

        [SetUp]
        public void SetUp()
        {
            _directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-tutorial-flow-test-{System.Guid.NewGuid():N}");
            _service = SaveService.CreateForTests(_directoryPath);
            SaveService.SetInstanceForTests(_service);
            _service.EnsureLoaded();
            _managerObject = new GameObject("TutorialManagerTest");
            _manager = _managerObject.AddComponent<TutorialManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_managerObject);
            SaveService.SetInstanceForTests(null);

            if (Directory.Exists(_directoryPath))
            {
                Directory.Delete(_directoryPath, recursive: true);
            }
        }

        [Test]
        public void ShouldRunGameplayTutorial_WhenGameplayFlagFalse()
        {
            Assert.That(_manager.ShouldRunGameplayTutorial(), Is.True);
        }

        [Test]
        public void ShouldNotRunGameplayTutorial_WhenGameplayFlagTrue()
        {
            _service.MarkGameplayTutorialCompleted();

            Assert.That(_manager.ShouldRunGameplayTutorial(), Is.False);
        }

        [Test]
        public void ShouldRunUpgradeTutorial_WhenGameplayCompleteAndUpgradeFalse()
        {
            _service.MarkGameplayTutorialCompleted();

            Assert.That(_manager.ShouldRunUpgradeTutorial(), Is.True);
        }

        [Test]
        public void ShouldNotRunUpgradeTutorial_BeforeGameplayTutorialComplete()
        {
            Assert.That(_manager.ShouldRunUpgradeTutorial(), Is.False);
        }

        [Test]
        public void ShouldNotRunUpgradeTutorial_WhenUpgradeComplete()
        {
            _service.MarkGameplayTutorialCompleted();
            _service.MarkUpgradeTutorialCompleted();

            Assert.That(_manager.ShouldRunUpgradeTutorial(), Is.False);
        }
    }
}
