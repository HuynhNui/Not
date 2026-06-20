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

namespace _Project.Editor
{
    public static class GameplayHudPanelBuilder
    {
        private const string HudPanelPath = "GameCanvas/UIRoot/SafeAreaRoot/GameplayHUDPanel";
        private const string PanelPath = "Assets/_Project/Art/UI/GameplayHudPanel/Optimized/HudPanel_optimized.png";
        private const string ClockPath = "Assets/_Project/Art/UI/GameplayHudPanel/clock.png";
        private const string ScopePath = "Assets/_Project/Art/UI/GameplayHudPanel/ScopeIcon.aseprite";
        private const string CoinPath = "Assets/_Project/Art/UI/GameplayHudPanel/Optimized/CoinIcon_optimized.png";
        private const string SkullPath = "Assets/_Project/Art/UI/GameplayHudPanel/Optimized/SkullIcon_optimized.png";
        private const string PausePath = "Assets/_Project/Art/UI/GameplayHudPanel/PauseBtn.ase";
        private const string AvatarPath = "Assets/_Project/Prefabs/Character/Girl-Sheet.png";
        private const string UpheavalTtfPath = "Assets/Front/upheavtt.ttf";
        private const string UpheavalTmpPath = "Assets/Front/Upheaval_TMP.asset";

        private static readonly Color32 Navy = new Color32(12, 51, 126, 255);
        private static readonly Color32 Cyan = new Color32(47, 144, 255, 255);
        private static readonly Color32 PaleBlue = new Color32(213, 232, 255, 255);

        [MenuItem("Chibi Pixel Gate/UI/Rebuild Gameplay HUD")]
        public static void Rebuild()
        {
            ImportUiTextures();

            TMP_FontAsset fontAsset = EnsureUpheavalFontAsset();
            Sprite panelSprite = LoadSprite(PanelPath);
            Sprite clockSprite = LoadSprite(ClockPath);
            Sprite scopeSprite = LoadSprite(ScopePath, "ScopeIcon");
            Sprite coinSprite = LoadSprite(CoinPath);
            Sprite skullSprite = LoadSprite(SkullPath);
            Sprite pauseSprite = LoadSprite(PausePath, "PauseBtn");
            Sprite avatarSprite = LoadSprite(AvatarPath, "Girl-Sheet_0");

            if (fontAsset == null || panelSprite == null || pauseSprite == null)
            {
                Debug.LogError("Gameplay HUD could not be rebuilt because a required font or sprite is missing.");
                return;
            }

            GameObject hudPanel = FindSceneObjectByPath(HudPanelPath);
            if (hudPanel == null)
            {
                Debug.LogError($"Could not find HUD panel at '{HudPanelPath}'.");
                return;
            }

            CleanupPanel(hudPanel);
            BuildHud(
                hudPanel.transform as RectTransform,
                panelSprite,
                clockSprite,
                scopeSprite,
                coinSprite,
                skullSprite,
                pauseSprite,
                avatarSprite,
                fontAsset);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Rebuilt GameplayHUDPanel with safe-area anchors and layout groups.");
        }

        private static void ImportUiTextures()
        {
            ConfigureSpriteImporter(PanelPath, new Vector4(38f, 30f, 38f, 30f), 512);
            ConfigureSpriteImporter(ClockPath, Vector4.zero, 512);
            ConfigureSpriteImporter(CoinPath, Vector4.zero, 128);
            ConfigureSpriteImporter(SkullPath, Vector4.zero, 128);
            ConfigureSpriteImporter(AvatarPath, Vector4.zero, 2048, false);
        }

        private static void ConfigureSpriteImporter(
            string assetPath,
            Vector4 border,
            int maxSize,
            bool forceSingleSprite = true)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Texture importer not found for {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            if (forceSingleSprite)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
            }

            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;

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
            return fontAsset != null
                && fontAsset.material != null
                && fontAsset.atlasTextures != null
                && fontAsset.atlasTextures.Length > 0
                && fontAsset.atlasTextures[0] != null;
        }

        private static void AddFontSubAssets(TMP_FontAsset fontAsset)
        {
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

            ResponsiveHudLayout oldLayout = hudPanel.GetComponent<ResponsiveHudLayout>();
            if (oldLayout != null)
            {
                Object.DestroyImmediate(oldLayout, true);
            }

            foreach (Canvas canvas in hudPanel.GetComponents<Canvas>())
            {
                Object.DestroyImmediate(canvas, true);
            }

            foreach (CanvasScaler scaler in hudPanel.GetComponents<CanvasScaler>())
            {
                Object.DestroyImmediate(scaler, true);
            }

            foreach (GraphicRaycaster raycaster in hudPanel.GetComponents<GraphicRaycaster>())
            {
                Object.DestroyImmediate(raycaster, true);
            }

            RectTransform root = hudPanel.GetComponent<RectTransform>();
            StretchToParent(root);
            EnsureSafeAreaFitter(root);
            SetLayerRecursive(hudPanel, LayerMask.NameToLayer("UI"));
        }

