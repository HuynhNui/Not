using System.Collections;
using _Project.Scripts.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.Dialogue
{
    public sealed class SpeechBubblePresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform bubbleRect;
        [SerializeField] private RectTransform layerRect;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image bubbleBackground;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.45f, 0f);
        [SerializeField] private Vector2 bubbleSize = new Vector2(520f, 170f);
        [SerializeField] private Vector4 textPadding = new Vector4(48f, 34f, 48f, 62f);
        [SerializeField] private float fadeInSeconds = 0.14f;
        [SerializeField] private float fadeOutSeconds = 0.15f;
        [SerializeField] private float popScale = 0.94f;

        private PlayerController _playerController;
        private Coroutine _animation;
        private bool _isVisible;

        public bool IsVisible => _isVisible;
        public TextMeshProUGUI DialogueText => dialogueText;
        public Image BubbleBackground => bubbleBackground;

        private void Awake()
        {
            ResolveReferences();
            HideImmediate();
        }

        private void LateUpdate()
        {
            if (!_isVisible)
            {
                return;
            }

            FollowAnchor();
        }

        public void Configure(PlayerController playerController, RectTransform targetLayer = null)
        {
            _playerController = playerController;
            if (targetLayer != null)
            {
                layerRect = targetLayer;
            }

            ResolveReferences();
        }

        public void Show(string text, PlayerController playerController)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                HideImmediate();
                return;
            }

            Configure(playerController);
            if (dialogueText == null || bubbleRect == null || canvasGroup == null)
            {
                return;
            }

            ApplyText(text);
            FollowAnchor();

            if (_animation != null)
            {
                StopCoroutine(_animation);
            }

            _animation = StartCoroutine(ShowRoutine(text.Length));
        }

        public void HideImmediate()
        {
            ResolveReferences();
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }

            _isVisible = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (bubbleRect != null)
            {
                bubbleRect.localScale = Vector3.one;
            }
        }

        private IEnumerator ShowRoutine(int textLength)
        {
            _isVisible = true;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < fadeInSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, fadeInSeconds));
                canvasGroup.alpha = t;
                bubbleRect.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, EaseOut(t));
                yield return null;
            }

            canvasGroup.alpha = 1f;
            bubbleRect.localScale = Vector3.one;

            float holdSeconds = Mathf.Clamp(2.5f + 0.035f * textLength, 3f, 5f);
            elapsed = 0f;
            while (elapsed < holdSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fadeOutSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, fadeOutSeconds));
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            HideImmediate();
        }

        private void ApplyText(string text)
        {
            dialogueText.text = text;
            dialogueText.raycastTarget = false;
            dialogueText.enableAutoSizing = true;
            dialogueText.fontSizeMax = Mathf.Max(dialogueText.fontSizeMax, 42f);
            dialogueText.fontSizeMin = dialogueText.fontSizeMin > 0f ? dialogueText.fontSizeMin : 24f;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.overflowMode = TextOverflowModes.Ellipsis;
            dialogueText.maxVisibleLines = 3;

            bubbleRect.sizeDelta = bubbleSize;

            RectTransform textRect = dialogueText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(textPadding.x, textPadding.w);
            textRect.offsetMax = new Vector2(-textPadding.z, -textPadding.y);

            if (bubbleBackground != null)
            {
                bubbleBackground.raycastTarget = false;
                bubbleBackground.type = Image.Type.Simple;
                RectTransform backgroundRect = bubbleBackground.rectTransform;
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = Vector2.zero;
                backgroundRect.offsetMax = Vector2.zero;
            }
        }

        private void FollowAnchor()
        {
            if (_playerController == null
                || _playerController.MainPlayerUnit == null
                || bubbleRect == null
                || layerRect == null)
            {
                return;
            }

            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return;
            }

            Vector3 worldPosition = _playerController.MainPlayerUnit.transform.position + worldOffset;
            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z < 0f)
            {
                HideImmediate();
                return;
            }

            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                layerRect,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
            {
                return;
            }

            Rect rect = layerRect.rect;
            Vector2 halfSize = bubbleRect.rect.size * 0.5f;
            float x = Mathf.Clamp(localPoint.x, rect.xMin + halfSize.x, rect.xMax - halfSize.x);
            float y = Mathf.Clamp(localPoint.y, rect.yMin + halfSize.y, rect.yMax - halfSize.y);
            bubbleRect.anchoredPosition = new Vector2(x, y);
        }

        private void ResolveReferences()
        {
            if (bubbleRect == null)
            {
                bubbleRect = transform as RectTransform;
            }

            if (layerRect == null && transform.parent != null)
            {
                layerRect = transform.parent as RectTransform;
            }

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (bubbleBackground == null)
            {
                bubbleBackground = GetComponentInChildren<Image>(true);
            }

            if (dialogueText == null)
            {
                dialogueText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private static float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
