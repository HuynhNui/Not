using UnityEngine;

namespace _Project.Scripts.Systems.UISystem
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;
        [Tooltip("Extra inset in pixels: left, bottom, right, top.")]
        [SerializeField] private Vector4 padding;

        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation;
        private Canvas _canvas;
        private bool _isApplying;

        private void Awake()
        {
            ResolveTarget();
        }

        private void OnEnable()
        {
            InvalidateCache();
            ApplySafeArea(force: true);
        }

        private void Update()
        {
            ApplySafeArea(force: false);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_isApplying)
            {
                return;
            }

            ApplySafeArea(force: true);
        }

        private void OnValidate()
        {
            InvalidateCache();
            ApplySafeArea(force: true);
        }

        private void ResolveTarget()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }

            if (_canvas == null && target != null)
            {
                _canvas = target.GetComponentInParent<Canvas>();
            }
        }

        private void InvalidateCache()
        {
            _lastSafeArea = default;
            _lastScreenSize = default;
            _lastOrientation = default;
            _canvas = null;
        }

        private void ApplySafeArea(bool force)
        {
            if (_isApplying)
            {
                return;
            }

            ResolveTarget();

            if (target == null
                || (_canvas != null && _canvas.renderMode == RenderMode.WorldSpace)
                || Screen.width <= 0
                || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            ScreenOrientation orientation = Screen.orientation;

            if (!force
                && safeArea == _lastSafeArea
                && screenSize == _lastScreenSize
                && orientation == _lastOrientation)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;
            _lastOrientation = orientation;

            if (!TryCalculateAnchors(
                    safeArea,
                    screenSize,
                    applyTop,
                    applyBottom,
                    applyLeft,
                    applyRight,
                    out Vector2 anchorMin,
                    out Vector2 anchorMax))
            {
                return;
            }

            Vector2 offsetMin = new Vector2(padding.x, padding.y);
            Vector2 offsetMax = new Vector2(-padding.z, -padding.w);

            bool needsAnchorUpdate = !Approximately(target.anchorMin, anchorMin)
                || !Approximately(target.anchorMax, anchorMax);
            bool needsOffsetUpdate = !Approximately(target.offsetMin, offsetMin)
                || !Approximately(target.offsetMax, offsetMax);

            if (!needsAnchorUpdate && !needsOffsetUpdate)
            {
                return;
            }

            _isApplying = true;
            try
            {
                if (!Approximately(target.anchorMin, anchorMin))
                {
                    target.anchorMin = anchorMin;
                }

                if (!Approximately(target.anchorMax, anchorMax))
                {
                    target.anchorMax = anchorMax;
                }

                if (!Approximately(target.offsetMin, offsetMin))
                {
                    target.offsetMin = offsetMin;
                }

                if (!Approximately(target.offsetMax, offsetMax))
                {
                    target.offsetMax = offsetMax;
                }
            }
            finally
            {
                _isApplying = false;
            }
        }

        public static bool TryCalculateAnchors(
            Rect safeArea,
            Vector2Int screenSize,
            bool applyTop,
            bool applyBottom,
            bool applyLeft,
            bool applyRight,
            out Vector2 anchorMin,
            out Vector2 anchorMax)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;

            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                return false;
            }

            float minX = Mathf.Clamp(safeArea.xMin, 0f, screenSize.x);
            float minY = Mathf.Clamp(safeArea.yMin, 0f, screenSize.y);
            float maxX = Mathf.Clamp(safeArea.xMax, minX, screenSize.x);
            float maxY = Mathf.Clamp(safeArea.yMax, minY, screenSize.y);

            if (applyLeft)
            {
                anchorMin.x = minX / screenSize.x;
            }

            if (applyBottom)
            {
                anchorMin.y = minY / screenSize.y;
            }

            if (applyRight)
            {
                anchorMax.x = maxX / screenSize.x;
            }

            if (applyTop)
            {
                anchorMax.y = maxY / screenSize.y;
            }

            return true;
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y);
        }
    }
}
