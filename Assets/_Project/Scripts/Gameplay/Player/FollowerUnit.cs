using UnityEngine;

namespace _Project.Scripts.Gameplay.Player
{
    /// <summary>
    /// Squad unit that follows another player unit without owning input logic.
    /// </summary>
    public sealed class FollowerUnit : PlayerUnit
    {
        [SerializeField] private PlayerUnit followTarget;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -1.25f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float damageMultiplier = 0.7f;
        [SerializeField] private float fireRateMultiplier = 1f;

        private Vector3 _followVelocity;
        private float _baseDamage;
        private float _baseFireRate;

        public PlayerUnit FollowTarget => followTarget;

        public override void Initialize()
        {
            CacheBaseStatsIfNeeded();

            base.Initialize();
            ApplyStatMultipliers();
        }

        private void Update()
        {
            Follow();
        }

        public void SetFollowTarget(PlayerUnit target)
        {
            followTarget = target;
        }

        public void SetFollowOffset(Vector3 offset)
        {
            followOffset = offset;
        }

        public override void SetDamage(float value)
        {
            _baseDamage = Mathf.Max(0f, value);
            base.SetDamage(_baseDamage * Mathf.Max(0f, damageMultiplier));
        }

        public override void SetFireRate(float value)
        {
            _baseFireRate = Mathf.Max(0f, value);
            base.SetFireRate(_baseFireRate * Mathf.Max(0f, fireRateMultiplier));
        }

        private void CacheBaseStatsIfNeeded()
        {
            if (_baseDamage > 0f || _baseFireRate > 0f)
            {
                return;
            }

            _baseDamage = Damage;
            _baseFireRate = FireRate;
        }

        private void ApplyStatMultipliers()
        {
            SetDamage(_baseDamage);
            SetFireRate(_baseFireRate);
        }

        private void Follow()
        {
            if (followTarget == null)
            {
                return;
            }

            Vector3 desiredPosition = followTarget.transform.position + followOffset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _followVelocity,
                Mathf.Max(0.01f, smoothTime));
        }
    }
}
