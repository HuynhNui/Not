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
        [SerializeField] private RectTransform focusHighlightFrame;
        [SerializeField] private Image focusHighlightImage;
        [SerializeField] private RectTransform swipeLeftRightIcon;
        [SerializeField] private Sprite swipeLeftRightSprite;
        [SerializeField] private Vector2 swipeIconAnchor = new Vector2(0.5f, 0.33f);
        [SerializeField] private Vector2 swipeIconAnchoredPosition = Vector2.zero;
        [SerializeField] private Vector2 swipeIconSize = new Vector2(260f, 160f);
        [SerializeField] private RectTransform tutorialDialogPanel;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button nextButton;
        [SerializeField] private RectTransform upgradeCallout;
        [SerializeField] private Button skipButton;

        private RectTransform _rectTransform;

        public event Action SkipClicked;
        public event Action NextClicked;

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
            HideDialogue();
            HideSwipeIcon();
            HideHighlight();
            HideUpgradeCallout();
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

        public void ShowDialogue(string speaker, string body, bool showNext = false)
        {
            SetActive(tutorialDialogPanel, true);
            SetText(speakerText, speaker);
            SetText(bodyText, body);
            ShowNextButton(showNext);
        }

        public void HideDialogue()
        {
            SetActive(tutorialDialogPanel, false);
            ShowNextButton(false);
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

        public void ShowNextButton(bool visible)
        {
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(visible);
            }
        }

        public void ShowUpgradeCallout(RectTransform target = null)
        {
            SetActive(upgradeCallout, true);
            if (target != null)
            {
                HighlightRect(target, new Vector2(24f, 16f));
            }
        }

        public void HideUpgradeCallout()
        {
            SetActive(upgradeCallout, false);
        }

        public void HighlightRect(RectTransform target, Vector2 padding)
        {
            ResolveReferences();

            if (target == null || focusHighlightFrame == null)
            {
                HideHighlight();
                return;
            }

            RectTransform root = _rectTransform != null
                ? _rectTransform
                : transform as RectTransform;

            if (root == null)
            {
                return;
            }

            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);

            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            for (int index = 0; index < worldCorners.Length; index++)
            {
                Vector2 localPoint = root.InverseTransformPoint(worldCorners[index]);
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            Vector2 safePadding = new Vector2(Mathf.Max(0f, padding.x), Mathf.Max(0f, padding.y));
            focusHighlightFrame.anchorMin = new Vector2(0.5f, 0.5f);
            focusHighlightFrame.anchorMax = new Vector2(0.5f, 0.5f);
            focusHighlightFrame.pivot = new Vector2(0.5f, 0.5f);
            focusHighlightFrame.anchoredPosition = (min + max) * 0.5f;
            focusHighlightFrame.sizeDelta = max - min + safePadding * 2f;
            SetActive(focusHighlightFrame, true);
        }

        public void HighlightWorld(Camera worldCamera, Camera uiCamera, Vector3 worldPosition, Vector2 size)
        {
            ResolveReferences();

            if (worldCamera == null || focusHighlightFrame == null)
            {
                HideHighlight();
                return;
            }

            RectTransform root = _rectTransform != null
                ? _rectTransform
                : transform as RectTransform;
            if (root == null)
            {
                return;
            }

            Vector2 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, uiCamera, out Vector2 localPoint);
            focusHighlightFrame.anchorMin = new Vector2(0.5f, 0.5f);
            focusHighlightFrame.anchorMax = new Vector2(0.5f, 0.5f);
            focusHighlightFrame.pivot = new Vector2(0.5f, 0.5f);
            focusHighlightFrame.anchoredPosition = localPoint;
            focusHighlightFrame.sizeDelta = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
            SetActive(focusHighlightFrame, true);
        }

        public void HideHighlight()
        {
            SetActive(focusHighlightFrame, false);
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

        public void SetOnlyAllowTarget(RectTransform target)
        {
            SetInputBlocking(target == null);
        }

        public void ClearAllowedTarget()
        {
            SetInputBlocking(true);
        }

        private void ResolveReferences()
        {
            _rectTransform ??= transform as RectTransform;
            canvasGroup ??= GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (focusHighlightFrame != null && focusHighlightImage == null)
            {
                focusHighlightImage = focusHighlightFrame.GetComponent<Image>();
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
            label.font = speakerText != null ? speakerText.font : bodyText != null ? bodyText.font : null;
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

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNextClicked);
                nextButton.onClick.AddListener(HandleNextClicked);
            }
        }

        private void HandleSkipClicked()
        {
            SkipClicked?.Invoke();
        }

        private void HandleNextClicked()
        {
            NextClicked?.Invoke();
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
