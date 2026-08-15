using System;
using _Project.Cutscenes;
using _Project.Scripts.Core.StateMachine;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Dialogue;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.CombatSystem;
using _Project.Scripts.Systems.EnemySpawnerSystem;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.LevelSystem;
using _Project.Scripts.Systems.MissionSystem;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.AudioSystem;
using _Project.Scripts.Systems.Telemetry;
using _Project.Scripts.Systems.TutorialSystem;
using _Project.Scripts.Systems.UISystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using RuntimeMissionSystem = _Project.Scripts.Systems.MissionSystem.MissionSystem;

namespace _Project.Scripts.Core.GameLoop
{
    /// <summary>
    /// Orchestrates the main run flow and connects high-level gameplay systems.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameStateMachine gameStateMachine;
        [SerializeField] private CombatSystem combatSystem;
        [SerializeField] private EnemySpawnerSystem enemySpawnerSystem;
        [SerializeField] private GateSystem gateSystem;
        [SerializeField] private UISystem uiSystem;
        [SerializeField] private LevelSystem levelSystem;
        [SerializeField] private RunStatsTracker runStatsTracker;
        [SerializeField] private BalanceBootstrapConfig balanceConfig;
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private BalanceTelemetryConfig telemetryConfig;
        [SerializeField] private BalanceBenchmarkProfile benchmarkProfile;
        [SerializeField] private BalanceTelemetryService telemetryService;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MainPlayerUnit mainPlayerUnit;
        [SerializeField] private StoryCutsceneRuntimeController storyCutsceneRuntime;
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private MissionCatalog missionCatalog;
        [SerializeField] private GameplayDialogueController gameplayDialogueController;
        [SerializeField] private AudioEventRouter audioEventRouter;

        private bool _isGameOver;
        private bool _isRunActive;
        private bool _isBenchmarkRun;
        private bool _isPerformanceBenchmarkRun;
        private RuntimeMissionSystem _missionSystem;
        private static bool _startRunAfterReload;
        private static bool _showUpdateOnboardingAfterReload;

        public event Action RunBecamePlayable;
        public event Action RunEnded;
        public event Action ReturnedToMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionState()
        {
            _startRunAfterReload = false;
            _showUpdateOnboardingAfterReload = false;
        }

