using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Interfaces;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Gates
{
    /// <summary>
    /// Stores the gate configuration and exposes the effect entry point applied during a run.
    /// </summary>
    public sealed class GateLogic : MonoBehaviour, IGateEffect
    {
        [SerializeField] private GateConfig gateConfig;
        [SerializeField] private bool consumeAfterUse = true;

        public void Init()
        {
        }

        private void Update()
        {
        }

        public void Spawn()
        {
        }

        public void Despawn()
        {
        }

        public void ApplyEffect()
        {
        }
    }
}
