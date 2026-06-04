using TMPro;
using UnityEngine;

namespace _Project.Scripts.Systems.UISystem
{
    /// <summary>
    /// Adapts the gameplay HUD to the current safe-area width.
    /// </summary>
    public sealed class ResponsiveHudLayout : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform topLeftHud;
        [SerializeField] private RectTransform topRightHud;
        [SerializeField] private RectTransform scoreFrame;

        [Header("HUD Items")]
        [SerializeField] private RectTransform timeFrame;
        [SerializeField] private RectTransform killFrame;
        [SerializeField] private RectTransform coinFrame;
        [SerializeField] private RectTransform pauseButtonRect;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI killText;
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Breakpoints")]
        [SerializeField] private float wideBreakpoint = 980f;
        [SerializeField] private float mediumBreakpoint = 760f;

        [Header("Layout")]
        [SerializeField] private float maxContentWidth = 1080f;
        [SerializeField] private float sidePadding = 36f;
        [SerializeField] private float minimumSidePadding = 18f;
        [SerializeField] private float topPadding = 48f;
        [SerializeField] private float rowGap = 10f;

        [Header("Scale")]
        [SerializeField] private float wideScale = 1.12f;
        [SerializeField] private float mediumScale = 1f;
        [SerializeField] private float narrowScale = 0.9f;

        private static readonly Vector2 TimeFrameSize = new Vector2(210f, 68f);
        private static readonly Vector2 KillFrameSize = new Vector2(180f, 58f);
        private static readonly Vector2 ScoreFrameSize = new Vector2(300f, 104f);
        private static readonly Vector2 CoinFrameSize = new Vector2(180f, 68f);
        private static readonly Vector2 PauseButtonSize = new Vector2(72f, 72f);

        private const float ItemGap = 8f;

        private RectTransform _root;
        private Vector2 _lastRootSize;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private HudMode _lastMode = (HudMode)(-1);

        private void Awake()
        {
            _root = transform as RectTransform;
        }

        private void OnEnable()
        {
            _root = transform as RectTransform;
            ApplyLayout(true);
        }

        private void LateUpdate()
        {
            ApplyLayout(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout(false);
        }

        public void RefreshNow()
        {
            ApplyLayout(true);
        }

        private void ApplyLayout(bool force)
        {
            if (_root == null)
            {
                _root = transform as RectTransform;
            }

            if (_root == null || contentRoot == null)
            {
                return;
            }

            Vector2 rootSize = _root.rect.size;
            if (rootSize.x <= 0f || rootSize.y <= 0f)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            HudMode mode = GetMode(rootSize.x);

            if (!force
                && rootSize == _lastRootSize
                && safeArea == _lastSafeArea
                && screenSize == _lastScreenSize
                && mode == _lastMode)
            {
                return;
            }

            _lastRootSize = rootSize;
            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;
            _lastMode = mode;

            ApplyTextAutoSize();
            ApplyBaseChildLayout();
            ApplyModeLayout(rootSize.x, mode);
        }

        private HudMode GetMode(float safeAreaWidth)
        {
            if (safeAreaWidth < mediumBreakpoint)
            {
                return HudMode.Narrow;
            }

            if (safeAreaWidth < wideBreakpoint)
            {
                return HudMode.Medium;
            }

            return HudMode.Wide;
        }

        private void ApplyBaseChildLayout()
        {
            ConfigureTopLeftStack();
            ConfigureTopRightStack();
            ConfigureScoreFrame();
        }

        private void ApplyModeLayout(float rootWidth, HudMode mode)
        {
            float contentWidth = Mathf.Min(Mathf.Max(1f, rootWidth), Mathf.Max(1f, maxContentWidth));
            float inset = Mathf.Clamp(contentWidth * 0.045f, minimumSidePadding, sidePadding);
            float scale = mode switch
            {
                HudMode.Medium => mediumScale,
                HudMode.Narrow => narrowScale,
                _ => wideScale
            };

            float topLeftHeight = TimeFrameSize.y + ItemGap + KillFrameSize.y;
            float rightGroupWidth = CoinFrameSize.x + ItemGap + PauseButtonSize.x;
            float rightGroupHeight = Mathf.Max(CoinFrameSize.y, PauseButtonSize.y);
            float contentHeight = mode == HudMode.Narrow
                ? topLeftHeight * scale + rowGap + ScoreFrameSize.y * scale
                : Mathf.Max(topLeftHeight, Mathf.Max(rightGroupHeight, ScoreFrameSize.y)) * scale;

            ConfigureRect(
                contentRoot,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -topPadding),
                new Vector2(contentWidth, contentHeight));

            SetScaledTopGroup(topLeftHud, new Vector2(0f, 1f), new Vector2(inset, 0f), new Vector2(TimeFrameSize.x, topLeftHeight), scale);
            SetScaledTopGroup(topRightHud, new Vector2(1f, 1f), new Vector2(-inset, 0f), new Vector2(rightGroupWidth, rightGroupHeight), scale);

            if (scoreFrame != null)
            {
                Vector2 scorePosition = mode == HudMode.Narrow
                    ? new Vector2(0f, -(topLeftHeight * scale + rowGap))
                    : Vector2.zero;

                ConfigureRect(
                    scoreFrame,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    scorePosition,
                    ScoreFrameSize);
                scoreFrame.localScale = Vector3.one * scale;
            }
        }

        private static void SetScaledTopGroup(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, float scale)
        {
            if (rectTransform == null)
            {
                return;
            }

            ConfigureRect(rectTransform, anchor, anchor, anchor, anchoredPosition, size);
            rectTransform.localScale = Vector3.one * scale;
        }

        private void ConfigureTopLeftStack()
        {
            if (timeFrame != null)
            {
                ConfigureRect(timeFrame, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, TimeFrameSize);
            }

            if (killFrame != null)
            {
                ConfigureRect(
                    killFrame,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, -(TimeFrameSize.y + ItemGap)),
                    KillFrameSize);
            }
        }

        private void ConfigureTopRightStack()
        {
            if (pauseButtonRect != null)
            {
                ConfigureRect(
                    pauseButtonRect,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    Vector2.zero,
                    PauseButtonSize);
            }

            if (coinFrame != null)
            {
                ConfigureRect(
                    coinFrame,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(-(PauseButtonSize.x + ItemGap), 0f),
                    CoinFrameSize);
            }
        }

        private void ConfigureScoreFrame()
        {
            if (scoreFrame == null)
            {
                return;
            }

            ConfigureRect(
                scoreFrame,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                ScoreFrameSize);
        }

        private void ApplyTextAutoSize()
        {
            ConfigureAutoSize(timeText, 28f, 36f);
            ConfigureAutoSize(killText, 24f, 34f);
            ConfigureAutoSize(coinText, 24f, 34f);
            ConfigureAutoSize(scoreText, 42f, 58f);
        }

        private static void ConfigureAutoSize(TextMeshProUGUI text, float minSize, float maxSize)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = Mathf.Max(minSize, maxSize);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        private static void ConfigureRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.localRotation = Quaternion.identity;
        }

        private enum HudMode
        {
            Wide,
            Medium,
            Narrow
        }
    }
}
