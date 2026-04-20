using _Project.Scripts.Interfaces;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Represents a pooled projectile instance fired by the player's weapon.
    /// </summary>
    public sealed class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private float damage = 1f;

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

        public void Fire()
        {
        }

        public void TakeDamage(float damageAmount)
        {
        }
    }
}
