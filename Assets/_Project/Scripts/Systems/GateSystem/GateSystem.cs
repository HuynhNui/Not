using System.Collections.Generic;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Gates;
using _Project.Scripts.Gameplay.Player;
using UnityEngine;
using RuntimePoolSystem = _Project.Scripts.Systems.PoolSystem.PoolSystem;

namespace _Project.Scripts.Systems.GateSystem
{
    /// <summary>
    /// Manages gate presentation, activation flow, and upgrade routing during the run.
    /// </summary>
    public sealed class GateSystem : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private GateLogic gatePrefab;
        [SerializeField] private float spawnIntervalSeconds = 20f;
        [SerializeField] private float spawnAboveCameraOffset = 1.25f;
        [SerializeField] private bool useViewportLanes = true;
        [SerializeField] private float viewportLaneMin = 0.12f;
        [SerializeField] private float viewportLaneMax = 0.88f;
        [SerializeField] private float gateHalfWidth = 0.75f;
        [SerializeField] private float laneSpacing = 2.2f;
        [SerializeField] private int gateCount = 3;

        [Header("Configs (pick 3 each spawn)")]
        [SerializeField] private List<GateConfig> availableGateConfigs = new List<GateConfig>();

        [Header("Runtime references")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MainPlayerUnit mainPlayerUnit;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private RuntimePoolSystem poolSystem;

        [SerializeField] private List<GateLogic> activeGates = new List<GateLogic>();

        private float _nextSpawnTime;
        private bool _isGateSetActive;
        private bool _choiceLocked;

        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            ResolveGameplayCamera();
            poolSystem ??= FindAnyObjectByType<RuntimePoolSystem>();

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (mainPlayerUnit == null)
            {
                mainPlayerUnit = FindAnyObjectByType<MainPlayerUnit>();
            }

            _nextSpawnTime = Time.time + Mathf.Max(0.01f, spawnIntervalSeconds);
            _isGateSetActive = false;
            _choiceLocked = false;
        }

        private void Update()
        {
            if (mainPlayerUnit == null || mainPlayerUnit.IsDead)
            {
                return;
            }

            if (_isGateSetActive)
            {
                return;
            }

            if (Time.time >= _nextSpawnTime)
            {
                Spawn();
                _nextSpawnTime = Time.time + Mathf.Max(0.01f, spawnIntervalSeconds);
            }
        }

        public void Spawn()
        {
            if (gatePrefab == null || mainPlayerUnit == null)
            {
                return;
            }

            if (availableGateConfigs == null || availableGateConfigs.Count <= 0)
            {
                return;
            }

            ClearActiveGates();
            _choiceLocked = false;
            _isGateSetActive = true;

            ResolveGameplayCamera();

            float spawnY = GetSpawnWorldY();
            int count = Mathf.Max(1, gateCount);

            for (int index = 0; index < count; index++)
            {
                GateConfig config = PickGateConfig(index);
                float laneWorldX = GetLaneWorldX(index, count);
                Vector3 spawnPosition = new Vector3(laneWorldX, spawnY, 0f);

                GateLogic instance = poolSystem != null
                    ? poolSystem.Spawn(gatePrefab, spawnPosition, Quaternion.identity)
                    : Instantiate(gatePrefab, spawnPosition, Quaternion.identity);

                if (instance == null)
                {
                    continue;
                }

                instance.Init(
                    config,
                    this,
                    mainPlayerUnit,
                    playerController,
                    gameplayCamera,
                    poolSystem,
                    laneWorldX);
                instance.Spawn();
                activeGates.Add(instance);
            }
        }

        public void ApplyEffect()
        {
        }

        public void HandleGateChosen(GateLogic chosen)
        {
            if (!_isGateSetActive || _choiceLocked || chosen == null)
            {
                return;
            }

            _choiceLocked = true;
            chosen.ApplyEffect();

            for (int index = activeGates.Count - 1; index >= 0; index--)
            {
                GateLogic gate = activeGates[index];
                if (gate == null)
                {
                    activeGates.RemoveAt(index);
                    continue;
                }

                if (gate != chosen)
                {
                    gate.Despawn();
                    activeGates.RemoveAt(index);
                }
            }

            if (chosen.ConsumeAfterUse)
            {
                chosen.Despawn();
            }

            activeGates.Clear();
            _isGateSetActive = false;
        }