        private static void BuildHud(
            RectTransform root,
            Sprite panelSprite,
            Sprite clockSprite,
            Sprite scopeSprite,
            Sprite coinSprite,
            Sprite skullSprite,
            Sprite pauseSprite,
            Sprite avatarSprite,
            TMP_FontAsset fontAsset)
        {
            RectTransform contentRoot = CreateRect(
                "HudContentRoot",
                root,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            RectTransform topBar = CreateRect(
                "HudTopBar",
                contentRoot,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -6f),
                new Vector2(-40f, 132f));

            HorizontalLayoutGroup topLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(topLayout, 10f, new RectOffset(0, 0, 2, 2), TextAnchor.MiddleCenter);

            BuildProfileCard(topBar, panelSprite, avatarSprite, fontAsset);

            RectTransform metricsPanel = CreateSlicedPanel(
                "MetricsPanel",
                topBar,
                panelSprite,
                420f,
                620f,
                1f,
                128f);

            HorizontalLayoutGroup metricsLayout = metricsPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(metricsLayout, 0f, new RectOffset(22, 22, 14, 14), TextAnchor.MiddleCenter);

            TextMeshProUGUI timeText = BuildMetric(metricsPanel, "TimeMetric", clockSprite, "TIME", "00:00", fontAsset);
            CreateDivider(metricsPanel);
            TextMeshProUGUI scoreText = BuildMetric(metricsPanel, "ScoreMetric", scopeSprite, "SCORE", "0", fontAsset);
            CreateDivider(metricsPanel);
            TextMeshProUGUI coinText = BuildMetric(metricsPanel, "CoinsMetric", coinSprite, "COINS", "0", fontAsset);
            CreateDivider(metricsPanel);
            TextMeshProUGUI killText = BuildMetric(metricsPanel, "KillsMetric", skullSprite, "KILLS", "0", fontAsset);

            RectTransform pauseButton = CreateButton("PauseButton", topBar, pauseSprite);
            SetLayoutElement(pauseButton.gameObject, 76f, 84f, 0f, 76f, 84f, 0f);

            BindUiSystemReferences(
                pauseButton.GetComponent<Button>(),
                timeText,
                coinText,
                killText,
                scoreText);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(topBar);
        }

