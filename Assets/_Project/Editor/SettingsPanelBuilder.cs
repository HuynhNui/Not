#if UNITY_EDITOR
using System.Linq;
using _Project.Scripts.Systems.UISystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Editor
{
    public static class SettingsPanelBuilder
    {
        private const string CanvasPath = "GameCanvas";
        private const string PanelPath = "GameCanvas/UIRoot/SafeAreaRoot/SettingsPanel";
        private const string FontPath = "Assets/Front/Upheaval_TMP.asset";
        private const string BackgroundPath = "Assets/_Project/Art/Sprites/background/BG-Temp.png";
        private const string BackgroundSpriteName = "ChatGPT Image 13_12_49 18 thg 6, 2026_0";
        private const string ArtRoot = "Assets/_Project/Art/UI/SettingPanel/";
        private const string MainPanelPath = ArtRoot + "setting_main_panel_9slice_source.png";
        private const string RowCardPath = ArtRoot + "setting_row_card_9slice_source.png";
        private const string BackButtonPath = ArtRoot + "back_button.png";
        private const string ToggleOnPath = ArtRoot + "toggle_on.png";
        private const string ToggleOffPath = ArtRoot + "toggle_off.png";
        private const string ResetButtonPath = ArtRoot + "reset_button_bg.png";
        private const string GearIconPath = ArtRoot + "icon_gear.png";
        private const string MusicIconPath = ArtRoot + "icon_music.png";
        private const string SfxIconPath = ArtRoot + "icon_sfx.png";
        private const string VibrationIconPath = ArtRoot + "icon_vibration.png";
        private const string DamageTextIconPath = ArtRoot + "icon_damage_text.png";
        private const string ResetIconPath = ArtRoot + "icon_reset.png";

        private static readonly Color32 OverlayColor = new Color32(3, 8, 18, 132);
        private static readonly Color32 PanelTint = new Color32(255, 255, 255, 255);
        private static readonly Color32 TextNavy = new Color32(10, 38, 88, 255);
        private static readonly Color32 DangerRed = new Color32(206, 55, 63, 255);
        private static readonly Color32 ConfirmShade = new Color32(2, 8, 18, 170);

        [MenuItem("Chibi Pixel Gate/UI/Rebuild Settings Panel")]
        public static void Rebuild()
        {
            ConfigureUiSprites();
            ConfigureCanvas();

            GameObject panel = FindSceneObjectByPath(PanelPath);
            if (panel == null)
            {
                Debug.LogError($"Could not find SettingsPanel at '{PanelPath}'.");
                return;
            }

            Sprite backgroundSprite = LoadSprite(BackgroundPath, BackgroundSpriteName);
            Sprite mainPanelSprite = LoadSprite(MainPanelPath);
            Sprite rowCardSprite = LoadSprite(RowCardPath);
            Sprite backButtonSprite = LoadSprite(BackButtonPath);
            Sprite toggleOnSprite = LoadSprite(ToggleOnPath);
            Sprite toggleOffSprite = LoadSprite(ToggleOffPath);
            Sprite resetButtonSprite = LoadSprite(ResetButtonPath);
            Sprite gearIconSprite = LoadSprite(GearIconPath);
            Sprite musicIconSprite = LoadSprite(MusicIconPath);
            Sprite sfxIconSprite = LoadSprite(SfxIconPath);
            Sprite vibrationIconSprite = LoadSprite(VibrationIconPath);
            Sprite damageTextIconSprite = LoadSprite(DamageTextIconPath);
            Sprite resetIconSprite = LoadSprite(ResetIconPath);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                ?? TMP_Settings.defaultFontAsset;

            if (mainPanelSprite == null || rowCardSprite == null || toggleOnSprite == null || toggleOffSprite == null)
            {
                Debug.LogError("SettingsPanel could not be rebuilt because required sprites are missing.");
                return;
            }

            CleanupPanel(panel);
            BuildPanel(
                panel.transform as RectTransform,
                backgroundSprite,
                mainPanelSprite,
                rowCardSprite,
                backButtonSprite,
                toggleOnSprite,
                toggleOffSprite,
                resetButtonSprite,
                gearIconSprite,
                musicIconSprite,
                sfxIconSprite,
                vibrationIconSprite,
                damageTextIconSprite,
                resetIconSprite,
                fontAsset,
                out Button backButton,
                out Toggle musicToggle,
                out Toggle sfxToggle,
                out Toggle vibrationToggle,
                out Toggle damageTextToggle,
                out Button resetDataButton,
                out GameObject confirmPopup,
                out Button confirmCancelButton,
                out Button confirmButton);

            BindUiSystem(
                backButton,
                musicToggle,
                sfxToggle,
                vibrationToggle,
                damageTextToggle,
                resetDataButton,
                confirmPopup,
                confirmCancelButton,
                confirmButton,
                toggleOnSprite,
                toggleOffSprite);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Rebuilt SettingsPanel with safe-area layout, four toggles, and reset confirmation popup.");
        }

        private static void ConfigureCanvas()
        {
            GameObject canvasObject = FindSceneObjectByPath(CanvasPath);
            if (canvasObject == null)
            {
                Debug.LogWarning("GameCanvas was not found while configuring SettingsPanel CanvasScaler.");
                return;
            }

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void ConfigureUiSprites()
        {
            ConfigureSprite(MainPanelPath, new Vector4(80f, 80f, 80f, 80f), 2048);
            ConfigureSprite(RowCardPath, new Vector4(48f, 42f, 48f, 42f), 1024);
            ConfigureSprite(ResetButtonPath, new Vector4(46f, 28f, 46f, 28f), 1024);
            ConfigureSprite(BackButtonPath, Vector4.zero, 512);
            ConfigureSprite(ToggleOnPath, Vector4.zero, 512);
            ConfigureSprite(ToggleOffPath, Vector4.zero, 512);
            ConfigureSprite(GearIconPath, Vector4.zero, 512);
            ConfigureSprite(MusicIconPath, Vector4.zero, 512);
            ConfigureSprite(SfxIconPath, Vector4.zero, 512);
            ConfigureSprite(VibrationIconPath, Vector4.zero, 512);
            ConfigureSprite(DamageTextIconPath, Vector4.zero, 512);
            ConfigureSprite(ResetIconPath, Vector4.zero, 512);
        }

        private static void ConfigureSprite(string path, Vector4 border, int maxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Could not configure sprite importer at '{path}'.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        private static void CleanupPanel(GameObject panel)
        {
            foreach (Transform child in panel.transform.Cast<Transform>().ToArray())
            {
                Object.DestroyImmediate(child.gameObject);
            }

            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                Object.DestroyImmediate(image, true);
            }

            CanvasRenderer renderer = panel.GetComponent<CanvasRenderer>();
            if (renderer != null)
            {
                Object.DestroyImmediate(renderer, true);
            }

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            StretchToParent(rectTransform);
            panel.layer = LayerMask.NameToLayer("UI");

            SafeAreaFitter fitter = panel.GetComponent<SafeAreaFitter>();
            if (fitter != null)
            {
                Object.DestroyImmediate(fitter, true);
            }
        }

        private static void BuildPanel(
            RectTransform root,
            Sprite backgroundSprite,
            Sprite mainPanelSprite,
            Sprite rowCardSprite,
            Sprite backButtonSprite,
            Sprite toggleOnSprite,
            Sprite toggleOffSprite,
            Sprite resetButtonSprite,
            Sprite gearIconSprite,
            Sprite musicIconSprite,
            Sprite sfxIconSprite,
            Sprite vibrationIconSprite,
            Sprite damageTextIconSprite,
            Sprite resetIconSprite,
            TMP_FontAsset fontAsset,
            out Button backButton,
            out Toggle musicToggle,
            out Toggle sfxToggle,
            out Toggle vibrationToggle,
            out Toggle damageTextToggle,
            out Button resetDataButton,
            out GameObject confirmPopup,
            out Button confirmCancelButton,
            out Button confirmButton)
        {
            RectTransform background = CreateImage(
                "Background",
                root,
                backgroundSprite,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                Color.white,
                true,
                false);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = true;
            if (backgroundSprite != null)
            {
                AspectRatioFitter aspectRatioFitter = background.gameObject.AddComponent<AspectRatioFitter>();
                aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspectRatioFitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
            }

            CreateImage(
                "Overlay",
                root,
                null,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                OverlayColor,
                false,
                true);

            RectTransform mainPanel = CreateImage(
                "MainPanel",
                root,
                mainPanelSprite,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(920f, 1180f),
                PanelTint);
            Image mainPanelImage = mainPanel.GetComponent<Image>();
            mainPanelImage.type = Image.Type.Sliced;

            VerticalLayoutGroup mainLayout = mainPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(mainLayout, 30f, new RectOffset(64, 64, 54, 46), TextAnchor.UpperCenter);

            SetLayoutElement(mainPanel.gameObject, 0f, 0f, 0f, 0f, 0f, 0f, true);

            RectTransform header = CreateRect("Header", mainPanel);
            SetLayoutElement(header.gameObject, 0f, 0f, 1f, 104f, 104f, 0f);
            HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(headerLayout, 18f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

            RectTransform backButtonRect = CreateButton("BackButton", header, backButtonSprite, Color.white, true);
            SetLayoutElement(backButtonRect.gameObject, 86f, 86f, 0f, 86f, 86f, 0f);
            backButton = backButtonRect.GetComponent<Button>();

            RectTransform titleGroup = CreateRect("TitleGroup", header);
            SetLayoutElement(titleGroup.gameObject, 0f, 0f, 1f, 96f, 96f, 0f);
            HorizontalLayoutGroup titleLayout = titleGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(titleLayout, 22f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

            RectTransform gearIcon = CreateImage("GearIcon", titleGroup, gearIconSprite, Color.white, true);
            SetLayoutElement(gearIcon.gameObject, 68f, 68f, 0f, 68f, 68f, 0f);
            TextMeshProUGUI titleText = CreateText("TitleText", titleGroup, fontAsset, "SETTINGS", 64f, TextNavy, TextAlignmentOptions.Center);
            titleText.fontSizeMin = 44f;
            titleText.fontSizeMax = 64f;
            SetLayoutElement(titleText.gameObject, 260f, 360f, 0f, 82f, 82f, 0f);

            RectTransform rightSpacer = CreateRect("RightSpacer", header);
            SetLayoutElement(rightSpacer.gameObject, 86f, 86f, 0f, 86f, 86f, 0f);

            RectTransform rowsContainer = CreateRect("RowsContainer", mainPanel);
            SetLayoutElement(rowsContainer.gameObject, 0f, 0f, 1f, 560f, 560f, 0f);
            VerticalLayoutGroup rowsLayout = rowsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(rowsLayout, 28f, new RectOffset(0, 0, 0, 0), TextAnchor.UpperCenter);

            musicToggle = BuildSettingsRow(rowsContainer, "MusicRow", musicIconSprite, "MUSIC", rowCardSprite, toggleOnSprite, fontAsset);
            sfxToggle = BuildSettingsRow(rowsContainer, "SfxRow", sfxIconSprite, "SFX", rowCardSprite, toggleOnSprite, fontAsset);
            vibrationToggle = BuildSettingsRow(rowsContainer, "VibrationRow", vibrationIconSprite, "VIBRATION", rowCardSprite, toggleOnSprite, fontAsset);
            damageTextToggle = BuildSettingsRow(rowsContainer, "DamageTextRow", damageTextIconSprite, "DAMAGE TEXT", rowCardSprite, toggleOnSprite, fontAsset);

            RectTransform resetButtonRect = CreateButton("ResetButton", mainPanel, resetButtonSprite, Color.white, false);
            resetButtonRect.GetComponent<Image>().type = Image.Type.Sliced;
            SetLayoutElement(resetButtonRect.gameObject, 340f, 380f, 0f, 84f, 84f, 0f);
            resetDataButton = resetButtonRect.GetComponent<Button>();

            HorizontalLayoutGroup resetLayout = resetButtonRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(resetLayout, 16f, new RectOffset(34, 28, 0, 0), TextAnchor.MiddleCenter);

            TextMeshProUGUI resetText = CreateText("ResetButtonText", resetButtonRect, fontAsset, "RESET DATA", 38f, DangerRed, TextAlignmentOptions.Center);
            resetText.fontSizeMin = 28f;
            resetText.fontSizeMax = 38f;
            SetLayoutElement(resetText.gameObject, 190f, 220f, 0f, 64f, 64f, 0f);

            RectTransform resetIcon = CreateImage("ResetIcon", resetButtonRect, resetIconSprite, Color.white, true);
            SetLayoutElement(resetIcon.gameObject, 46f, 46f, 0f, 46f, 46f, 0f);

            BuildConfirmPopup(
                root,
                mainPanelSprite,
                resetButtonSprite,
                fontAsset,
                out confirmPopup,
                out confirmCancelButton,
                out confirmButton);

            confirmPopup.SetActive(false);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainPanel);
        }

        private static Toggle BuildSettingsRow(
            RectTransform parent,
            string rowName,
            Sprite iconSprite,
            string label,
            Sprite rowCardSprite,
            Sprite toggleOnSprite,
            TMP_FontAsset fontAsset)
        {
            RectTransform row = CreateImage(rowName, parent, rowCardSprite, PanelTint);
            row.GetComponent<Image>().type = Image.Type.Sliced;
            SetLayoutElement(row.gameObject, 0f, 0f, 1f, 118f, 118f, 0f);

            HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(rowLayout, 28f, new RectOffset(42, 42, 0, 0), TextAnchor.MiddleCenter);

            RectTransform icon = CreateImage("Icon", row, iconSprite, Color.white, true);
            SetLayoutElement(icon.gameObject, 72f, 72f, 0f, 72f, 72f, 0f);

            TextMeshProUGUI labelText = CreateText("LabelText", row, fontAsset, label, 42f, TextNavy, TextAlignmentOptions.MidlineLeft);
            labelText.fontSizeMin = 26f;
            labelText.fontSizeMax = 42f;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            SetLayoutElement(labelText.gameObject, 0f, 0f, 1f, 74f, 74f, 0f);

            RectTransform toggleRect = CreateToggle("ToggleButton", row, toggleOnSprite);
            SetLayoutElement(toggleRect.gameObject, 164f, 164f, 0f, 72f, 72f, 0f);
            return toggleRect.GetComponent<Toggle>();
        }

        private static void BuildConfirmPopup(
            RectTransform root,
            Sprite panelSprite,
            Sprite buttonSprite,
            TMP_FontAsset fontAsset,
            out GameObject confirmPopup,
            out Button cancelButton,
            out Button confirmButton)
        {
            RectTransform popupRoot = CreateRect(
                "ResetConfirmPopup",
                root,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            confirmPopup = popupRoot.gameObject;

            CreateImage(
                "ConfirmOverlay",
                popupRoot,
                null,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                ConfirmShade,
                false,
                true);

            RectTransform confirmPanel = CreateImage(
                "ConfirmPanel",
                popupRoot,
                panelSprite,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(780f, 430f),
                PanelTint);
            confirmPanel.GetComponent<Image>().type = Image.Type.Sliced;

            VerticalLayoutGroup panelLayout = confirmPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(panelLayout, 24f, new RectOffset(54, 54, 52, 44), TextAnchor.UpperCenter);

            TextMeshProUGUI title = CreateText("TitleText", confirmPanel, fontAsset, "RESET DATA?", 50f, DangerRed, TextAlignmentOptions.Center);
            title.fontSizeMin = 34f;
            title.fontSizeMax = 50f;
            SetLayoutElement(title.gameObject, 0f, 0f, 1f, 72f, 72f, 0f);

            TextMeshProUGUI body = CreateText(
                "BodyText",
                confirmPanel,
                fontAsset,
                "CLEAR PLAYER PROGRESS ONLY",
                32f,
                TextNavy,
                TextAlignmentOptions.Center);
            body.fontSizeMin = 24f;
            body.fontSizeMax = 32f;
            body.enableWordWrapping = true;
            SetLayoutElement(body.gameObject, 0f, 0f, 1f, 96f, 96f, 0f);

            RectTransform buttonRow = CreateRect("ButtonRow", confirmPanel);
            SetLayoutElement(buttonRow.gameObject, 0f, 0f, 1f, 92f, 92f, 0f);
            HorizontalLayoutGroup buttonRowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(buttonRowLayout, 34f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

            RectTransform cancelRect = CreateButton("CancelButton", buttonRow, buttonSprite, Color.white, false);
            cancelRect.GetComponent<Image>().type = Image.Type.Sliced;
            SetLayoutElement(cancelRect.gameObject, 250f, 250f, 0f, 78f, 78f, 0f);
            cancelButton = cancelRect.GetComponent<Button>();
            TextMeshProUGUI cancelText = CreateText("Text", cancelRect, fontAsset, "CANCEL", 34f, TextNavy, TextAlignmentOptions.Center);
            StretchToParent(cancelText.rectTransform);
            cancelText.fontSizeMin = 24f;
            cancelText.fontSizeMax = 34f;

            RectTransform confirmRect = CreateButton("ConfirmButton", buttonRow, buttonSprite, Color.white, false);
            confirmRect.GetComponent<Image>().type = Image.Type.Sliced;
            SetLayoutElement(confirmRect.gameObject, 250f, 250f, 0f, 78f, 78f, 0f);
            confirmButton = confirmRect.GetComponent<Button>();
            TextMeshProUGUI confirmText = CreateText("Text", confirmRect, fontAsset, "RESET", 34f, DangerRed, TextAlignmentOptions.Center);
            StretchToParent(confirmText.rectTransform);
            confirmText.fontSizeMin = 24f;
            confirmText.fontSizeMax = 34f;
        }

        private static RectTransform CreateToggle(string name, Transform parent, Sprite sprite)
        {
            RectTransform rectTransform = CreateImage(name, parent, sprite, Color.white, true);
            Image image = rectTransform.GetComponent<Image>();
            image.raycastTarget = true;

            Toggle toggle = rectTransform.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            toggle.transition = Selectable.Transition.None;
            toggle.isOn = true;
            return rectTransform;
        }

        private static RectTransform CreateButton(string name, Transform parent, Sprite sprite, Color color, bool preserveAspect)
        {
            RectTransform rectTransform = CreateImage(name, parent, sprite, color, preserveAspect, true);
            Image image = rectTransform.GetComponent<Image>();
            Button button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            colors.pressedColor = new Color(0.78f, 0.86f, 0.96f, 1f);
            colors.disabledColor = new Color(0.5f, 0.55f, 0.65f, 0.7f);
            button.colors = colors;
            return rectTransform;
        }

        private static RectTransform CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            bool preserveAspect = false,
            bool raycastTarget = false)
        {
            RectTransform rectTransform = CreateRect(name, parent);
            Image image = rectTransform.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = raycastTarget;
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
            Color color,
            bool preserveAspect = false,
            bool raycastTarget = false)
        {
            RectTransform rectTransform = CreateRect(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            Image image = rectTransform.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = raycastTarget;
            return rectTransform;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            TMP_FontAsset fontAsset,
            string value,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            RectTransform rectTransform = CreateRect(name, parent);
            TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(18f, fontSize * 0.65f);
            text.fontSizeMax = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.characterSpacing = 0f;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            return CreateRect(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
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
            float flexibleHeight,
            bool ignoreLayout = false)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.AddComponent<LayoutElement>();
            }

            element.ignoreLayout = ignoreLayout;
            element.minWidth = minWidth;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            element.minHeight = minHeight;
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = flexibleHeight;
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
            ConfigureRect(
                rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
        }

        private static void BindUiSystem(
            Button backButton,
            Toggle musicToggle,
            Toggle sfxToggle,
            Toggle vibrationToggle,
            Toggle damageTextToggle,
            Button resetDataButton,
            GameObject confirmPopup,
            Button confirmCancelButton,
            Button confirmButton,
            Sprite toggleOnSprite,
            Sprite toggleOffSprite)
        {
            UISystem uiSystem = Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            if (uiSystem == null)
            {
                Debug.LogError("UISystem was not found while binding SettingsPanel.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(uiSystem);
            SetReference(serializedObject, "settingsBackButton", backButton);
            SetReference(serializedObject, "musicToggle", musicToggle);
            SetReference(serializedObject, "sfxToggle", sfxToggle);
            SetReference(serializedObject, "vibrationToggle", vibrationToggle);
            SetReference(serializedObject, "damageTextToggle", damageTextToggle);
            SetReference(serializedObject, "resetDataButton", resetDataButton);
            SetReference(serializedObject, "resetConfirmPopup", confirmPopup);
            SetReference(serializedObject, "resetConfirmCancelButton", confirmCancelButton);
            SetReference(serializedObject, "resetConfirmButton", confirmButton);
            SetReference(serializedObject, "settingsToggleOnSprite", toggleOnSprite);
            SetReference(serializedObject, "settingsToggleOffSprite", toggleOffSprite);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uiSystem);
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static Sprite LoadSprite(string path, string spriteName = null)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == spriteName);
        }

        private static GameObject FindSceneObjectByPath(string path)
        {
            string[] parts = path.Split('/');
            GameObject current = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .FirstOrDefault(root => root.name == parts[0]);

            for (int index = 1; current != null && index < parts.Length; index++)
            {
                Transform child = current.transform.Find(parts[index]);
                current = child != null ? child.gameObject : null;
            }

            return current;
        }

        private static void SetLayerRecursive(GameObject gameObject, int layer)
        {
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }

            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
#endif
