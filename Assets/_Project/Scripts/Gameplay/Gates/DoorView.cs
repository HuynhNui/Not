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
        [SerializeField] private TextMeshPro worldLabelText;
        [SerializeField] private TextMesh legacyLabelText;
        [SerializeField] private Color positiveColor = new Color(0.2f, 0.85f, 0.45f, 0.95f);
        [SerializeField] private Color negativeColor = new Color(0.95f, 0.3f, 0.25f, 0.95f);
        [SerializeField] private Color neutralColor = new Color(0.35f, 0.65f, 1f, 0.95f);
        [SerializeField] private bool useCompactLabel = true;
        [SerializeField] private float minFontSize = 8f;
        [SerializeField] private float maxFontSize = 28f;
        [SerializeField] private Vector2 worldLabelPadding = new Vector2(0.06f, 0.14f);
        [SerializeField] private float worldLabelZOffset = -0.1f;
        [SerializeField] private int labelSortingOrderOffset = 5;

        private float _targetWorldWidth = 1.6f;
        private float _targetWorldHeight = 2.4f;

        public void Init()
        {
            if (frameRenderer == null)
            {
                frameRenderer = GetComponent<SpriteRenderer>();
            }

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
                worldLabelText.fontSizeMin = Mathf.Max(1f, minFontSize);
                worldLabelText.fontSizeMax = Mathf.Max(worldLabelText.fontSizeMin, maxFontSize);
                worldLabelText.textWrappingMode = TextWrappingModes.NoWrap;
                worldLabelText.overflowMode = TextOverflowModes.Overflow;
                ApplyWorldLabelBounds();
                ApplyWorldLabelSorting();
                worldLabelText.ForceMeshUpdate();
            }

            if (frameRenderer != null)
            {
                frameRenderer.color = GetFrameColor(config.OperationType);
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
                    ApplyWorldLabelSorting();
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
                ApplyWorldLabelSorting();
                return;
            }

            GameObject labelObject = new GameObject(RuntimeLabelName);
            Transform labelObjectTransform = labelObject.transform;
            labelObjectTransform.SetParent(transform, false);
            labelObjectTransform.localPosition = new Vector3(0f, 0f, worldLabelZOffset);
            labelObjectTransform.localRotation = Quaternion.identity;
            labelObjectTransform.localScale = Vector3.one;

            worldLabelText = labelObject.AddComponent<TextMeshPro>();
            ApplyWorldLabelSorting();
            ApplyWorldLabelBounds();
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
            float width = Mathf.Max(0.1f, (_targetWorldWidth - worldLabelPadding.x * 2f) / parentScaleX);
            float height = Mathf.Max(0.1f, (_targetWorldHeight - worldLabelPadding.y * 2f) / parentScaleY);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.localPosition = new Vector3(0f, 0f, worldLabelZOffset);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
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
