#if UNITY_EDITOR
using System.IO;
using _Project.Scripts.Systems.UISystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Editor
{
    public static class MissionLogPanelBuilder
    {
        private const string SafeAreaPath = "GameCanvas/UIRoot/SafeAreaRoot";
        private const string PanelScenePath = SafeAreaPath + "/MissionLogPanel";
        private const string PanelPrefabPath = "Assets/_Project/Prefabs/UI/MissionLogPanel.prefab";
        private const string RowPrefabPath = "Assets/_Project/Prefabs/UI/MissionRow.prefab";
        private const string FontPath = "Assets/Front/Upheaval_TMP.asset";
        private const string BackgroundPath = "Assets/_Project/Art/Sprites/background/BG-Temp.png";
        private const string ArtRoot = "Assets/_Project/Art/UI/MissionSystem/";
        private const string PanelSpritePath = ArtRoot + "mission_panel_9slice_128.png";
        private const string RowActivePath = ArtRoot + "mission_row_active_320x80.png";
        private const string RowCompletedPath = ArtRoot + "mission_row_completed_320x80.png";
        private const string RowLockedPath = ArtRoot + "mission_row_locked_320x80.png";
        private const string ProgressBgPath = ArtRoot + "mission_progress_bg_128x16.png";
        private const string ProgressFillPath = ArtRoot + "mission_progress_fill_128x16.png";
        private const string CheckIconPath = ArtRoot + "mission_check_icon_48.png";
        private const string LockIconPath = ArtRoot + "mission_lock_icon_48.png";

        private static readonly Color32 OverlayColor = new Color32(0, 0, 0, 0);
        private static readonly Color32 BackgroundTint = new Color32(255, 255, 255, 242);
        private static readonly Color32 BackgroundShade = new Color32(36, 48, 79, 189);
        private static readonly Color32 Ink = new Color32(13, 27, 52, 255);
        private static readonly Color32 MutedInk = new Color32(62, 77, 104, 255);
        private static readonly Color32 White = new Color32(246, 250, 255, 255);
        private static readonly Color32 ActiveFrame = new Color32(255, 219, 83, 255);
        private static readonly Color32 CompletedFrame = new Color32(112, 229, 163, 255);
        private static readonly Color32 LockedFrame = new Color32(113, 129, 158, 255);
        private static readonly Color32 RowFill = new Color32(242, 248, 255, 238);
        private static readonly Color32 ActiveRowFill = new Color32(255, 246, 194, 245);

        [MenuItem("Chibi Pixel Gate/UI/Rebuild Mission Log Panel")]
        public static void Rebuild()
        {
            GenerateCleanRowFrames();
            ConfigureSprites();

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                ?? TMP_Settings.defaultFontAsset;
            Sprite backgroundSprite = LoadSprite(BackgroundPath);
            Sprite panelSprite = LoadSprite(PanelSpritePath);
            Sprite rowActiveSprite = LoadSprite(RowActivePath);
            Sprite rowCompletedSprite = LoadSprite(RowCompletedPath);
            Sprite rowLockedSprite = LoadSprite(RowLockedPath);
            Sprite progressBgSprite = LoadSprite(ProgressBgPath);
            Sprite progressFillSprite = LoadSprite(ProgressFillPath);
            Sprite checkIconSprite = LoadSprite(CheckIconPath);
            Sprite lockIconSprite = LoadSprite(LockIconPath);

            if (panelSprite == null
                || rowActiveSprite == null
                || rowCompletedSprite == null
                || rowLockedSprite == null
                || progressBgSprite == null
                || progressFillSprite == null)
            {
                Debug.LogError("Mission Log could not be rebuilt because required MissionSystem sprites are missing.");
                return;
            }

            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/UI");

            GameObject rowPrefabObject = BuildRowObject(
                fontAsset,
                rowActiveSprite,
                rowCompletedSprite,
                rowLockedSprite,
                progressBgSprite,
                progressFillSprite,
                checkIconSprite,
                lockIconSprite);
            PrefabUtility.SaveAsPrefabAsset(rowPrefabObject, RowPrefabPath);
            Object.DestroyImmediate(rowPrefabObject);

            MissionRowUI rowPrefab = AssetDatabase.LoadAssetAtPath<MissionRowUI>(RowPrefabPath);
            GameObject panelPrefabObject = BuildPanelObject(fontAsset, backgroundSprite, panelSprite, rowPrefab, out _, out _);
            PrefabUtility.SaveAsPrefabAsset(panelPrefabObject, PanelPrefabPath);

            InstallPanelInScene(panelPrefabObject);

            Object.DestroyImmediate(panelPrefabObject);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Rebuilt Mission Log panel prefab, row prefab, and scene instance.");
        }

        private static GameObject BuildPanelObject(
            TMP_FontAsset fontAsset,
            Sprite backgroundSprite,
            Sprite panelSprite,
            MissionRowUI rowPrefab,
            out Button backButton,
            out MissionLogPanelUI panelUi)
        {
            GameObject panel = CreateRect("MissionLogPanel", null);
            RectTransform panelRect = panel.transform as RectTransform;
            Stretch(panelRect);

            Image overlay = panel.AddComponent<Image>();
            overlay.color = OverlayColor;

            panelUi = panel.AddComponent<MissionLogPanelUI>();

            RectTransform backgroundLayer = CreateImage(
                "BackgroundLayer",
                panelRect,
                backgroundSprite,
                Image.Type.Simple,
                BackgroundTint);
            Stretch(backgroundLayer);

            Image backgroundImage = backgroundLayer.GetComponent<Image>();
            backgroundImage.preserveAspect = false;

            RectTransform shade = CreateImage(
                "BackgroundShade",
                backgroundLayer,
                null,
                Image.Type.Simple,
                BackgroundShade);
            Stretch(shade);

            RectTransform card = CreateImage("PanelCard", panelRect, panelSprite, Image.Type.Sliced, Color.white);
            Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            card.sizeDelta = new Vector2(900f, 1380f);
            card.anchoredPosition = Vector2.zero;

            RectTransform header = CreateRect("Header", card).transform as RectTransform;
            Anchor(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            header.sizeDelta = new Vector2(-64f, 150f);
            header.anchoredPosition = new Vector2(0f, -42f);

            backButton = CreateButton("BackButton", header, "<", fontAsset, 38f);
            RectTransform backRect = backButton.transform as RectTransform;
            Anchor(backRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            backRect.sizeDelta = new Vector2(88f, 88f);
            backRect.anchoredPosition = new Vector2(42f, 0f);

            TextMeshProUGUI title = CreateText("TitleText", header, fontAsset, "MISSION LOG", 58f, Ink, TextAlignmentOptions.Center);
            Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(110f, 46f);
            title.rectTransform.offsetMax = new Vector2(-110f, -10f);

            TextMeshProUGUI activeMissionText = CreateText("ActiveMissionText", card, fontAsset, "MAIN OBJECTIVE", 34f, Ink, TextAlignmentOptions.Center);
            Anchor(activeMissionText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            activeMissionText.rectTransform.sizeDelta = new Vector2(-96f, 54f);
            activeMissionText.rectTransform.anchoredPosition = new Vector2(0f, -176f);

            TextMeshProUGUI summaryText = CreateText("SummaryText", card, fontAsset, "COMPLETE ALL DIRECTIVES\n00 / 24 COMPLETE", 25f, MutedInk, TextAlignmentOptions.Center);
            Anchor(summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            summaryText.textWrappingMode = TextWrappingModes.Normal;
            summaryText.rectTransform.sizeDelta = new Vector2(-96f, 76f);
            summaryText.rectTransform.anchoredPosition = new Vector2(0f, -236f);

            RectTransform scrollRoot = CreateRect("MissionScrollView", card).transform as RectTransform;
            Anchor(scrollRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
            scrollRoot.offsetMin = new Vector2(54f, 72f);
            scrollRoot.offsetMax = new Vector2(-54f, -326f);

            Image scrollMaskImage = scrollRoot.gameObject.AddComponent<Image>();
            scrollMaskImage.color = new Color32(255, 255, 255, 12);
            Mask mask = scrollRoot.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 42f;

            RectTransform content = CreateRect("Content", scrollRoot).transform as RectTransform;
            Anchor(content, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject templateObject = PrefabUtility.InstantiatePrefab(rowPrefab.gameObject, content) as GameObject;
            MissionRowUI template = templateObject.GetComponent<MissionRowUI>();
            template.gameObject.name = "MissionRowTemplate";
            template.gameObject.SetActive(false);

            scrollRect.content = content;
            scrollRect.viewport = scrollRoot;

            SerializedObject serializedPanel = new SerializedObject(panelUi);
            SetRef(serializedPanel, "panelRoot", panel);
            SetRef(serializedPanel, "activeMissionText", activeMissionText);
            SetRef(serializedPanel, "summaryText", summaryText);
            SetRef(serializedPanel, "scrollRect", scrollRect);
            SetRef(serializedPanel, "contentRoot", content);
            SetRef(serializedPanel, "rowPrefab", template);
            SetRef(serializedPanel, "backButton", backButton);
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return panel;
        }

        private static GameObject BuildRowObject(
            TMP_FontAsset fontAsset,
            Sprite activeSprite,
            Sprite completedSprite,
            Sprite lockedSprite,
            Sprite progressBgSprite,
            Sprite progressFillSprite,
            Sprite checkIconSprite,
            Sprite lockIconSprite)
        {
            GameObject row = CreateRect("MissionRow", null);
            RectTransform rowRect = row.transform as RectTransform;
            rowRect.sizeDelta = new Vector2(792f, 148f);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 148f;
            layout.minHeight = 148f;

            Image backgroundImage = row.AddComponent<Image>();
            backgroundImage.sprite = activeSprite;
            backgroundImage.type = Image.Type.Sliced;

            MissionRowUI rowUi = row.AddComponent<MissionRowUI>();

            Image statusIcon = CreateImage("StatusIcon", rowRect, null, Image.Type.Simple, Color.white).GetComponent<Image>();
            RectTransform statusRect = statusIcon.transform as RectTransform;
            Anchor(statusRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
            statusRect.sizeDelta = new Vector2(54f, 54f);
            statusRect.anchoredPosition = new Vector2(54f, 0f);

            TextMeshProUGUI phaseText = CreateText("PhaseText", rowRect, fontAsset, "01 / BOOT", 22f, MutedInk, TextAlignmentOptions.MidlineLeft);
            Anchor(phaseText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            phaseText.rectTransform.offsetMin = new Vector2(114f, -54f);
            phaseText.rectTransform.offsetMax = new Vector2(-178f, -18f);

            TextMeshProUGUI titleText = CreateText("TitleText", rowRect, fontAsset, "FINISH TUTORIAL", 29f, Ink, TextAlignmentOptions.MidlineLeft);
            Anchor(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            titleText.rectTransform.offsetMin = new Vector2(114f, -92f);
            titleText.rectTransform.offsetMax = new Vector2(-178f, -48f);

            RectTransform progressBg = CreateImage("ProgressBackground", rowRect, progressBgSprite, Image.Type.Sliced, Color.white);
            Anchor(progressBg, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            progressBg.offsetMin = new Vector2(114f, 24f);
            progressBg.offsetMax = new Vector2(-178f, 44f);
            Image progressBackgroundImage = progressBg.GetComponent<Image>();

            Image progressFill = CreateImage("ProgressFill", progressBg, progressFillSprite, Image.Type.Filled, Color.white).GetComponent<Image>();
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;
            progressFill.fillAmount = 0f;
            Stretch(progressFill.rectTransform);

            TextMeshProUGUI progressText = CreateText("ProgressText", rowRect, fontAsset, "0 / 1", 19f, White, TextAlignmentOptions.Center);
            Anchor(progressText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            progressText.rectTransform.offsetMin = new Vector2(114f, 20f);
            progressText.rectTransform.offsetMax = new Vector2(-178f, 48f);

            TextMeshProUGUI rewardText = CreateText("RewardText", rowRect, fontAsset, "+1,000", 25f, Ink, TextAlignmentOptions.MidlineRight);
            Anchor(rewardText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            rewardText.rectTransform.sizeDelta = new Vector2(138f, 42f);
            rewardText.rectTransform.anchoredPosition = new Vector2(-28f, 20f);

            TextMeshProUGUI stateText = CreateText("StateText", rowRect, fontAsset, "ACTIVE", 18f, MutedInk, TextAlignmentOptions.MidlineRight);
            Anchor(stateText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            stateText.rectTransform.sizeDelta = new Vector2(138f, 34f);
            stateText.rectTransform.anchoredPosition = new Vector2(-28f, -24f);

            SerializedObject serializedRow = new SerializedObject(rowUi);
            SetRef(serializedRow, "backgroundImage", backgroundImage);
            SetRef(serializedRow, "statusIconImage", statusIcon);
            SetRef(serializedRow, "progressBackgroundImage", progressBackgroundImage);
            SetRef(serializedRow, "progressFillImage", progressFill);
            SetRef(serializedRow, "activeSprite", activeSprite);
            SetRef(serializedRow, "completedSprite", completedSprite);
            SetRef(serializedRow, "lockedSprite", lockedSprite);
            SetRef(serializedRow, "checkIconSprite", checkIconSprite);
            SetRef(serializedRow, "lockIconSprite", lockIconSprite);
            SetRef(serializedRow, "phaseText", phaseText);
            SetRef(serializedRow, "titleText", titleText);
            SetRef(serializedRow, "progressText", progressText);
            SetRef(serializedRow, "rewardText", rewardText);
            SetRef(serializedRow, "stateText", stateText);
            serializedRow.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }

        private static void InstallPanelInScene(GameObject panelPrefabObject)
        {
            GameObject safeArea = FindSceneObjectByPath(SafeAreaPath);
            if (safeArea == null)
            {
                Debug.LogError($"Could not find safe area at '{SafeAreaPath}'.");
                return;
            }

            GameObject existingPanel = FindSceneObjectByPath(PanelScenePath);
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel);
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath) ?? panelPrefabObject;
            GameObject panelInstance = PrefabUtility.InstantiatePrefab(prefabAsset, safeArea.transform) as GameObject;
            if (panelInstance == null)
            {
                Debug.LogError("Mission Log panel prefab could not be instantiated into the scene.");
                return;
            }

            panelInstance.name = "MissionLogPanel";
            Stretch(panelInstance.transform as RectTransform);
            panelInstance.SetActive(false);

            MissionLogPanelUI panelUi = panelInstance.GetComponent<MissionLogPanelUI>();
            Button backButton = panelUi.BackButton;
            UISystem uiSystem = Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            if (uiSystem == null)
            {
                Debug.LogWarning("UISystem not found while binding Mission Log panel.");
                return;
            }

            SerializedObject serializedUi = new SerializedObject(uiSystem);
            SetRef(serializedUi, "missionPanel", panelInstance);
            SetRef(serializedUi, "missionBackButton", backButton);
            SetRef(serializedUi, "missionLogPanelUI", panelUi);
            serializedUi.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uiSystem);
        }

        private static Button CreateButton(
            string name,
            RectTransform parent,
            string label,
            TMP_FontAsset fontAsset,
            float fontSize)
        {
            RectTransform rect = CreateImage(name, parent, LoadSprite(PanelSpritePath), Image.Type.Sliced, Color.white);
            Button button = rect.gameObject.AddComponent<Button>();
            TextMeshProUGUI text = CreateText("Text", rect, fontAsset, label, fontSize, Ink, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return button;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            RectTransform parent,
            TMP_FontAsset fontAsset,
            string textValue,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject gameObject = CreateRect(name, parent);
            TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.text = textValue;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateImage(
            string name,
            RectTransform parent,
            Sprite sprite,
            Image.Type imageType,
            Color color)
        {
            GameObject gameObject = CreateRect(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = imageType;
            image.color = color;
            return gameObject.transform as RectTransform;
        }

        private static GameObject CreateRect(string name, RectTransform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static void Stretch(RectTransform rect)
        {
            Anchor(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ConfigureSprites()
        {
            ConfigureSprite(PanelSpritePath, new Vector4(24f, 24f, 24f, 24f));
            ConfigureSprite(RowActivePath, new Vector4(14f, 14f, 14f, 14f));
            ConfigureSprite(RowCompletedPath, new Vector4(14f, 14f, 14f, 14f));
            ConfigureSprite(RowLockedPath, new Vector4(14f, 14f, 14f, 14f));
            ConfigureSprite(ProgressBgPath, new Vector4(4f, 4f, 4f, 4f));
            ConfigureSprite(ProgressFillPath, new Vector4(4f, 4f, 4f, 4f));
            ConfigureSprite(CheckIconPath, Vector4.zero);
            ConfigureSprite(LockIconPath, Vector4.zero);
        }

        private static void GenerateCleanRowFrames()
        {
            WriteCleanRowFrame(RowActivePath, ActiveFrame, ActiveRowFill);
            WriteCleanRowFrame(RowCompletedPath, CompletedFrame, RowFill);
            WriteCleanRowFrame(RowLockedPath, LockedFrame, RowFill);
            AssetDatabase.Refresh();
        }

        private static void WriteCleanRowFrame(string assetPath, Color32 frameColor, Color32 fillColor)
        {
            const int width = 320;
            const int height = 80;
            const int outer = 4;
            const int inner = 9;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32 innerLine = new Color32(
                (byte)Mathf.RoundToInt(frameColor.r * 0.72f),
                (byte)Mathf.RoundToInt(frameColor.g * 0.72f),
                (byte)Mathf.RoundToInt(frameColor.b * 0.72f),
                255);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inOuterFrame = x < outer || x >= width - outer || y < outer || y >= height - outer;
                    bool inInnerFrame = x < inner || x >= width - inner || y < inner || y >= height - inner;
                    Color32 pixel = transparent;

                    if (inOuterFrame)
                    {
                        pixel = frameColor;
                    }
                    else if (inInnerFrame)
                    {
                        pixel = innerLine;
                    }
                    else
                    {
                        pixel = fillColor;
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullPath = Path.Combine(projectRoot, assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            byte[] pngBytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            if (File.Exists(fullPath) && BytesMatch(File.ReadAllBytes(fullPath), pngBytes))
            {
                return;
            }

            File.WriteAllBytes(fullPath, pngBytes);
        }

        private static bool BytesMatch(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static void ConfigureSprite(string path, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 100f))
            {
                importer.spritePixelsPerUnit = 100f;
                changed = true;
            }

            if (importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void SetRef(SerializedObject serializedObject, string fieldName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogWarning($"Missing serialized field '{fieldName}' on {serializedObject.targetObject}.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string name = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject FindSceneObjectByPath(string path)
        {
            string[] parts = path.Split('/');
            if (parts.Length == 0)
            {
                return null;
            }

            GameObject current = GameObject.Find(parts[0]);
            for (int index = 1; index < parts.Length && current != null; index++)
            {
                Transform child = current.transform.Find(parts[index]);
                current = child != null ? child.gameObject : null;
            }

            return current;
        }
    }
}
#endif
