#if UNITY_EDITOR
using System.Linq;
using _Project.Scripts.Systems.UISystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using LegacyText = UnityEngine.UI.Text;

namespace _Project.Editor
{
    public static class GameplayHudPanelBuilder
    {
        private const string HudPanelPath = "GameCanvas/UIRoot/SafeAreaRoot/GameplayHUDPanel";
        private const string UiframePath = "Assets/_Project/Art/UI/Generated/UIframe_clean.png";
        private const string ClockPath = "Assets/_Project/Art/UI/clock.png";
        private const string CoinPath = "Assets/_Project/Art/UI/vecteezy_game-coin-pixelated_54978935.png";
        private const string SkullPath = "Assets/_Project/Art/UI/Skull&Bones - FrodoUndead.png";
        private const string PausePath = "Assets/_Project/Art/UI/Generated/pauseicon_clean.png";
        private const string UpheavalTtfPath = "Assets/Front/upheavtt.ttf";
        private const string UpheavalTmpPath = "Assets/Front/Upheaval_TMP.asset";

        [MenuItem("Chibi Pixel Gate/UI/Rebuild Gameplay HUD")]
        public static void Rebuild()
        {
            ImportUiTextures();

            TMP_FontAsset fontAsset = EnsureUpheavalFontAsset();
            Sprite frameSprite = LoadSprite(UiframePath);
            Sprite clockSprite = LoadSprite(ClockPath);
            Sprite coinSprite = LoadSprite(CoinPath);
            Sprite skullSprite = LoadSprite(SkullPath, "Skull&Bones - FrodoUndead_0");
            Sprite pauseSprite = LoadSprite(PausePath);

            GameObject hudPanel = FindSceneObjectByPath(HudPanelPath);
            if (hudPanel == null)
            {
                Debug.LogError($"Could not find HUD panel at '{HudPanelPath}'.");
                return;
            }

            CleanupPanel(hudPanel);
            BuildHud(hudPanel.transform as RectTransform, frameSprite, clockSprite, coinSprite, skullSprite, pauseSprite, fontAsset);
            ApplyFontToGameCanvas(fontAsset);
            BindUiSystem();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Rebuilt GameplayHUDPanel with pixel HUD layout.");
        }

        private static void ImportUiTextures()
        {
            ConfigureSpriteImporter(UiframePath, SpriteImportMode.Single, new Vector4(64f, 24f, 64f, 24f), 2048);
            ConfigureSpriteImporter(PausePath, SpriteImportMode.Single, Vector4.zero, 1024);
            ConfigureSpriteImporter(ClockPath, SpriteImportMode.Single, Vector4.zero, 512);
            ConfigureSpriteImporter(CoinPath, SpriteImportMode.Single, Vector4.zero, 1024);
        }

        private static void ConfigureSpriteImporter(string assetPath, SpriteImportMode mode, Vector4 border, int maxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Texture importer not found for {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;

            TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
            androidSettings.overridden = true;
            androidSettings.maxTextureSize = maxSize;
            androidSettings.format = TextureImporterFormat.Automatic;
            androidSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(androidSettings);

            importer.SaveAndReimport();
        }

        private static TMP_FontAsset EnsureUpheavalFontAsset()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UpheavalTmpPath);
            if (IsUsableFontAsset(existing))
            {
                return existing;
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(UpheavalTmpPath);
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(UpheavalTtfPath);
            if (font == null)
            {
                Debug.LogWarning($"Upheaval TTF not found at {UpheavalTtfPath}. Falling back to TMP default font.");
                return TMP_Settings.defaultFontAsset;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            fontAsset.name = "Upheaval_TMP";
            AssetDatabase.CreateAsset(fontAsset, UpheavalTmpPath);
            AddFontSubAssets(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static bool IsUsableFontAsset(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null || fontAsset.material == null || fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0)
            {
                return false;
            }

            return fontAsset.atlasTextures[0] != null;
        }

        private static void AddFontSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            Material material = fontAsset.material;
            if (material != null && !AssetDatabase.Contains(material))
            {
                material.name = "Upheaval_TMP Material";
                AssetDatabase.AddObjectToAsset(material, fontAsset);
            }

            Texture2D atlasTexture = fontAsset.atlasTexture;
            if (atlasTexture != null && !AssetDatabase.Contains(atlasTexture))
            {
                atlasTexture.name = "Upheaval_TMP Atlas";
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
        }

        private static Sprite LoadSprite(string assetPath, string spriteName = null)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == spriteName);
        }

