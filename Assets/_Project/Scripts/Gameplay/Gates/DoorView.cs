using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Gates
{
    /// <summary>
    /// 2D door presentation: sprite frame + world-space TMP label in a fixed rect.
    /// Gameplay pivot stays on the door root; text size does not affect lane position.
    /// </summary>
    public sealed class DoorView : MonoBehaviour
    {
        private const string RuntimeLabelName = "GateLabelTMP";

        [SerializeField] private SpriteRenderer frameRenderer;
        [SerializeField] private GateSpriteLibrary gateSpriteLibrary;
        [SerializeField] private TextMeshPro worldLabelText;
        [SerializeField] private TextMesh legacyLabelText;
        [SerializeField] private Color positiveColor = new Color(0.2f, 0.85f, 0.45f, 0.95f);
        [SerializeField] private Color negativeColor = new Color(0.95f, 0.3f, 0.25f, 0.95f);
        [SerializeField] private Color neutralColor = new Color(0.35f, 0.65f, 1f, 0.95f);
        [SerializeField] private bool useCompactLabel = true;
        [SerializeField] private float minFontSize = 0.12f;
        [SerializeField] private float maxFontSize = 1.2f;
        [SerializeField] private Vector2 worldLabelPadding = new Vector2(0.015f, 0.015f);
        [SerializeField] private Vector2 worldLabelRectMin = new Vector2(0.28f, 0.12f);
        [SerializeField] private Vector2 worldLabelRectMax = new Vector2(0.72f, 0.44f);
        [SerializeField] private float worldLabelZOffset = -0.1f;
        [SerializeField] private int labelSortingOrderOffset = 5;

        private float _targetWorldWidth = 1.6f;
        private float _targetWorldHeight = 2.4f;
        private Sprite _fallbackFrameSprite;
        private bool _hasFallbackFrameSprite;

        public void Init()
        {
            if (frameRenderer == null)
            {
                frameRenderer = GetComponent<SpriteRenderer>();
            }

            CacheFallbackFrameSprite();

            if (worldLabelText == null)
            {
                worldLabelText = GetComponentInChildren<TextMeshPro>(true);
            }

            if (legacyLabelText == null)
            {
                legacyLabelText = GetComponentInChildren<TextMesh>(true);
            }

            EnsureWorldLabel();
        }

        private void Awake()
        {
            Init();
        }

        public void Bind(GateConfig config)
        {
            Init();

            if (config == null)
            {
                return;
            }

            if (worldLabelText != null)
            {
                worldLabelText.text = useCompactLabel ? config.GetCompactDisplayText() : config.GetDisplayText();
                worldLabelText.color = Color.white;
                worldLabelText.fontStyle = FontStyles.Bold;
                worldLabelText.alignment = TextAlignmentOptions.Center;
                worldLabelText.enableAutoSizing = true;
                worldLabelText.fontSizeMin = Mathf.Max(0.01f, minFontSize);
                worldLabelText.fontSizeMax = Mathf.Max(worldLabelText.fontSizeMin, maxFontSize);
                worldLabelText.textWrappingMode = TextWrappingModes.Normal;
                worldLabelText.overflowMode = TextOverflowModes.Ellipsis;
                ApplyWorldLabelBounds();
                ApplyWorldLabelSorting();
                worldLabelText.ForceMeshUpdate();
            }

            if (frameRenderer != null)
            {
                if (!TryApplyGateSprite(config))
                {
                    ApplyFallbackFrame(config);
                }
            }
        }

        public void ConfigureWorldBounds(float width, float height)
        {
            _targetWorldWidth = Mathf.Max(0.1f, width);
            _targetWorldHeight = Mathf.Max(0.1f, height);
            ApplyWorldLabelBounds();
        }

        private void EnsureWorldLabel()
        {
            DisableLegacyLabel();

            if (worldLabelText != null)
            {
                if (worldLabelText.GetComponent<TextMesh>() == null)
                {
                    ApplyWorldLabelDefaults();
                    ApplyWorldLabelSorting();
                    ApplyWorldLabelBounds();
                    return;
                }

                worldLabelText = null;
            }

            Transform labelTransform = transform.Find(RuntimeLabelName);
            if (labelTransform != null)
            {
                worldLabelText = labelTransform.GetComponent<TextMeshPro>();
            }

            if (worldLabelText != null)
            {
                ApplyWorldLabelDefaults();
                ApplyWorldLabelSorting();
                ApplyWorldLabelBounds();
                return;
            }

            GameObject labelObject = new GameObject(RuntimeLabelName);
            Transform labelObjectTransform = labelObject.transform;
            labelObjectTransform.SetParent(transform, false);
            labelObjectTransform.localPosition = new Vector3(0f, 0f, worldLabelZOffset);
            labelObjectTransform.localRotation = Quaternion.identity;
            labelObjectTransform.localScale = Vector3.one;

            worldLabelText = labelObject.AddComponent<TextMeshPro>();
            ApplyWorldLabelDefaults();
            ApplyWorldLabelSorting();
            ApplyWorldLabelBounds();
        }

        private void ApplyWorldLabelDefaults()
        {
            if (worldLabelText == null)
            {
                return;
            }

            if (worldLabelText.font == null && TMP_Settings.defaultFontAsset != null)
            {
                worldLabelText.font = TMP_Settings.defaultFontAsset;
            }

            worldLabelText.text = string.IsNullOrEmpty(worldLabelText.text) ? "+1 DMG" : worldLabelText.text;
            worldLabelText.color = Color.white;
            worldLabelText.fontStyle = FontStyles.Bold;
            worldLabelText.alignment = TextAlignmentOptions.Center;
            worldLabelText.enableAutoSizing = true;
            worldLabelText.fontSizeMin = Mathf.Max(0.01f, minFontSize);
            worldLabelText.fontSizeMax = Mathf.Max(worldLabelText.fontSizeMin, maxFontSize);
            worldLabelText.textWrappingMode = TextWrappingModes.Normal;
            worldLabelText.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void DisableLegacyLabel()
        {
            if (legacyLabelText == null)
            {
                return;
            }

            MeshRenderer legacyRenderer = legacyLabelText.GetComponent<MeshRenderer>();
            if (legacyRenderer != null)
            {
                legacyRenderer.enabled = false;
            }

            legacyLabelText.text = string.Empty;
        }

        private void ApplyWorldLabelBounds()
        {
            if (worldLabelText == null)
            {
                return;
            }

            RectTransform rectTransform = worldLabelText.rectTransform;
            Vector3 lossyScale = transform.lossyScale;
            float parentScaleX = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x));
            float parentScaleY = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y));

            Vector2 rectMin = Vector2.Min(Clamp01(worldLabelRectMin), Clamp01(worldLabelRectMax));
            Vector2 rectMax = Vector2.Max(Clamp01(worldLabelRectMin), Clamp01(worldLabelRectMax));
            Vector2 rectSize = new Vector2(
                Mathf.Max(0.01f, rectMax.x - rectMin.x),
                Mathf.Max(0.01f, rectMax.y - rectMin.y));
            Vector2 rectCenter = (rectMin + rectMax) * 0.5f;

            float widthWorld = Mathf.Max(0.1f, _targetWorldWidth * rectSize.x - worldLabelPadding.x * 2f);
            float heightWorld = Mathf.Max(0.1f, _targetWorldHeight * rectSize.y - worldLabelPadding.y * 2f);
            float width = widthWorld / parentScaleX;
            float height = heightWorld / parentScaleY;
            float localX = _targetWorldWidth * (rectCenter.x - 0.5f) / parentScaleX;
            float localY = _targetWorldHeight * (rectCenter.y - 0.5f) / parentScaleY;

            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.localPosition = new Vector3(localX, localY, worldLabelZOffset);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        private static Vector2 Clamp01(Vector2 value)
        {
            return new Vector2(
                Mathf.Clamp01(value.x),
                Mathf.Clamp01(value.y));
        }

        private void ApplyWorldLabelSorting()
        {
            if (worldLabelText == null)
            {
                return;
            }

            Renderer textRenderer = worldLabelText.renderer;
            if (textRenderer == null)
            {
                return;
            }

            if (frameRenderer != null)
            {
                textRenderer.sortingLayerID = frameRenderer.sortingLayerID;
                textRenderer.sortingOrder = frameRenderer.sortingOrder + labelSortingOrderOffset;
                return;
            }

            textRenderer.sortingOrder = labelSortingOrderOffset;
        }

        private void CacheFallbackFrameSprite()
        {
            if (_hasFallbackFrameSprite || frameRenderer == null)
            {
                return;
            }

            _fallbackFrameSprite = frameRenderer.sprite;
            _hasFallbackFrameSprite = true;
        }

        private bool TryApplyGateSprite(GateConfig config)
        {
            if (frameRenderer == null
                || gateSpriteLibrary == null
                || config == null
                || !gateSpriteLibrary.TryGetSprite(config.GateId, out Sprite gateSprite))
            {
                return false;
            }

            frameRenderer.sprite = gateSprite;
            frameRenderer.color = Color.white;
            return true;
        }

        private void ApplyFallbackFrame(GateConfig config)
        {
            if (frameRenderer == null || config == null)
            {
                return;
            }

            if (_hasFallbackFrameSprite)
            {
                frameRenderer.sprite = _fallbackFrameSprite;
            }

            frameRenderer.color = GetFrameColor(config.OperationType);
        }

        private Color GetFrameColor(GateOperationType operationType)
        {
            return operationType switch
            {
                GateOperationType.Add => positiveColor,
                GateOperationType.Multiply => positiveColor,
                GateOperationType.Subtract => negativeColor,
                GateOperationType.Divide => negativeColor,
                _ => neutralColor
            };
        }
    }
}
