using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Cutscenes
{
    public sealed class CutsceneDemoSceneBootstrap : MonoBehaviour
    {
        private const string Unit07SpritePath = "Assets/_Project/Art/Maincharacter/UNIX07.aseprite";
        private const string SystemSpritePath = "Assets/Easy Cutscene/Assets/Sprites/Characters/SampleTwo_Idle.png";

        private static readonly string[] ButtonLabels =
        {
            "CS1 Boot Sequence",
            "CS2 First Death Recovery",
            "CS3 Enemy Does Not Charge",
            "CS4 Gate Memory Leak",
            "CS5 Human Command",
            "CS6 System Fatigue",
            "CS7 Final Choice"
        };

        private void Awake()
        {
            EnsureCamera();
            EnsureEventSystem();

            Transform managerRoot = EnsureChild(transform, "CutsceneManager");
            Transform canvasTransform = EnsureChild(transform, "DemoCanvas");
            Canvas canvas = EnsureCanvas(canvasTransform.gameObject);

            StoryCutsceneDirector director = managerRoot.GetComponent<StoryCutsceneDirector>();
            if (director == null)
            {
                director = managerRoot.gameObject.AddComponent<StoryCutsceneDirector>();
            }

            CutsceneDemoController controller = canvasTransform.GetComponent<CutsceneDemoController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<CutsceneDemoController>();
            }

            BuildDemoUi(canvas.transform, director, controller);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            EventSystem existingEventSystem = FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existingEventSystem != null)
            {
                EventSystem.current = existingEventSystem;
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null
                || FindAnyObjectByType<Camera>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f, 1f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            if (cameraObject.GetComponent<AudioListener>() == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                return existing;
            }

            GameObject child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Canvas EnsureCanvas(GameObject canvasObject)
        {
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private static void BuildDemoUi(
            Transform canvas,
            StoryCutsceneDirector director,
            CutsceneDemoController controller)
        {
            ClearGeneratedChildren(canvas);

            GameObject root = CreateRect("GeneratedDemoUI", canvas);
            Stretch(root.GetComponent<RectTransform>());
            Image rootBackground = root.AddComponent<Image>();
            rootBackground.color = new Color(0.025f, 0.035f, 0.055f, 1f);

            CutsceneDemoUIView view = root.AddComponent<CutsceneDemoUIView>();

            GameObject menuRoot = CreateRect("MenuRoot", root.transform);
            Stretch(menuRoot.GetComponent<RectTransform>());

            TMP_Text title = CreateText(
                "TitleText",
                menuRoot.transform,
                "True Gate? Cutscene Demo",
                58f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchors(title.rectTransform, 0.08f, 0.88f, 0.92f, 0.96f);

            TMP_Text subtitle = CreateText(
                "SubtitleText",
                menuRoot.transform,
                "Demo Mode: unlock conditions bypassed",
                30f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            SetAnchors(subtitle.rectTransform, 0.08f, 0.82f, 0.92f, 0.87f);

            GameObject buttonListRoot = CreateRect("ButtonListRoot", menuRoot.transform);
            SetAnchors(buttonListRoot.GetComponent<RectTransform>(), 0.12f, 0.22f, 0.88f, 0.78f);

            Button[] buttons = new Button[ButtonLabels.Length];
            for (int index = 0; index < ButtonLabels.Length; index++)
            {
                GameObject buttonObject = CreateButton(
                    GetButtonObjectName(index),
                    buttonListRoot.transform,
                    ButtonLabels[index],
                    30f);
                SetAnchors(
                    buttonObject.GetComponent<RectTransform>(),
                    0f,
                    1f - (index + 1) / 7f + 0.018f,
                    1f,
                    1f - index / 7f - 0.018f);
                buttons[index] = buttonObject.GetComponent<Button>();
            }

            GameObject cutsceneRoot = CreateRect("CutsceneRoot", root.transform);
            Stretch(cutsceneRoot.GetComponent<RectTransform>());

            GameObject cutsceneBackground = CreateRect("CutsceneBackground", cutsceneRoot.transform);
            Stretch(cutsceneBackground.GetComponent<RectTransform>());
            Image cutsceneBackgroundImage = cutsceneBackground.AddComponent<Image>();
            cutsceneBackgroundImage.color = new Color(0.015f, 0.023f, 0.045f, 1f);

            GameObject actorRoot = CreateRect("ActorRoot", cutsceneRoot.transform);
            SetAnchors(actorRoot.GetComponent<RectTransform>(), 0.05f, 0.27f, 0.95f, 0.8f);

            GameObject actorUnit07 = CreateActor("Actor_UNIT07", actorRoot.transform, "UNIT-07", new Color(0.1f, 0.17f, 0.25f, 0.94f));
            SetAnchors(actorUnit07.GetComponent<RectTransform>(), 0.04f, 0.08f, 0.36f, 0.96f);

            GameObject actorSystem = CreateActor("Actor_SYSTEM", actorRoot.transform, "SYSTEM", new Color(0.12f, 0.11f, 0.24f, 0.94f));
            SetAnchors(actorSystem.GetComponent<RectTransform>(), 0.64f, 0.08f, 0.96f, 0.96f);

            GameObject actorAlienAdult = CreateActor("Actor_ALIEN_ADULT", actorRoot.transform, "ALIEN", new Color(0.1f, 0.24f, 0.16f, 0.94f));
            SetAnchors(actorAlienAdult.GetComponent<RectTransform>(), 0.24f, 0.05f, 0.56f, 0.84f);

            GameObject actorAlienChild = CreateActor("Actor_ALIEN_CHILD", actorRoot.transform, "ALIEN CHILD", new Color(0.08f, 0.18f, 0.13f, 0.94f));
            SetAnchors(actorAlienChild.GetComponent<RectTransform>(), 0.48f, 0.02f, 0.7f, 0.54f);

            GameObject actorHumanCommand = CreateActor("Actor_HUMAN_COMMAND", actorRoot.transform, "HUMAN COMMAND", new Color(0.2f, 0.13f, 0.08f, 0.94f));
            SetAnchors(actorHumanCommand.GetComponent<RectTransform>(), 0.34f, 0.12f, 0.66f, 0.9f);

            GameObject propRoot = CreateRect("PropRoot", cutsceneRoot.transform);
            SetAnchors(propRoot.GetComponent<RectTransform>(), 0.05f, 0.27f, 0.95f, 0.8f);

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

            GameObject closeButtonObject = CreateButton("CloseButton", dialoguePanel.transform, "CLOSE", 22f);
            SetAnchors(closeButtonObject.GetComponent<RectTransform>(), 0.78f, 0.06f, 0.95f, 0.24f);
            Button closeButton = closeButtonObject.GetComponent<Button>();

            GameObject finalChoicePanel = CreateRect("FinalChoicePanel", dialoguePanel.transform);
            SetAnchors(finalChoicePanel.GetComponent<RectTransform>(), 0.05f, 0.04f, 0.5f, 0.25f);

            GameObject continueButtonObject = CreateButton("ContinueProtocolButton", finalChoicePanel.transform, "CONTINUE PROTOCOL", 18f);
            SetAnchors(continueButtonObject.GetComponent<RectTransform>(), 0f, 0.52f, 1f, 1f);
            Button continueButton = continueButtonObject.GetComponent<Button>();

            GameObject shutDownButtonObject = CreateButton("ShutDownCoreButton", finalChoicePanel.transform, "SHUT DOWN CORE", 18f);
            SetAnchors(shutDownButtonObject.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.48f);
            Button shutDownButton = shutDownButtonObject.GetComponent<Button>();

            AssignDemoSprites(
                actorUnit07.GetComponent<Image>(),
                actorSystem.GetComponent<Image>());

            view.Init(
                menuRoot,
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

            director.Init(null, view);
            controller.Init(
                director,
                buttons[0],
                buttons[1],
                buttons[2],
                buttons[3],
                buttons[4],
                buttons[5],
                buttons[6]);
        }

        private static string GetButtonObjectName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Button_CS1_BootSequence";
                case 1:
                    return "Button_CS2_FirstDeathRecovery";
                case 2:
                    return "Button_CS3_EnemyDoesNotCharge";
                case 3:
                    return "Button_CS4_GateMemoryLeak";
                case 4:
                    return "Button_CS5_HumanCommand";
                case 5:
                    return "Button_CS6_SystemFatigue";
                default:
                    return "Button_CS7_FinalChoice";
            }
        }

        private static void ClearGeneratedChildren(Transform canvas)
        {
            Transform existing = canvas.Find("GeneratedDemoUI");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
        }

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

        private static GameObject CreateActor(string objectName, Transform parent, string label, Color backgroundColor)
        {
            GameObject actorRoot = CreateRect(objectName, parent);
            Image image = actorRoot.AddComponent<Image>();
            image.color = backgroundColor;
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

        private static void AssignDemoSprites(Image unitPortrait, Image systemPortrait)
        {
#if UNITY_EDITOR
            Sprite unitSprite = LoadSpriteAsset(Unit07SpritePath, "Frame_0");
            if (unitSprite != null)
            {
                unitPortrait.sprite = unitSprite;
                unitPortrait.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"Could not load UNIT-07 demo sprite '{Unit07SpritePath}' Frame_0.");
            }

            Sprite systemSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SystemSpritePath);
            if (systemSprite != null)
            {
                systemPortrait.sprite = systemSprite;
                systemPortrait.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"Could not load SYSTEM demo sprite '{SystemSpritePath}'.");
            }
#else
            Debug.LogWarning("CutsceneDemo actor sprites are assigned through AssetDatabase in the editor demo only.");
#endif
        }

#if UNITY_EDITOR
        private static Sprite LoadSpriteAsset(string assetPath, string preferredName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Sprite firstSprite = null;
            foreach (UnityEngine.Object asset in assets)
            {
                Sprite sprite = asset as Sprite;
                if (sprite == null)
                {
                    continue;
                }

                if (firstSprite == null)
                {
                    firstSprite = sprite;
                }

                if (sprite.name == preferredName)
                {
                    return sprite;
                }
            }

            return firstSprite;
        }
#endif

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
