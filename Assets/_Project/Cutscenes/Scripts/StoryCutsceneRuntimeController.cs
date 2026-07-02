using System;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Cutscenes
{
    public sealed class StoryCutsceneRuntimeController : MonoBehaviour
    {
        private const string Unit07SpritePath = "Assets/_Project/Art/Maincharacter/UNIX07ForDialogue.png";
        private const string Unit07SpriteName = "UNIX07ForDialogue_0";
        private const string SystemSpritePath = "Assets/Easy Cutscene/Assets/Sprites/Characters/SampleTwo_Idle.png";

        [SerializeField] private StoryCutsceneDirector director;
        [SerializeField] private CutsceneDemoUIView view;
        [SerializeField] private Canvas runtimeCanvas;
        [SerializeField] private Sprite unit07Portrait;
        [SerializeField] private Sprite systemPortrait;
        [SerializeField] private bool buildRuntimeUiIfMissing = true;

        private Action _onPlaybackComplete;
        private bool _isInitialized;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public void Init()
        {
            if (_isInitialized)
            {
                return;
            }

            EnsureDirectorAndView();

            if (director != null)
            {
                director.SetFallbackWarningsEnabled(false);
                director.OnCutsceneStarted -= HandleCutsceneStarted;
                director.OnCutsceneStarted += HandleCutsceneStarted;
                director.OnCutsceneFinished -= HandleCutsceneFinished;
                director.OnCutsceneFinished += HandleCutsceneFinished;
            }

            _isInitialized = true;
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            if (director == null)
            {
                return;
            }

            director.OnCutsceneStarted -= HandleCutsceneStarted;
            director.OnCutsceneFinished -= HandleCutsceneFinished;
        }

        public bool TryPlayInitialCutscene(Action onComplete = null)
        {
            SaveData saveData = SaveService.Instance.Data;
            var context = new StoryCutsceneProgressContext(
                saveData.totalRunsCompleted,
                0f,
                0,
                saveData.totalEnemyKills);

            if (!StoryCutsceneUnlockRules.IsEligible(
                    StoryCutsceneIds.BootSequence,
                    saveData,
                    context))
            {
                return false;
            }

            return TryPlayCutscene(StoryCutsceneIds.BootSequence, onComplete);
        }

        public bool TryPlayPostRunCutscene(
            RunStatsSnapshot snapshot,
            Action onComplete = null)
        {
            SaveData saveData = SaveService.Instance.Data;
            var context = new StoryCutsceneProgressContext(
                saveData.totalRunsCompleted,
                snapshot.SurvivalTime,
                snapshot.EnemyKills,
                saveData.totalEnemyKills);

            if (!StoryCutsceneUnlockRules.TryGetFirstEligible(saveData, context, out string cutsceneId))
            {
                return false;
            }

            return TryPlayCutscene(cutsceneId, onComplete);
        }

        public bool TryPlayCutscene(string cutsceneId, Action onComplete = null)
        {
            Init();

            string playableId = StoryCutsceneUnlockRules.NormalizePlayableCutsceneId(cutsceneId);
            if (director == null || !StoryCutsceneLibrary.TryGet(playableId, out _))
            {
                return false;
            }

            _onPlaybackComplete = onComplete;
            _isPlaying = true;
            director.Play(playableId);
            return true;
        }

        private void HandleCutsceneStarted(string cutsceneId)
        {
            _isPlaying = true;

            if (cutsceneId == StoryCutsceneIds.FinalChoiceContinueProtocol
                || cutsceneId == StoryCutsceneIds.FinalChoiceShutDownCore)
            {
                SaveService.Instance.RecordCutsceneSeen(StoryCutsceneIds.FinalChoicePreChoice);
            }
        }

        private void HandleCutsceneFinished(string cutsceneId)
        {
            SaveService.Instance.RecordCutsceneSeen(
                StoryCutsceneUnlockRules.NormalizePlayableCutsceneId(cutsceneId));

            _isPlaying = false;
            Action callback = _onPlaybackComplete;
            _onPlaybackComplete = null;
            callback?.Invoke();
        }

        private void EnsureDirectorAndView()
        {
            if (director == null)
            {
                director = GetComponent<StoryCutsceneDirector>();
            }

            if (director == null)
            {
                director = gameObject.AddComponent<StoryCutsceneDirector>();
            }

            if (view == null)
            {
                view = GetComponentInChildren<CutsceneDemoUIView>(true);
            }

            if (view == null && buildRuntimeUiIfMissing)
            {
                view = BuildRuntimeUi();
            }

            if (view != null)
            {
                director.Init(null, view);
            }
        }

        private CutsceneDemoUIView BuildRuntimeUi()
        {
            Canvas canvas = EnsureRuntimeCanvas();
            GameObject root = CreateRect("RuntimeStoryCutsceneRoot", canvas.transform);
            Stretch(root.GetComponent<RectTransform>());

            CutsceneDemoUIView runtimeView = root.AddComponent<CutsceneDemoUIView>();

            GameObject cutsceneRoot = CreateRect("CutsceneRoot", root.transform);
            Stretch(cutsceneRoot.GetComponent<RectTransform>());

            GameObject background = CreateRect("CutsceneBackground", cutsceneRoot.transform);
            Stretch(background.GetComponent<RectTransform>());
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.015f, 0.023f, 0.045f, 0.98f);

            GameObject actorRoot = CreateRect("ActorRoot", cutsceneRoot.transform);
            SetAnchors(actorRoot.GetComponent<RectTransform>(), 0.05f, 0.27f, 0.95f, 0.8f);

            GameObject actorUnit07 = CreateActor(
                "Actor_UNIT07",
                actorRoot.transform,
                "UNIT-07",
                new Color(0.1f, 0.17f, 0.25f, 0.94f),
                ResolveUnit07Portrait());
            SetAnchors(actorUnit07.GetComponent<RectTransform>(), 0.04f, 0.08f, 0.36f, 0.96f);

            GameObject actorSystem = CreateActor(
                "Actor_SYSTEM",
                actorRoot.transform,
                "SYSTEM",
                new Color(0.12f, 0.11f, 0.24f, 0.94f),
                ResolveSystemPortrait());
            SetAnchors(actorSystem.GetComponent<RectTransform>(), 0.64f, 0.08f, 0.96f, 0.96f);

            GameObject actorAlienAdult = CreateActor(
                "Actor_ALIEN_ADULT",
                actorRoot.transform,
                "ALIEN",
                new Color(0.1f, 0.24f, 0.16f, 0.94f),
                null);
            SetAnchors(actorAlienAdult.GetComponent<RectTransform>(), 0.24f, 0.05f, 0.56f, 0.84f);

            GameObject actorAlienChild = CreateActor(
                "Actor_ALIEN_CHILD",
                actorRoot.transform,
                "ALIEN CHILD",
                new Color(0.08f, 0.18f, 0.13f, 0.94f),
                null);
            SetAnchors(actorAlienChild.GetComponent<RectTransform>(), 0.48f, 0.02f, 0.7f, 0.54f);

            GameObject actorHumanCommand = CreateActor(
                "Actor_HUMAN_COMMAND",
                actorRoot.transform,
                "HUMAN COMMAND",
                new Color(0.2f, 0.13f, 0.08f, 0.94f),
                null);
            SetAnchors(actorHumanCommand.GetComponent<RectTransform>(), 0.34f, 0.12f, 0.66f, 0.9f);

            GameObject dialoguePanel = CreateRect("DialoguePanel", cutsceneRoot.transform);
            SetAnchors(dialoguePanel.GetComponent<RectTransform>(), 0.06f, 0.035f, 0.94f, 0.245f);
            Image dialogueImage = dialoguePanel.AddComponent<Image>();
            dialogueImage.color = new Color(0.02f, 0.025f, 0.035f, 0.97f);

            TMP_Text speakerText = CreateText(
                "SpeakerNameText",
                dialoguePanel.transform,
                string.Empty,
                30f,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            SetAnchors(speakerText.rectTransform, 0.05f, 0.73f, 0.48f, 0.92f);

            TMP_Text emotionText = CreateText(
                "EmotionText",
                dialoguePanel.transform,
                string.Empty,
                24f,
                FontStyles.Italic,
                TextAlignmentOptions.Right);
            emotionText.color = new Color(1f, 0.78f, 0.22f, 1f);
            SetAnchors(emotionText.rectTransform, 0.52f, 0.73f, 0.95f, 0.92f);

            TMP_Text dialogueText = CreateText(
                "DialogueText",
                dialoguePanel.transform,
                string.Empty,
                34f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            dialogueText.color = new Color(0.9f, 0.97f, 1f, 1f);
            SetAnchors(dialogueText.rectTransform, 0.05f, 0.27f, 0.95f, 0.7f);

            GameObject nextButtonObject = CreateButton("NextButton", dialoguePanel.transform, "NEXT", 22f);
            SetAnchors(nextButtonObject.GetComponent<RectTransform>(), 0.55f, 0.06f, 0.75f, 0.24f);
            Button nextButton = nextButtonObject.GetComponent<Button>();

            GameObject closeButtonObject = CreateButton("CloseButton", dialoguePanel.transform, "SKIP", 22f);
            SetAnchors(closeButtonObject.GetComponent<RectTransform>(), 0.78f, 0.06f, 0.95f, 0.24f);
            Button closeButton = closeButtonObject.GetComponent<Button>();

            GameObject finalChoicePanel = CreateRect("FinalChoicePanel", dialoguePanel.transform);
            SetAnchors(finalChoicePanel.GetComponent<RectTransform>(), 0.05f, 0.04f, 0.5f, 0.25f);

            GameObject continueButtonObject = CreateButton(
                "ContinueProtocolButton",
                finalChoicePanel.transform,
                "CONTINUE PROTOCOL",
                18f);
            SetAnchors(continueButtonObject.GetComponent<RectTransform>(), 0f, 0.52f, 1f, 1f);
            Button continueButton = continueButtonObject.GetComponent<Button>();

            GameObject shutDownButtonObject = CreateButton(
                "ShutDownCoreButton",
                finalChoicePanel.transform,
                "SHUT DOWN CORE",
                18f);
            SetAnchors(shutDownButtonObject.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.48f);
            Button shutDownButton = shutDownButtonObject.GetComponent<Button>();

            runtimeView.Init(
                null,
                cutsceneRoot,
                dialoguePanel,
                speakerText,
                emotionText,
                dialogueText,
                nextButton,
                closeButton,
                finalChoicePanel,
                continueButton,
                shutDownButton,
                actorUnit07,
                actorSystem,
                actorAlienAdult,
                actorAlienChild,
                actorHumanCommand);

            return runtimeView;
        }

        private Canvas EnsureRuntimeCanvas()
        {
            if (runtimeCanvas != null)
            {
                return runtimeCanvas;
            }

            GameObject canvasObject = new GameObject("RuntimeStoryCutsceneCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);

            runtimeCanvas = canvasObject.AddComponent<Canvas>();
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeCanvas.sortingOrder = 250;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return runtimeCanvas;
        }

        private Sprite ResolveUnit07Portrait()
        {
            if (unit07Portrait != null)
            {
                return unit07Portrait;
            }

#if UNITY_EDITOR
            unit07Portrait = LoadSpriteAsset(Unit07SpritePath, Unit07SpriteName);
#endif
            return unit07Portrait;
        }

        private Sprite ResolveSystemPortrait()
        {
            if (systemPortrait != null)
            {
                return systemPortrait;
            }

#if UNITY_EDITOR
            systemPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(SystemSpritePath);
#endif
            return systemPortrait;
        }

#if UNITY_EDITOR
        private static Sprite LoadSpriteAsset(string assetPath, string preferredName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Sprite firstSprite = null;
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is not Sprite sprite)
                {
                    continue;
                }

                firstSprite ??= sprite;
                if (sprite.name == preferredName)
                {
                    return sprite;
                }
            }

            return firstSprite;
        }
#endif

        private static GameObject CreateRect(string objectName, Transform parent)
        {
            GameObject rect = new GameObject(objectName, typeof(RectTransform));
            rect.transform.SetParent(parent, false);
            return rect;
        }

        private static GameObject CreateButton(string objectName, Transform parent, string label, float fontSize)
        {
            GameObject buttonObject = CreateRect(objectName, parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.95f, 0.76f, 0.1f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text labelText = CreateText(
                "Label",
                buttonObject.transform,
                label,
                fontSize,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform);
            labelText.color = new Color(0.02f, 0.05f, 0.12f, 1f);

            return buttonObject;
        }

        private static GameObject CreateActor(
            string objectName,
            Transform parent,
            string label,
            Color backgroundColor,
            Sprite portrait)
        {
            GameObject actorRoot = CreateRect(objectName, parent);
            Image image = actorRoot.AddComponent<Image>();
            image.color = portrait != null ? Color.white : backgroundColor;
            image.sprite = portrait;
            image.preserveAspect = true;

            TMP_Text labelText = CreateText(
                "Label",
                actorRoot.transform,
                label,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            labelText.color = new Color(1f, 0.78f, 0.22f, 1f);
            SetAnchors(labelText.rectTransform, 0.08f, 0.04f, 0.92f, 0.18f);

            return actorRoot;
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            string text,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateRect(objectName, parent);
            TMP_Text tmpText = textObject.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.fontStyle = style;
            tmpText.alignment = alignment;
            tmpText.color = new Color(0.86f, 0.96f, 1f, 1f);
            tmpText.enableAutoSizing = true;
            tmpText.fontSizeMin = Mathf.Max(13f, fontSize * 0.5f);
            tmpText.fontSizeMax = fontSize;
            tmpText.raycastTarget = false;
            return tmpText;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetAnchors(rectTransform, 0f, 0f, 1f, 1f);
        }

        private static void SetAnchors(
            RectTransform rectTransform,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            rectTransform.anchorMin = new Vector2(minX, minY);
            rectTransform.anchorMax = new Vector2(maxX, maxY);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
