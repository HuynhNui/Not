using _Project.Cutscenes;
using _Project.Scripts.Core.GameLoop;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;
using UnityEngine.UI;
using RuntimeEnemySpawnerSystem = _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimeGateSystem = _Project.Scripts.Systems.GateSystem.GateSystem;
using RuntimeUISystem = _Project.Scripts.Systems.UISystem.UISystem;
using UIScreen = _Project.Scripts.Systems.UISystem.UISystem.UIScreen;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public sealed class TutorialManager : MonoBehaviour
    {
        private enum TutorialFlow
        {
            None,
            Gameplay,
            UpdateOnboarding
        }

        [SerializeField] private TutorialConfig config;
        [SerializeField] private TutorialOverlayUI overlayUI;
        [SerializeField] private TutorialGameplayDirector gameplayDirector;
        [SerializeField] private UpdateOnboardingDirector updateOnboardingDirector;
        [SerializeField] private MainMenuSpotlightOverlayUI mainMenuSpotlightOverlay;
        [SerializeField] private float startButtonSpotlightOpacity = 0.62f;
        [SerializeField] private Vector2 startButtonSpotlightPadding = new Vector2(18f, 14f);

        private GameManager _gameManager;
        private RuntimeUISystem _uiSystem;
        private StoryCutsceneRuntimeController _storyCutsceneRuntime;
        private TutorialFlow _currentFlow;

        public TutorialConfig Config => config;
        public TutorialOverlayUI OverlayUI => overlayUI;

        public bool IsRunningGameplayTutorial =>
            _currentFlow == TutorialFlow.Gameplay
            && gameplayDirector != null
            && gameplayDirector.IsRunning;

        public bool IsRunningUpdateOnboarding =>
            _currentFlow == TutorialFlow.UpdateOnboarding
            && updateOnboardingDirector != null
            && updateOnboardingDirector.IsRunning;

        private void Awake()
        {
            EnsureComponents();
        }

        private void OnDestroy()
        {
            if (overlayUI != null)
            {
                overlayUI.SkipClicked -= SkipCurrentTutorial;
            }
        }

        public void Init(
            GameManager gameManager,
            RuntimeUISystem uiSystem,
            StoryCutsceneRuntimeController storyCutsceneRuntime,
            PlayerController playerController,
            MainPlayerUnit mainPlayerUnit,
            RuntimeEnemySpawnerSystem enemySpawnerSystem,
            RuntimeGateSystem gateSystem,
            RunStatsTracker runStatsTracker)
        {
            _gameManager = gameManager;
            _uiSystem = uiSystem;
            _storyCutsceneRuntime = storyCutsceneRuntime;
            EnsureComponents();

            gameplayDirector?.Init(
                this,
                overlayUI,
                _storyCutsceneRuntime,
                playerController,
                mainPlayerUnit,
                enemySpawnerSystem,
                gateSystem,
                runStatsTracker);

            updateOnboardingDirector?.Init(
                this,
                _uiSystem,
                _storyCutsceneRuntime,
                mainMenuSpotlightOverlay);
        }

        public bool ShouldRunGameplayTutorial()
        {
            return SaveService.HasInstance
                && !SaveService.Instance.IsGameplayTutorialCompleted();
        }

        public bool ShouldRunUpgradeTutorial()
        {
            return SaveService.HasInstance
                && SaveService.Instance.IsGameplayTutorialCompleted()
                && !SaveService.Instance.IsUpdateOnboardingCompleted();
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

        public void StartGameplayTutorial()
        {
            if (_currentFlow != TutorialFlow.None || !ShouldRunGameplayTutorial())
            {
                return;
            }

            StopUpdateOnboarding();
            EnsureComponents();
            mainMenuSpotlightOverlay?.Hide();
            _currentFlow = TutorialFlow.Gameplay;
            _gameManager?.PrepareRunForTutorial();
            gameplayDirector?.StartTutorial();
        }

        public void ShowStartRunSpotlightIfNeeded()
        {
            if (_currentFlow != TutorialFlow.None
                || _uiSystem == null
                || _uiSystem.CurrentScreen != UIScreen.MainMenu
                || !ShouldRunGameplayTutorial())
            {
                return;
            }

            EnsureComponents();
            RectTransform startTarget = _uiSystem.MainMenuPlayButtonTarget;
            if (startTarget == null)
            {
                return;
            }

            mainMenuSpotlightOverlay?.Show(
                startTarget,
                startButtonSpotlightOpacity,
                startButtonSpotlightPadding);
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
            updateOnboardingDirector?.StartFromMainMenu();
        }

        public void SkipCurrentTutorial()
        {
            if (_currentFlow == TutorialFlow.Gameplay)
            {
                gameplayDirector?.SkipTutorial();
                return;
            }

            if (_currentFlow == TutorialFlow.UpdateOnboarding)
            {
                CompleteUpdateOnboarding();
            }
        }

        public void CompleteGameplayTutorial(bool startNormalRun)
        {
            if (SaveService.HasInstance)
            {
                SaveService.Instance.MarkGameplayTutorialCompleted();
            }

            _gameManager?.NotifyGameplayTutorialCompleted();
            overlayUI?.HideOverlay();
            _currentFlow = TutorialFlow.None;

            if (startNormalRun)
            {
                _gameManager?.StartNormalRunFromTutorial();
            }
        }

        public void CompleteUpdateOnboarding()
        {
            if (SaveService.HasInstance)
            {
                SaveService.Instance.MarkUpdateOnboardingCompleted();
            }

            updateOnboardingDirector?.Stop();
            mainMenuSpotlightOverlay?.Hide();
            _currentFlow = TutorialFlow.None;
        }

        public void StopUpdateOnboarding()
        {
            updateOnboardingDirector?.Stop();
            mainMenuSpotlightOverlay?.Hide();
            if (_currentFlow == TutorialFlow.UpdateOnboarding)
            {
                _currentFlow = TutorialFlow.None;
            }
        }

        private void EnsureComponents()
        {
            if (overlayUI == null)
            {
                overlayUI = GetComponentInChildren<TutorialOverlayUI>(true);
            }

            if (gameplayDirector == null)
            {
                gameplayDirector = GetComponent<TutorialGameplayDirector>();
            }

            if (gameplayDirector == null)
            {
                gameplayDirector = gameObject.AddComponent<TutorialGameplayDirector>();
            }

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

            if (overlayUI != null)
            {
                overlayUI.SkipClicked -= SkipCurrentTutorial;
                overlayUI.SkipClicked += SkipCurrentTutorial;
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
