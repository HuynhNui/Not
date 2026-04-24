using _Project.Scripts.Interfaces;
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

        private readonly List<IBulletModifier> _modifiers = new List<IBulletModifier>();
        private readonly List<BulletModifierConfig> _modifierConfigs = new List<BulletModifierConfig>();

        private BulletSpawner _ownerSpawner;
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
            Destroy(gameObject);
        }

        public void Configure(BulletSpawner ownerSpawner, IReadOnlyList<BulletModifierConfig> modifierConfigs)
        {
            _ownerSpawner = ownerSpawner;
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

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.collider);
        }

        private void HandleHit(Collider target)
        {
            if (!_isActive || target == null)
            {
                return;
            }

            _preserveAfterHit = false;

            IDamageable damageable = target.GetComponent<IDamageable>();
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
    }
}
