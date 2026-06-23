using System;
using _Project.Scripts.Data.ScriptableObjects.CutsceneConfigs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace _Project.Scripts.Systems.CutsceneSystem
{
    public sealed class DialoguePanel : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject root;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private TextMeshProUGUI continueText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;

        public event Action NextRequested;
        public event Action SkipRequested;

        public static DialoguePanel CreateRuntimePanel()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("CutsceneCanvas");
            Canvas runtimeCanvas = canvasObject.AddComponent<Canvas>();
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeCanvas.sortingOrder = 500;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panelObject = new GameObject("DialoguePanel", typeof(RectTransform));
            panelObject.transform.SetParent(canvasObject.transform, false);
            DialoguePanel panel = panelObject.AddComponent<DialoguePanel>();
            panel.canvas = runtimeCanvas;
            panel.BuildRuntimeVisuals();
            panel.Hide();
            return panel;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            EventSystem existingEventSystem = FindAnyObjectByType<EventSystem>();
            if (existingEventSystem != null)
            {
                EventSystem.current = existingEventSystem;
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private void Awake()
        {
            WireButtons();
            Hide();
        }

        private void OnDestroy()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNextClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipClicked);
            }
        }

        public void Show(bool canSkip)
        {
            WireButtons();
            SetActive(root != null ? root : gameObject, true);
            SetActive(skipButton != null ? skipButton.gameObject : null, canSkip);
            SetContinueVisible(false);
        }

        public void Hide()
        {
            SetActive(root != null ? root : gameObject, false);
        }

        public void SetLine(DialogueLine line, string visibleText)
        {
            if (line == null)
            {
                SetText(speakerText, string.Empty);
                SetBodyText(string.Empty);
                SetActive(avatarImage != null ? avatarImage.gameObject : null, false);
                return;
            }

            SetText(speakerText, line.SpeakerName);
            SetBodyText(visibleText);

            if (avatarImage == null)
            {
                return;
            }

            avatarImage.sprite = line.Avatar;
            SetActive(avatarImage.gameObject, line.Avatar != null);
        }

        public void SetBodyText(string value)
        {
            SetText(bodyText, value);
        }

        public void SetContinueVisible(bool visible)
        {
            SetActive(continueText != null ? continueText.gameObject : null, visible);
        }

        private void BuildRuntimeVisuals()
        {
            root = gameObject;
            RectTransform panelTransform = gameObject.GetComponent<RectTransform>();
            panelTransform.anchorMin = new Vector2(0.04f, 0.03f);
            panelTransform.anchorMax = new Vector2(0.96f, 0.32f);
            panelTransform.offsetMin = Vector2.zero;
            panelTransform.offsetMax = Vector2.zero;

            Image panelImage = gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.025f, 0.03f, 0.94f);

            nextButton = gameObject.AddComponent<Button>();
            nextButton.transition = Selectable.Transition.None;

            GameObject borderObject = CreateChild("Border", gameObject.transform);
            RectTransform borderTransform = borderObject.GetComponent<RectTransform>();
            borderTransform.anchorMin = Vector2.zero;
            borderTransform.anchorMax = Vector2.one;
            borderTransform.offsetMin = new Vector2(3f, 3f);
            borderTransform.offsetMax = new Vector2(-3f, -3f);
            Image borderImage = borderObject.AddComponent<Image>();
            borderImage.color = new Color(0.1f, 0.9f, 0.78f, 0.24f);
            borderImage.raycastTarget = false;

            GameObject speakerObject = CreateChild("SpeakerText", gameObject.transform);
            speakerText = speakerObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(speakerText, 24f, FontStyles.Bold, TextAlignmentOptions.Left);
            RectTransform speakerTransform = speakerObject.GetComponent<RectTransform>();
            speakerTransform.anchorMin = new Vector2(0.05f, 0.68f);
            speakerTransform.anchorMax = new Vector2(0.7f, 0.92f);
            speakerTransform.offsetMin = Vector2.zero;
            speakerTransform.offsetMax = Vector2.zero;

            GameObject bodyObject = CreateChild("BodyText", gameObject.transform);
            bodyText = bodyObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(bodyText, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform bodyTransform = bodyObject.GetComponent<RectTransform>();
            bodyTransform.anchorMin = new Vector2(0.05f, 0.2f);
            bodyTransform.anchorMax = new Vector2(0.92f, 0.68f);
            bodyTransform.offsetMin = Vector2.zero;
            bodyTransform.offsetMax = Vector2.zero;

            GameObject continueObject = CreateChild("ContinuePrompt", gameObject.transform);
            continueText = continueObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(continueText, 18f, FontStyles.Bold, TextAlignmentOptions.Right);
            continueText.text = "TAP";
            RectTransform continueTransform = continueObject.GetComponent<RectTransform>();
            continueTransform.anchorMin = new Vector2(0.74f, 0.04f);
            continueTransform.anchorMax = new Vector2(0.94f, 0.18f);
            continueTransform.offsetMin = Vector2.zero;
            continueTransform.offsetMax = Vector2.zero;

            GameObject skipObject = CreateChild("SkipButton", gameObject.transform);
            skipButton = skipObject.AddComponent<Button>();
            Image skipImage = skipObject.AddComponent<Image>();
            skipImage.color = new Color(0.08f, 0.1f, 0.11f, 0.95f);
            RectTransform skipTransform = skipObject.GetComponent<RectTransform>();
            skipTransform.anchorMin = new Vector2(0.78f, 0.76f);
            skipTransform.anchorMax = new Vector2(0.94f, 0.92f);
            skipTransform.offsetMin = Vector2.zero;
            skipTransform.offsetMax = Vector2.zero;

            GameObject skipTextObject = CreateChild("SkipText", skipObject.transform);
            TextMeshProUGUI skipText = skipTextObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(skipText, 16f, FontStyles.Bold, TextAlignmentOptions.Center);
            skipText.text = "SKIP";
            RectTransform skipTextTransform = skipTextObject.GetComponent<RectTransform>();
            skipTextTransform.anchorMin = Vector2.zero;
            skipTextTransform.anchorMax = Vector2.one;
            skipTextTransform.offsetMin = Vector2.zero;
            skipTextTransform.offsetMax = Vector2.zero;

            skipButton.targetGraphic = skipImage;
            WireButtons();
        }

        private void WireButtons()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNextClicked);
                nextButton.onClick.AddListener(HandleNextClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipClicked);
                skipButton.onClick.AddListener(HandleSkipClicked);
            }
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void ConfigureText(
            TextMeshProUGUI text,
            float size,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = new Color(0.82f, 1f, 0.95f, 1f);
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10f, size * 0.55f);
            text.fontSizeMax = size;
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private void HandleNextClicked()
        {
            NextRequested?.Invoke();
        }

        private void HandleSkipClicked()
        {
            SkipRequested?.Invoke();
        }
    }
}
