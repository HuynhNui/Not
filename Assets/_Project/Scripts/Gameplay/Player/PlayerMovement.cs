using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using _Project.Scripts.Gameplay.Units;
using InputTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace _Project.Scripts.Gameplay.Player
{
    public sealed class PlayerMovement : UnitMovement
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Collider2D movementBoundsCollider;
        [SerializeField] private float horizontalClamp = 3.5f;
        [SerializeField] private bool useCameraBounds = true;
        [SerializeField] private bool ignoreTouchesOverUi = true;

        [Header("Horizontal Bounds")]
        [SerializeField, Range(0f, 0.1f)] private float playerHorizontalViewportPadding = 0.01f;
        [SerializeField] private float playerHorizontalWorldPadding = 0f;

        [Header("Viewport Safe Zone")]
        [SerializeField, Range(0f, 0.3f)] private float bottomReservedViewport = 0.16f;
        [SerializeField, Range(0.05f, 0.45f)] private float runStartViewportY = 0.25f;
        [SerializeField] private float bottomWorldPadding = 0.08f;
        [SerializeField] private bool keepMainAboveBottomSafeZone = true;

        private bool _hasActivePointer;
        private int _activeFingerId = -1;

        private float _targetX;
        private float _dragOffset;
        private bool _inputEnabled = true;
        private Transform _mainPlayerTransform;
        private Transform _fireLineAnchorTransform;
        private Renderer _mainPlayerRenderer;

        public override void Init()
        {
            gameplayCamera ??= Camera.main;
            movementBoundsCollider ??= GetComponentInChildren<Collider2D>();
            CacheMainPlayerReference();
            _targetX = transform.position.x;
            _hasActivePointer = false;
            _activeFingerId = -1;
        }

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        protected override void Update()
        {
            if (!_inputEnabled)
            {
                return;
            }

            ReadInput();
            Move();
        }

        public void SetInputEnabled(bool isEnabled)
        {
            _inputEnabled = isEnabled;

            if (!isEnabled)
            {
                _hasActivePointer = false;
                _activeFingerId = -1;
            }
        }

        public void SnapToRunStartViewport(Transform mainPlayerTransform)
        {
            _mainPlayerTransform = mainPlayerTransform != null ? mainPlayerTransform : _mainPlayerTransform;
            CacheMainPlayerRenderer();

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (!useCameraBounds || gameplayCamera == null || !gameplayCamera.orthographic)
            {
                ResetTargetToCurrentPosition();
                return;
            }

            Vector3 anchorPosition = GetMainVisualCenter();
            float targetWorldY = ViewportToWorldPoint(new Vector2(0.5f, runStartViewportY)).y;
            float deltaY = targetWorldY - anchorPosition.y;
            transform.position = new Vector3(transform.position.x, transform.position.y + deltaY, transform.position.z);

            ResetTargetToCurrentPosition();
            ClampMainBottomToSafeZone();
        }

        public void ResetTargetToCurrentPosition()
        {
            _targetX = transform.position.x;
            _dragOffset = 0f;
            _hasActivePointer = false;
            _activeFingerId = -1;
        }

        // ===================== INPUT =====================

        private void ReadInput()
        {
            if (TryReadTouchInput())
                return;

#if UNITY_EDITOR || UNITY_STANDALONE
            ReadMouseInput();
#endif
        }

        private bool TryReadTouchInput()
        {
            if (InputTouch.activeTouches.Count <= 0)
            {
                _hasActivePointer = false;
                _activeFingerId = -1;
                return false;
            }

            foreach (InputTouch touch in InputTouch.activeTouches)
            {
                if (!_hasActivePointer)
                {
                    if (touch.phase != InputTouchPhase.Began)
                        continue;

                    if (ignoreTouchesOverUi && EventSystem.current != null &&
                        EventSystem.current.IsPointerOverGameObject(touch.touchId))
                        continue;

                    _hasActivePointer = true;
                    _activeFingerId = touch.touchId;

                    float startWorldX = ScreenToWorldX(touch.screenPosition);
                    _dragOffset = transform.position.x - startWorldX;
                }

                if (touch.touchId != _activeFingerId)
                    continue;

                if (touch.phase == InputTouchPhase.Ended || touch.phase == InputTouchPhase.Canceled)
                {
                    _hasActivePointer = false;
                    _activeFingerId = -1;
                    return true;
                }

                float currentWorldX = ScreenToWorldX(touch.screenPosition);
                _targetX = currentWorldX + _dragOffset;

                return true;
            }

            return false;
        }

        private void ReadMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (ignoreTouchesOverUi && EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject())
                    return;

                _hasActivePointer = true;

                float startWorldX = ScreenToWorldX(mouse.position.ReadValue());
                _dragOffset = transform.position.x - startWorldX;
            }

            if (mouse.leftButton.isPressed && _hasActivePointer)
            {
                float currentWorldX = ScreenToWorldX(mouse.position.ReadValue());
                _targetX = currentWorldX + _dragOffset;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _hasActivePointer = false;
            }
        }

        // ===================== CORE =====================

        private float ScreenToWorldX(Vector2 screenPos)
        {
            if (gameplayCamera == null)
                gameplayCamera = Camera.main;

            float distance = Mathf.Abs(transform.position.z - gameplayCamera.transform.position.z);

            Vector3 world = gameplayCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, distance)
            );

            return world.x;
        }

        private void Move()
        {
            float minX = GetMinX();
            float maxX = GetMaxX();

            if (minX > maxX)
            {
                float centerX = (minX + maxX) * 0.5f;
                minX = centerX;
                maxX = centerX;
            }

            float clampedX = Mathf.Clamp(_targetX, minX, maxX);

            transform.position = new Vector3(
                clampedX,
                transform.position.y,
                transform.position.z
            );

            ClampMainBottomToSafeZone();
        }

        private float GetMinX()
        {
            float anchorOffsetX = GetFireLineAnchorOffsetX();

            if (!useCameraBounds || gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return -horizontalClamp + Mathf.Max(0f, playerHorizontalWorldPadding) - anchorOffsetX;
            }

            float viewportPadding = Mathf.Clamp01(playerHorizontalViewportPadding);
            float leftWorld = ViewportToWorldPoint(new Vector2(viewportPadding, 0.5f)).x;
            return leftWorld + Mathf.Max(0f, playerHorizontalWorldPadding) - anchorOffsetX;
        }

        private float GetMaxX()
        {
            float anchorOffsetX = GetFireLineAnchorOffsetX();

            if (!useCameraBounds || gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return horizontalClamp - Mathf.Max(0f, playerHorizontalWorldPadding) - anchorOffsetX;
            }

            float viewportPadding = Mathf.Clamp01(playerHorizontalViewportPadding);
            float rightWorld = ViewportToWorldPoint(new Vector2(1f - viewportPadding, 0.5f)).x;
            return rightWorld - Mathf.Max(0f, playerHorizontalWorldPadding) - anchorOffsetX;
        }

        private void ClampMainBottomToSafeZone()
        {
            if (!keepMainAboveBottomSafeZone || !useCameraBounds || gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return;
            }

            float mainBottom = GetMainVisualBottom();
            float safeBottom = ViewportToWorldPoint(new Vector2(0.5f, bottomReservedViewport)).y + bottomWorldPadding;
            if (mainBottom >= safeBottom)
            {
                return;
            }

            float deltaY = safeBottom - mainBottom;
            transform.position = new Vector3(transform.position.x, transform.position.y + deltaY, transform.position.z);
            ResetTargetToCurrentPosition();
        }

        private Vector3 GetMainVisualCenter()
        {
            CacheMainPlayerRenderer();

            if (_mainPlayerRenderer != null && _mainPlayerRenderer.enabled)
            {
                return _mainPlayerRenderer.bounds.center;
            }

            if (_mainPlayerTransform != null)
            {
                return _mainPlayerTransform.position;
            }

            return transform.position;
        }

        private float GetMainVisualBottom()
        {
            CacheMainPlayerRenderer();

            if (_mainPlayerRenderer != null && _mainPlayerRenderer.enabled)
            {
                return _mainPlayerRenderer.bounds.min.y;
            }

            if (movementBoundsCollider == null)
            {
                movementBoundsCollider = GetComponentInChildren<Collider2D>();
            }

            if (movementBoundsCollider != null && movementBoundsCollider.enabled)
            {
                return movementBoundsCollider.bounds.min.y;
            }

            return transform.position.y;
        }

        private void CacheMainPlayerRenderer()
        {
            if (_mainPlayerRenderer != null || _mainPlayerTransform == null)
            {
                return;
            }

            _mainPlayerRenderer = _mainPlayerTransform.GetComponentInChildren<Renderer>();
        }

        private float GetFireLineAnchorOffsetX()
        {
            CacheFireLineAnchor();
            Transform anchor = _fireLineAnchorTransform != null ? _fireLineAnchorTransform : transform;
            return anchor.position.x - transform.position.x;
        }

        private void CacheFireLineAnchor()
        {
            CacheMainPlayerReference();

            if (_fireLineAnchorTransform != null || _mainPlayerTransform == null)
            {
                return;
            }

            _fireLineAnchorTransform = _mainPlayerTransform.Find("FirePoint");
            if (_fireLineAnchorTransform == null)
            {
                _fireLineAnchorTransform = _mainPlayerTransform;
            }
        }

        private void CacheMainPlayerReference()
        {
            if (_mainPlayerTransform == null)
            {
                if (movementBoundsCollider == null)
                {
                    movementBoundsCollider = GetComponentInChildren<Collider2D>();
                }

                if (movementBoundsCollider != null)
                {
                    _mainPlayerTransform = movementBoundsCollider.transform;
                }
            }

            CacheMainPlayerRenderer();
        }

        private Vector3 ViewportToWorldPoint(Vector2 viewportPoint)
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (gameplayCamera == null)
            {
                return transform.position;
            }

            float distance = Mathf.Abs(transform.position.z - gameplayCamera.transform.position.z);
            return gameplayCamera.ViewportToWorldPoint(new Vector3(viewportPoint.x, viewportPoint.y, distance));
        }
    }
}
