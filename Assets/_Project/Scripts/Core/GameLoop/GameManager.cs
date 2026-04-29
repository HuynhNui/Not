using _Project.Scripts.Core.StateMachine;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.CombatSystem;
using _Project.Scripts.Systems.EnemySpawnerSystem;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Systems.LevelSystem;
using _Project.Scripts.Systems.UISystem;
using UnityEngine;

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
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MainPlayerUnit mainPlayerUnit;

        private bool _isGameOver;

        public void Init()
        {
            uiSystem?.Init();

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (mainPlayerUnit == null)
            {
                mainPlayerUnit = FindAnyObjectByType<MainPlayerUnit>();
            }

            if (mainPlayerUnit != null)
            {
                mainPlayerUnit.Died -= HandlePlayerDied;
                mainPlayerUnit.Died += HandlePlayerDied;
            }

            Time.timeScale = 1f;
            _isGameOver = false;
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            if (mainPlayerUnit != null)
            {
                mainPlayerUnit.Died -= HandlePlayerDied;
            }
        }

        private void HandlePlayerDied(MainPlayerUnit deadPlayer)
        {
            if (_isGameOver)
            {
                return;
            }

            _isGameOver = true;

            playerController?.SetControlsEnabled(false);
            enemySpawnerSystem?.SetSpawningEnabled(false);
            uiSystem?.ShowGameOver();
        }
    }
}