        public void Init()
        {
            SaveService.Instance.EnsureLoaded();
            _ = SaveService.Instance.LoadAsync();

            if (runStatsTracker == null)
            {
                runStatsTracker = FindAnyObjectByType<RunStatsTracker>();
            }

            if (runStatsTracker == null)
            {
                runStatsTracker = gameObject.AddComponent<RunStatsTracker>();
            }

            if (gameStateMachine == null)
            {
                gameStateMachine = FindAnyObjectByType<GameStateMachine>(FindObjectsInactive.Include);
            }

            if (gameStateMachine == null)
            {
                gameStateMachine = GetComponent<GameStateMachine>();
            }

            if (gameStateMachine == null)
            {
                gameStateMachine = gameObject.AddComponent<GameStateMachine>();
            }

            gameStateMachine.Init();

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (mainPlayerUnit == null)
            {
                mainPlayerUnit = FindAnyObjectByType<MainPlayerUnit>();
            }

            if (storyCutsceneRuntime == null)
            {
                storyCutsceneRuntime = FindAnyObjectByType<StoryCutsceneRuntimeController>(FindObjectsInactive.Include);
            }

            if (audioEventRouter == null)
            {
                audioEventRouter = FindAnyObjectByType<AudioEventRouter>(FindObjectsInactive.Include);
            }

            if (tutorialManager == null)
            {
                tutorialManager = FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
            }

            storyCutsceneRuntime?.Init();

            ApplyBalanceConfiguration();
            InitializeMissionSystem();

            if (runStatsTracker != null)
            {
                runStatsTracker.SetEconomyConfig(economyConfig);
                runStatsTracker.Init(enemySpawnerSystem);
            }

            if (telemetryService == null)
            {
                telemetryService = GetComponent<BalanceTelemetryService>();
            }

            if (telemetryService == null)
            {
                telemetryService = gameObject.AddComponent<BalanceTelemetryService>();
            }

            telemetryService.Configure(
                telemetryConfig,
                runStatsTracker,
                playerController,
                mainPlayerUnit,
                enemySpawnerSystem,
                gateSystem);

            if (uiSystem != null)
            {
                uiSystem.Init(runStatsTracker);
                uiSystem.PlayRequested -= RequestStartRun;
                uiSystem.PlayRequested += RequestStartRun;
                uiSystem.PauseRequested -= PauseRun;
                uiSystem.PauseRequested += PauseRun;
                uiSystem.ResumeRequested -= ResumeRun;
                uiSystem.ResumeRequested += ResumeRun;
                uiSystem.RestartRequested -= RestartCurrentScene;
                uiSystem.RestartRequested += RestartCurrentScene;
                uiSystem.HomeRequested -= ReturnHome;
                uiSystem.HomeRequested += ReturnHome;
            }

            gateSystem?.Init();
            if (gateSystem != null)
            {
                gateSystem.GateSelected -= HandleMissionGateSelected;
                gateSystem.GateSelected += HandleMissionGateSelected;
            }

            if (tutorialManager == null)
            {
                tutorialManager = gameObject.AddComponent<TutorialManager>();
            }

            tutorialManager.Init(
                this,
                uiSystem,
                storyCutsceneRuntime,
                playerController,
                mainPlayerUnit,
                enemySpawnerSystem,
                gateSystem,
                runStatsTracker);

            if (gameplayDialogueController == null)
            {
                gameplayDialogueController = GetComponentInChildren<GameplayDialogueController>(true);
            }

            if (gameplayDialogueController == null)
            {
                gameplayDialogueController = gameObject.AddComponent<GameplayDialogueController>();
            }

            gameplayDialogueController.Init(
                gameStateMachine,
                uiSystem,
                tutorialManager,
                playerController);
            gameplayDialogueController.EndRun();

            if (playerController != null)
            {
                playerController.SquadDefeated -= HandleSquadDefeated;
                playerController.SquadDefeated += HandleSquadDefeated;
            }

            SaveService.Instance.UpgradePurchased -= HandleMissionUpgradePurchased;
            SaveService.Instance.UpgradePurchased += HandleMissionUpgradePurchased;

            Time.timeScale = 1f;
            _isGameOver = false;
            _isRunActive = false;
            _isPerformanceBenchmarkRun = false;
            playerController?.SetControlsEnabled(false);
            if (playerController != null)
            {
                playerController.gameObject.SetActive(false);
            }

            enemySpawnerSystem?.SetSpawningEnabled(false);
            gateSystem?.SetSpawningEnabled(false);
            gameStateMachine?.SetState(GameState.MainMenu);
            uiSystem?.ShowMainMenu();

            bool shouldStartRunAfterReload = _startRunAfterReload;
            bool shouldShowUpdateOnboardingAfterReload = _showUpdateOnboardingAfterReload;
            _startRunAfterReload = false;
            _showUpdateOnboardingAfterReload = false;

            if (TryPlayInitialStoryCutscene(shouldStartRunAfterReload, shouldShowUpdateOnboardingAfterReload))
            {
                return;
            }

            if (shouldShowUpdateOnboardingAfterReload)
            {
                gameStateMachine?.SetState(GameState.MainMenu);
                uiSystem?.ShowMainMenu();
                tutorialManager?.StartUpdateOnboardingFromMainMenu();
                return;
            }

            if (shouldStartRunAfterReload)
            {
                RequestStartRun();
                return;
            }

            tutorialManager?.ShowStartRunSpotlightIfNeeded();
        }

        private void ApplyBalanceConfiguration()
        {
            if (balanceConfig == null)
            {
                PlayerMetaUpgradeService.Configure(null, null);
                return;
            }

            balanceConfig.ValidateValues();

            CombatScalingConfig combatScalingConfig = balanceConfig.CombatScalingConfig;
            PlayerMetaUpgradeService.Configure(
                balanceConfig.PlayerMetaBalanceConfig,
                balanceConfig.PlayerMetaEconomyConfig,
                combatScalingConfig);

            if (combatScalingConfig != null)
            {
                playerController?.SetCombatScalingConfig(combatScalingConfig);
            }

            enemySpawnerSystem?.SetBalanceConfiguration(
                balanceConfig.RunPressureConfig,
                balanceConfig.EnemyRoleConfigs);
            gateSystem?.SetGatePoolConfig(balanceConfig.GatePoolConfig);
            gateSystem?.SetGateScalingProfile(balanceConfig.GateScalingProfile);

            economyConfig = balanceConfig.EconomyConfig != null
                ? balanceConfig.EconomyConfig
                : economyConfig;
            telemetryConfig = balanceConfig.TelemetryConfig != null
                ? balanceConfig.TelemetryConfig
                : telemetryConfig;
            benchmarkProfile = balanceConfig.ActiveBenchmarkProfile;
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            if (uiSystem != null)
            {
                uiSystem.PlayRequested -= RequestStartRun;
                uiSystem.PauseRequested -= PauseRun;
                uiSystem.ResumeRequested -= ResumeRun;
                uiSystem.RestartRequested -= RestartCurrentScene;
                uiSystem.HomeRequested -= ReturnHome;
            }

            if (playerController != null)
            {
                playerController.SquadDefeated -= HandleSquadDefeated;
            }

            if (gateSystem != null)
            {
                gateSystem.GateSelected -= HandleMissionGateSelected;
            }

            if (SaveService.HasInstance)
            {
                SaveService.Instance.UpgradePurchased -= HandleMissionUpgradePurchased;
            }

            if (_missionSystem != null)
            {
                _missionSystem.MissionCompleted -= HandleMissionCompleted;
            }

            _missionSystem?.Dispose();
            _missionSystem = null;
            gameplayDialogueController?.EndRun();
        }

