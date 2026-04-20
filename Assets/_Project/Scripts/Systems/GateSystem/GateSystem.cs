using System.Collections.Generic;
using _Project.Scripts.Gameplay.Gates;
using UnityEngine;

namespace _Project.Scripts.Systems.GateSystem
{
    /// <summary>
    /// Manages gate presentation, activation flow, and upgrade routing during the run.
    /// </summary>
    public sealed class GateSystem : MonoBehaviour
    {
        [SerializeField] private List<GateLogic> activeGates = new List<GateLogic>();

        public void Init()
        {
        }

        private void Update()
        {
        }

        public void Spawn()
        {
        }

        public void ApplyEffect()
        {
        }
    }
}
