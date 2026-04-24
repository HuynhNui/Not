using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Spawns extra bullets when the projectile hits a target.
    /// </summary>
    public sealed class SplitModifier : IBulletModifier
    {
        private readonly SplitModifierConfig _sourceConfig;
        private readonly int _splitCount;
        private readonly float _spreadAngle;
        private readonly float _damageMultiplier;

        public SplitModifier(SplitModifierConfig sourceConfig, int splitCount, float spreadAngle, float damageMultiplier)
        {
            _sourceConfig = sourceConfig;
            _splitCount = splitCount;
            _spreadAngle = spreadAngle;
            _damageMultiplier = damageMultiplier;
        }

        public void OnInit(Bullet bullet)
        {
        }

        public void OnUpdate(Bullet bullet)
        {
        }

        public void OnHit(Bullet bullet, Collider target)
        {
            if (_splitCount <= 0)
            {
                return;
            }

            float totalSpread = _splitCount == 1 ? 0f : _spreadAngle;
            float angleStep = _splitCount == 1 ? 0f : totalSpread / (_splitCount - 1);
            float startAngle = -totalSpread * 0.5f;

            for (int index = 0; index < _splitCount; index++)
            {
                float angle = startAngle + angleStep * index;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.forward) * bullet.Direction;
                bullet.SpawnChildBullet(direction, _damageMultiplier, _sourceConfig);
            }
        }
    }
}
