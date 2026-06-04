using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Interfaces;
using _Project.Scripts.Systems.PoolSystem;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemies
{
    /// <summary>
    /// Visual explosion owned by Chomboom. Damage is applied once when the FX appears.
    /// </summary>
    public sealed class ChomboomBoomFx : MonoBehaviour, IPoolable
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float damage = 2f;
        [SerializeField] private float radius = 1.75f;
        [SerializeField] private float lifetime = 0.6f;
        [SerializeField] private string boomStateName = "boom fx";

        private PoolSystem _poolSystem;
        private MainPlayerUnit _playerUnit;
        private float _remainingLifetime;
        private bool _isActive;
        private bool _hasAppliedDamage;

        public void Init(MainPlayerUnit playerUnit, float explosionDamage, float explosionRadius)
        {
            _playerUnit = playerUnit;
            damage = Mathf.Max(0f, explosionDamage);
            radius = Mathf.Max(0f, explosionRadius);
        }

        private void Awake()
        {
            animator ??= GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            _remainingLifetime -= Time.deltaTime;

            if (_remainingLifetime <= 0f)
            {
                Despawn();
            }
        }

        public void Spawn()
        {
            _remainingLifetime = Mathf.Max(0.01f, lifetime);
            _isActive = true;
            _hasAppliedDamage = false;
            PlayState(boomStateName);
            ApplyDamageOnce();
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

        private void ApplyDamageOnce()
        {
            if (_hasAppliedDamage)
            {
                return;
            }

            _hasAppliedDamage = true;

            PlayerUnit[] playerUnits = FindObjectsByType<PlayerUnit>();
            for (int index = 0; index < playerUnits.Length; index++)
            {
                PlayerUnit playerUnit = playerUnits[index];

                if (playerUnit == null || playerUnit.IsDead)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, playerUnit.transform.position);

                if (distance <= radius)
                {
                    playerUnit.TakeDamage(damage);
                }
            }
        }

        private void PlayState(string stateName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            animator.Play(stateName, 0, 0f);
        }
    }
}
