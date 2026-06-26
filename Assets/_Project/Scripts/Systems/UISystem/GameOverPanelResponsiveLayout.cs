using UnityEngine;

namespace _Project.Scripts.Systems.UISystem
{
    [ExecuteAlways]
    public sealed class GameOverPanelResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform panelCard;
        [SerializeField] private Vector2 referencePanelSize = new Vector2(980f, 1780f);
        [SerializeField] private float safeAreaWidthRatio = 0.92f;
        [SerializeField] private float safeAreaHeightRatio = 0.96f;
        [SerializeField] private float maximumScale = 1.03f;
        [SerializeField] private float minimumScale = 0.62f;

        private RectTransform _root;
        private Vector2 _lastRootSize;
        private Vector2 _lastReferenceSize;
        private float _lastScale = -1f;

        private void Awake()
        {
            ResolveRoot();
            ApplyLayout(true);
        }

        private void OnEnable()
        {
            ResolveRoot();
            ApplyLayout(true);
        }

        private void LateUpdate()
        {
            ApplyLayout(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout(true);
        }

        public void RefreshNow()
        {
            ApplyLayout(true);
        }

        private void ResolveRoot()
        {
            if (_root == null)
            {
                _root = transform as RectTransform;
            }
        }

        private void ApplyLayout(bool force)
        {
            ResolveRoot();

            if (_root == null || panelCard == null)
            {
                return;
            }

            Vector2 rootSize = _root.rect.size;
            if (rootSize.x <= 1f || rootSize.y <= 1f)
            {
                return;
            }

            Vector2 safeReference = new Vector2(
                Mathf.Max(1f, referencePanelSize.x),
                Mathf.Max(1f, referencePanelSize.y));
            float widthScale = rootSize.x * Mathf.Clamp01(safeAreaWidthRatio) / safeReference.x;
            float heightScale = rootSize.y * Mathf.Clamp01(safeAreaHeightRatio) / safeReference.y;
            float scale = Mathf.Clamp(
                Mathf.Min(widthScale, heightScale),
                Mathf.Max(0.1f, minimumScale),
                Mathf.Max(minimumScale, maximumScale));

            if (!force
                && rootSize == _lastRootSize
                && safeReference == _lastReferenceSize
                && Mathf.Approximately(scale, _lastScale))
            {
                return;
            }

            _lastRootSize = rootSize;
            _lastReferenceSize = safeReference;
            _lastScale = scale;

            panelCard.anchorMin = new Vector2(0.5f, 0.5f);
            panelCard.anchorMax = new Vector2(0.5f, 0.5f);
            panelCard.pivot = new Vector2(0.5f, 0.5f);
            panelCard.anchoredPosition = Vector2.zero;
            panelCard.sizeDelta = safeReference;
            panelCard.localScale = Vector3.one * scale;
        }
    }
}
