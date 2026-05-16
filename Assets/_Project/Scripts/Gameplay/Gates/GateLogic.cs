using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Interfaces;
using _Project.Scripts.Systems.GateSystem;
using _Project.Scripts.Gameplay.Player;
using UnityEngine;
using RuntimePoolSystem = _Project.Scripts.Systems.PoolSystem.PoolSystem;

namespace _Project.Scripts.Gameplay.Gates
{
    /// <summary>
    /// 2D door runtime: fixed lane X at spawn, drifts straight down on Y.
    /// </summary>
    public sealed class GateLogic : MonoBehaviour, IGateEffect, IPoolable
    {
        [SerializeField] private GateConfig gateConfig;
        [SerializeField] private bool consumeAfterUse = true;
        [SerializeField] private DoorView doorView;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float despawnBelowCameraOffset = 1.5f;

        [Header("Optional pooling")]
        [SerializeField] private RuntimePoolSystem poolSystem;

        private float _lockedLaneWorldX;
        private Camera _gameplayCamera;
        private bool _isActive;
        private bool _hasLane;
        private GateSystem _gateSystem;
        private MainPlayerUnit _playerUnit;
        private PlayerController _playerController;
        private Rigidbody2D _rigidbody;

        public bool ConsumeAfterUse => consumeAfterUse;
        public GateConfig GateConfig => gateConfig;

        public void Init(
            GateConfig config,
            GateSystem gateSystem,
            MainPlayerUnit playerUnit,
            PlayerController playerController,
            Camera gameplayCamera,
            RuntimePoolSystem runtimePoolSystem,
            float lockedLaneWorldX)
        {
            gateConfig = config;
            _gateSystem = gateSystem;
            _playerUnit = playerUnit;
            _playerController = playerController;
            _gameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            poolSystem = runtimePoolSystem != null ? runtimePoolSystem : poolSystem;
            _lockedLaneWorldX = lockedLaneWorldX;
            _hasLane = true;
            _isActive = true;
            _rigidbody ??= GetComponent<Rigidbody2D>();

            if (doorView == null)
            {
                doorView = GetComponent<DoorView>();
            }

            doorView?.Bind(gateConfig);
            ApplyLanePosition();
        }

        private void Update()
        {
            if (!_isActive || !_hasLane)
            {
                return;
            }

            MoveStraightDown();
            DespawnIfOutOfBounds();
        }

        public void Spawn()
        {
            _isActive = true;
            doorView?.Bind(gateConfig);
            ApplyLanePosition();
        }

        public void Despawn()
        {
            _isActive = false;
            _hasLane = false;

            if (poolSystem != null)
            {
                poolSystem.Release(this);
                return;
            }

            Destroy(gameObject);
        }

        public void ApplyEffect()
        {
            if (gateConfig == null || _playerUnit == null)
            {
                return;
            }

            if (_playerController != null)
            {
                _playerController.ApplyGateEffect(gateConfig);
                return;
            }

            GateEffectApplier.Apply(gateConfig, _playerUnit, null);
        }

        public void HandlePlayerTriggered(MainPlayerUnit hitPlayer)
        {
            if (!_isActive || hitPlayer == null || hitPlayer.IsDead)
            {
                return;
            }

            _gateSystem?.HandleGateChosen(this);
        }

        private void ApplyLanePosition()
        {
            if (_rigidbody != null)
            {
                Vector2 bodyPosition = _rigidbody.position;
                bodyPosition.x = _lockedLaneWorldX;
                bodyPosition.y = transform.position.y;
                _rigidbody.position = bodyPosition;
            }

            Vector3 position = transform.position;
            position.x = _lockedLaneWorldX;
            transform.position = position;
        }

        private void MoveStraightDown()
        {
            float nextY = transform.position.y - Mathf.Max(0f, moveSpeed) * Time.deltaTime;

            if (_rigidbody != null)
            {
                _rigidbody.MovePosition(new Vector2(_lockedLaneWorldX, nextY));
                return;
            }

            transform.position = new Vector3(_lockedLaneWorldX, nextY, transform.position.z);
        }

        private void DespawnIfOutOfBounds()
        {
            if (_gameplayCamera == null || !_gameplayCamera.orthographic)
            {
                return;
            }

            float bottomLimit = _gameplayCamera.transform.position.y - _gameplayCamera.orthographicSize - despawnBelowCameraOffset;

            if (transform.position.y < bottomLimit)
            {
                Despawn();
            }
        }
    }
}
