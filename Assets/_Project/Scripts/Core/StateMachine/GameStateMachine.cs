using UnityEngine;

namespace _Project.Scripts.Core.StateMachine
{
    /// <summary>
    /// Holds and transitions the current runtime state of the game loop.
    /// </summary>
    public sealed class GameStateMachine : MonoBehaviour
    {
        [SerializeField] private GameState currentState = GameState.Bootstrap;

        public GameState CurrentState => currentState;

        public void Init()
        {
        }

        private void Update()
        {
        }

        public void SetState(GameState nextState)
        {
        }
    }

    public enum GameState
    {
        Bootstrap,
        Playing,
        Paused,
        GameOver
    }
}
