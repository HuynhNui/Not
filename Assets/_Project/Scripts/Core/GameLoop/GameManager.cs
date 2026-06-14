using _Project.Scripts.Core.StateMachine;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.CombatSystem;
using _Project.Scripts.Systems.EnemySpawnerSystem;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.LevelSystem;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.UISystem;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MainPlayerUnit mainPlayerUnit;

        private bool _isGameOver;
        private bool _isRunActive;
        private static bool _startRunAfterReload;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionState()
        {
            _startRunAfterReload = false;
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

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (mainPlayerUnit == null)
            {
                mainPlayerUnit = FindAnyObjectByType<MainPlayerUnit>();
            }

            if (runStatsTracker != null)
            {
                runStatsTracker.Init(enemySpawnerSystem);
            }

            if (uiSystem != null)
            {
                uiSystem.Init(runStatsTracker);
                uiSystem.PlayRequested -= StartRun;
                uiSystem.PlayRequested += StartRun;
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

            if (playerController != null)
            {
                playerController.SquadDefeated -= HandleSquadDefeated;
                playerController.SquadDefeated += HandleSquadDefeated;
            }

            Time.timeScale = 1f;
            _isGameOver = false;
            _isRunActive = false;
            playerController?.SetControlsEnabled(false);
            if (playerController != null)
            {
                playerController.gameObject.SetActive(false);
            }

            enemySpawnerSystem?.SetSpawningEnabled(false);
            gameStateMachine?.SetState(GameState.MainMenu);
            uiSystem?.ShowMainMenu();

            if (_startRunAfterReload)
            {
                _startRunAfterReload = false;
                StartRun();
            }
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            if (uiSystem != null)
            {
                uiSystem.PlayRequested -= StartRun;
                uiSystem.PauseRequested -= PauseRun;
                uiSystem.ResumeRequested -= ResumeRun;
                uiSystem.RestartRequested -= RestartCurrentScene;
                uiSystem.HomeRequested -= ReturnHome;
            }

            if (playerController != null)
            {
                playerController.SquadDefeated -= HandleSquadDefeated;
            }
        }

        private void StartRun()
        {
            Time.timeScale = 1f;
            _isGameOver = false;
            _isRunActive = true;

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
            runStatsTracker?.BeginRun();
            playerController?.SetControlsEnabled(true);
            enemySpawnerSystem?.BeginRun();
            enemySpawnerSystem?.SetSpawningEnabled(true);
            gameStateMachine?.SetState(GameState.Playing);
            uiSystem?.ShowGameplayHud();
        }

        private void PauseRun()
        {
            if (!_isRunActive || _isGameOver)
            {
                return;
            }

            playerController?.SetControlsEnabled(false);
            enemySpawnerSystem?.SetSpawningEnabled(false);
            gameStateMachine?.SetState(GameState.Paused);
            uiSystem?.ShowPause();
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
            gameStateMachine?.SetState(GameState.Playing);
            uiSystem?.ShowGameplayHud();
        }

        private void ReturnHome()
        {
            _startRunAfterReload = false;
            ReloadCurrentScene();
        }

        private void RestartCurrentScene()
        {
            _startRunAfterReload = true;
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

            playerController?.SetControlsEnabled(false);
            enemySpawnerSystem?.SetSpawningEnabled(false);
            runStatsTracker?.EndRun();
            gameStateMachine?.SetState(GameState.GameOver);

            if (runStatsTracker != null)
            {
                uiSystem?.ShowGameOver(runStatsTracker.CreateSnapshot());
                return;
            }

            uiSystem?.ShowGameOver();
        }
    }
}
