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
        [SerializeField] private float horizontalClamp = 3.5f;
        [SerializeField] private bool useCameraBounds = true;
        [SerializeField] private float edgePadding = 0.35f;
        [SerializeField] private bool ignoreTouchesOverUi = true;

        private bool _hasActivePointer;
        private int _activeFingerId = -1;

        private float _targetX;
        private float _dragOffset;
        private bool _inputEnabled = true;

        public override void Init()
        {
            gameplayCamera ??= Camera.main;
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
            float clampedX = Mathf.Clamp(_targetX, GetMinX(), GetMaxX());

            transform.position = new Vector3(
                clampedX,
                transform.position.y,
                transform.position.z
            );
        }

        private float GetMinX()
        {
            if (!useCameraBounds || gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return -horizontalClamp;
            }

            float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
            return gameplayCamera.transform.position.x - halfWidth + edgePadding;
        }

        private float GetMaxX()
        {
            if (!useCameraBounds || gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return horizontalClamp;
            }

            float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
            return gameplayCamera.transform.position.x + halfWidth - edgePadding;
        }
    }
}
