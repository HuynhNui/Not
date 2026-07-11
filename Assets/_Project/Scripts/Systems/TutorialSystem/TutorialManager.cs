using _Project.Cutscenes;
using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;
using UnityEngine.UI;
using RuntimeUISystem = _Project.Scripts.Systems.UISystem.UISystem;
using UIScreen = _Project.Scripts.Systems.UISystem.UISystem.UIScreen;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public sealed class TutorialManager : MonoBehaviour
    {
        private enum TutorialFlow
        {
            None,
            UpdateOnboarding
        }

        [SerializeField] private UpdateOnboardingDirector updateOnboardingDirector;
        [SerializeField] private MainMenuSpotlightOverlayUI mainMenuSpotlightOverlay;

        private RuntimeUISystem _uiSystem;
        private StoryCutsceneRuntimeController _storyCutsceneRuntime;
        private TutorialFlow _currentFlow;

        public bool IsRunningUpdateOnboarding =>
            _currentFlow == TutorialFlow.UpdateOnboarding
            && updateOnboardingDirector != null
            && updateOnboardingDirector.IsRunning;

        public void Init(
            RuntimeUISystem uiSystem,
            StoryCutsceneRuntimeController storyCutsceneRuntime)
        {
            _uiSystem = uiSystem;
            _storyCutsceneRuntime = storyCutsceneRuntime;
            EnsureComponents();
            updateOnboardingDirector?.Init(
                this,
                _uiSystem,
                _storyCutsceneRuntime,
                mainMenuSpotlightOverlay);
        }

        public bool ShouldRunUpdateOnboardingAfterFirstDeath()
        {
            if (!SaveService.HasInstance)
            {
                return false;
            }

            SaveService saveService = SaveService.Instance;
            SaveData data = saveService.Data;
            return saveService.IsGameplayTutorialCompleted()
                && !saveService.IsUpdateOnboardingCompleted()
                && data.totalRunsCompleted >= 1;
        }

        public void StartUpdateOnboardingFromMainMenu()
        {
            if (_currentFlow != TutorialFlow.None
                || _uiSystem == null
                || _uiSystem.CurrentScreen != UIScreen.MainMenu
                || !ShouldRunUpdateOnboardingAfterFirstDeath())
            {
                return;
            }

            EnsureComponents();
            _currentFlow = TutorialFlow.UpdateOnboarding;
            updateOnboardingDirector.StartFromMainMenu();
        }

        public void CompleteUpdateOnboarding()
        {
            if (SaveService.HasInstance)
            {
                SaveService.Instance.MarkUpdateOnboardingCompleted();
            }

            mainMenuSpotlightOverlay?.Hide();
            _currentFlow = TutorialFlow.None;
        }

        public void StopUpdateOnboarding()
        {
            updateOnboardingDirector?.Stop();
            mainMenuSpotlightOverlay?.Hide();
            _currentFlow = TutorialFlow.None;
        }

        private void EnsureComponents()
        {
            if (updateOnboardingDirector == null)
            {
                updateOnboardingDirector = GetComponent<UpdateOnboardingDirector>();
            }

            if (updateOnboardingDirector == null)
            {
                updateOnboardingDirector = gameObject.AddComponent<UpdateOnboardingDirector>();
            }

            if (mainMenuSpotlightOverlay == null)
            {
                mainMenuSpotlightOverlay = FindAnyObjectByType<MainMenuSpotlightOverlayUI>(FindObjectsInactive.Include);
            }

            if (mainMenuSpotlightOverlay == null)
            {
                mainMenuSpotlightOverlay = CreateSpotlightOverlay();
            }
        }

        private MainMenuSpotlightOverlayUI CreateSpotlightOverlay()
        {
            RectTransform parent = ResolveOverlayParent();
            if (parent == null)
            {
                return null;
            }

            GameObject overlayObject = new GameObject("MainMenuSpotlightOverlay", typeof(RectTransform));
            overlayObject.transform.SetParent(parent, false);
            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            MainMenuSpotlightOverlayUI overlay = overlayObject.AddComponent<MainMenuSpotlightOverlayUI>();
            overlay.EnsureBuilt();
            overlay.Hide();
            return overlay;
        }

        private RectTransform ResolveOverlayParent()
        {
            RectTransform updateTarget = _uiSystem != null ? _uiSystem.MainMenuUpgradeButtonTarget : null;
            if (updateTarget != null)
            {
                Canvas canvas = updateTarget.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    return canvas.transform as RectTransform;
                }
            }

            Canvas fallbackCanvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            return fallbackCanvas != null ? fallbackCanvas.transform as RectTransform : null;
        }
    }
}
