using UnityEngine;
using System;

namespace _Project.Scripts.Core.StateMachine
{
    /// <summary>
    /// Holds and transitions the current runtime state of the game loop.
    /// </summary>
    public sealed class GameStateMachine : MonoBehaviour
    {
        [SerializeField] private GameState currentState = GameState.Bootstrap;

        public GameState CurrentState => currentState;
        public event Action<GameState, GameState> StateChanged;

        public void Init()
        {
        }

        private void Update()
        {
        }

        public void SetState(GameState nextState)
        {
            if (currentState == nextState)
            {
                return;
            }

            GameState previousState = currentState;
            currentState = nextState;
            StateChanged?.Invoke(previousState, currentState);
        }
    }

    public enum GameState
    {
        Bootstrap,
        MainMenu,
        Playing,
        Cutscene,
        Paused,
        GameOver
    }
}
