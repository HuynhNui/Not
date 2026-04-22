//playerController.cs
using _Project.Scripts.Gameplay.Combat;
using _Project.Scripts.Gameplay.Units;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Player
{
    /// <summary>
    /// Serves as the player composition root and coordinates movement, combat, and damage entry points.
    /// </summary>
    public sealed class PlayerController : UnitController
    {
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private WeaponController playerWeapon;
        [SerializeField] private Collider2D hitboxCollider;
        [SerializeField] private float currentHealth = 1f;

        public override void Init()
        {
            if (playerMovement != null)
            {
                playerMovement.Init();
            }

            if (playerWeapon != null)
            {
                playerWeapon.Init();
            }
        }

        private void Awake()
        {
            Init();
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void Fire()
        {
            if (playerWeapon == null)
            {
                return;
            }

            playerWeapon.Fire();
        }

        public override void TakeDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
        }
    }
}
