using TMPro;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Small world-space text popup used for bullet damage feedback.
    /// </summary>
    public sealed class DamageTextView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro text;
        [SerializeField] private TextMeshPro outlineText;
        [SerializeField] private float lifetime = 0.55f;
        [SerializeField] private float riseDistance = 0.35f;
        [SerializeField] private float baseScale = 0.75f;
        [SerializeField] private float popScale = 1.3f;
        [SerializeField] private float fadeStartNormalized = 0.35f;
        [SerializeField] private float outlineWidth = 0.25f;
        [SerializeField] private Vector2 outlineLocalOffset = new Vector2(-0.08f, -0.06f);

        private Vector3 _startPosition;
        private Color _baseColor;
        private float _elapsed;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            EnsureText();
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float duration = Mathf.Max(0.01f, lifetime);
            float normalized = Mathf.Clamp01(_elapsed / duration);
            float easedRise = 1f - Mathf.Pow(1f - normalized, 2f);

            transform.position = _startPosition + Vector3.up * (riseDistance * easedRise);

            float scale = baseScale * Mathf.Lerp(popScale, 1f, normalized);
            transform.localScale = new Vector3(scale, scale, 1f);

            if (text != null)
            {
                float fadeStart = Mathf.Clamp01(fadeStartNormalized);
                float alpha = normalized <= fadeStart
                    ? 1f
                    : 1f - Mathf.InverseLerp(fadeStart, 1f, normalized);
                text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _baseColor.a * alpha);

                if (outlineText != null)
                {
                    outlineText.color = new Color(0f, 0f, 0f, alpha);
                }
            }

            if (normalized >= 1f)
            {
                Stop();
            }
        }

        public void Show(
            string value,
            Vector3 position,
            Color color,
            int sortingLayerId,
            int sortingOrder)
        {
            EnsureText();

            _startPosition = position;
            _baseColor = color;
            _elapsed = 0f;
            _isPlaying = true;

            transform.position = position;
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(baseScale * popScale, baseScale * popScale, 1f);

            if (text != null)
            {
                text.text = value;
                text.color = color;
                text.sortingLayerID = sortingLayerId;
                text.sortingOrder = sortingOrder;
            }

            if (outlineText != null)
            {
                outlineText.text = value;
                outlineText.color = Color.black;
                outlineText.sortingLayerID = sortingLayerId;
                outlineText.sortingOrder = sortingOrder - 1;
                ApplyOutlineScale();
            }

            gameObject.SetActive(true);
        }

        public void ConfigureStyle(TMP_FontAsset fontAsset, float outline)
        {
            EnsureText();

            outlineWidth = Mathf.Max(0f, outline);
            if (text == null)
            {
                return;
            }

            if (fontAsset != null)
            {
                text.font = fontAsset;
                if (outlineText != null)
                {
                    outlineText.font = fontAsset;
                }
            }

            ConfigureText(text);
            ConfigureText(outlineText);
            ApplyOutlineScale();
        }

        public void Stop()
        {
            _isPlaying = false;
            gameObject.SetActive(false);
        }

        private void EnsureText()
        {
            text = GetComponent<TextMeshPro>();
            if (text == null)
            {
                text = gameObject.AddComponent<TextMeshPro>();
            }

            if (outlineText == null)
            {
                Transform outlineTransform = transform.Find("DamageTextOutline");
                if (outlineTransform != null)
                {
                    outlineText = outlineTransform.GetComponent<TextMeshPro>();
                }
            }

            if (outlineText == null)
            {
                GameObject outlineObject = new GameObject("DamageTextOutline");
                outlineObject.transform.SetParent(transform, false);
                outlineObject.transform.localPosition = Vector3.zero;
                outlineObject.transform.localRotation = Quaternion.identity;
                outlineText = outlineObject.AddComponent<TextMeshPro>();
            }

            ConfigureText(text);
            ConfigureText(outlineText);
            outlineText.color = Color.black;
            ApplyOutlineScale();
        }

        private void ConfigureText(TextMeshPro targetText)
        {
            if (targetText == null)
            {
                return;
            }

            targetText.alignment = TextAlignmentOptions.Center;
            targetText.fontStyle = FontStyles.Bold;
            targetText.fontSize = 3f;
            targetText.textWrappingMode = TextWrappingModes.NoWrap;
            targetText.overflowMode = TextOverflowModes.Overflow;
            targetText.outlineWidth = 0f;
        }

        private void ApplyOutlineScale()
        {
            if (outlineText == null)
            {
                return;
            }

            float scale = 1f + outlineWidth;
            outlineText.transform.localPosition = new Vector3(outlineLocalOffset.x, outlineLocalOffset.y, 0f);
            outlineText.transform.localRotation = Quaternion.identity;
            outlineText.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
