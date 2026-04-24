using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Steers a bullet toward the nearest target inside a search radius.
    /// </summary>
    public sealed class HomingModifier : IBulletModifier
    {
        private readonly float _searchRadius;
        private readonly float _turnSpeed;
        private readonly LayerMask _targetLayers;

        public HomingModifier(float searchRadius, float turnSpeed, LayerMask targetLayers)
        {
            _searchRadius = searchRadius;
            _turnSpeed = turnSpeed;
            _targetLayers = targetLayers;
        }

        public void OnInit(Bullet bullet)
        {
        }

        public void OnUpdate(Bullet bullet)
        {
            Collider[] targets = Physics.OverlapSphere(bullet.Position, _searchRadius, _targetLayers);
            Transform closestTarget = null;
            float closestSqrDistance = float.MaxValue;

            for (int index = 0; index < targets.Length; index++)
            {
                Collider target = targets[index];
                float sqrDistance = (target.transform.position - bullet.Position).sqrMagnitude;

                if (sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closestSqrDistance = sqrDistance;
                closestTarget = target.transform;
            }

            if (closestTarget == null)
            {
                return;
            }

            Vector3 directionToTarget = (closestTarget.position - bullet.Position).normalized;
            Vector3 nextDirection = Vector3.RotateTowards(
                bullet.Direction,
                directionToTarget,
                _turnSpeed * Mathf.Deg2Rad * Time.deltaTime,
                0f);

            bullet.SetDirection(nextDirection);
        }

        public void OnHit(Bullet bullet, Collider target)
        {
        }
    }
}
