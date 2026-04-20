using System;
using System.Collections.Generic;
using _Project.Scripts.Interfaces;
using UnityEngine;

namespace _Project.Scripts.Systems.PoolSystem
{
    /// <summary>
    /// Centralizes pooled object registration and lifecycle entry points for bullets and enemies.
    /// </summary>
    public sealed class PoolSystem : MonoBehaviour
    {
        [SerializeField] private List<PoolDefinition> poolDefinitions = new List<PoolDefinition>();

        public void Init()
        {
        }

        private void Update()
        {
        }

        public IPoolable Spawn()
        {
            return null;
        }

        public void Despawn(IPoolable poolable)
        {
        }
    }

    [Serializable]
    public sealed class PoolDefinition
    {
        [SerializeField] private string poolId;
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 16;

        public string PoolId => poolId;
        public GameObject Prefab => prefab;
        public int InitialSize => initialSize;
    }
}
