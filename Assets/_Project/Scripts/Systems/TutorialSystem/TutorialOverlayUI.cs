using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RuntimeUISystem = _Project.Scripts.Systems.UISystem.UISystem;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public sealed class TutorialOverlayUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image dimBackground;
        [SerializeField] private RectTransform swipeLeftRightIcon;
        [SerializeField] private Sprite swipeLeftRightSprite;
        [SerializeField] private Vector2 swipeIconAnchor = new Vector2(0.5f, 0.33f);
        [SerializeField] private Vector2 swipeIconAnchoredPosition = Vector2.zero;
        [SerializeField] private Vector2 swipeIconSize = new Vector2(260f, 160f);
        [SerializeField] private Button skipButton;

        private RectTransform _rectTransform;

        public event Action SkipClicked;

        private void Awake()
        {
            ResolveReferences();
            StyleSkipButton();
            WireButtons();
            HideOverlay();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        public void ShowOverlay(bool dimBackgroundVisible = true, bool blockInput = true)
        {
            ResolveReferences();
            gameObject.SetActive(true);
            _rectTransform?.SetAsLastSibling();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (dimBackground != null)
            {
                dimBackground.enabled = dimBackgroundVisible;
                dimBackground.raycastTarget = blockInput;
            }
        }

        public void HideOverlay()
        {
            ResolveReferences();
            HideSwipeIcon();
            ShowSkipButton(false);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (dimBackground != null)
            {
                dimBackground.enabled = false;
                dimBackground.raycastTarget = false;
            }
        }

        public void ShowSwipeIcon()
        {
            EnsureSwipeIconSprite();
            SetActive(swipeLeftRightIcon, true);
        }

        public void HideSwipeIcon()
        {
            SetActive(swipeLeftRightIcon, false);
        }

        public void ShowSkipButton(bool visible)
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(visible);
            }
        }

        public void SetInputBlocking(bool block)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            if (dimBackground != null)
            {
                dimBackground.raycastTarget = block;
            }
        }

        private void ResolveReferences()
        {
            _rectTransform ??= transform as RectTransform;
            canvasGroup ??= GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (swipeLeftRightIcon != null)
            {
                Image swipeImage = swipeLeftRightIcon.GetComponent<Image>();
                if (swipeImage != null && swipeLeftRightSprite == null)
                {
                    swipeLeftRightSprite = swipeImage.sprite;
                }
            }
        }

        private void StyleSkipButton()
        {
            if (skipButton == null)
            {
                return;
            }

            RectTransform rect = skipButton.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-24f, 24f);
                rect.sizeDelta = new Vector2(132f, 58f);
            }

            Image image = skipButton.targetGraphic as Image ?? skipButton.GetComponent<Image>();
            RuntimeUISystem uiSystem = FindAnyObjectByType<RuntimeUISystem>(FindObjectsInactive.Include);
            if (image != null && uiSystem != null && uiSystem.SharedYellowButtonSprite != null)
            {
                image.sprite = uiSystem.SharedYellowButtonSprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.color = Color.white;
            }

            TextMeshProUGUI label = skipButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(skipButton.transform, false);
                label = labelObject.GetComponent<TextMeshProUGUI>();
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.text = "SKIP";
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.035f, 0.15f, 0.34f, 1f);
            label.raycastTarget = false;
        }

        private void EnsureSwipeIconSprite()
        {
            if (swipeLeftRightIcon == null)
            {
                return;
            }

            Image image = swipeLeftRightIcon.GetComponent<Image>();
            if (image == null)
            {
                image = swipeLeftRightIcon.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
            }

            if (image.sprite == null && swipeLeftRightSprite != null)
            {
                image.sprite = swipeLeftRightSprite;
            }

            image.enabled = image.sprite != null;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            swipeLeftRightIcon.anchorMin = swipeIconAnchor;
            swipeLeftRightIcon.anchorMax = swipeIconAnchor;
            swipeLeftRightIcon.pivot = new Vector2(0.5f, 0.5f);
            swipeLeftRightIcon.anchoredPosition = swipeIconAnchoredPosition;
            swipeLeftRightIcon.sizeDelta = new Vector2(
                Mathf.Max(1f, swipeIconSize.x),
                Mathf.Max(1f, swipeIconSize.y));
            swipeLeftRightIcon.localScale = Vector3.one;
            swipeLeftRightIcon.SetAsLastSibling();
        }

        private void WireButtons()
        {
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipClicked);
                skipButton.onClick.AddListener(HandleSkipClicked);
            }
        }

        private void HandleSkipClicked()
        {
            SkipClicked?.Invoke();
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }
    }
}
