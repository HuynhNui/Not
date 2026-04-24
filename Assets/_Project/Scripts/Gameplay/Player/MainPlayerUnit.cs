using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Player
{
    /// <summary>
    /// Main squad unit with health and death handling.
    /// Combat behavior remains inherited from PlayerUnit.
    /// </summary>
    public sealed class MainPlayerUnit : PlayerUnit
    {
        [SerializeField] private float maxHp = 10f;

        private float _currentHp;
        private bool _isDead;

        public event Action<MainPlayerUnit> Died;

        public float CurrentHp => _currentHp;
        public float MaxHp => maxHp;
        public bool IsDead => _isDead;

        public override void Initialize()
        {
            base.Initialize();

            if (_currentHp <= 0f && !_isDead)
            {
                _currentHp = Mathf.Max(1f, maxHp);
            }
        }

        public void TakeDamage(float value)
        {
            if (_isDead)
            {
                return;
            }

            _currentHp = Mathf.Max(0f, _currentHp - Mathf.Max(0f, value));

            if (_currentHp <= 0f)
            {
                Die();
            }
        }

        public void RestoreFullHealth()
        {
            _isDead = false;
            _currentHp = Mathf.Max(1f, maxHp);
        }

        public void Die()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            Died?.Invoke(this);
        }
    }
}
