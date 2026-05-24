using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Systems.UISystem
{
    /// <summary>
    /// Prefab-driven world-space health bar view.
    /// Visual hierarchy is authored in Unity; this component only updates fill and keeps placement stable.
    /// </summary>
    public sealed class WorldHealthBarView : MonoBehaviour
    {
        [SerializeField] private Transform fillTransform;
        [SerializeField] private SpriteRenderer fillRenderer;
        [SerializeField] private Image fillImage;
        [SerializeField] private bool compensateParentScale = true;
        [SerializeField] private bool alignToWorld = true;
        [SerializeField] private Color fullColor = new Color(0.19f, 0.86f, 0.33f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.9f, 0.2f, 0.18f, 1f);

        private Vector3 _localOffset;
        private Vector3 _baseLocalScale;

        private void Awake()
        {
            _baseLocalScale = transform.localScale;

            if (fillImage == null)
            {
                fillImage = FindFillImage();
            }
        }

        private void LateUpdate()
        {
            transform.localPosition = _localOffset;

            if (alignToWorld)
            {
                transform.localRotation = Quaternion.identity;
            }

            if (compensateParentScale)
            {
                ApplyCompensatedScale();
            }
        }

        public void Configure(Vector3 localOffset)
        {
            _localOffset = localOffset;
            transform.localPosition = _localOffset;
        }

        public void SetNormalized(float value)
        {
            float clampedValue = Mathf.Clamp01(value);

            if (fillTransform != null)
            {
                fillTransform.localScale = new Vector3(clampedValue, 1f, 1f);
            }

            if (fillRenderer != null)
            {
                fillRenderer.color = Color.Lerp(emptyColor, fullColor, clampedValue);
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = clampedValue;
                fillImage.color = Color.Lerp(emptyColor, fullColor, clampedValue);
            }
        }

        private void ApplyCompensatedScale()
        {
            if (transform.parent == null)
            {
                transform.localScale = _baseLocalScale;
                return;
            }

            Vector3 parentScale = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                _baseLocalScale.x * InverseScale(parentScale.x),
                _baseLocalScale.y * InverseScale(parentScale.y),
                _baseLocalScale.z);
        }

        private static float InverseScale(float value)
        {
            return Mathf.Abs(value) <= 0.0001f ? 1f : 1f / value;
        }

        private Image FindFillImage()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                Image image = images[index];
                if (image != null && image.name == "Fill")
                {
                    return image;
                }
            }

            return images.Length > 0 ? images[0] : null;
        }
    }
}
