using UnityEngine;

namespace _Project.Scripts.Gameplay.Units
{
    /// <summary>
    /// Owns shared 2D movement configuration for player and enemy units.
    /// </summary>
    public class UnitMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Rigidbody2D cachedRigidbody;

        public virtual void Init()
        {
        }

        protected virtual void Update()
        {
        }

        public virtual void Spawn()
        {
        }

        public virtual void Despawn()
        {
        }
    }
}
