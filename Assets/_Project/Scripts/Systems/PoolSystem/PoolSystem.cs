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

        private readonly Dictionary<GameObject, Queue<GameObject>> _poolByPrefab = new Dictionary<GameObject, Queue<GameObject>>();
        private bool _isInitialized;

        public void Init()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            for (int index = 0; index < poolDefinitions.Count; index++)
            {
                PoolDefinition definition = poolDefinitions[index];

                if (definition == null || definition.Prefab == null)
                {
                    continue;
                }

                EnsurePool(definition.Prefab);

                for (int instanceIndex = 0; instanceIndex < definition.InitialSize; instanceIndex++)
                {
                    _poolByPrefab[definition.Prefab].Enqueue(CreateInstance(definition.Prefab));
                }
            }
        }

        private void Awake()
        {
            Init();
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component, IPoolable
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject prefabObject = prefab.gameObject;
            EnsurePool(prefabObject);

            GameObject instance = _poolByPrefab[prefabObject].Count > 0
                ? _poolByPrefab[prefabObject].Dequeue()
                : CreateInstance(prefabObject);

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            return instance.GetComponent<T>();
        }

        public void Release(IPoolable poolable)
        {
            if (poolable is not Component component)
            {
                return;
            }

            PooledObject pooledObject = component.GetComponent<PooledObject>();

            if (pooledObject == null || pooledObject.Prefab == null)
            {
                Destroy(component.gameObject);
                return;
            }

            EnsurePool(pooledObject.Prefab);
            component.gameObject.SetActive(false);
            _poolByPrefab[pooledObject.Prefab].Enqueue(component.gameObject);
        }

        public void Despawn(IPoolable poolable)
        {
            Release(poolable);
        }

        public PoolDiagnostics CaptureDiagnostics()
        {
            int activeInstances = 0;
            int inactiveInstances = 0;
            int activeProjectiles = 0;

            for (int index = 0; index < transform.childCount; index++)
            {
                Transform child = transform.GetChild(index);
                if (child == null)
                {
                    continue;
                }

                GameObject instance = child.gameObject;
                PooledObject pooledObject = instance.GetComponent<PooledObject>();
                if (pooledObject == null)
                {
                    continue;
                }

                if (!instance.activeSelf)
                {
                    inactiveInstances++;
                    continue;
                }

                activeInstances++;
                GameObject prefab = pooledObject.Prefab;
                if (prefab != null
                    && (prefab.GetComponent<_Project.Scripts.Gameplay.Combat.Bullet>() != null
                        || prefab.GetComponent<_Project.Scripts.Gameplay.Enemies.EnemyProjectile>() != null))
                {
                    activeProjectiles++;
                }
            }

            return new PoolDiagnostics(activeInstances, inactiveInstances, activeProjectiles);
        }

        private void EnsurePool(GameObject prefab)
        {
            if (!_poolByPrefab.ContainsKey(prefab))
            {
                _poolByPrefab.Add(prefab, new Queue<GameObject>());
            }
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.SetActive(false);

            PooledObject pooledObject = instance.GetComponent<PooledObject>();

            if (pooledObject == null)
            {
                pooledObject = instance.AddComponent<PooledObject>();
            }

            pooledObject.Configure(this, prefab);
            return instance;
        }
    }

    public readonly struct PoolDiagnostics
    {
        public readonly int ActiveInstances;
        public readonly int InactiveInstances;
        public readonly int ActiveProjectiles;

        public int TotalInstances => ActiveInstances + InactiveInstances;

        public PoolDiagnostics(int activeInstances, int inactiveInstances, int activeProjectiles)
        {
            ActiveInstances = Mathf.Max(0, activeInstances);
            InactiveInstances = Mathf.Max(0, inactiveInstances);
            ActiveProjectiles = Mathf.Max(0, activeProjectiles);
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

    public sealed class PooledObject : MonoBehaviour
    {
        private PoolSystem _poolSystem;
        private GameObject _prefab;

        public PoolSystem PoolSystem => _poolSystem;
        public GameObject Prefab => _prefab;

        public void Configure(PoolSystem poolSystem, GameObject prefab)
        {
            _poolSystem = poolSystem;
            _prefab = prefab;
        }
    }
}
