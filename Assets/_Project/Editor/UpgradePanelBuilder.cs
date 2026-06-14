#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.UISystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Editor
{
    public static class UpgradePanelBuilder
    {
        private const string PanelPath = "GameCanvas/UIRoot/SafeAreaRoot/UpgradePanel";
        private const string BackgroundPath = "Assets/_Project/Art/Sprites/background/BG_01.png";
        private const string FramePath = "Assets/_Project/Art/UI/Generated/UIframe_clean.png";
        private const string CoinPath = "Assets/_Project/Art/UI/vecteezy_game-coin-pixelated_54978935.png";
        private const string CharacterPath = "Assets/_Project/Prefabs/Character/Girl-Sheet.png";
        private const string PixelButtonsPath = "Assets/DEVNIK 2D/2D UI PIXEL BUTTONS/UI SIMPLE PIXEL UNSPLIT.png";
        private const string FontPath = "Assets/Front/Upheaval_TMP.asset";
        private const string DamageIconPath = "Assets/_Project/Art/UI/Generated/DamageIcon_clean.png";
        private const string FireRateIconPath = "Assets/_Project/Art/UI/Generated/FireIcon_clean.png";
        private const string MaxHpIconPath = "Assets/_Project/Art/UI/Generated/HPIcon_clean.png";
        private const string ProjectileIconPath = "Assets/_Project/Art/UI/Generated/BulletIcon_clean.png";
        private const string SquadIconPath = "Assets/_Project/Art/UI/Generated/SquadIcon_clean.png";
        private const string RowBackgroundPath = "Assets/_Project/Art/UI/Generated/RowBG_clean.png";
        private const string UpgradeButtonPath = "Assets/_Project/Art/UI/Generated/UpgradeButton_clean.png";

        private static readonly Color Navy = new Color(0.035f, 0.12f, 0.28f, 0.98f);
        private static readonly Color DarkNavy = new Color(0.018f, 0.055f, 0.13f, 0.98f);
        private static readonly Color IceBlue = new Color(0.55f, 0.83f, 1f, 1f);
        private static readonly Color LightCard = new Color(0.91f, 0.96f, 1f, 0.98f);
        private static readonly Color TextNavy = new Color(0.035f, 0.15f, 0.34f, 1f);
        private static readonly Color Gold = new Color(1f, 0.68f, 0.12f, 1f);
        private static readonly Color Green = new Color(0.17f, 0.72f, 0.19f, 1f);

        private static readonly Color[] RowAccentColors =
        {
            new Color(0.12f, 0.42f, 0.94f, 1f),
            new Color(0.87f, 0.18f, 0.17f, 1f),
            new Color(0.16f, 0.64f, 0.18f, 1f),
            new Color(0.08f, 0.48f, 0.94f, 1f),
            new Color(0.63f, 0.22f, 0.86f, 1f)
        };

        private sealed class RowReferences
        {
            public PlayerMetaUpgradeType Type;
            public TextMeshProUGUI LevelText;
            public TextMeshProUGUI CurrentValueText;
            public TextMeshProUGUI NextValueText;
            public TextMeshProUGUI CostText;
            public TextMeshProUGUI ButtonText;
            public Button Button;
        }

        [MenuItem("Chibi Pixel Gate/UI/Rebuild Upgrade Panel")]
        public static void Rebuild()
        {
            GameObject panel = FindSceneObjectByPath(PanelPath);
            if (panel == null)
            {
                Debug.LogError($"Could not find UpgradePanel at '{PanelPath}'.");
                return;
            }

            ConfigureGeneratedSprites();

            Sprite backgroundSprite = LoadSprite(BackgroundPath);
            Sprite frameSprite = LoadSprite(FramePath);
            Sprite coinSprite = LoadSprite(CoinPath);
            Sprite characterSprite = LoadSprite(CharacterPath, "Girl-Sheet_0");
            Sprite backSprite = LoadSprite(PixelButtonsPath, "A_LEFT");
            Sprite rowBackgroundSprite = LoadSprite(RowBackgroundPath);
            Sprite upgradeButtonSprite = LoadSprite(UpgradeButtonPath);
            var upgradeIcons = new Dictionary<PlayerMetaUpgradeType, Sprite>
            {
                [PlayerMetaUpgradeType.Damage] = LoadSprite(DamageIconPath),
                [PlayerMetaUpgradeType.FireRate] = LoadSprite(FireRateIconPath),
                [PlayerMetaUpgradeType.MaxHp] = LoadSprite(MaxHpIconPath),
                [PlayerMetaUpgradeType.ProjectileCount] = LoadSprite(ProjectileIconPath),
                [PlayerMetaUpgradeType.SquadSize] = LoadSprite(SquadIconPath)
            };
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                ?? TMP_Settings.defaultFontAsset;

            CleanupPanel(panel);

            var rows = new List<RowReferences>();
            BuildPanel(
                panel.transform as RectTransform,
                backgroundSprite,
                frameSprite,
                coinSprite,
                characterSprite,
                backSprite,
                rowBackgroundSprite,
                upgradeButtonSprite,
                upgradeIcons,
                fontAsset,
                rows,
                out TextMeshProUGUI currencyText,
                out TextMeshProUGUI powerText,
                out TextMeshProUGUI squadText,
                out Button backButton);

            BindUiSystem(currencyText, powerText, squadText, backButton, rows);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Rebuilt UpgradePanel with five-tier player and squad upgrades.");
        }

        private static void BuildPanel(
            RectTransform root,
            Sprite backgroundSprite,
            Sprite frameSprite,
            Sprite coinSprite,
            Sprite characterSprite,
            Sprite backSprite,
            Sprite rowBackgroundSprite,
            Sprite upgradeButtonSprite,
            IReadOnlyDictionary<PlayerMetaUpgradeType, Sprite> upgradeIcons,
            TMP_FontAsset fontAsset,
            List<RowReferences> rows,
            out TextMeshProUGUI currencyText,
            out TextMeshProUGUI powerText,
            out TextMeshProUGUI squadText,
            out Button backButton)
        {
            CreateImage(
                "Background",
                root,
                backgroundSprite,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.58f, 0.76f, 0.92f, 1f));

            CreateImage(
                "BackgroundShade",
                root,
                null,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.02f, 0.08f, 0.2f, 0.48f));

            RectTransform content = CreateRect(
                "UpgradeContentRoot",
                root,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -34f),
                new Vector2(1320f, 3000f));
            content.localScale = Vector3.one * 0.72f;

            BuildTopBar(
                content,
                frameSprite,
                coinSprite,
                backSprite,
                fontAsset,
                out currencyText,
                out backButton);

            BuildCommanderCard(
                content,
                frameSprite,
                characterSprite,
                fontAsset,
                out powerText,
                out squadText);

            RectTransform listRoot = CreateRect(
                "UpgradeList",
                content,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -870f),
                new Vector2(1320f, 1780f));

            for (int index = 0; index < PlayerMetaUpgradeService.Definitions.Length; index++)
            {
                UpgradeDefinition definition = PlayerMetaUpgradeService.Definitions[index];
                rows.Add(BuildUpgradeRow(
                    listRoot,
                    definition,
                    RowAccentColors[index],
                    index,
                    rowBackgroundSprite,
                    upgradeButtonSprite,
                    upgradeIcons.TryGetValue(definition.Type, out Sprite iconSprite)
                        ? iconSprite
                        : null,
                    coinSprite,
                    fontAsset));
            }
        }

        private static void BuildTopBar(
            RectTransform content,
            Sprite frameSprite,
            Sprite coinSprite,
            Sprite backSprite,
            TMP_FontAsset fontAsset,
            out TextMeshProUGUI currencyText,
            out Button backButton)
        {
            RectTransform bar = CreateRect(
                "TopBar",
                content,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(1320f, 150f));

            RectTransform back = CreateButton(
                "BackButton",
                bar,
                frameSprite,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(58f, 0f),
                new Vector2(118f, 104f),
                new Color(0.12f, 0.55f, 0.94f, 1f));
            backButton = back.GetComponent<Button>();

            if (backSprite != null)
            {
                CreateImage(
                    "BackIcon",
                    back,
                    backSprite,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(64f, 64f),
                    Color.white,
                    true);
            }
            else
            {
                CreateText(
                    "BackIcon",
                    back,
                    fontAsset,
                    "<",
                    54f,
                    Color.white,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero);
            }

            CreateText(
                "TitleText",
                bar,
                fontAsset,
                "COMMANDER UPGRADE",
                62f,
                Color.white,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(700f, 90f),
                true,
                48f,
                62f);

            RectTransform wallet = CreateFrame(
                "WalletFrame",
                bar,
                frameSprite,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-8f, 0f),
                new Vector2(340f, 104f),
                Navy);

            CreateImage(
                "CoinIcon",
                wallet,
                coinSprite,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(64f, 0f),
                new Vector2(70f, 70f),
                Color.white,
                true);

            currencyText = CreateText(
                "CurrencyText",
                wallet,
                fontAsset,
                "0",
                48f,
                Color.white,
                TextAlignmentOptions.MidlineRight,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(-30f, 0f),
                new Vector2(-120f, -20f),
                true,
                34f,
                48f);
        }

        private static void BuildCommanderCard(
            RectTransform content,
            Sprite frameSprite,
            Sprite characterSprite,
            TMP_FontAsset fontAsset,
            out TextMeshProUGUI powerText,
            out TextMeshProUGUI squadText)
        {
            RectTransform card = CreateFrame(
                "CommanderCard",
                content,
                frameSprite,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -170f),
                new Vector2(1320f, 650f),
                new Color(0.18f, 0.52f, 0.88f, 0.94f));
            AddOutline(card.gameObject, new Color(0.55f, 0.88f, 1f, 1f), new Vector2(6f, -6f));

            RectTransform label = CreateFrame(
                "CommanderLabel",
                card,
                frameSprite,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -18f),
                new Vector2(360f, 84f),
                Navy);
            CreateText(
                "CommanderLabelText",
                label,
                fontAsset,
                "COMMANDER",
                42f,
                Color.white,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            RectTransform stage = CreateImage(
                "CharacterStage",
                card,
                null,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(48f, -30f),
                new Vector2(700f, 500f),
                new Color(0.55f, 0.84f, 1f, 0.3f));
            AddOutline(stage.gameObject, IceBlue, new Vector2(5f, -5f));

            CreateImage(
                "CharacterGlow",
                stage,
                null,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 34f),
                new Vector2(440f, 94f),
                new Color(0.18f, 0.85f, 1f, 0.48f));

            CreateImage(
                "CommanderAvatar",
                stage,
                characterSprite,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 12f),
                new Vector2(430f, 430f),
                Color.white,
                true);

            RectTransform summary = CreateImage(
                "PowerSummary",
                card,
                null,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-38f, -30f),
                new Vector2(500f, 500f),
                LightCard);
            AddOutline(summary.gameObject, IceBlue, new Vector2(5f, -5f));

            RectTransform summaryHeader = CreateImage(
                "SummaryHeader",
                summary,
                null,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(500f, 92f),
                new Color(0.12f, 0.47f, 0.86f, 1f));
            CreateText(
                "SummaryHeaderText",
                summaryHeader,
                fontAsset,
                "POWER SUMMARY",
                38f,
                Color.white,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            CreateText(
                "PowerLabelText",
                summary,
                fontAsset,
                "POWER",
                34f,
                TextNavy,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(50f, -132f),
                new Vector2(200f, 54f));
            powerText = CreateText(
                "PowerValueText",
                summary,
                fontAsset,
                "2,000",
                72f,
                new Color(0.1f, 0.42f, 0.9f, 1f),
                TextAlignmentOptions.MidlineRight,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-42f, -188f),
                new Vector2(330f, 88f),
                true,
                46f,
                72f);

            CreateImage(
                "Divider",
                summary,
                null,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -24f),
                new Vector2(410f, 4f),
                new Color(0.31f, 0.55f, 0.78f, 0.65f));

            CreateText(
                "SquadLabelText",
                summary,
                fontAsset,
                "SQUAD",
                34f,
                TextNavy,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(50f, 116f),
                new Vector2(210f, 54f));
            squadText = CreateText(
                "SquadValueText",
                summary,
                fontAsset,
                "1 / 12",
                58f,
                new Color(0.1f, 0.42f, 0.9f, 1f),
                TextAlignmentOptions.MidlineRight,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-42f, 78f),
                new Vector2(310f, 74f),
                true,
                42f,
                58f);
        }

        private static RowReferences BuildUpgradeRow(
            RectTransform parent,
            UpgradeDefinition definition,
            Color accent,
            int index,
            Sprite rowBackgroundSprite,
            Sprite upgradeButtonSprite,
            Sprite iconSprite,
            Sprite coinSprite,
            TMP_FontAsset fontAsset)
        {
            float y = -(index * 350f);
            RectTransform row = CreateFrame(
                definition.Type + "UpgradeRow",
                parent,
                rowBackgroundSprite,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, y),
                new Vector2(1320f, 326f),
                Color.white);

            RectTransform accentStrip = CreateImage(
                "AccentStrip",
                row,
                null,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(18f, -16f),
                accent);

            RectTransform icon = CreateImage(
                "IconTile",
                row,
                iconSprite,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(34f, 0f),
                new Vector2(210f, 210f),
                Color.white,
                true);

            CreateText(
                "NameText",
                row,
                fontAsset,
                definition.DisplayName,
                48f,
                TextNavy,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(252f, 86f),
                new Vector2(380f, 64f),
                true,
                36f,
                48f);
            CreateText(
                "StatNameText",
                row,
                fontAsset,
                definition.StatName,
                31f,
                accent,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(252f, 34f),
                new Vector2(400f, 48f),
                true,
                24f,
                31f);
            CreateText(
                "DescriptionText",
                row,
                fontAsset,
                definition.Description,
                25f,
                new Color(0.19f, 0.34f, 0.5f, 1f),
                TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(252f, -42f),
                new Vector2(405f, 80f),
                true,
                20f,
                25f);

            RectTransform levelBadge = CreateImage(
                "LevelBadge",
                row,
                null,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(672f, 84f),
                new Vector2(174f, 62f),
                new Color(0.08f, 0.32f, 0.66f, 1f));
            TextMeshProUGUI levelText = CreateText(
                "LevelText",
                levelBadge,
                fontAsset,
                "LV. 0/5",
                29f,
                Color.white,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                true,
                22f,
                29f);

            TextMeshProUGUI currentValueText = CreateText(
                "CurrentValueText",
                row,
                fontAsset,
                PlayerMetaUpgradeService.FormatValue(definition.Type, definition.BaseValue),
                52f,
                TextNavy,
                TextAlignmentOptions.Center,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(620f, -42f),
                new Vector2(130f, 76f),
                true,
                34f,
                52f);
            CreateText(
                "ArrowText",
                row,
                fontAsset,
                ">",
                46f,
                new Color(0.14f, 0.56f, 0.94f, 1f),
                TextAlignmentOptions.Center,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(758f, -42f),
                new Vector2(54f, 72f));
            TextMeshProUGUI nextValueText = CreateText(
                "NextValueText",
                row,
                fontAsset,
                PlayerMetaUpgradeService.FormatValue(
                    definition.Type,
                    definition.UsesWholeNumbers
                        ? Mathf.CeilToInt(definition.BaseValue * PlayerMetaUpgradeService.UpgradeMultiplier)
                        : definition.BaseValue * PlayerMetaUpgradeService.UpgradeMultiplier),
                52f,
                Green,
                TextAlignmentOptions.Center,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(826f, -42f),
                new Vector2(142f, 76f),
                true,
                34f,
                52f);

            CreateImage(
                "CostCoinIcon",
                row,
                coinSprite,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-272f, 88f),
                new Vector2(50f, 50f),
                Color.white,
                true);
            TextMeshProUGUI costText = CreateText(
                "CostText",
                row,
                fontAsset,
                index == 0 ? "100" : "0",
                36f,
                TextNavy,
                TextAlignmentOptions.Center,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-142f, 88f),
                new Vector2(190f, 54f),
                true,
                26f,
                36f);

            RectTransform buttonRect = CreateButton(
                "UpgradeButton",
                row,
                upgradeButtonSprite,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-26f, -72f),
                new Vector2(310f, 106f),
                Color.white);
            TextMeshProUGUI buttonText = CreateText(
                "UpgradeButtonText",
                buttonRect,
                fontAsset,
                "UPGRADE",
                42f,
                TextNavy,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-22f, -18f),
                true,
                30f,
                42f);

            return new RowReferences
            {
                Type = definition.Type,
                LevelText = levelText,
                CurrentValueText = currentValueText,
                NextValueText = nextValueText,
                CostText = costText,
                ButtonText = buttonText,
                Button = buttonRect.GetComponent<Button>()
            };
        }

        private static void ConfigureGeneratedSprites()
        {
            ConfigureSprite(DamageIconPath, Vector4.zero);
            ConfigureSprite(FireRateIconPath, Vector4.zero);
            ConfigureSprite(MaxHpIconPath, Vector4.zero);
            ConfigureSprite(ProjectileIconPath, Vector4.zero);
            ConfigureSprite(SquadIconPath, Vector4.zero);
            ConfigureSprite(RowBackgroundPath, new Vector4(90f, 64f, 90f, 64f));
            ConfigureSprite(UpgradeButtonPath, new Vector4(110f, 80f, 110f, 80f));
        }

        private static void ConfigureSprite(string path, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not configure UI sprite importer at '{path}'.");
                return;
            }

            bool needsReimport = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || importer.filterMode != FilterMode.Point
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.maxTextureSize != 4096
                || importer.spriteBorder != border;

            if (!needsReimport)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
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
            gameObject.layer = LayerMask.NameToLayer("UI");

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            ConfigureRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
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
            bool preserveAspect = false)
        {
            RectTransform rectTransform = CreateRect(
                name,
                parent,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                sizeDelta);
            Image image = rectTransform.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;

            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                image.type = Image.Type.Sliced;
            }

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
            Color color)
        {
            return CreateImage(
                name,
                parent,
                sprite,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                sizeDelta,
                color);
        }

        private static RectTransform CreateButton(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            RectTransform rectTransform = CreateImage(
                name,
                parent,
                sprite,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                sizeDelta,
                color);

            Image image = rectTransform.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.88f);
            colors.pressedColor = new Color(0.8f, 0.86f, 0.92f, 1f);
            colors.disabledColor = new Color(0.48f, 0.53f, 0.6f, 0.72f);
            button.colors = colors;
            return rectTransform;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            bool autoSize = false,
            float minSize = 18f,
            float maxSize = 72f)
        {
            RectTransform rectTransform = CreateRect(
                name,
                parent,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                sizeDelta);
            TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = autoSize;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.characterSpacing = 0f;
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

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void BindUiSystem(
            TextMeshProUGUI currencyText,
            TextMeshProUGUI powerText,
            TextMeshProUGUI squadText,
            Button backButton,
            IReadOnlyList<RowReferences> rows)
        {
            UISystem uiSystem = Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            if (uiSystem == null)
            {
                Debug.LogError("UISystem was not found while binding UpgradePanel.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(uiSystem);
            SetReference(serializedObject, "upgradeCurrencyText", currencyText);
            SetReference(serializedObject, "upgradePowerText", powerText);
            SetReference(serializedObject, "upgradeSquadText", squadText);
            SetReference(serializedObject, "upgradeBackButton", backButton);

            SerializedProperty rowsProperty = serializedObject.FindProperty("upgradeRows");
            rowsProperty.ClearArray();

            for (int index = 0; index < rows.Count; index++)
            {
                RowReferences row = rows[index];
                rowsProperty.InsertArrayElementAtIndex(index);
                SerializedProperty rowProperty = rowsProperty.GetArrayElementAtIndex(index);
                rowProperty.FindPropertyRelative("upgradeType").enumValueIndex = (int)row.Type;
                rowProperty.FindPropertyRelative("levelText").objectReferenceValue = row.LevelText;
                rowProperty.FindPropertyRelative("currentValueText").objectReferenceValue = row.CurrentValueText;
                rowProperty.FindPropertyRelative("nextValueText").objectReferenceValue = row.NextValueText;
                rowProperty.FindPropertyRelative("costText").objectReferenceValue = row.CostText;
                rowProperty.FindPropertyRelative("upgradeButtonText").objectReferenceValue = row.ButtonText;
                rowProperty.FindPropertyRelative("upgradeButton").objectReferenceValue = row.Button;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uiSystem);
        }

        private static void SetReference(
            SerializedObject serializedObject,
            string propertyName,
            Object value)
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

        private static void CleanupPanel(GameObject panel)
        {
            foreach (Transform child in panel.transform.Cast<Transform>().ToArray())
            {
                Object.DestroyImmediate(child.gameObject);
            }

            foreach (SafeAreaFitter fitter in panel.GetComponents<SafeAreaFitter>())
            {
                Object.DestroyImmediate(fitter, true);
            }

            Canvas canvas = panel.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = panel.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            if (panel.GetComponent<GraphicRaycaster>() == null)
            {
                panel.AddComponent<GraphicRaycaster>();
            }

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.sprite = null;
                panelImage.color = Color.clear;
                panelImage.raycastTarget = false;
            }

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            ConfigureRect(
                rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            panel.layer = LayerMask.NameToLayer("UI");
        }
    }
}
#endif
