using _Project.Scripts.Data.ScriptableObjects.UnitData;
using _Project.Scripts.Interfaces;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemies
{
    /// <summary>
    /// Defines a pooled enemy unit that can spawn, move toward the player, and receive damage.
    /// </summary>
    public sealed class EnemyController : MonoBehaviour, IPoolable
    {
        [SerializeField] private UnitData unitData;
        [SerializeField] private float currentHealth = 1f;
        [SerializeField] private int scoreValue = 1;

        public void Init()
        {
        }

        private void Update()
        {
        }

        public void Spawn()
        {
        }

        public void Despawn()
        {
        }

        public void TakeDamage(float damageAmount)
        {
        }
    }
}
