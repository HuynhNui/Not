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

        private Vector3 _followVelocity;

        public PlayerUnit FollowTarget => followTarget;

        private void Update()
        {
            if (IsDead)
            {
                return;
            }

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
