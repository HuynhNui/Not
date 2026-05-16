using UnityEngine;
using _Project.Scripts.Gameplay.Player;

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
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            if (gateLogic == null)
            {
                gateLogic = GetComponent<GateLogic>();
            }
        }

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (gateLogic == null || other == null)
            {
                return;
            }

            MainPlayerUnit hitPlayer = other.GetComponent<MainPlayerUnit>();

            if (hitPlayer == null)
            {
                return;
            }

            gateLogic.HandlePlayerTriggered(hitPlayer);
        }
    }
}
