#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using _Project.Scripts.Core.GameLoop;
using _Project.Scripts.Core.StateMachine;
using _Project.Scripts.Gameplay.Dialogue;
using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Systems.TutorialSystem;
using _Project.Scripts.Systems.UISystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace _Project.Editor
{
    public static class GameplayDialogueSetup
    {
        private const string MenuRoot = "Tools/True Gate/Gameplay Dialogue/";
        private const string SafeAreaPath = "GameCanvas/UIRoot/SafeAreaRoot";
        private const string LayerPath = SafeAreaPath + "/GameplayDialogueLayer";
        private const string BubblePrefabPath = "Assets/_Project/Prefabs/UI/GameplaySpeechBubble.prefab";
        private const string BubbleSpritePath = "Assets/_Project/Art/UI/GameplayDialogue/BubbleMessage.png";
        private const string BubbleFrameSpritePath = "Assets/_Project/Art/UI/GameplayDialogue/BubbleMessage_Frame.png";
        private const string CsvPath = "Assets/_Project/Data/Dialogue/GameplayDialogueContent_v0.1.csv";
        private const string CatalogPath = "Assets/_Project/Data/Dialogue/GameplayDialogueCatalog.asset";
        private const string FontPath = "Assets/Front/Upheaval_TMP.asset";
        private const int BubbleFrameWidth = 520;
        private const int BubbleFrameHeight = 170;
        private const int BubbleBorderScale = 3;

        private static readonly Vector4 BubbleBorder = new Vector4(10f, 12f, 8f, 8f);
        private static readonly Vector4 TextPadding = new Vector4(48f, 34f, 48f, 62f);
        private static readonly Color32 TextColor = new Color32(8, 16, 36, 255);

        [MenuItem(MenuRoot + "Setup")]
        public static void Setup()
        {
            AssetDatabase.Refresh();
            EnsureFolders();
            ConfigureBubbleSprite();
            CreateOrUpdateBakedBubbleFrame();
            GameplayDialogueCatalog catalog = CreateOrUpdateCatalog();
            if (catalog == null)
            {
                Debug.LogError("Gameplay Dialogue setup failed: catalog could not be created.");
                return;
            }

            GameObject prefab = CreateOrUpdateBubblePrefab();
            if (prefab == null)
            {
                Debug.LogError("Gameplay Dialogue setup failed: speech bubble prefab could not be created.");
                return;
            }

            RectTransform layer = FindOrCreateDialogueLayer();
            if (layer == null)
            {
                Debug.LogError($"Gameplay Dialogue setup failed: could not find or create '{LayerPath}'.");
                return;
            }

            SpeechBubblePresenter presenter = InstallBubbleInstance(layer, prefab);
            if (presenter == null)
            {
                Debug.LogError("Gameplay Dialogue setup failed: speech bubble scene instance is missing presenter.");
                return;
            }

            bool wired = WireSceneReferences(catalog, presenter);
            if (!wired)
            {
                Debug.LogError("Gameplay Dialogue setup finished with missing scene references. See earlier warnings.");
                return;
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Gameplay Dialogue setup complete.");
        }

        [MenuItem(MenuRoot + "Validate Content")]
        public static void ValidateContent()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
            if (csv == null)
            {
                Debug.LogError($"Gameplay Dialogue CSV missing at '{CsvPath}'.");
                return;
            }

            List<GameplayDialogueEntry> entries = GameplayDialogueCsvParser.Parse(csv.text, CsvPath);
            LogContentSummary(entries);
        }

        private static GameplayDialogueCatalog CreateOrUpdateCatalog()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
            if (csv == null)
            {
                Debug.LogError($"Gameplay Dialogue CSV missing at '{CsvPath}'.");
                return null;
            }

            List<GameplayDialogueEntry> entries;
            try
            {
                entries = GameplayDialogueCsvParser.Parse(csv.text, CsvPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Gameplay Dialogue CSV validation failed: {exception.Message}");
                return null;
            }

            GameplayDialogueCatalog catalog = AssetDatabase.LoadAssetAtPath<GameplayDialogueCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameplayDialogueCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.ReplaceEntries(csv, entries);
            EditorUtility.SetDirty(catalog);
            LogContentSummary(entries);
            return catalog;
        }

        private static GameObject CreateOrUpdateBubblePrefab()
        {
            Sprite bubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BubbleFrameSpritePath);
            if (bubbleSprite == null)
            {
                Debug.LogError($"Baked bubble frame sprite missing at '{BubbleFrameSpritePath}'.");
                return null;
            }

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                ?? TMP_Settings.defaultFontAsset;
            if (fontAsset == null)
            {
                Debug.LogWarning("Gameplay Dialogue setup could not find Upheaval_TMP or TMP default font.");
            }

            GameObject root = CreateRect("GameplaySpeechBubble", null);
            RectTransform rootRect = root.transform as RectTransform;
            rootRect.sizeDelta = new Vector2(BubbleFrameWidth, BubbleFrameHeight);
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;
            SpeechBubblePresenter presenter = root.AddComponent<SpeechBubblePresenter>();

            RectTransform background = CreateImage(
                "BubbleBackground",
                rootRect,
                bubbleSprite,
                Image.Type.Simple,
                Color.white);
            Stretch(background);
            background.GetComponent<Image>().raycastTarget = false;

            TextMeshProUGUI text = CreateText(
                "DialogueText",
                rootRect,
                fontAsset,
                "UNIT-07 online.",
                42f,
                TextColor,
                TextAlignmentOptions.Center);
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 42f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.maxVisibleLines = 3;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(TextPadding.x, TextPadding.w);
            text.rectTransform.offsetMax = new Vector2(-TextPadding.z, -TextPadding.y);

            SerializedObject serializedPresenter = new SerializedObject(presenter);
            SetRef(serializedPresenter, "bubbleRect", rootRect);
            SetRef(serializedPresenter, "canvasGroup", canvasGroup);
            SetRef(serializedPresenter, "bubbleBackground", background.GetComponent<Image>());
            SetRef(serializedPresenter, "dialogueText", text);
            serializedPresenter.FindProperty("bubbleSize").vector2Value = new Vector2(BubbleFrameWidth, BubbleFrameHeight);
            serializedPresenter.FindProperty("textPadding").vector4Value = TextPadding;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BubblePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static RectTransform FindOrCreateDialogueLayer()
        {
            GameObject safeArea = FindSceneObjectByPath(SafeAreaPath);
            if (safeArea == null)
            {
                return null;
            }

            GameObject layerObject = FindSceneObjectByPath(LayerPath);
            if (layerObject == null)
            {
                layerObject = CreateRect("GameplayDialogueLayer", safeArea.transform as RectTransform);
            }

            RectTransform layer = layerObject.transform as RectTransform;
            Stretch(layer);
            layerObject.SetActive(true);

            Transform hud = safeArea.transform.Find("GameplayHUDPanel");
            if (hud != null)
            {
                layer.SetSiblingIndex(Mathf.Min(hud.GetSiblingIndex() + 1, safeArea.transform.childCount - 1));
            }

            return layer;
        }

        private static SpeechBubblePresenter InstallBubbleInstance(RectTransform layer, GameObject prefab)
        {
            Transform existing = layer.Find("GameplaySpeechBubble");
            GameObject instance;
            if (existing == null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, layer) as GameObject;
                if (instance == null)
                {
                    return null;
                }
            }
            else
            {
                instance = existing.gameObject;
                if (PrefabUtility.GetCorrespondingObjectFromSource(instance) == null)
                {
                    Object.DestroyImmediate(instance);
                    instance = PrefabUtility.InstantiatePrefab(prefab, layer) as GameObject;
                }
            }

            if (instance == null)
            {
                return null;
            }

            instance.name = "GameplaySpeechBubble";
            RectTransform rect = instance.transform as RectTransform;
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(BubbleFrameWidth, BubbleFrameHeight);

            SpeechBubblePresenter presenter = instance.GetComponent<SpeechBubblePresenter>();
            SerializedObject serializedPresenter = new SerializedObject(presenter);
            SetRef(serializedPresenter, "layerRect", layer);
            serializedPresenter.FindProperty("bubbleSize").vector2Value = new Vector2(BubbleFrameWidth, BubbleFrameHeight);
            serializedPresenter.FindProperty("textPadding").vector4Value = TextPadding;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(instance);
            return presenter;
        }

        private static bool WireSceneReferences(GameplayDialogueCatalog catalog, SpeechBubblePresenter presenter)
        {
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gameManager == null)
            {
                Debug.LogWarning("Gameplay Dialogue setup could not find GameManager.");
                return false;
            }

            GameStateMachine stateMachine = Object.FindAnyObjectByType<GameStateMachine>(FindObjectsInactive.Include);
            if (stateMachine == null)
            {
                stateMachine = gameManager.GetComponent<GameStateMachine>();
            }

            if (stateMachine == null)
            {
                stateMachine = gameManager.gameObject.AddComponent<GameStateMachine>();
            }

            UISystem uiSystem = Object.FindAnyObjectByType<UISystem>(FindObjectsInactive.Include);
            TutorialManager tutorialManager = Object.FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
            PlayerController playerController = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);

            GameplayDialogueController controller = gameManager.GetComponent<GameplayDialogueController>();
            if (controller == null)
            {
                controller = gameManager.gameObject.AddComponent<GameplayDialogueController>();
            }

            SerializedObject serializedController = new SerializedObject(controller);
            SetRef(serializedController, "gameStateMachine", stateMachine);
            SetRef(serializedController, "uiSystem", uiSystem);
            SetRef(serializedController, "tutorialManager", tutorialManager);
            SetRef(serializedController, "playerController", playerController);
            SetRef(serializedController, "catalog", catalog);
            SetRef(serializedController, "presenter", presenter);
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            SetRef(serializedGameManager, "gameStateMachine", stateMachine);
            SetRef(serializedGameManager, "gameplayDialogueController", controller);
            serializedGameManager.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(gameManager);

            bool success = uiSystem != null
                && tutorialManager != null
                && playerController != null
                && catalog != null
                && presenter != null;

            if (!success)
            {
                Debug.LogWarning(
                    $"Gameplay Dialogue missing refs: ui={uiSystem != null}, tutorial={tutorialManager != null}, player={playerController != null}, catalog={catalog != null}, presenter={presenter != null}.");
            }

            return success;
        }

        private static void ConfigureBubbleSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(BubbleSpritePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not configure missing bubble texture '{BubbleSpritePath}'.");
                return;
            }

            bool changed = false;
            changed |= SetValue(importer.textureType, TextureImporterType.Sprite, value => importer.textureType = value);
            changed |= SetValue(importer.spriteImportMode, SpriteImportMode.Single, value => importer.spriteImportMode = value);
            changed |= SetValue(importer.spritePixelsPerUnit, 100f, value => importer.spritePixelsPerUnit = value);
            changed |= SetValue(importer.mipmapEnabled, false, value => importer.mipmapEnabled = value);
            changed |= SetValue(importer.isReadable, false, value => importer.isReadable = value);
            changed |= SetValue(importer.alphaIsTransparency, true, value => importer.alphaIsTransparency = value);
            changed |= SetValue(importer.filterMode, FilterMode.Point, value => importer.filterMode = value);
            changed |= SetValue(importer.wrapMode, TextureWrapMode.Clamp, value => importer.wrapMode = value);
            changed |= SetValue(importer.textureCompression, TextureImporterCompression.Uncompressed, value => importer.textureCompression = value);

            if (importer.spriteBorder != BubbleBorder)
            {
                importer.spriteBorder = BubbleBorder;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void CreateOrUpdateBakedBubbleFrame()
        {
            string sourcePath = ToFullPath(BubbleSpritePath);
            if (!System.IO.File.Exists(sourcePath))
            {
                Debug.LogError($"Could not bake missing bubble source '{BubbleSpritePath}'.");
                return;
            }

            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(System.IO.File.ReadAllBytes(sourcePath)))
                {
                    Debug.LogError($"Could not decode bubble source '{BubbleSpritePath}'.");
                    return;
                }

                Texture2D frame = BuildBakedFrame(source);
                try
                {
                    byte[] pngBytes = frame.EncodeToPNG();
                    string framePath = ToFullPath(BubbleFrameSpritePath);
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(framePath));
                    if (!System.IO.File.Exists(framePath) || !BytesMatch(System.IO.File.ReadAllBytes(framePath), pngBytes))
                    {
                        System.IO.File.WriteAllBytes(framePath, pngBytes);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(frame);
                }
            }
            finally
            {
                Object.DestroyImmediate(source);
            }

            AssetDatabase.ImportAsset(BubbleFrameSpritePath, ImportAssetOptions.ForceUpdate);
            ConfigureFrameSprite();
        }

        private static Texture2D BuildBakedFrame(Texture2D source)
        {
            int left = Mathf.RoundToInt(BubbleBorder.x);
            int bottom = Mathf.RoundToInt(BubbleBorder.y);
            int right = Mathf.RoundToInt(BubbleBorder.z);
            int top = Mathf.RoundToInt(BubbleBorder.w);
            int scaledLeft = left * BubbleBorderScale;
            int scaledRight = right * BubbleBorderScale;
            int scaledBottom = bottom * BubbleBorderScale;
            int scaledTop = top * BubbleBorderScale;

            Texture2D frame = new Texture2D(BubbleFrameWidth, BubbleFrameHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] clearPixels = new Color32[BubbleFrameWidth * BubbleFrameHeight];
            for (int index = 0; index < clearPixels.Length; index++)
            {
                clearPixels[index] = new Color32(0, 0, 0, 0);
            }

            frame.SetPixels32(clearPixels);

            int[] sourceColumns = { 0, left, source.width - right, source.width };
            int[] sourceRows = { 0, bottom, source.height - top, source.height };
            int[] targetColumns = { 0, scaledLeft, BubbleFrameWidth - scaledRight, BubbleFrameWidth };
            int[] targetRows = { 0, scaledBottom, BubbleFrameHeight - scaledTop, BubbleFrameHeight };

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    CopyScaledSlice(
                        source,
                        frame,
                        sourceColumns[column],
                        sourceRows[row],
                        sourceColumns[column + 1] - sourceColumns[column],
                        sourceRows[row + 1] - sourceRows[row],
                        targetColumns[column],
                        targetRows[row],
                        targetColumns[column + 1] - targetColumns[column],
                        targetRows[row + 1] - targetRows[row]);
                }
            }

            frame.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return frame;
        }

        private static void CopyScaledSlice(
            Texture2D source,
            Texture2D target,
            int sourceX,
            int sourceY,
            int sourceWidth,
            int sourceHeight,
            int targetX,
            int targetY,
            int targetWidth,
            int targetHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0 || targetWidth <= 0 || targetHeight <= 0)
            {
                return;
            }

            for (int y = 0; y < targetHeight; y++)
            {
                int sampleY = sourceY + Mathf.Min(sourceHeight - 1, Mathf.FloorToInt((float)y / targetHeight * sourceHeight));
                for (int x = 0; x < targetWidth; x++)
                {
                    int sampleX = sourceX + Mathf.Min(sourceWidth - 1, Mathf.FloorToInt((float)x / targetWidth * sourceWidth));
                    target.SetPixel(targetX + x, targetY + y, source.GetPixel(sampleX, sampleY));
                }
            }
        }

        private static void ConfigureFrameSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(BubbleFrameSpritePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not configure missing baked bubble frame '{BubbleFrameSpritePath}'.");
                return;
            }

            bool changed = false;
            changed |= SetValue(importer.textureType, TextureImporterType.Sprite, value => importer.textureType = value);
            changed |= SetValue(importer.spriteImportMode, SpriteImportMode.Single, value => importer.spriteImportMode = value);
            changed |= SetValue(importer.spritePixelsPerUnit, 100f, value => importer.spritePixelsPerUnit = value);
            changed |= SetValue(importer.mipmapEnabled, false, value => importer.mipmapEnabled = value);
            changed |= SetValue(importer.isReadable, false, value => importer.isReadable = value);
            changed |= SetValue(importer.alphaIsTransparency, true, value => importer.alphaIsTransparency = value);
            changed |= SetValue(importer.filterMode, FilterMode.Point, value => importer.filterMode = value);
            changed |= SetValue(importer.wrapMode, TextureWrapMode.Clamp, value => importer.wrapMode = value);
            changed |= SetValue(importer.textureCompression, TextureImporterCompression.Uncompressed, value => importer.textureCompression = value);

            if (importer.spriteBorder != Vector4.zero)
            {
                importer.spriteBorder = Vector4.zero;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
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

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            return System.IO.Path.Combine(projectRoot, assetPath);
        }

        private static bool SetValue<T>(T current, T expected, Action<T> setter)
        {
            if (EqualityComparer<T>.Default.Equals(current, expected))
            {
                return false;
            }

            setter(expected);
            return true;
        }

        private static void LogContentSummary(List<GameplayDialogueEntry> entries)
        {
            int protocol = Count(entries, PsychologyPhase.Protocol);
            int doubt = Count(entries, PsychologyPhase.Doubt);
            int awakening = Count(entries, PsychologyPhase.Awakening);
            Debug.Log($"Gameplay Dialogue content valid: {entries.Count} total / PROTOCOL {protocol} / DOUBT {doubt} / AWAKENING {awakening}.");
        }

        private static int Count(List<GameplayDialogueEntry> entries, PsychologyPhase phase)
        {
            int count = 0;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] != null && entries[index].PsychologyPhase == phase)
                {
                    count++;
                }
            }

            return count;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/UI");
            EnsureFolder("Assets/_Project/Art/UI/GameplayDialogue");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Dialogue");
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/UI");
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
            image.raycastTarget = false;
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
