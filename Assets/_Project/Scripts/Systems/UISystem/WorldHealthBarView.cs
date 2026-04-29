using UnityEngine;

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
        [SerializeField] private bool compensateParentScale = true;
        [SerializeField] private bool alignToWorld = true;
        [SerializeField] private Color fullColor = new Color(0.19f, 0.86f, 0.33f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.9f, 0.2f, 0.18f, 1f);

        private Vector3 _localOffset;

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
        }

        private void ApplyCompensatedScale()
        {
            if (transform.parent == null)
            {
                transform.localScale = Vector3.one;
                return;
            }

            Vector3 parentScale = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                InverseScale(parentScale.x),
                InverseScale(parentScale.y),
                1f);
        }

        private static float InverseScale(float value)
        {
            return Mathf.Abs(value) <= 0.0001f ? 1f : 1f / value;
        }
    }
}
