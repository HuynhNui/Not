using _Project.Scripts.Systems.MissionSystem;
using UnityEditor;
using UnityEngine;

public static class MissionCatalogBuilder
{
    private const string MissionDataFolder = "Assets/_Project/Data/Missions";
    private const string MissionCatalogPath = MissionDataFolder + "/MissionCatalog_v1.asset";

    [MenuItem("Tools/Missions/Rebuild Mission Catalog V1")]
    public static MissionCatalog RebuildMissionCatalog()
    {
        EnsureFolder(MissionDataFolder);

        MissionCatalog catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(MissionCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<MissionCatalog>();
            AssetDatabase.CreateAsset(catalog, MissionCatalogPath);
        }

        catalog.ResetToDefaultMissionChain();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Rebuilt Mission catalog at {MissionCatalogPath}");
        return catalog;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] segments = folderPath.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[index]);
            }

            current = next;
        }
    }
}
