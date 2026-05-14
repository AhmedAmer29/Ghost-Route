using UnityEngine;
using UnityEditor;
using System.IO;

public class SewerMaterialSetup : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/CREATE WET SEWER MATERIALS")]
    public static void CreateMaterials()
    {
        string matPath = "Assets/Materials/Sewer";
        if (!AssetDatabase.IsValidFolder(matPath))
        {
            Directory.CreateDirectory(matPath);
            AssetDatabase.Refresh();
        }

        CreateWetMaterial(
            "Sewer_Wet_Wall", 
            "Assets/Models/SewerKit/Sewer/Textures/concrete_dirty.jpg", 
            new Vector2(4, 2), 
            0.75f
        );

        CreateWetMaterial(
            "Sewer_Wet_Floor", 
            "Assets/Models/SewerKit/Sewer/Textures/soil_mud.jpg", 
            new Vector2(3, 3), 
            0.85f
        );

        CreateWetMaterial(
            "Sewer_Wet_Bricks", 
            "Assets/Models/SewerKit/Sewer/Textures/bricks.jpg", 
            new Vector2(4, 2), 
            0.7f
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[SewerMaterialSetup] Created wet materials in " + matPath);
    }

    static void CreateWetMaterial(string name, string texturePath, Vector2 tiling, float smoothness)
    {
        string path = $"Assets/Materials/Sewer/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            mat.mainTexture = tex;
            mat.SetTextureScale("_MainTex", tiling);
        }
        else
        {
            Debug.LogWarning("[SewerMaterialSetup] Texture not found at: " + texturePath);
        }

        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", 0.1f);
        
        // Optional: If you find normal maps later, you can add them here
        EditorUtility.SetDirty(mat);
    }
}
