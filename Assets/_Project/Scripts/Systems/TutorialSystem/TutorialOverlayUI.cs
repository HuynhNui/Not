using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public sealed class TutorialOverlayUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image dimBackground;
        [SerializeField] private RectTransform focusHighlightFrame;
        [SerializeField] private Image focusHighlightImage;
        [SerializeField] private RectTransform swipeLeftRightIcon;
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