        private void RequestStartRun()
        {
            if (tutorialManager != null
                && tutorialManager.ShouldRunGameplayTutorial())
            {
                tutorialManager.StartGameplayTutorial();
                return;
            }

            StartRun();
        }

        public void PrepareRunForTutorial()
        {
            gameplayDialogueController?.EndRun();
            Time.timeScale = 1f;
            _isGameOver = false;
            _isRunActive = true;
            _isBenchmarkRun = false;
            _isPerformanceBenchmarkRun = false;
            _missionSystem?.SetProgressionSuppressed(false);
            runStatsTracker?.SetPersistenceSuppressed(false);

            if (playerController != null && !playerController.gameObject.activeSelf)
            {
                playerController.gameObject.SetActive(true);
            }

            if (mainPlayerUnit != null)
            {
                mainPlayerUnit.Initialize();
                PlayerMetaUpgradeService.ApplyToPlayer(mainPlayerUnit, playerController);
            }

            playerController?.ResetRunPosition();
            telemetryService?.SetRunContext(
                "tutorial",
                string.Empty,
                PlayerMetaUpgradeService.BuildCurrentRunStartStats());
            runStatsTracker?.BeginRun();
            playerController?.SetControlsEnabled(true);
            enemySpawnerSystem?.BeginRun();
            enemySpawnerSystem?.SetSpawningEnabled(false);
            gateSystem?.SetBenchmarkMode(false);
            gateSystem?.BeginRun();
            gateSystem?.SetSpawningEnabled(false);
            telemetryService?.BeginRun();
            gameStateMachine?.SetState(GameState.Playing);
            uiSystem?.ShowGameplayHud();
            RunBecamePlayable?.Invoke();
        }

        public void StartNormalRunFromTutorial()
        {
            Time.timeScale = 1f;
            _isGameOver = false;
            _isRunActive = true;
            _isBenchmarkRun = IsBenchmarkProfileActive();
            _isPerformanceBenchmarkRun = false;
            _missionSystem?.SetProgressionSuppressed(_isBenchmarkRun);

            if (playerController != null && !playerController.gameObject.activeSelf)
            {
                playerController.gameObject.SetActive(true);
            }

            playerController?.SetControlsEnabled(true);
            enemySpawnerSystem?.SetSpawningEnabled(true);
            gateSystem?.SetBenchmarkMode(_isBenchmarkRun);
            gateSystem?.SetSpawningEnabled(true);
            gameStateMachine?.SetState(GameState.Playing);
            uiSystem?.ShowGameplayHud();
            gameplayDialogueController?.BeginNormalRun();
        }

