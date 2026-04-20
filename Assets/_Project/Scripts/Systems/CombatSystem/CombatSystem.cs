using _Project.Scripts.Gameplay.Combat;
using UnityEngine;

namespace _Project.Scripts.Systems.CombatSystem
{
    /// <summary>
    /// Coordinates active combat flow, projectile fire cadence, and combat stat upgrades.
    /// </summary>
    public sealed class CombatSystem : MonoBehaviour
    {
        [SerializeField] private WeaponController playerWeapon;
        [SerializeField] private Transform playerFireOrigin;

        public void Init()
        {
        }

        private void Update()
        {
        }

        public void Fire()
        {
        }

        public void ApplyEffect()
        {
        }
    }
}