        private static void BuildProfileCard(
            RectTransform parent,
            Sprite panelSprite,
            Sprite avatarSprite,
            TMP_FontAsset fontAsset)
        {
            RectTransform profileCard = CreateSlicedPanel(
                "ProfileCard",
                parent,
                panelSprite,
                170f,
                220f,
                0f,
                128f);

            HorizontalLayoutGroup profileLayout = profileCard.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(profileLayout, 8f, new RectOffset(14, 14, 14, 14), TextAnchor.MiddleLeft);

            RectTransform avatar = CreateImage("AvatarImage", profileCard, avatarSprite, true);
            SetLayoutElement(avatar.gameObject, 64f, 76f, 0f, 88f, 96f, 0f);

            RectTransform details = CreateRect(
                "ProfileDetails",
                profileCard,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            SetLayoutElement(details.gameObject, 82f, 100f, 1f, 88f, 96f, 0f);

            VerticalLayoutGroup detailsLayout = details.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(detailsLayout, 1f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);

            CreateText("LevelText", details, fontAsset, "LV.1", Navy, 18f, 24f, TextAlignmentOptions.Left, 25f);
            CreateText("RoleText", details, fontAsset, "COMMANDER", Navy, 12f, 17f, TextAlignmentOptions.Left, 20f);
            CreateText("XpText", details, fontAsset, "0 / 100", Cyan, 11f, 15f, TextAlignmentOptions.Left, 18f);

            RectTransform xpBar = CreateColorImage("XpBar", details, PaleBlue);
            SetLayoutElement(xpBar.gameObject, 40f, 80f, 1f, 8f, 10f, 0f);

            RectTransform fill = CreateColorImage("Fill", xpBar, Cyan);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.25f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = Vector2.zero;
        }

        private static TextMeshProUGUI BuildMetric(
            RectTransform parent,
            string name,
            Sprite iconSprite,
            string label,
            string value,
            TMP_FontAsset fontAsset)
        {
            RectTransform metric = CreateRect(
                name,
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            SetLayoutElement(metric.gameObject, 86f, 124f, 1f, 96f, 100f, 0f);

            VerticalLayoutGroup metricLayout = metric.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(metricLayout, 1f, new RectOffset(5, 5, 3, 3), TextAnchor.MiddleCenter);

            RectTransform header = CreateRect(
                "Header",
                metric,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            SetLayoutElement(header.gameObject, 70f, 100f, 1f, 27f, 29f, 0f);

            HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(headerLayout, 5f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

            RectTransform icon = CreateImage("Icon", header, iconSprite, true);
            SetLayoutElement(icon.gameObject, 22f, 26f, 0f, 22f, 26f, 0f);

            CreateText("LabelText", header, fontAsset, label, Navy, 13f, 19f, TextAlignmentOptions.Left, 26f, 1f);

            return CreateText(
                "ValueText",
                metric,
                fontAsset,
                value,
                Navy,
                21f,
                32f,
                TextAlignmentOptions.Center,
                45f,
                1f);
        }

        private static void CreateDivider(RectTransform parent)
        {
            RectTransform divider = CreateColorImage("Divider", parent, new Color32(77, 131, 216, 100));
            SetLayoutElement(divider.gameObject, 2f, 2f, 0f, 72f, 78f, 0f);
        }

        private static RectTransform CreateSlicedPanel(
            string name,
            Transform parent,
            Sprite sprite,
            float minWidth,
            float preferredWidth,
            float flexibleWidth,
            float height)
        {
            RectTransform panel = CreateImage(name, parent, sprite, false);
            Image image = panel.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            SetLayoutElement(panel.gameObject, minWidth, preferredWidth, flexibleWidth, height, height, 0f);
            return panel;
        }

        private static RectTransform CreateButton(string name, Transform parent, Sprite sprite)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            SetLayerRecursive(gameObject, LayerMask.NameToLayer("UI"));

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            ResetRect(rectTransform);

            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = true;

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.75f, 0.85f, 1f, 1f);
            button.colors = colors;
            return rectTransform;
        }

        private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, bool preserveAspect)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            SetLayerRecursive(gameObject, LayerMask.NameToLayer("UI"));

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            ResetRect(rectTransform);

            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return rectTransform;
        }

        private static RectTransform CreateColorImage(string name, Transform parent, Color color)
        {
            RectTransform rectTransform = CreateImage(name, parent, null, false);
            rectTransform.GetComponent<Image>().color = color;
            return rectTransform;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            TMP_FontAsset fontAsset,
            string value,
            Color color,
            float minimumSize,
            float maximumSize,
            TextAlignmentOptions alignment,
            float preferredHeight,
            float flexibleWidth = 0f)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            SetLayerRecursive(gameObject, LayerMask.NameToLayer("UI"));

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            ResetRect(rectTransform);

            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.text = value;
            text.fontSize = maximumSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = minimumSize;
            text.fontSizeMax = maximumSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;

            SetLayoutElement(gameObject, 0f, 0f, flexibleWidth, preferredHeight, preferredHeight, 0f);
            return text;
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

        private static void ConfigureHorizontalLayout(
            HorizontalLayoutGroup layout,
            float spacing,
            RectOffset padding,
            TextAnchor alignment)
        {
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
        }

        private static void ConfigureVerticalLayout(
            VerticalLayoutGroup layout,
            float spacing,
            RectOffset padding,
            TextAnchor alignment)
        {
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
        }

        private static void SetLayoutElement(
            GameObject target,
            float minWidth,
            float preferredWidth,
            float flexibleWidth,
            float minHeight,
            float preferredHeight,
            float flexibleHeight)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.AddComponent<LayoutElement>();
            }

            element.minWidth = minWidth;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            element.minHeight = minHeight;
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = flexibleHeight;
        }

        private static void EnsureSafeAreaFitter(RectTransform root)
        {
            SafeAreaFitter fitter = root.GetComponent<SafeAreaFitter>();
            if (fitter == null)
            {
                fitter = root.gameObject.AddComponent<SafeAreaFitter>();
            }

            SerializedObject serializedObject = new SerializedObject(fitter);
            serializedObject.FindProperty("target").objectReferenceValue = root;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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
            serializedObject.FindProperty("scoreText").objectReferenceValue = scoreText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uiSystem);
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

        private static void ResetRect(RectTransform rectTransform)
        {
            ConfigureRect(
                rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            ConfigureRect(
                rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
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