        private void StartRun()
        {
            bool isBenchmarkRun = IsBenchmarkProfileActive();
            PlayerRunStartStats startStats = isBenchmarkRun
                ? benchmarkProfile.ToRunStartStats()
                : PlayerMetaUpgradeService.BuildCurrentRunStartStats();
            string benchmarkProfileId = isBenchmarkRun
                ? benchmarkProfile.ProfileId
                : string.Empty;
            bool suppressPersistence = isBenchmarkRun
                && (benchmarkProfile.SuppressSaveCommit || benchmarkProfile.SuppressWalletReward);

            StartRunInternal(
                isBenchmarkRun: isBenchmarkRun,
                isPerformanceBenchmarkRun: false,
                startStats: startStats,
                benchmarkProfileId: benchmarkProfileId,
                suppressPersistence: suppressPersistence);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool TryStartPerformanceBenchmarkRun(
            PlayerRunStartStats startStats,
            string benchmarkProfileId)
        {
            if (_isRunActive || _isGameOver)
            {
                return false;
            }

            StartRunInternal(
                isBenchmarkRun: true,
                isPerformanceBenchmarkRun: true,
                startStats: startStats,
                benchmarkProfileId: string.IsNullOrWhiteSpace(benchmarkProfileId)
                    ? "performance-baseline"
                    : benchmarkProfileId.Trim(),
                suppressPersistence: true);
            return true;
        }
#endif

        private void StartRunInternal(
            bool isBenchmarkRun,
            bool isPerformanceBenchmarkRun,
            PlayerRunStartStats startStats,
            string benchmarkProfileId,
            bool suppressPersistence)
        {
            Time.timeScale = 1f;
            _isGameOver = false;
            _isRunActive = true;
            _isBenchmarkRun = isBenchmarkRun;
            _isPerformanceBenchmarkRun = isPerformanceBenchmarkRun;
            _missionSystem?.SetProgressionSuppressed(_isBenchmarkRun);

            if (playerController != null && !playerController.gameObject.activeSelf)
            {
                playerController.gameObject.SetActive(true);
            }

            if (mainPlayerUnit != null)
            {
                mainPlayerUnit.Initialize();
                PlayerMetaUpgradeService.ApplyStatsToPlayer(startStats, mainPlayerUnit, playerController);
                telemetryService?.SetRunContext(
                    _isBenchmarkRun ? "benchmark" : "standard",
                    _isBenchmarkRun ? benchmarkProfileId : string.Empty,
                    startStats);
            }

            playerController?.ResetRunPosition();
            runStatsTracker?.SetPersistenceSuppressed(suppressPersistence);
            runStatsTracker?.BeginRun();
            playerController?.SetControlsEnabled(true);
            enemySpawnerSystem?.BeginRun();
            enemySpawnerSystem?.SetSpawningEnabled(true);
            gateSystem?.SetBenchmarkMode(_isBenchmarkRun);
            gateSystem?.BeginRun();
            telemetryService?.BeginRun();
            gameStateMachine?.SetState(GameState.Playing);
            uiSystem?.ShowGameplayHud();
            gameplayDialogueController?.BeginNormalRun();
            RunBecamePlayable?.Invoke();
        }

        private void PauseRun()
        {
            if (!_isRunActive || _isGameOver)
            {
                return;
            }

            playerController?.SetControlsEnabled(false);
            enemySpawnerSystem?.SetSpawningEnabled(false);
            gateSystem?.SetSpawningEnabled(false);
            gameStateMachine?.SetState(GameState.Paused);
            uiSystem?.ShowPause();
            gameplayDialogueController?.Suspend();
        }

        private void ResumeRun()
        {
            if (!_isRunActive || _isGameOver)
            {
                return;
            }

            Time.timeScale = 1f;
            playerController?.SetControlsEnabled(true);
            enemySpawnerSystem?.SetSpawningEnabled(true);
            gateSystem?.SetSpawningEnabled(true);
            gameStateMachine?.SetState(GameState.Playing);
            uiSystem?.ShowGameplayHud();
            gameplayDialogueController?.Resume();
        }

        private void ReturnHome()
        {
            gameplayDialogueController?.EndRun();
            _startRunAfterReload = false;
            _showUpdateOnboardingAfterReload = false;
            ReturnedToMenu?.Invoke();
            ReloadCurrentScene();
        }

        private void RestartCurrentScene()
        {
            gameplayDialogueController?.EndRun();
            _startRunAfterReload = true;
            _showUpdateOnboardingAfterReload = false;
            ReloadCurrentScene();
        }

        private static void ReloadCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            SceneManager.LoadScene(activeScene.name);
        }

        private void HandleSquadDefeated(PlayerController defeatedSquad)
        {
            if (_isGameOver)
            {
                return;
            }

            _isGameOver = true;
            _isRunActive = false;
            RunEnded?.Invoke();
            gameplayDialogueController?.EndRun();

            playerController?.SetControlsEnabled(false);
            enemySpawnerSystem?.SetSpawningEnabled(false);
            gateSystem?.SetSpawningEnabled(false);
            runStatsTracker?.EndRun();

            RunStatsSnapshot snapshot = runStatsTracker != null
                ? runStatsTracker.CreateSnapshot()
                : default;

            if (!_isBenchmarkRun && runStatsTracker != null)
            {
                _missionSystem?.EndRun(snapshot);
            }

            if (runStatsTracker != null)
            {
                telemetryService?.EndRun(snapshot);
            }

            Action afterStory = () => CompleteRunEnd(snapshot, runStatsTracker != null);
            if (!ShouldSuppressBenchmarkStory()
                && TryPlayPostRunStoryCutscene(snapshot, runStatsTracker != null, afterStory))
            {
                return;
            }

            afterStory.Invoke();
        }

