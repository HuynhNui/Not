using _Project.Scripts.Gameplay.Units;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Player
{
    /// <summary>
    /// Handles the player's lane or horizontal movement input in the active run.
    /// </summary>
    public sealed class PlayerMovement : UnitMovement
    {
        [SerializeField] private float horizontalClamp = 3.5f;
        [SerializeField] private bool useInputSystem = true;

        public override void Init()
        {
        }

        protected override void Update()
        {
        }
    }
}
