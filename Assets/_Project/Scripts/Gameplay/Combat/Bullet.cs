using _Project.Scripts.Interfaces;
using _Project.Scripts.Systems.PoolSystem;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Runtime projectile that owns movement, hit handling, and modifier dispatch.
    /// </summary>
    public sealed class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private float damage = 1f;
        [SerializeField] private bool destroyOnHitByDefault = true;
        [SerializeField] private bool requireDamageableHit = true;

        private readonly List<IBulletModifier> _modifiers = new List<IBulletModifier>();
        private readonly List<BulletModifierConfig> _modifierConfigs = new List<BulletModifierConfig>();

        private BulletSpawner _ownerSpawner;
        private PoolSystem _poolSystem;
        private Transform _ownerRoot;
        private float _remainingLifetime;
        private bool _isActive;
        private bool _preserveAfterHit;

        public float Damage => damage;
        public float Speed => speed;
        public Vector3 Position => transform.position;
        public Vector3 Direction => transform.up;

        public void Init(float bulletDamage, float bulletSpeed)
        {
            damage = Mathf.Max(0f, bulletDamage);
            speed = Mathf.Max(0f, bulletSpeed);
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            for (int index = 0; index < _modifiers.Count; index++)
            {
                _modifiers[index].OnUpdate(this);
            }

            transform.position += Direction * (speed * Time.deltaTime);
            _remainingLifetime -= Time.deltaTime;

            if (_remainingLifetime <= 0f)
            {
                Despawn();
            }
        }

        public void Spawn()
        {
            _remainingLifetime = lifetime;
            _isActive = true;
            _preserveAfterHit = false;

            for (int index = 0; index < _modifiers.Count; index++)
            {
                _modifiers[index].OnInit(this);
            }
        }

        public void Despawn()
        {
            _isActive = false;

            if (_poolSystem != null)
            {
                _poolSystem.Release(this);
                return;
            }

            Destroy(gameObject);
        }

        public void SetPoolSystem(PoolSystem poolSystem)
        {
            _poolSystem = poolSystem;
        }

        public void Configure(BulletSpawner ownerSpawner, IReadOnlyList<BulletModifierConfig> modifierConfigs)
        {
            _ownerSpawner = ownerSpawner;
            _ownerRoot = ownerSpawner != null ? ownerSpawner.transform.root : null;
            _modifiers.Clear();
            _modifierConfigs.Clear();

            if (modifierConfigs == null)
            {
                return;
            }

            for (int index = 0; index < modifierConfigs.Count; index++)
            {
                BulletModifierConfig config = modifierConfigs[index];

                if (config == null)
                {
                    continue;
                }

                _modifierConfigs.Add(config);
                _modifiers.Add(config.CreateModifier(ownerSpawner));
            }
        }

        public void SetDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            transform.up = direction.normalized;
        }

        public void PreserveAfterHit()
        {
            _preserveAfterHit = true;
        }

        public void SpawnChildBullet(Vector3 direction, float damageMultiplier, BulletModifierConfig excludedModifier)
        {
            if (_ownerSpawner == null)
            {
                return;
            }

            float childDamage = damage * Mathf.Max(0f, damageMultiplier);
            _ownerSpawner.SpawnChildBullet(transform.position, direction, childDamage, speed, _modifierConfigs, excludedModifier);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleHit(collision.collider);
        }

        private void HandleHit(Collider2D target)
        {
            if (!_isActive || target == null)
            {
                return;
            }

            if (_ownerRoot != null && target.transform.root == _ownerRoot)
            {
                return;
            }

            IDamageable damageable = FindDamageableTarget(target);

            if (damageable == null && requireDamageableHit)
            {
                return;
            }

            _preserveAfterHit = false;
            damageable?.TakeDamage(damage);

            for (int index = 0; index < _modifiers.Count; index++)
            {
                _modifiers[index].OnHit(this, target);
            }

            if (destroyOnHitByDefault && !_preserveAfterHit)
            {
                Despawn();
            }
        }

        private static IDamageable FindDamageableTarget(Collider2D target)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                return damageable;
            }

            return target.GetComponentInParent<IDamageable>();
        }
    }
}
