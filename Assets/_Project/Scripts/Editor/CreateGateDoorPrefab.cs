#if UNITY_EDITOR
using _Project.Scripts.Gameplay.Gates;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.EditorTools
{
    public static class CreateGateDoorPrefab
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Gates/GateDoor.prefab";
        private const string TmpFontPath = "Packages/com.unity.textmeshpro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Chibi Pixel Gate/Create Gate Door Prefab")]
        public static void Create()
        {
            EnsureFolder("Assets/_Project/Prefabs/Gates");

            GameObject door = new GameObject("GateDoor");

            SpriteRenderer frame = door.AddComponent<SpriteRenderer>();
            frame.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            frame.color = new Color(0.2f, 0.85f, 0.45f, 0.95f);
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = new Vector2(1.6f, 2.4f);
            door.transform.localScale = Vector3.one;

            BoxCollider2D collider = door.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.4f, 2.2f);

            Rigidbody2D body = door.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            DoorView doorView = door.AddComponent<DoorView>();
            door.AddComponent<GateLogic>();
            door.AddComponent<GateTrigger>();

            TextMeshProUGUI label = CreateWorldSpaceLabel(door.transform);
            SerializedObject doorViewSo = new SerializedObject(doorView);
            doorViewSo.FindProperty("frameRenderer").objectReferenceValue = frame;
            doorViewSo.FindProperty("labelText").objectReferenceValue = label;
            doorViewSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(door, PrefabPath);
            Object.DestroyImmediate(door);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created gate door prefab (World Space Canvas + TMP) at {PrefabPath}");
        }

        private static TextMeshProUGUI CreateWorldSpaceLabel(Transform doorRoot)
        {
            GameObject canvasObject = new GameObject("LabelCanvas");
            canvasObject.transform.SetParent(doorRoot, false);
            canvasObject.transform.localPosition = Vector3.zero;
            canvasObject.transform.localRotation = Quaternion.identity;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(160f, 64f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(canvasObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;

            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.font = LoadDefaultTmpFont();
            tmp.text = "+1 DMG";
            tmp.fontSize = 36f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;

            return tmp;
        }

        private static TMP_FontAsset LoadDefaultTmpFont()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (font != null)
            {
                return font;
            }

            return TMP_Settings.defaultFontAsset;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }

            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Gates");
        }
    }
}
#endif
