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
    public static class GameOverPanelBuilder
    {
        private const string PanelPath = "GameCanvas/UIRoot/SafeAreaRoot/GameOverPanel";
        private const string LosePanelPath = "Assets/_Project/Art/UI/LOSE/Panel.png";
        private const string CoinPanelPath = "Assets/_Project/Art/UI/LOSE/CoinPanel.png";
        private const string RetryButtonPath = "Assets/_Project/Art/UI/LOSE/RetryBtn.png";
        private const string UpgradeButtonPath = "Assets/_Project/Art/UI/LOSE/UpgradeBtn.png";
        private const string HomeButtonPath = "Assets/_Project/Art/UI/LOSE/HomeBtn.png";
        private const string DefeatIconPath = "Assets/_Project/Art/UI/LOSE/UNIX07_Death.aseprite";
        private const string ClockIconPath = "Assets/_Project/Art/UI/LOSE/ClockIcon.aseprite";
        private const string ScopeIconPath = "Assets/_Project/Art/UI/LOSE/ScopeIcon.aseprite";
        private const string CoinIconPath = "Assets/_Project/Art/UI/LOSE/CoinIcon_optimized.png";
        private const string SkullIconPath = "Assets/_Project/Art/UI/LOSE/skull icon.aseprite";
        private const string BestScoreIconPath = "Assets/_Project/Art/UI/LOSE/BestscoreIcon.aseprite";
        private const string FontPath = "Assets/Front/Upheaval_TMP.asset";

        private static readonly Color32 Navy = new Color32(9, 43, 116, 255);
        private static readonly Color32 DeepNavy = new Color32(4, 20, 62, 255);
        private static readonly Color32 PalePanel = new Color32(249, 253, 255, 255);
        private static readonly Color32 PaleBlue = new Color32(226, 241, 255, 255);
        private static readonly Color32 LineBlue = new Color32(80, 153, 255, 255);
        private static readonly Color32 OverlayColor = new Color32(3, 9, 28, 145);
        private static readonly Color32 WhiteText = new Color32(255, 255, 255, 255);

        private sealed class GameOverReferences
        {
            public GameOverPanelUI Controller;
            public TextMeshProUGUI TitleText;
            public TextMeshProUGUI SubtitleText;
            public TextMeshProUGUI FinalTimeText;
            public TextMeshProUGUI FinalScoreText;
            public TextMeshProUGUI MoneyEarnedText;
            public TextMeshProUGUI FinalKillText;
            public TextMeshProUGUI CoinRewardValueText;
            public TextMeshProUGUI BestScoreText;
            public TextMeshProUGUI BestTimeText;
            public TextMeshProUGUI BestKillsText;
            public Button RetryButton;
            public Button UpgradeButton;
            public Button HomeButton;
        }

        [MenuItem("Chibi Pixel Gate/UI/Rebuild Game Over Panel")]
        public static void Rebuild()
        {
            ConfigureUiSprite(LosePanelPath, new Vector4(190f, 190f, 190f, 190f), 2048);
            ConfigureUiSprite(CoinPanelPath, new Vector4(38f, 30f, 38f, 30f), 1024);
            ConfigureUiSprite(RetryButtonPath, new Vector4(360f, 220f, 360f, 220f), 2048);
            ConfigureUiSprite(UpgradeButtonPath, new Vector4(360f, 220f, 360f, 220f), 2048);
            ConfigureUiSprite(HomeButtonPath, new Vector4(360f, 220f, 360f, 220f), 2048);
            ConfigureUiSprite(CoinIconPath, Vector4.zero, 256);

            Sprite panelSprite = LoadSprite(LosePanelPath);
            Sprite cardSprite = LoadSprite(CoinPanelPath);
            Sprite retrySprite = LoadSprite(RetryButtonPath);
            Sprite upgradeSprite = LoadSprite(UpgradeButtonPath);
            Sprite homeSprite = LoadSprite(HomeButtonPath);
            Sprite defeatIcon = LoadSprite(DefeatIconPath, "UNIX07_Death");
            Sprite clockIcon = LoadSprite(ClockIconPath, "ClockIcon");
            Sprite scopeIcon = LoadSprite(ScopeIconPath, "ScopeIcon");
            Sprite coinIcon = LoadSprite(CoinIconPath);
            Sprite skullIcon = LoadSprite(SkullIconPath, "skull icon");
            Sprite bestScoreIcon = LoadSprite(BestScoreIconPath, "BestscoreIcon");
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                ?? TMP_Settings.defaultFontAsset;

            if (panelSprite == null || cardSprite == null || retrySprite == null || upgradeSprite == null || homeSprite == null)
            {
                Debug.LogError("GameOverPanel could not be rebuilt because a required LOSE UI sprite is missing.");
                return;
            }

            GameObject panel = FindSceneObjectByPath(PanelPath) ?? CreatePanelRoot();
            if (panel == null)
            {
                Debug.LogError($"Could not create or find GameOverPanel at '{PanelPath}'.");
                return;
            }

            CleanupPanel(panel);
            BuildPanel(
                panel.transform as RectTransform,
                panelSprite,
                cardSprite,
                retrySprite,
                upgradeSprite,
                homeSprite,
                defeatIcon,
                clockIcon,
                scopeIcon,
                coinIcon,
                skullIcon,
                bestScoreIcon,
                fontAsset,
                out GameOverReferences references);

            BindPanelController(panel, references);
            BindUiSystem(panel, references);

            panel.SetActive(false);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Rebuilt GameOverPanel with responsive safe-area layout and UI bindings.");
        }

        private static void BuildPanel(
            RectTransform root,
            Sprite panelSprite,
            Sprite cardSprite,
            Sprite retrySprite,
            Sprite upgradeSprite,
            Sprite homeSprite,
            Sprite defeatIcon,
            Sprite clockIcon,
            Sprite scopeIcon,
            Sprite coinIcon,
            Sprite skullIcon,
            Sprite bestScoreIcon,
            TMP_FontAsset fontAsset,
            out GameOverReferences references)
        {
            references = new GameOverReferences();

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

            RectTransform contentFrame = CreateRect(
                "GameOverContentFrame",
                root,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            EnsureSafeAreaFitter(contentFrame);

            RectTransform panelCard = CreateRect(
                "PanelCard",
                contentFrame,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(980f, 1780f));

            RectTransform panelBackground = CreateImage(
                "PanelBackground",
                panelCard,
                panelSprite,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1140f, 1940f),
                PalePanel,
                false,
                false);
            panelBackground.SetAsFirstSibling();

            GameOverPanelResponsiveLayout responsiveLayout = contentFrame.gameObject.AddComponent<GameOverPanelResponsiveLayout>();
            SerializedObject layoutObject = new SerializedObject(responsiveLayout);
            layoutObject.FindProperty("panelCard").objectReferenceValue = panelCard;
            layoutObject.FindProperty("referencePanelSize").vector2Value = new Vector2(980f, 1780f);
            layoutObject.FindProperty("safeAreaWidthRatio").floatValue = 0.92f;
            layoutObject.FindProperty("safeAreaHeightRatio").floatValue = 0.965f;
            layoutObject.ApplyModifiedPropertiesWithoutUndo();

            RectTransform contentRoot = CreateRect(
                "ContentRoot",
                panelCard,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            contentRoot.offsetMin = new Vector2(62f, 62f);
            contentRoot.offsetMax = new Vector2(-62f, -62f);

            VerticalLayoutGroup contentLayout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(contentLayout, 18f, new RectOffset(0, 0, 8, 0), TextAnchor.UpperCenter);

            BuildHeader(contentRoot, defeatIcon, fontAsset, references);
            BuildStatsSection(contentRoot, cardSprite, clockIcon, scopeIcon, coinIcon, skullIcon, fontAsset, references);
            BuildRewardSection(contentRoot, cardSprite, coinIcon, fontAsset, references);
            BuildButtonsSection(contentRoot, retrySprite, upgradeSprite, homeSprite, fontAsset, references);
            BuildFooterSection(contentRoot, scopeIcon, clockIcon, skullIcon, bestScoreIcon, fontAsset, references);

            references.Controller = root.gameObject.AddComponent<GameOverPanelUI>();
        }

        private static void BuildHeader(
            RectTransform parent,
            Sprite defeatIcon,
            TMP_FontAsset fontAsset,
            GameOverReferences references)
        {
            RectTransform section = CreateLayoutSection("HeaderSection", parent, 400f);
            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(layout, 6f, new RectOffset(0, 0, 4, 0), TextAnchor.UpperCenter);

            RectTransform icon = CreateImage("DefeatIcon", section, defeatIcon, true);
            SetLayoutElement(icon.gameObject, 190f, 210f, 0f, 170f, 190f, 0f);

            references.TitleText = CreateText(
                "TitleText",
                section,
                fontAsset,
                "RUN FAILED",
                92f,
                Navy,
                TextAlignmentOptions.Center,
                104f,
                true,
                58f,
                96f);
            AddShadow(references.TitleText.gameObject, new Color(0f, 0f, 0f, 0.14f), new Vector2(0f, -3f));

            references.SubtitleText = CreateText(
                "SubtitleText",
                section,
                fontAsset,
                "THE LOOP WILL CONTINUE, COMMANDER!",
                33f,
                Navy,
                TextAlignmentOptions.Center,
                50f,
                true,
                22f,
                36f);
        }

        private static void BuildStatsSection(
            RectTransform parent,
            Sprite cardSprite,
            Sprite clockIcon,
            Sprite scopeIcon,
            Sprite coinIcon,
            Sprite skullIcon,
            TMP_FontAsset fontAsset,
            GameOverReferences references)
        {
            RectTransform section = CreateLayoutSection("StatsSection", parent, 238f);
            RectTransform grid = CreateRect(
                "StatsGrid",
                section,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            HorizontalLayoutGroup layout = grid.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(layout, 12f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

            references.FinalTimeText = BuildStatCard(grid, "TimeStat", clockIcon, "TIME", "01:42", "FinalTimeText", fontAsset);
            references.FinalScoreText = BuildStatCard(grid, "ScoreStat", scopeIcon, "SCORE", "12,350", "FinalScoreText", fontAsset);
            references.MoneyEarnedText = BuildStatCard(grid, "CoinsStat", coinIcon, "COINS", "218", "MoneyEarnedText", fontAsset);
            references.FinalKillText = BuildStatCard(grid, "KillsStat", skullIcon, "KILLS", "156", "FinalKillText", fontAsset);

            foreach (Transform child in grid)
            {
                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = cardSprite;
                    image.type = Image.Type.Sliced;
                }
            }
        }

        private static TextMeshProUGUI BuildStatCard(
            RectTransform parent,
            string name,
            Sprite iconSprite,
            string label,
            string value,
            string valueName,
            TMP_FontAsset fontAsset)
        {
            RectTransform card = CreateImage(
                name,
                parent,
                null,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                Color.white,
                false,
                false);
            SetLayoutElement(card.gameObject, 0f, 196f, 1f, 220f, 226f, 0f);

            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(layout, 4f, new RectOffset(12, 12, 18, 14), TextAnchor.UpperCenter);

            RectTransform icon = CreateImage("Icon", card, iconSprite, true);
            SetLayoutElement(icon.gameObject, 56f, 62f, 0f, 56f, 62f, 0f);

            CreateText(
                "LabelText",
                card,
                fontAsset,
                label,
                28f,
                Navy,
                TextAlignmentOptions.Center,
                36f,
                true,
                18f,
                29f);

            return CreateText(
                valueName,
                card,
                fontAsset,
                value,
                38f,
                Navy,
                TextAlignmentOptions.Center,
                52f,
                true,
                24f,
                40f);
        }

        private static void BuildRewardSection(
            RectTransform parent,
            Sprite cardSprite,
            Sprite coinIcon,
            TMP_FontAsset fontAsset,
            GameOverReferences references)
        {
            RectTransform section = CreateLayoutSection("RewardSection", parent, 138f);
            RectTransform panel = CreateImage(
                "CoinRewardPanel",
                section,
                cardSprite,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                Color.white,
                false,
                false);

            HorizontalLayoutGroup layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(layout, 30f, new RectOffset(58, 58, 18, 18), TextAnchor.MiddleCenter);

            RectTransform icon = CreateImage("CoinIcon", panel, coinIcon, true);
            SetLayoutElement(icon.gameObject, 86f, 92f, 0f, 86f, 92f, 0f);

            CreateText(
                "CoinRewardLabelText",
                panel,
                fontAsset,
                "COINS",
                52f,
                Navy,
                TextAlignmentOptions.MidlineLeft,
                90f,
                true,
                34f,
                54f,
                1f);

            references.CoinRewardValueText = CreateText(
                "CoinRewardValueText",
                panel,
                fontAsset,
                "+0",
                52f,
                Navy,
                TextAlignmentOptions.MidlineRight,
                90f,
                true,
                34f,
                54f);
            SetLayoutElement(references.CoinRewardValueText.gameObject, 170f, 230f, 0f, 86f, 92f, 0f);
        }

        private static void BuildButtonsSection(
            RectTransform parent,
            Sprite retrySprite,
            Sprite upgradeSprite,
            Sprite homeSprite,
            TMP_FontAsset fontAsset,
            GameOverReferences references)
        {
            RectTransform section = CreateLayoutSection("ButtonsSection", parent, 432f);
            RectTransform stack = CreateRect(
                "ButtonsStack",
                section,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            VerticalLayoutGroup layout = stack.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(layout, 22f, new RectOffset(12, 12, 0, 0), TextAnchor.MiddleCenter);

            references.RetryButton = BuildButton(stack, "RetryButton", retrySprite, "RETRY", fontAsset, WhiteText, true);
            references.UpgradeButton = BuildButton(stack, "GameOverUpgradeButton", upgradeSprite, "UPGRADE", fontAsset, WhiteText, true);
            references.HomeButton = BuildButton(stack, "GameOverHomeButton", homeSprite, "HOME", fontAsset, Navy, false);
        }

        private static Button BuildButton(
            RectTransform parent,
            string name,
            Sprite sprite,
            string label,
            TMP_FontAsset fontAsset,
            Color textColor,
            bool shadow)
        {
            RectTransform buttonRect = CreateButton(name, parent, sprite, Color.white);
            SetLayoutElement(buttonRect.gameObject, 0f, 790f, 1f, 120f, 124f, 0f);

            TextMeshProUGUI labelText = CreateText(
                "Label",
                buttonRect,
                fontAsset,
                label,
                68f,
                textColor,
                TextAlignmentOptions.Center,
                0f,
                true,
                42f,
                70f);
            StretchToParent(labelText.rectTransform);
            labelText.margin = new Vector4(38f, 0f, 38f, 10f);

            if (shadow)
            {
                AddShadow(labelText.gameObject, new Color(0.02f, 0.08f, 0.23f, 0.42f), new Vector2(0f, -4f));
            }

            return buttonRect.GetComponent<Button>();
        }

        private static void BuildFooterSection(
            RectTransform parent,
            Sprite scopeIcon,
            Sprite clockIcon,
            Sprite skullIcon,
            Sprite trophyIcon,
            TMP_FontAsset fontAsset,
            GameOverReferences references)
        {
            RectTransform section = CreateLayoutSection("FooterSection", parent, 188f);
            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(layout, 8f, new RectOffset(30, 30, 8, 0), TextAnchor.UpperCenter);

            RectTransform line = CreateImage("TopDivider", section, null, PaleBlue);
            SetLayoutElement(line.gameObject, 0f, 780f, 1f, 5f, 5f, 0f);

            RectTransform header = CreateRect("BestRecordHeader", section, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetLayoutElement(header.gameObject, 0f, 360f, 0f, 44f, 48f, 0f);
            HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(headerLayout, 10f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

            RectTransform trophy = CreateImage("TrophyIcon", header, trophyIcon, true);
            SetLayoutElement(trophy.gameObject, 36f, 42f, 0f, 36f, 42f, 0f);

            CreateText(
                "BestRecordLabelText",
                header,
                fontAsset,
                "BEST RECORD",
                29f,
                Navy,
                TextAlignmentOptions.MidlineLeft,
                42f,
                true,
                20f,
                30f);

            RectTransform row = CreateRect("BestRecordRow", section, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetLayoutElement(row.gameObject, 0f, 720f, 1f, 64f, 68f, 0f);
            HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(rowLayout, 18f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

            references.BestScoreText = BuildBestRecordGroup(row, "BestScoreGroup", scopeIcon, "12,350", "GameOverBestScoreText", fontAsset);
            references.BestTimeText = BuildBestRecordGroup(row, "BestTimeGroup", clockIcon, "01:42", "GameOverBestTimeText", fontAsset);
            references.BestKillsText = BuildBestRecordGroup(row, "BestKillsGroup", skullIcon, "156", "GameOverBestKillsText", fontAsset);
        }

        private static TextMeshProUGUI BuildBestRecordGroup(
            RectTransform parent,
            string name,
            Sprite iconSprite,
            string value,
            string textName,
            TMP_FontAsset fontAsset)
        {
            RectTransform group = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetLayoutElement(group.gameObject, 160f, 200f, 1f, 54f, 58f, 0f);
            HorizontalLayoutGroup layout = group.gameObject.AddComponent<HorizontalLayoutGroup>();
            ConfigureHorizontalLayout(layout, 8f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

            RectTransform icon = CreateImage("Icon", group, iconSprite, true);
            SetLayoutElement(icon.gameObject, 32f, 36f, 0f, 32f, 36f, 0f);

            return CreateText(
                textName,
                group,
                fontAsset,
                value,
                26f,
                Navy,
                TextAlignmentOptions.MidlineLeft,
                42f,
                true,
                18f,
                27f,
                1f);
        }

        private static RectTransform CreateLayoutSection(string name, Transform parent, float preferredHeight)
        {
            RectTransform section = CreateRect(
                name,
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            SetLayoutElement(section.gameObject, 0f, 0f, 1f, preferredHeight, preferredHeight, 0f);
            return section;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            TMP_FontAsset fontAsset,
            string value,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            float preferredHeight,
            bool autoSize,
            float minSize,
            float maxSize,
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
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = autoSize;
            text.fontSizeMin = minSize;
            text.fontSizeMax = Mathf.Max(minSize, maxSize);
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.characterSpacing = 0f;

            SetLayoutElement(gameObject, 0f, 0f, flexibleWidth, preferredHeight, preferredHeight, 0f);
            return text;
        }

        private static RectTransform CreateButton(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rectTransform = CreateImage(
                name,
                parent,
                null,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                Color.clear,
                false,
                true);

            rectTransform.gameObject.AddComponent<RectMask2D>();
            RectTransform art = CreateImage(
                "ButtonArt",
                rectTransform,
                sprite,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(0f, 280f),
                color,
                false,
                false);
            art.SetAsFirstSibling();

            Button button = rectTransform.gameObject.AddComponent<Button>();
            Image image = art.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            colors.pressedColor = new Color(0.82f, 0.88f, 0.96f, 1f);
            colors.disabledColor = new Color(0.5f, 0.55f, 0.62f, 0.75f);
            button.colors = colors;
            return rectTransform;
        }

        private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, bool preserveAspect)
        {
            return CreateImage(
                name,
                parent,
                sprite,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                Color.white,
                preserveAspect,
                false);
        }

        private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            return CreateImage(
                name,
                parent,
                sprite,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                color,
                false,
                false);
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
            bool preserveAspect,
            bool raycastTarget)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            SetLayerRecursive(gameObject, LayerMask.NameToLayer("UI"));

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            ConfigureRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);

            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = raycastTarget;

            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                image.type = Image.Type.Sliced;
            }

            return rectTransform;
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
            layout.childForceExpandWidth = true;
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

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = target.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
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

        private static void BindPanelController(GameObject panel, GameOverReferences references)
        {
            SerializedObject serializedObject = new SerializedObject(references.Controller);
            SetReference(serializedObject, "panelRoot", panel);
            SetReference(serializedObject, "titleText", references.TitleText);
            SetReference(serializedObject, "subtitleText", references.SubtitleText);
            SetReference(serializedObject, "timeValueText", references.FinalTimeText);
            SetReference(serializedObject, "scoreValueText", references.FinalScoreText);
            SetReference(serializedObject, "coinsValueText", references.MoneyEarnedText);
            SetReference(serializedObject, "killsValueText", references.FinalKillText);
            SetReference(serializedObject, "rewardCoinsValueText", references.CoinRewardValueText);
            SetReference(serializedObject, "bestScoreValueText", references.BestScoreText);
            SetReference(serializedObject, "bestTimeValueText", references.BestTimeText);
            SetReference(serializedObject, "bestKillsValueText", references.BestKillsText);
            SetReference(serializedObject, "retryButton", references.RetryButton);
            SetReference(serializedObject, "upgradeButton", references.UpgradeButton);
            SetReference(serializedObject, "homeButton", references.HomeButton);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(references.Controller);
        }

        private static void BindUiSystem(GameObject panel, GameOverReferences references)
        {
            UISystem uiSystem = UnityEngine.Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            if (uiSystem == null)
            {
                Debug.LogError("UISystem was not found while binding GameOverPanel.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(uiSystem);
            SetReference(serializedObject, "gameOverPanel", panel);
            SetReference(serializedObject, "gameOverPanelUI", references.Controller);
            SetReference(serializedObject, "finalTimeText", references.FinalTimeText);
            SetReference(serializedObject, "finalScoreText", references.FinalScoreText);
            SetReference(serializedObject, "finalKillText", references.FinalKillText);
            SetReference(serializedObject, "moneyEarnedText", references.MoneyEarnedText);
            SetReference(serializedObject, "coinRewardText", references.CoinRewardValueText);
            SetReference(serializedObject, "gameOverBestScoreText", references.BestScoreText);
            SetReference(serializedObject, "gameOverBestTimeText", references.BestTimeText);
            SetReference(serializedObject, "gameOverBestKillText", references.BestKillsText);
            SetReference(serializedObject, "retryButton", references.RetryButton);
            SetReference(serializedObject, "gameOverUpgradeButton", references.UpgradeButton);
            SetReference(serializedObject, "gameOverHomeButton", references.HomeButton);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uiSystem);
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void CleanupPanel(GameObject panel)
        {
            foreach (Transform child in panel.transform.Cast<Transform>().ToArray())
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            foreach (SafeAreaFitter fitter in panel.GetComponents<SafeAreaFitter>())
            {
                UnityEngine.Object.DestroyImmediate(fitter, true);
            }

            foreach (CanvasScaler scaler in panel.GetComponents<CanvasScaler>())
            {
                UnityEngine.Object.DestroyImmediate(scaler, true);
            }

            foreach (GameOverPanelUI controller in panel.GetComponents<GameOverPanelUI>())
            {
                UnityEngine.Object.DestroyImmediate(controller, true);
            }

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = panel.AddComponent<RectTransform>();
            }

            StretchToParent(rectTransform);
            panel.transform.SetAsLastSibling();

            Canvas canvas = panel.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = panel.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = 120;

            if (panel.GetComponent<GraphicRaycaster>() == null)
            {
                panel.AddComponent<GraphicRaycaster>();
            }

            Image image = panel.GetComponent<Image>();
            if (image == null)
            {
                image = panel.AddComponent<Image>();
            }

            image.sprite = null;
            image.color = Color.clear;
            image.raycastTarget = false;
            SetLayerRecursive(panel, LayerMask.NameToLayer("UI"));
        }

        private static GameObject CreatePanelRoot()
        {
            GameObject safeAreaRoot = FindSceneObjectByPath("GameCanvas/UIRoot/SafeAreaRoot");
            if (safeAreaRoot == null)
            {
                return null;
            }

            GameObject panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(safeAreaRoot.transform, false);
            StretchToParent(panel.GetComponent<RectTransform>());
            SetLayerRecursive(panel, LayerMask.NameToLayer("UI"));
            return panel;
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

        private static void ConfigureUiSprite(string path, Vector4 border, int maxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool needsReimport = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.spriteBorder != border
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || importer.filterMode != FilterMode.Point
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.maxTextureSize != maxSize;

            if (!needsReimport)
            {
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
