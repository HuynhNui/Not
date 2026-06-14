using UnityEngine;

namespace _Project.Scripts.Systems.UISystem
{
    /// <summary>
    /// Keeps a fullscreen UI root inside the device safe area.
    /// </summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform target;

        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private bool _isApplying;

        private void Awake()
        {
            ResolveTarget();
        }

        private void OnEnable()
        {
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

        private void ResolveTarget()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }
        }

        private void ApplySafeArea(bool force)
        {
            if (_isApplying)
            {
                return;
            }

            ResolveTarget();

            if (target == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            if (!force && safeArea == _lastSafeArea && screenSize == _lastScreenSize)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            bool needsAnchorUpdate = !Approximately(target.anchorMin, anchorMin)
                || !Approximately(target.anchorMax, anchorMax);
            bool needsOffsetUpdate = !Approximately(target.offsetMin, Vector2.zero)
                || !Approximately(target.offsetMax, Vector2.zero);

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

                if (!Approximately(target.offsetMin, Vector2.zero))
                {
                    target.offsetMin = Vector2.zero;
                }

                if (!Approximately(target.offsetMax, Vector2.zero))
                {
                    target.offsetMax = Vector2.zero;
                }
            }
            finally
            {
                _isApplying = false;
            }
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y);
        }
    }
}