        private void CompleteRunEnd(RunStatsSnapshot snapshot, bool hasSnapshot)
        {
            if (!ShouldSuppressBenchmarkTutorial()
                && tutorialManager != null
                && tutorialManager.ShouldRunUpdateOnboardingAfterFirstDeath())
            {
                RouteToMainMenuForUpdateOnboarding();
                return;
            }

            ShowGameOverScreen(snapshot, hasSnapshot);
        }

        private void RouteToMainMenuForUpdateOnboarding()
        {
            _startRunAfterReload = false;
            _showUpdateOnboardingAfterReload = true;
            ReloadCurrentScene();
        }

        private bool TryPlayInitialStoryCutscene(
            bool startRunAfterCutscene,
            bool showUpdateOnboardingAfterCutscene)
        {
            if (storyCutsceneRuntime == null)
            {
                return false;
            }

            bool started = storyCutsceneRuntime.TryPlayInitialCutscene(() =>
            {
                gameStateMachine?.SetState(GameState.MainMenu);
                uiSystem?.ShowMainMenu();

                if (showUpdateOnboardingAfterCutscene)
                {
                    tutorialManager?.StartUpdateOnboardingFromMainMenu();
                    return;
                }

                if (startRunAfterCutscene)
                {
                    RequestStartRun();
                    return;
                }

                tutorialManager?.ShowStartRunSpotlightIfNeeded();
            });

            if (!started)
            {
                return false;
            }

            Time.timeScale = 0f;
            gameStateMachine?.SetState(GameState.Cutscene);
            return true;
        }

        private bool TryPlayPostRunStoryCutscene(
            RunStatsSnapshot snapshot,
            bool hasSnapshot,
            Action onComplete)
        {
            if (!hasSnapshot || storyCutsceneRuntime == null)
            {
                return false;
            }

            bool started = storyCutsceneRuntime.TryPlayPostRunCutscene(
                snapshot,
                onComplete);

            if (!started)
            {
                return false;
            }

            Time.timeScale = 0f;
            gameStateMachine?.SetState(GameState.Cutscene);
            return true;
        }

        private void ShowGameOverScreen(RunStatsSnapshot snapshot, bool hasSnapshot)
        {
            gameStateMachine?.SetState(GameState.GameOver);

            if (hasSnapshot)
            {
                uiSystem?.ShowGameOver(snapshot);
                return;
            }

            uiSystem?.ShowGameOver();
        }

        private bool IsBenchmarkProfileActive()
        {
            return benchmarkProfile != null && benchmarkProfile.IsActive;
        }

        public void NotifyGameplayTutorialCompleted()
        {
            _missionSystem?.NotifyGameplayTutorialCompleted();
        }

        private void InitializeMissionSystem()
        {
            if (missionCatalog == null)
            {
                missionCatalog = MissionCatalog.CreateRuntimeDefault();
            }

            if (_missionSystem != null)
            {
                _missionSystem.MissionCompleted -= HandleMissionCompleted;
            }

            _missionSystem?.Dispose();
            _missionSystem = new RuntimeMissionSystem(missionCatalog, SaveService.Instance);
            _missionSystem.MissionCompleted += HandleMissionCompleted;
            _missionSystem.InitializeFromSave();
            audioEventRouter?.BindMissionSystem(_missionSystem);
        }

        private void HandleMissionCompleted(MissionDefinition completedMission, MissionDefinition unlockedMission)
        {
            uiSystem?.ShowMissionButtonCompleteFeedback();
        }

        private void HandleMissionGateSelected(int gateSet, GateConfig config)
        {
            bool isTutorialGate = tutorialManager != null && tutorialManager.IsRunningGameplayTutorial;
            _missionSystem?.NotifyGateSelected(config, isTutorialGate);
        }

        private void HandleMissionUpgradePurchased(UpgradePurchaseTelemetry purchase)
        {
            _missionSystem?.NotifyUpgradePurchased();
        }

        private bool ShouldSuppressBenchmarkStory()
        {
            return _isPerformanceBenchmarkRun
                || (_isBenchmarkRun
                    && benchmarkProfile != null
                    && benchmarkProfile.SuppressStoryProgress);
        }

        private bool ShouldSuppressBenchmarkTutorial()
        {
            return _isPerformanceBenchmarkRun
                || (_isBenchmarkRun
                    && benchmarkProfile != null
                    && benchmarkProfile.SuppressTutorialProgress);
        }
    }
}
