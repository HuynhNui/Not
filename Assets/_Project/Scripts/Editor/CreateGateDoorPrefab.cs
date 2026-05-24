#if UNITY_EDITOR
using _Project.Scripts.Gameplay.Gates;
using TMPro;
using UnityEditor;
using UnityEngine;

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

            TextMeshPro label = CreateWorldSpaceLabel(door.transform);
            SerializedObject doorViewSo = new SerializedObject(doorView);
            doorViewSo.FindProperty("frameRenderer").objectReferenceValue = frame;
            doorViewSo.FindProperty("worldLabelText").objectReferenceValue = label;
            doorViewSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(door, PrefabPath);
            Object.DestroyImmediate(door);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created gate door prefab (World Space TMP) at {PrefabPath}");
        }

        private static TextMeshPro CreateWorldSpaceLabel(Transform doorRoot)
        {
            GameObject textObject = new GameObject("GateLabelTMP");
            textObject.transform.SetParent(doorRoot, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            textObject.transform.localRotation = Quaternion.identity;
            textObject.transform.localScale = Vector3.one;

            TextMeshPro tmp = textObject.AddComponent<TextMeshPro>();
            tmp.font = LoadDefaultTmpFont();
            tmp.text = "+1 DMG";
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.08f;
            tmp.fontSizeMax = 3.36f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.rectTransform.sizeDelta = new Vector2(1.48f, 2.12f);

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
