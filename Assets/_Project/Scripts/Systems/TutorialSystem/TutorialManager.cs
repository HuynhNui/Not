using _Project.Scripts.Core.GameLoop;
using _Project.Cutscenes;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.EnemySpawnerSystem;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.UISystem;
using UnityEngine;
using RuntimeEnemySpawnerSystem = _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimeGateSystem = _Project.Scripts.Systems.GateSystem.GateSystem;
using RuntimeUISystem = _Project.Scripts.Systems.UISystem.UISystem;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public sealed class TutorialManager : MonoBehaviour
    {
        [SerializeField] private TutorialConfig config;
        [SerializeField] private TutorialOverlayUI overlayUI;
        [SerializeField] private TutorialGameplayDirector gameplayDirector;
        [SerializeField] private TutorialUpgradeDirector upgradeDirector;

        private GameManager _gameManager;
        private RuntimeUISystem _uiSystem;
        private TutorialFlow _currentFlow;

        public TutorialConfig Config => config;
        public TutorialOverlayUI OverlayUI => overlayUI;

        private void Awake()
        {
            ResolveReferences();
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
            ResolveReferences();

            gameplayDirector.Init(
                this,
                overlayUI,
                storyCutsceneRuntime,
                playerController,
                mainPlayerUnit,
                enemySpawnerSystem,
                gateSystem,
                runStatsTracker);
            upgradeDirector.Init(this, overlayUI, uiSystem);
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
                && !SaveService.Instance.IsUpgradeTutorialCompleted();
        }

        public void StartGameplayTutorial()
        {
            if (_currentFlow != TutorialFlow.None || !ShouldRunGameplayTutorial())
            {
                return;
            }

            _currentFlow = TutorialFlow.Gameplay;
            _gameManager?.PrepareRunForTutorial();
            gameplayDirector.StartTutorial();
        }

        public void StartUpgradeTutorialIfNeeded()
        {
            if (_currentFlow != TutorialFlow.None || !ShouldRunUpgradeTutorial())
            {
                return;
            }

            _currentFlow = TutorialFlow.Upgrade;
            upgradeDirector.StartTutorial();
        }

        public void SkipCurrentTutorial()
        {
            if (_currentFlow == TutorialFlow.Gameplay)
            {
                gameplayDirector.SkipTutorial();
                return;
            }

            if (_currentFlow == TutorialFlow.Upgrade)
            {
                upgradeDirector.SkipTutorial();
            }
        }

        public void CompleteGameplayTutorial(bool startNormalRun)
        {
            SaveService.Instance.MarkGameplayTutorialCompleted();
            overlayUI?.HideOverlay();
            _currentFlow = TutorialFlow.None;

            if (startNormalRun)
            {
                _gameManager?.StartNormalRunFromTutorial();
            }
        }

        public void CompleteUpgradeTutorial()
        {
            SaveService.Instance.MarkUpgradeTutorialCompleted();
            overlayUI?.HideOverlay();
            _currentFlow = TutorialFlow.None;
        }

        private void ResolveReferences()
        {
            overlayUI ??= GetComponentInChildren<TutorialOverlayUI>(true);
            gameplayDirector ??= GetComponent<TutorialGameplayDirector>();
            if (gameplayDirector == null)
            {
                gameplayDirector = gameObject.AddComponent<TutorialGameplayDirector>();
            }

            upgradeDirector ??= GetComponent<TutorialUpgradeDirector>();
            if (upgradeDirector == null)
            {
                upgradeDirector = gameObject.AddComponent<TutorialUpgradeDirector>();
            }

            if (overlayUI != null)
            {
                overlayUI.SkipClicked -= SkipCurrentTutorial;
                overlayUI.SkipClicked += SkipCurrentTutorial;
            }
        }

        private enum TutorialFlow
        {
            None,
            Gameplay,
            Upgrade
        }
    }
}
