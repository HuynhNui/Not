using UnityEngine;

namespace _Project.Scripts.Data.ScriptableObjects.PlayerConfigs
{
    /// <summary>
    /// Stores player unit tuning that should be reusable across scenes and prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerUnitConfig", menuName = "Chibi Pixel Gate/Data/Player Unit Config")]
    public sealed class PlayerUnitConfig : ScriptableObject
    {
        [SerializeField] private float maxHealth = 10f;
        [SerializeField] private float damage = 1f;
        [SerializeField] private float fireRate = 4f;
        [SerializeField] private float bulletSpeed = 12f;

        public float MaxHealth => maxHealth;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float BulletSpeed => bulletSpeed;
    }
}
