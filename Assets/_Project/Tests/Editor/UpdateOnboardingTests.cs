using System.IO;
using _Project.Cutscenes;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.TutorialSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Tests.Editor
{
    public sealed class UpdateOnboardingTests
    {
        [Test]
        public void UpdateOnboardingSaveAliases_UseUpgradeTutorialCompatibilityFlag()
        {
            string directoryPath = CreateTempDirectoryPath("true-gate-update-onboarding-save");
            SaveService service = SaveService.CreateForTests(directoryPath);
            SaveService.SetInstanceForTests(service);

            try
            {
                service.EnsureLoaded();

                Assert.That(service.IsGameplayTutorialCompleted(), Is.False);
                Assert.That(service.IsUpgradeTutorialCompleted(), Is.False);
                Assert.That(service.IsUpdateOnboardingCompleted(), Is.False);

                service.MarkUpdateOnboardingCompleted();

                Assert.That(service.IsUpgradeTutorialCompleted(), Is.True);
                Assert.That(service.IsUpdateOnboardingCompleted(), Is.True);

                service.ResetPlayerProgression();

                Assert.That(service.IsGameplayTutorialCompleted(), Is.False);
                Assert.That(service.IsUpgradeTutorialCompleted(), Is.False);
                Assert.That(service.IsUpdateOnboardingCompleted(), Is.False);
            }
            finally
            {
                CleanupSaveService(directoryPath);
            }
        }

        [Test]
        public void TutorialManager_UpdateOnboardingRequiresGameplayTutorialAndCompletedRun()
        {
            string directoryPath = CreateTempDirectoryPath("true-gate-update-onboarding-decision");
            SaveService service = SaveService.CreateForTests(directoryPath);
            SaveService.SetInstanceForTests(service);
            GameObject managerObject = new GameObject("TutorialManagerTest");
            TutorialManager manager = managerObject.AddComponent<TutorialManager>();

            try
            {
                service.EnsureLoaded();

                Assert.That(manager.ShouldRunUpdateOnboardingAfterFirstDeath(), Is.False);

                service.RecordRunResult(1f, 0, 0, 0);

                Assert.That(manager.ShouldRunUpdateOnboardingAfterFirstDeath(), Is.False);

                service.MarkGameplayTutorialCompleted();

                Assert.That(manager.ShouldRunUpdateOnboardingAfterFirstDeath(), Is.True);

                service.MarkUpdateOnboardingCompleted();

                Assert.That(manager.ShouldRunUpdateOnboardingAfterFirstDeath(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                CleanupSaveService(directoryPath);
            }
        }

        [Test]
        public void TransientCutscenePlayback_DoesNotRecordSeenCutscene()
        {
            string directoryPath = CreateTempDirectoryPath("true-gate-transient-cutscene");
            SaveService service = SaveService.CreateForTests(directoryPath);
            SaveService.SetInstanceForTests(service);
            GameObject runtimeObject = new GameObject("StoryCutsceneRuntimeTest");
            StoryCutsceneRuntimeController runtime =
                runtimeObject.AddComponent<StoryCutsceneRuntimeController>();
            const string cutsceneId = "tutorial_update_onboarding_test";
            bool completed = false;

            try
            {
                service.EnsureLoaded();
                var definition = new StoryCutsceneDefinition(
                    cutsceneId,
                    new[]
                    {
                        new StoryDialogueLine("SYSTEM", "cold", "Open UPDATE.")
                    });

                Assert.That(
                    runtime.TryPlayTransientCutscene(
                        definition,
                        () => completed = true,
                        StoryCutscenePresentationMode.DialogueOnlyOverlay),
                    Is.True);

                Button closeButton = FindButton(runtimeObject, "CloseButton");
                Assert.That(closeButton, Is.Not.Null);

                closeButton.onClick.Invoke();

                Assert.That(completed, Is.True);
                Assert.That(runtime.IsPlaying, Is.False);
                Assert.That(service.Data.HasSeenCutscene(cutsceneId), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(runtimeObject);
                CleanupSaveService(directoryPath);
            }
        }

        private static Button FindButton(GameObject root, string buttonName)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name == buttonName)
                {
                    return button;
                }
            }

            return null;
        }

        private static string CreateTempDirectoryPath(string prefix)
        {
            return Path.Combine(Path.GetTempPath(), $"{prefix}-{System.Guid.NewGuid():N}");
        }

        private static void CleanupSaveService(string directoryPath)
        {
            SaveService.SetInstanceForTests(null);
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }
}
