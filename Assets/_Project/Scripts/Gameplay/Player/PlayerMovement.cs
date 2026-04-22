using UnityEngine;
using UnityEngine.EventSystems;
using _Project.Scripts.Gameplay.Units;

namespace _Project.Scripts.Gameplay.Player
{
    public sealed class PlayerMovement : UnitMovement
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private float horizontalClamp = 3.5f;
        [SerializeField] private bool ignoreTouchesOverUi = true;

        private bool _hasActivePointer;
        private int _activeFingerId = -1;

        private float _targetX;
        private float _dragOffset;

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

        protected override void Update()
        {
            ReadInput();
            Move();
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
            if (Input.touchCount <= 0)
            {
                _hasActivePointer = false;
                _activeFingerId = -1;
                return false;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // START TOUCH
                if (!_hasActivePointer)
                {
                    if (touch.phase != TouchPhase.Began)
                        continue;

                    if (ignoreTouchesOverUi && EventSystem.current != null &&
                        EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                        continue;

                    _hasActivePointer = true;
                    _activeFingerId = touch.fingerId;

                    float startWorldX = ScreenToWorldX(touch.position);
                    _dragOffset = transform.position.x - startWorldX;
                }

                if (touch.fingerId != _activeFingerId)
                    continue;

                // END TOUCH
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _hasActivePointer = false;
                    _activeFingerId = -1;
                    return true;
                }

                // DRAG
                float currentWorldX = ScreenToWorldX(touch.position);
                _targetX = currentWorldX + _dragOffset;

                return true;
            }

            return false;
        }

        private void ReadMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (ignoreTouchesOverUi && EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject())
                    return;

                _hasActivePointer = true;

                float startWorldX = ScreenToWorldX(Input.mousePosition);
                _dragOffset = transform.position.x - startWorldX;
            }

            if (Input.GetMouseButton(0) && _hasActivePointer)
            {
                float currentWorldX = ScreenToWorldX(Input.mousePosition);
                _targetX = currentWorldX + _dragOffset;
            }

            if (Input.GetMouseButtonUp(0))
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
            float clampedX = Mathf.Clamp(_targetX, -horizontalClamp, horizontalClamp);

            transform.position = new Vector3(
                clampedX,
                transform.position.y,
                transform.position.z
            );
        }
    }
}