        private GateConfig PickGateConfig(int indexHint)
        {
            if (availableGateConfigs == null || availableGateConfigs.Count <= 0)
            {
                return null;
            }

            // Best-effort unique pick for the three gates.
            // If there are fewer than 3 configs, duplicates are allowed.
            for (int attempt = 0; attempt < 8; attempt++)
            {
                GateConfig candidate = availableGateConfigs[Random.Range(0, availableGateConfigs.Count)];
                if (candidate == null)
                {
                    continue;
                }

                if (availableGateConfigs.Count < 3)
                {
                    return candidate;
                }

                bool alreadyUsed = false;
                for (int gateIndex = 0; gateIndex < activeGates.Count; gateIndex++)
                {
                    GateLogic existing = activeGates[gateIndex];
                    if (existing != null && existing.GateConfig == candidate)
                    {
                        alreadyUsed = true;
                        break;
                    }
                }

                if (!alreadyUsed)
                {
                    return candidate;
                }
            }

            return availableGateConfigs[Random.Range(0, availableGateConfigs.Count)];
        }

        private void ResolveGameplayCamera()
        {
            if (gameplayCamera != null)
            {
                return;
            }

            gameplayCamera = Camera.main;

            if (gameplayCamera == null)
            {
                gameplayCamera = FindAnyObjectByType<Camera>();
            }
        }

        private float GetSpawnWorldY()
        {
            if (gameplayCamera != null && gameplayCamera.orthographic)
            {
                float halfHeight = gameplayCamera.orthographicSize;
                return gameplayCamera.transform.position.y + halfHeight + spawnAboveCameraOffset;
            }

            return mainPlayerUnit != null ? mainPlayerUnit.transform.position.y + 6f : transform.position.y + 6f;
        }

        private float GetLaneWorldX(int laneIndex, int totalLanes)
        {
            if (useViewportLanes && TryGetViewportLaneX(laneIndex, totalLanes, out float viewportLaneWorldX))
            {
                return viewportLaneWorldX;
            }

            float centerX = gameplayCamera != null
                ? gameplayCamera.transform.position.x
                : 0f;

            return centerX + GetLaneOffsetX(laneIndex, totalLanes);
        }

        private bool TryGetViewportLaneX(int laneIndex, int totalLanes, out float worldX)
        {
            worldX = 0f;

            if (gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return false;
            }

            float halfGate = GetGateHalfWidth();
            float zDistance = GetViewportZDistance();

            Vector3 worldMin = gameplayCamera.ViewportToWorldPoint(
                new Vector3(viewportLaneMin, 0.5f, zDistance));
            Vector3 worldMax = gameplayCamera.ViewportToWorldPoint(
                new Vector3(viewportLaneMax, 0.5f, zDistance));

            float leftCenter = worldMin.x + halfGate;
            float rightCenter = worldMax.x - halfGate;

            if (totalLanes <= 1)
            {
                worldX = (leftCenter + rightCenter) * 0.5f;
                return true;
            }

            float laneT = laneIndex / (totalLanes - 1f);
            worldX = Mathf.Lerp(leftCenter, rightCenter, laneT);
            return true;
        }

        private float GetGateHalfWidth()
        {
            if (gatePrefab == null)
            {
                return Mathf.Max(0f, gateHalfWidth);
            }

            BoxCollider2D gateCollider = gatePrefab.GetComponent<BoxCollider2D>();
            if (gateCollider == null)
            {
                return Mathf.Max(0f, gateHalfWidth);
            }

            float prefabScaleX = Mathf.Abs(gatePrefab.transform.localScale.x);
            return Mathf.Max(gateHalfWidth, gateCollider.size.x * 0.5f * prefabScaleX);
        }

        private float GetViewportZDistance()
        {
            if (gameplayCamera == null)
            {
                return 10f;
            }

            return Mathf.Abs(gameplayCamera.transform.position.z);
        }

        private float GetLaneOffsetX(int laneIndex, int totalLanes)
        {
            if (totalLanes <= 1)
            {
                return 0f;
            }

            float centerIndex = (totalLanes - 1) * 0.5f;
            return (laneIndex - centerIndex) * laneSpacing;
        }

        private void ClearActiveGates()
        {
            for (int index = activeGates.Count - 1; index >= 0; index--)
            {
                GateLogic gate = activeGates[index];
                if (gate == null)
                {
                    continue;
                }

                gate.Despawn();
            }

            activeGates.Clear();
            _isGateSetActive = false;
        }
    }
}
