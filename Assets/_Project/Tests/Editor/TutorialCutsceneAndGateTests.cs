using System.IO;
using _Project.Cutscenes;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.TutorialSystem;
using NUnit.Framework;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class TutorialCutsceneAndGateTests
    {
        private string _directoryPath;

        [SetUp]
        public void SetUp()
        {
            _directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"true-gate-tutorial-cutscene-test-{System.Guid.NewGuid():N}");
            SaveService.SetInstanceForTests(SaveService.CreateForTests(_directoryPath));
            SaveService.Instance.EnsureLoaded();
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
        public void TransientTutorialCutscene_DoesNotMarkStorySeen()
        {
            var controllerObject = new GameObject("TransientCutsceneController");
            StoryCutsceneRuntimeController controller =
                controllerObject.AddComponent<StoryCutsceneRuntimeController>();

            try
            {
                bool callbackInvoked = false;
                StoryCutsceneDefinition definition = TutorialCutsceneDefinitions.MovementIntro;

                bool started = controller.TryPlayTransientCutscene(
                    definition,
                    () => callbackInvoked = true);
                CutsceneDemoUIView view = controller.GetComponentInChildren<CutsceneDemoUIView>(true);

                Assert.That(started, Is.True);
                Assert.That(view, Is.Not.Null);

                view.NextButton.onClick.Invoke();

                Assert.That(callbackInvoked, Is.True);
                Assert.That(SaveService.Instance.Data.HasSeenCutscene(definition.CutsceneId), Is.False);
                Assert.That(SaveService.Instance.Data.storyStage, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
            }
        }

        [Test]
        public void TutorialRecruitGate_ResolvesRealMajorRecruitGate()
        {
            var gateSystemObject = new GameObject("TutorialGateSystem");
            GateSystem gateSystem = gateSystemObject.AddComponent<GateSystem>();

            try
            {
                bool found = gateSystem.TryResolveTutorialGateConfig("major_recruit", out var config);

                Assert.That(found, Is.True);
                Assert.That(config, Is.Not.Null);
                Assert.That(config.GateId, Is.EqualTo("major_recruit"));
                Assert.That(config.GetDisplayText(), Is.EqualTo("RECRUIT +1"));
            }
            finally
            {
                Object.DestroyImmediate(gateSystemObject);
            }
        }

        [Test]
        public void TutorialDefaultGateSet_ResolvesThreeRealGateConfigs()
        {
            var gateSystemObject = new GameObject("TutorialDefaultGateSystem");
            GateSystem gateSystem = gateSystemObject.AddComponent<GateSystem>();

            try
            {
                var configs = gateSystem.GetTutorialDefaultGateConfigs();

                Assert.That(configs, Has.Count.EqualTo(3));
                Assert.That(configs[0].GateId, Is.EqualTo("stable_damage"));
                Assert.That(configs[1].GateId, Is.EqualTo("utility_repair"));
                Assert.That(configs[2].GateId, Is.EqualTo("risky_glass_cannon"));
            }
            finally
            {
                Object.DestroyImmediate(gateSystemObject);
            }
        }
    }
}
