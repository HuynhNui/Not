using System.IO;
using System.Reflection;
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
        public void UpdateOnboardingDefinition_IsIncludedInSharedTutorialSource()
        {
            StoryCutsceneDefinition definition = TutorialCutsceneDefinitions.UpdateOnboarding;

            Assert.That(definition.CutsceneId, Is.EqualTo("TUTORIAL_UPDATE_ONBOARDING"));
            Assert.That(definition.Lines.Count, Is.EqualTo(4));
            Assert.That(definition.Lines[0].Text, Is.EqualTo("Combat shell destroyed."));
            Assert.That(definition.Lines[3].Text, Is.EqualTo("Open UPDATE."));
            Assert.That(TutorialCutsceneDefinitions.GetAll(), Does.Contain(definition));
        }

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

        [Test]
        public void Spotlight_TargetHole_HasNoEnabledGraphicOverlay()
        {
            MainMenuSpotlightOverlayUI overlay = CreateSpotlightFixture(out GameObject rootObject, out RectTransform target);

            try
            {
                overlay.EnsureBuilt();
                overlay.Show(target);

                Transform focusFrame = rootObject.transform.Find("FocusHighlightFrame");
                if (focusFrame == null)
                {
                    return;
                }

                Image image = focusFrame.GetComponent<Image>();
                Assert.That(
                    !focusFrame.gameObject.activeSelf || image == null || !image.enabled || image.color.a <= 0f,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Spotlight_SurroundingDimPanels_RemainEnabled()
        {
            MainMenuSpotlightOverlayUI overlay = CreateSpotlightFixture(out GameObject rootObject, out RectTransform target);

            try
            {
                overlay.EnsureBuilt();
                overlay.Show(target);

                Assert.That(GetPanel(rootObject, "TopDimPanel").enabled, Is.True);
                Assert.That(GetPanel(rootObject, "BottomDimPanel").enabled, Is.True);
                Assert.That(GetPanel(rootObject, "LeftDimPanel").enabled, Is.True);
                Assert.That(GetPanel(rootObject, "RightDimPanel").enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Spotlight_DimPanels_AreRaycastTargets()
        {
            MainMenuSpotlightOverlayUI overlay = CreateSpotlightFixture(out GameObject rootObject, out RectTransform target);

            try
            {
                overlay.EnsureBuilt();
                overlay.Show(target);

                Assert.That(GetPanel(rootObject, "TopDimPanel").raycastTarget, Is.True);
                Assert.That(GetPanel(rootObject, "BottomDimPanel").raycastTarget, Is.True);
                Assert.That(GetPanel(rootObject, "LeftDimPanel").raycastTarget, Is.True);
                Assert.That(GetPanel(rootObject, "RightDimPanel").raycastTarget, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Spotlight_TargetHole_HasNoRaycastBlockingGraphic()
        {
            MainMenuSpotlightOverlayUI overlay = CreateSpotlightFixture(out GameObject rootObject, out RectTransform target);
            Image targetImage = target.GetComponent<Image>();
            Image legacyHighlight = CreateLegacyFocusHighlight(rootObject.transform);

            try
            {
                overlay.EnsureBuilt();
                overlay.Show(target);

                Assert.That(targetImage.enabled, Is.True);
                Assert.That(targetImage.raycastTarget, Is.True);
                Assert.That(legacyHighlight.enabled, Is.False);
                Assert.That(legacyHighlight.raycastTarget, Is.False);
                Assert.That(legacyHighlight.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Spotlight_Hide_DisablesOverlay()
        {
            MainMenuSpotlightOverlayUI overlay = CreateSpotlightFixture(out GameObject rootObject, out RectTransform target);
            Image legacyHighlight = CreateLegacyFocusHighlight(rootObject.transform);

            try
            {
                overlay.Show(target);
                overlay.Hide();

                Assert.That(rootObject.activeSelf, Is.False);
                Assert.That(legacyHighlight.gameObject.activeSelf, Is.False);
                Assert.That(legacyHighlight.enabled, Is.False);
                Assert.That(GetSpotlightTarget(overlay), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
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

        private static MainMenuSpotlightOverlayUI CreateSpotlightFixture(
            out GameObject rootObject,
            out RectTransform target)
        {
            rootObject = new GameObject("MainMenuSpotlightOverlay", typeof(RectTransform));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(400f, 400f);
            root.pivot = new Vector2(0.5f, 0.5f);

            GameObject targetObject = new GameObject("UpdateButton", typeof(RectTransform), typeof(Image), typeof(Button));
            targetObject.transform.SetParent(rootObject.transform, false);
            target = targetObject.GetComponent<RectTransform>();
            target.sizeDelta = new Vector2(80f, 40f);
            target.anchoredPosition = Vector2.zero;

            Image targetImage = targetObject.GetComponent<Image>();
            targetImage.raycastTarget = true;

            return rootObject.AddComponent<MainMenuSpotlightOverlayUI>();
        }

        private static Image CreateLegacyFocusHighlight(Transform root)
        {
            GameObject legacyObject = new GameObject("FocusHighlightFrame", typeof(RectTransform), typeof(Image), typeof(Outline));
            legacyObject.transform.SetParent(root, false);
            Image image = legacyObject.GetComponent<Image>();
            image.color = new Color(1f, 0.82f, 0.08f, 0.95f);
            image.raycastTarget = true;
            image.enabled = true;
            legacyObject.GetComponent<Outline>().enabled = true;
            return image;
        }

        private static Image GetPanel(GameObject rootObject, string name)
        {
            Transform panel = rootObject.transform.Find(name);
            Assert.That(panel, Is.Not.Null, $"{name} should exist.");
            Image image = panel.GetComponent<Image>();
            Assert.That(image, Is.Not.Null, $"{name} should have an Image.");
            return image;
        }

        private static RectTransform GetSpotlightTarget(MainMenuSpotlightOverlayUI overlay)
        {
            FieldInfo targetField = typeof(MainMenuSpotlightOverlayUI)
                .GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
            return targetField?.GetValue(overlay) as RectTransform;
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
