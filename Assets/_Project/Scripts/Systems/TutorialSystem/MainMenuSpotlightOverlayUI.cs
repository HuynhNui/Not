using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public sealed class MainMenuSpotlightOverlayUI : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image topDimPanel;
        [SerializeField] private Image bottomDimPanel;
        [SerializeField] private Image leftDimPanel;
        [SerializeField] private Image rightDimPanel;
        [SerializeField] private Image focusHighlightFrame;
        [SerializeField, Range(0f, 1f)] private float defaultOpacity = 0.62f;
        [SerializeField] private Vector2 defaultPadding = new Vector2(14f, 10f);

        private RectTransform _target;
        private readonly Vector3[] _targetCorners = new Vector3[4];
        private readonly Vector3[] _rootCorners = new Vector3[4];
        private float _opacity;
        private Vector2 _padding;

        private void Awake()
        {
            EnsureBuilt();
            Hide();
        }

        private void LateUpdate()
        {
            if (gameObject.activeSelf && _target != null)
            {
                RefreshTarget();
            }
        }

        public void Show(RectTransform target, float opacity = -1f, Vector2 padding = default)
        {
            EnsureBuilt();
            _target = target;
            _opacity = opacity >= 0f ? Mathf.Clamp01(opacity) : defaultOpacity;
            _padding = padding == default ? defaultPadding : padding;
            gameObject.SetActive(true);
            SetOpacity(_opacity);
            RefreshTarget();
        }

        public void Hide()
        {
            _target = null;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void RefreshTarget()
        {
            if (_target == null || root == null)
            {
                return;
            }

            Rect hole = CalculateHoleRect(_target, _padding);
            Rect bounds = root.rect;

            SetPanelRect(topDimPanel, bounds.xMin, hole.yMax, bounds.width, bounds.yMax - hole.yMax);
            SetPanelRect(bottomDimPanel, bounds.xMin, bounds.yMin, bounds.width, hole.yMin - bounds.yMin);
            SetPanelRect(leftDimPanel, bounds.xMin, hole.yMin, hole.xMin - bounds.xMin, hole.height);
            SetPanelRect(rightDimPanel, hole.xMax, hole.yMin, bounds.xMax - hole.xMax, hole.height);
            SetPanelRect(focusHighlightFrame, hole.xMin, hole.yMin, hole.width, hole.height);
        }

        public void EnsureBuilt()
        {
            root ??= transform as RectTransform;
            if (root == null)
            {
                return;
            }

            topDimPanel ??= CreatePanel("TopDimPanel", raycastTarget: true);
            bottomDimPanel ??= CreatePanel("BottomDimPanel", raycastTarget: true);
            leftDimPanel ??= CreatePanel("LeftDimPanel", raycastTarget: true);
            rightDimPanel ??= CreatePanel("RightDimPanel", raycastTarget: true);
            focusHighlightFrame ??= CreatePanel("FocusHighlightFrame", raycastTarget: false);
            focusHighlightFrame.color = new Color(1f, 0.82f, 0.08f, 0.95f);
            Outline outline = focusHighlightFrame.GetComponent<Outline>();
            if (outline == null)
            {
                outline = focusHighlightFrame.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0.05f, 0.49f, 1f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private Rect CalculateHoleRect(RectTransform target, Vector2 padding)
        {
            root.GetWorldCorners(_rootCorners);
            target.GetWorldCorners(_targetCorners);

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int index = 0; index < _targetCorners.Length; index++)
            {
                Vector2 localPoint = root.InverseTransformPoint(_targetCorners[index]);
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            Rect bounds = root.rect;
            min -= padding;
            max += padding;
            min.x = Mathf.Clamp(min.x, bounds.xMin, bounds.xMax);
            min.y = Mathf.Clamp(min.y, bounds.yMin, bounds.yMax);
            max.x = Mathf.Clamp(max.x, bounds.xMin, bounds.xMax);
            max.y = Mathf.Clamp(max.y, bounds.yMin, bounds.yMax);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void SetOpacity(float opacity)
        {
            Color color = new Color(0f, 0f, 0f, opacity);
            SetPanelColor(topDimPanel, color);
            SetPanelColor(bottomDimPanel, color);
            SetPanelColor(leftDimPanel, color);
            SetPanelColor(rightDimPanel, color);
        }

        private void SetPanelColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private Image CreatePanel(string objectName, bool raycastTarget)
        {
            Transform existing = root.Find(objectName);
            GameObject panelObject = existing != null
                ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform));
            panelObject.transform.SetParent(root, false);
            Image image = panelObject.GetComponent<Image>();
            if (image == null)
            {
                image = panelObject.AddComponent<Image>();
            }

            image.raycastTarget = raycastTarget;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return image;
        }

        private static void SetPanelRect(Image image, float x, float y, float width, float height)
        {
            if (image == null)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            float safeWidth = Mathf.Max(0f, width);
            float safeHeight = Mathf.Max(0f, height);
            rect.anchoredPosition = new Vector2(x + safeWidth * 0.5f, y + safeHeight * 0.5f);
            rect.sizeDelta = new Vector2(safeWidth, safeHeight);
            image.enabled = safeWidth > 0.5f && safeHeight > 0.5f;
        }
    }
}