        private static void CleanupPanel(GameObject hudPanel)
        {
            foreach (Transform child in hudPanel.transform.Cast<Transform>().ToArray())
            {
                Object.DestroyImmediate(child.gameObject);
            }

            foreach (Canvas canvas in hudPanel.GetComponents<Canvas>())
            {
                Object.DestroyImmediate(canvas, true);
            }

            foreach (CanvasScaler canvasScaler in hudPanel.GetComponents<CanvasScaler>())
            {
                Object.DestroyImmediate(canvasScaler, true);
            }

            foreach (GraphicRaycaster graphicRaycaster in hudPanel.GetComponents<GraphicRaycaster>())
            {
                Object.DestroyImmediate(graphicRaycaster, true);
            }

            RectTransform rectTransform = hudPanel.GetComponent<RectTransform>();
            StretchToParent(rectTransform);
            SetLayerRecursive(hudPanel, LayerMask.NameToLayer("UI"));
        }

        private static void BuildHud(
            RectTransform root,
            Sprite frameSprite,
            Sprite clockSprite,
            Sprite coinSprite,
            Sprite skullSprite,
            Sprite pauseSprite,
            TMP_FontAsset fontAsset)
        {
            RectTransform contentRoot = CreateRect("HudContentRoot", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(1080f, 160f));

            RectTransform topLeft = CreateRect("TopLeftHud", contentRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, 0f), new Vector2(210f, 134f));
            RectTransform timeFrame = CreateFrame("TimeFrame", topLeft, frameSprite, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(210f, 68f));
            CreateImage("ClockIcon", timeFrame, clockSprite, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(42f, 42f), true);
            TextMeshProUGUI timeText = CreateText("TimeText", timeFrame, fontAsset, "00:00", 36f, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78f, -1f), new Vector2(118f, 48f), true, 28f, 36f);

