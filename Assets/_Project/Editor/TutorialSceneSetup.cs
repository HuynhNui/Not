using _Project.Scripts.Core.GameLoop;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.TutorialSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Editor
{
    public static class TutorialSceneSetup
    {
        private const string TutorialAssetRoot = "Assets/_Project/Art/UI/Tutorial";
        private const string SharedButtonPath = "Assets/_Project/Art/UI/SettingPanel/reset_button_bg.png";
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Tools/True Gate/Tutorial/Setup Tutorial UI")]
        public static void SetupTutorialUI()
        {
            ApplyTutorialSpriteImportSettings();

            GameObject safeAreaRoot = FindOrCreatePath("GameCanvas/UIRoot/SafeAreaRoot");
            GameObject overlayObject = FindOrCreateChild(safeAreaRoot.transform, "TutorialOverlayPanel");
            RectTransform overlayRect = EnsureRectTransform(overlayObject);
            StretchFullScreen(overlayRect);

            CanvasGroup canvasGroup = overlayObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = overlayObject.AddComponent<CanvasGroup>();
            }

            TutorialOverlayUI overlay = overlayObject.GetComponent<TutorialOverlayUI>();
            if (overlay == null)
            {
                overlay = overlayObject.AddComponent<TutorialOverlayUI>();
            }

            Image dimBackground = CreateImage(
                overlayRect,
                "DimBackground",
                null,
                new Color(0f, 0f, 0f, 0.45f),
                stretch: true,
                raycastTarget: true);

            RectTransform swipeIcon = CreateImage(
                overlayRect,
                "SwipeLeftRightIcon",
                LoadSprite("swipe_left_right_icon.png"),
                Color.white,
                stretch: false,
                raycastTarget: false).rectTransform;
            swipeIcon.anchorMin = new Vector2(0.5f, 0.18f);
            swipeIcon.anchorMax = new Vector2(0.5f, 0.18f);
            swipeIcon.anchoredPosition = Vector2.zero;
            swipeIcon.sizeDelta = new Vector2(150f, 92f);
            swipeIcon.gameObject.SetActive(false);

            Button skipButton = CreateButton(overlayRect, "SkipButton", AssetDatabase.LoadAssetAtPath<Sprite>(SharedButtonPath));
            skipButton.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform skipRect = skipButton.transform as RectTransform;
            skipRect.anchorMin = new Vector2(1f, 0f);
            skipRect.anchorMax = new Vector2(1f, 0f);
            skipRect.pivot = new Vector2(1f, 0f);
            skipRect.anchoredPosition = new Vector2(-24f, 24f);
            skipRect.sizeDelta = new Vector2(132f, 58f);
            TextMeshProUGUI skipText = CreateText(skipRect, "Label", 24, FontStyles.Bold);
            skipText.rectTransform.anchorMin = Vector2.zero;
            skipText.rectTransform.anchorMax = Vector2.one;
            skipText.rectTransform.offsetMin = Vector2.zero;
            skipText.rectTransform.offsetMax = Vector2.zero;
            skipText.alignment = TextAlignmentOptions.Center;
            skipText.color = new Color(0.035f, 0.15f, 0.34f, 1f);
            skipText.text = "SKIP";
            skipButton.gameObject.SetActive(false);

            AssignOverlayReferences(
                overlay,
                canvasGroup,
                dimBackground,
                swipeIcon,
                skipButton);

            GameManager gameManager = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gameManager == null)
            {
                Debug.LogError("Tutorial setup failed: GameManager was not found in the active scene.");
                return;
            }

            TutorialManager tutorialManager = gameManager.GetComponent<TutorialManager>();
            if (tutorialManager == null)
            {
                tutorialManager = gameManager.gameObject.AddComponent<TutorialManager>();
            }

            TutorialGameplayDirector gameplayDirector = gameManager.GetComponent<TutorialGameplayDirector>();
            if (gameplayDirector == null)
            {
                gameplayDirector = gameManager.gameObject.AddComponent<TutorialGameplayDirector>();
            }

            AssignManagerReferences(tutorialManager, overlay, gameplayDirector);
            AssignGameManagerReference(gameManager, tutorialManager);

            EditorUtility.SetDirty(overlay);
            EditorUtility.SetDirty(tutorialManager);
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(gameManager.gameObject.scene);
            EditorSceneManager.SaveScene(gameManager.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Tutorial UI setup complete.");
        }

        [MenuItem("Tools/True Gate/Tutorial/Apply Sprite Import Settings")]
        public static void ApplyTutorialSpriteImportSettings()
        {
            string[] texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { TutorialAssetRoot });
            for (int index = 0; index < texturePaths.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(texturePaths[index]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        [MenuItem("Tools/True Gate/Tutorial/Reset Tutorial Flags")]
        public static void ResetTutorialFlags()
        {
            SaveService.Instance.ResetPlayerProgression();
            Debug.Log("Tutorial flags reset with player progression.");
        }

        [MenuItem("Tools/True Gate/Tutorial/Mark Tutorial Complete")]
        public static void MarkTutorialComplete()
        {
            SaveService.Instance.MarkGameplayTutorialCompleted();
            SaveService.Instance.MarkUpgradeTutorialCompleted();
            Debug.Log("Tutorial flags marked complete.");
        }

        private static GameObject FindOrCreatePath(string path)
        {
            string[] parts = path.Split('/');
            Transform current = null;

            for (int index = 0; index < parts.Length; index++)
            {
                GameObject found = current == null
                    ? GameObject.Find(parts[index])
                    : current.Find(parts[index])?.gameObject;

                if (found == null)
                {
                    found = new GameObject(parts[index], typeof(RectTransform));
                    if (current != null)
                    {
                        found.transform.SetParent(current, false);
                    }
                }

                current = found.transform;
            }

            return current != null ? current.gameObject : null;
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject childObject = new GameObject(name, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static Image CreateImage(
            RectTransform parent,
            string name,
            Sprite sprite,
            Color color,
            bool stretch,
            bool raycastTarget)
        {
            GameObject child = FindOrCreateChild(parent, name);
            child.transform.SetParent(parent, false);
            RectTransform rect = EnsureRectTransform(child);
            if (stretch)
            {
                StretchFullScreen(rect);
            }

            Image image = child.GetComponent<Image>();
            if (image == null)
            {
                image = child.AddComponent<Image>();
            }

            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        private static Button CreateButton(RectTransform parent, string name, Sprite sprite)
        {
            Image image = CreateImage(parent, name, sprite, Color.white, stretch: false, raycastTarget: true);
            Button button = image.GetComponent<Button>();
            if (button == null)
            {
                button = image.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = image;
            return button;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string name, int fontSize, FontStyles fontStyle)
        {
            GameObject child = FindOrCreateChild(parent, name);
            RectTransform rect = EnsureRectTransform(child);
            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = child.AddComponent<TextMeshProUGUI>();
            }

            rect.localScale = Vector3.one;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static RectTransform EnsureRectTransform(GameObject target)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect != null)
            {
                return rect;
            }

            return target.AddComponent<RectTransform>();
        }

        private static void StretchFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{TutorialAssetRoot}/{fileName}");
        }

        private static void AssignOverlayReferences(
            TutorialOverlayUI overlay,
            CanvasGroup canvasGroup,
            Image dimBackground,
            RectTransform swipeIcon,
            Button skipButton)
        {
            SerializedObject serialized = new SerializedObject(overlay);
            SetReference(serialized, "canvasGroup", canvasGroup);
            SetReference(serialized, "dimBackground", dimBackground);
            SetReference(serialized, "swipeLeftRightIcon", swipeIcon);
            SetReference(serialized, "skipButton", skipButton);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignManagerReferences(
            TutorialManager manager,
            TutorialOverlayUI overlay,
            TutorialGameplayDirector gameplayDirector)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SetReference(serialized, "overlayUI", overlay);
            SetReference(serialized, "gameplayDirector", gameplayDirector);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignGameManagerReference(GameManager gameManager, TutorialManager tutorialManager)
        {
            SerializedObject serialized = new SerializedObject(gameManager);
            SetReference(serialized, "tutorialManager", tutorialManager);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
