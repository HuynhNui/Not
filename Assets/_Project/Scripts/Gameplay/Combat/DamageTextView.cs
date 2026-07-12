using System;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    public sealed class DamageTextView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro text;
        [SerializeField] private TextMeshPro outlineText;
        [SerializeField] private float lifetime = 0.7f;
        [SerializeField] private float riseDistance = 0.35f;
        [SerializeField] private float baseScale = 0.75f;
        [SerializeField] private float fontSize = 3.5f;
        [SerializeField] private float outlineWidth = 0.25f;
        [SerializeField] private Vector2 outlineLocalOffset = new Vector2(-0.08f, -0.06f);

        private Vector3 _startPosition;
        private float _timer;
        private Action<DamageTextView> _finished;

        public void Play(
            string value,
            Vector3 position,
            TMP_FontAsset font,
            Color textColor,
            int sortingOrder,
            Action<DamageTextView> finished)
        {
            EnsureTextObjects();

            _finished = finished;
            _timer = 0f;
            _startPosition = position;
            transform.position = position;
            transform.localScale = Vector3.one * baseScale;

            ApplyText(text, value, font, textColor, sortingOrder);
            ApplyText(outlineText, value, font, Color.black, sortingOrder - 1);
            outlineText.transform.localPosition = new Vector3(outlineLocalOffset.x, outlineLocalOffset.y, 0.01f);
            outlineText.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, outlineWidth));

            gameObject.SetActive(true);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = lifetime > 0f ? Mathf.Clamp01(_timer / lifetime) : 1f;
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            transform.position = _startPosition + Vector3.up * (riseDistance * eased);
            transform.localScale = Vector3.one * (baseScale * Mathf.Lerp(1f, 1.12f, Mathf.Sin(t * Mathf.PI)));

            float alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.55f) / 0.45f));
            SetAlpha(text, alpha);
            SetAlpha(outlineText, alpha);

            if (t < 1f)
            {
                return;
            }

            gameObject.SetActive(false);
            _finished?.Invoke(this);
        }

        private void EnsureTextObjects()
        {
            if (text == null)
            {
                text = GetComponent<TextMeshPro>() ?? gameObject.AddComponent<TextMeshPro>();
            }

            if (outlineText == null)
            {
                Transform existingOutline = transform.Find("DamageTextOutline");
                if (existingOutline == null)
                {
                    GameObject outlineObject = new GameObject("DamageTextOutline");
                    existingOutline = outlineObject.transform;
                    existingOutline.SetParent(transform, false);
                }

                outlineText = existingOutline.GetComponent<TextMeshPro>()
                    ?? existingOutline.gameObject.AddComponent<TextMeshPro>();
            }

            ConfigureText(text);
            ConfigureText(outlineText);
        }

        private void ConfigureText(TextMeshPro target)
        {
            target.alignment = TextAlignmentOptions.Center;
            target.enableWordWrapping = false;
            target.fontSize = fontSize;
            target.raycastTarget = false;
        }

        private void ApplyText(TextMeshPro target, string value, TMP_FontAsset font, Color color, int sortingOrder)
        {
            target.text = value;
            target.color = color;
            target.sortingOrder = sortingOrder;

            if (font != null)
            {
                target.font = font;
            }
        }

        private static void SetAlpha(TextMeshPro target, float alpha)
        {
            Color color = target.color;
            color.a = alpha;
            target.color = color;
        }
    }
}