            RectTransform killFrame = CreateFrame("KillFrame", topLeft, frameSprite, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -76f), new Vector2(180f, 58f));
            CreateImage("SkullIcon", killFrame, skullSprite, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(40f, 40f), true);
            TextMeshProUGUI killText = CreateText("KillText", killFrame, fontAsset, "0", 34f, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(76f, -1f), new Vector2(86f, 44f), true, 24f, 34f);

            RectTransform scoreFrame = CreateFrame("ScoreFrame", contentRoot, frameSprite, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(300f, 104f));
            CreateText("ScoreLabelText", scoreFrame, fontAsset, "SCORE", 28f, new Color(0.18f, 0.95f, 1f, 1f), TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(240f, 30f));
            TextMeshProUGUI scoreText = CreateText("ScoreValueText", scoreFrame, fontAsset, "0", 58f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(240f, 54f), true, 42f, 58f);

            RectTransform topRight = CreateRect("TopRightHud", contentRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, 0f), new Vector2(260f, 72f));
            RectTransform pauseButton = CreateButton("PauseButton", topRight, pauseSprite, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(72f, 72f));
            RectTransform coinFrame = CreateFrame("CoinFrame", topRight, frameSprite, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, 0f), new Vector2(180f, 68f));
            CreateImage("CoinIcon", coinFrame, coinSprite, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(40f, 40f), true);
            TextMeshProUGUI coinText = CreateText("CoinText", coinFrame, fontAsset, "0", 34f, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78f, -1f), new Vector2(90f, 44f), true, 24f, 34f);

            BindResponsiveLayout(root, contentRoot, topLeft, topRight, scoreFrame, timeFrame, killFrame, coinFrame, pauseButton, timeText, killText, coinText, scoreText);
            BindUiSystemReferences(pauseButton.GetComponent<Button>(), timeText, coinText, killText, scoreText);
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            SetLayerRecursive(gameObject, LayerMask.NameToLayer("UI"));

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            ConfigureRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            return rectTransform;
        }

        private static RectTransform CreateFrame(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            bool enableAutoSizing = false,
            float fontSizeMin = 0f,
            float fontSizeMax = 0f)
        {
            RectTransform rectTransform = CreateImage(name, parent, sprite, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, false);
            Image image = rectTransform.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return rectTransform;
        }

        private static RectTransform CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            bool preserveAspect)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            SetLayerRecursive(gameObject, LayerMask.NameToLayer("UI"));

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            ConfigureRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);

            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return rectTransform;
        }

        private static RectTransform CreateButton(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            SetLayerRecursive(gameObject, LayerMask.NameToLayer("UI"));

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            ConfigureRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);

            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = true;

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            return rectTransform;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            TMP_FontAsset fontAsset,
            string value,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            bool enableAutoSizing = false,
            float fontSizeMin = 0f,
            float fontSizeMax = 0f)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            SetLayerRecursive(gameObject, LayerMask.NameToLayer("UI"));

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            ConfigureRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);

            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.enableAutoSizing = enableAutoSizing;
            if (enableAutoSizing)
            {
                text.fontSizeMin = Mathf.Max(1f, fontSizeMin);
                text.fontSizeMax = Mathf.Max(text.fontSizeMin, fontSizeMax);
            }
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static void ConfigureRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        private static void ApplyFontToGameCanvas(TMP_FontAsset fontAsset)
        {
            GameObject gameCanvas = FindSceneObjectByPath("GameCanvas");
            if (gameCanvas == null || fontAsset == null)
            {
                return;
            }

            ConvertLegacyTexts(gameCanvas, fontAsset);

            TextMeshProUGUI[] textComponents = gameCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in textComponents)
            {
                text.font = fontAsset;
            }
        }

        private static void ConvertLegacyTexts(GameObject root, TMP_FontAsset fontAsset)
        {
            LegacyText[] legacyTexts = root.GetComponentsInChildren<LegacyText>(true);
            foreach (LegacyText legacyText in legacyTexts)
            {
                GameObject gameObject = legacyText.gameObject;
                if (gameObject.GetComponent<TextMeshProUGUI>() != null)
                {
                    Object.DestroyImmediate(legacyText);
                    continue;
                }

                string value = legacyText.text;
                int fontSize = legacyText.fontSize;
                Color color = legacyText.color;
                bool raycastTarget = legacyText.raycastTarget;
                TextAnchor alignment = legacyText.alignment;
                bool resizeText = legacyText.resizeTextForBestFit;
                int resizeMin = legacyText.resizeTextMinSize;
                int resizeMax = legacyText.resizeTextMaxSize;

                Object.DestroyImmediate(legacyText);

                TextMeshProUGUI tmp = gameObject.AddComponent<TextMeshProUGUI>();
                tmp.font = fontAsset;
                tmp.text = value;
                tmp.fontSize = fontSize;
                tmp.color = color;
                tmp.raycastTarget = raycastTarget;
                tmp.alignment = MapAlignment(alignment);
                tmp.enableAutoSizing = resizeText;
                tmp.fontSizeMin = Mathf.Max(1, resizeMin);
                tmp.fontSizeMax = Mathf.Max(tmp.fontSizeMin, resizeMax);
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
            }
        }

        private static TextAlignmentOptions MapAlignment(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center
            };
        }

        private static void BindResponsiveLayout(
            RectTransform root,
            RectTransform contentRoot,
            RectTransform topLeftHud,
            RectTransform topRightHud,
            RectTransform scoreFrame,
            RectTransform timeFrame,
            RectTransform killFrame,
            RectTransform coinFrame,
            RectTransform pauseButtonRect,
            TextMeshProUGUI timeText,
            TextMeshProUGUI killText,
            TextMeshProUGUI coinText,
            TextMeshProUGUI scoreText)
        {
            if (root == null)
            {
                return;
            }

            ResponsiveHudLayout layout = root.GetComponent<ResponsiveHudLayout>();
            if (layout == null)
            {
                layout = root.gameObject.AddComponent<ResponsiveHudLayout>();
            }

            SafeAreaFitter safeAreaFitter = root.GetComponent<SafeAreaFitter>();
            if (safeAreaFitter == null)
            {
                safeAreaFitter = root.gameObject.AddComponent<SafeAreaFitter>();
            }

            SerializedObject safeAreaObject = new SerializedObject(safeAreaFitter);
            safeAreaObject.FindProperty("target").objectReferenceValue = root;
            safeAreaObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedObject = new SerializedObject(layout);
            serializedObject.FindProperty("contentRoot").objectReferenceValue = contentRoot;
            serializedObject.FindProperty("topLeftHud").objectReferenceValue = topLeftHud;
            serializedObject.FindProperty("topRightHud").objectReferenceValue = topRightHud;
            serializedObject.FindProperty("scoreFrame").objectReferenceValue = scoreFrame;
            serializedObject.FindProperty("timeFrame").objectReferenceValue = timeFrame;
            serializedObject.FindProperty("killFrame").objectReferenceValue = killFrame;
            serializedObject.FindProperty("coinFrame").objectReferenceValue = coinFrame;
            serializedObject.FindProperty("pauseButtonRect").objectReferenceValue = pauseButtonRect;
            serializedObject.FindProperty("timeText").objectReferenceValue = timeText;
            serializedObject.FindProperty("killText").objectReferenceValue = killText;
            serializedObject.FindProperty("coinText").objectReferenceValue = coinText;
            serializedObject.FindProperty("scoreText").objectReferenceValue = scoreText;
            serializedObject.FindProperty("topPadding").floatValue = 48f;
            serializedObject.FindProperty("wideScale").floatValue = 1.12f;
            serializedObject.FindProperty("mediumScale").floatValue = 1f;
            serializedObject.FindProperty("narrowScale").floatValue = 0.9f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            layout.RefreshNow();
        }

        private static void BindUiSystemReferences(
            Button pauseButton,
            TextMeshProUGUI timeText,
            TextMeshProUGUI coinText,
            TextMeshProUGUI killText,
            TextMeshProUGUI scoreText)
        {
            UISystem uiSystem = Resources.FindObjectsOfTypeAll<UISystem>()
                .FirstOrDefault(system => system.gameObject.scene.IsValid());

            if (uiSystem == null)
            {
                Debug.LogWarning("UISystem not found in open scene; HUD references were not bound.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(uiSystem);
            serializedObject.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            serializedObject.FindProperty("timeSurvivalText").objectReferenceValue = timeText;
            serializedObject.FindProperty("moneyText").objectReferenceValue = coinText;
            serializedObject.FindProperty("enemyDefeatedCountText").objectReferenceValue = killText;
            SerializedProperty scoreProperty = serializedObject.FindProperty("scoreText");
            if (scoreProperty != null)
            {
                scoreProperty.objectReferenceValue = scoreText;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindUiSystem()
        {
            UISystem uiSystem = Resources.FindObjectsOfTypeAll<UISystem>()
                .FirstOrDefault(system => system.gameObject.scene.IsValid());

            if (uiSystem == null)
            {
                return;
            }

            EditorUtility.SetDirty(uiSystem);
        }

        private static GameObject FindSceneObjectByPath(string path)
        {
            string[] parts = path.Split('/');
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name != parts[0])
                {
                    continue;
                }

                Transform current = root.transform;
                for (int index = 1; index < parts.Length; index++)
                {
                    current = current.Find(parts[index]);
                    if (current == null)
                    {
                        break;
                    }
                }

                if (current != null)
                {
                    return current.gameObject;
                }
            }

            return null;
        }

        private static void SetLayerRecursive(GameObject gameObject, int layer)
        {
            if (layer < 0)
            {
                return;
            }

            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
#endif
