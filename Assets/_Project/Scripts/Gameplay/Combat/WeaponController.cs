using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Controls automatic projectile firing and stores upgradeable weapon stats.
    /// </summary>
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private float fireInterval = 0.2f;
        [SerializeField] private int projectileCount = 1;
        [SerializeField] private float baseDamage = 1f;

        public Transform FirePoint => firePoint;
        public Bullet BulletPrefab => bulletPrefab;
        public float FireInterval => fireInterval;
        public int ProjectileCount => projectileCount;
        public float BaseDamage => baseDamage;

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
