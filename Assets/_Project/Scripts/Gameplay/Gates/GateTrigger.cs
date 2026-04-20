using UnityEngine;

namespace _Project.Scripts.Gameplay.Gates
{
    /// <summary>
    /// Detects player passage through a gate and forwards the activation request to gate logic.
    /// </summary>
    public sealed class GateTrigger : MonoBehaviour
    {
        [SerializeField] private GateLogic gateLogic;
        [SerializeField] private Collider2D triggerCollider;

        public void Init()
        {
        }

        private void Update()
        {
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
        }
    }
}
