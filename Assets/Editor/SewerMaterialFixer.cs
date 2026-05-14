using UnityEngine;
using UnityEditor;
using System.IO;

public class SewerMaterialFixer : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/BRUTE FORCE: Fix Scene Materials")]
    public static void BruteForceFix()
    {
        FixMaterialsInternal();

        MeshRenderer[] renderers = GameObject.FindObjectsOfType<MeshRenderer>();
        int fixedCount = 0;

        foreach (var renderer in renderers)
        {
            string name = renderer.gameObject.name.ToLower();
            string folderName = GetFolderNameForObject(name);
            
            if (string.IsNullOrEmpty(folderName)) continue;

            // Search for ANY material in that folder
            string folderPath = $"Assets/Models/SewerProps/{folderName}";
            if (!Directory.Exists(folderPath)) continue;

            string[] mats = Directory.GetFiles(folderPath, "*.mat");
            if (mats.Length > 0)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(mats[0]);
                if (mat != null)
                {
                    renderer.sharedMaterial = mat;
                    fixedCount++;
                }
            }
        }
        Debug.Log($"[SewerMaterialFixer] Brute Force Complete! Fixed {fixedCount} objects in the scene.");
    }

    static string GetFolderNameForObject(string name)
    {
        if (name.Contains("garbage") || name.Contains("mountain")) return "Garbage_Mountain";
        if (name.Contains("mound") || name.Contains("decay")) return "Decay_Mound_Shredded";
        if (name.Contains("trash") || name.Contains("pile")) return "Pile_of_Trash";
        if (name.Contains("rockpile") || name.Contains("rock")) return "Rusted_Rockpile_0514205424";
        if (name.Contains("backpack") || name.Contains("military")) return "Weathered_Military_Ba";
        if (name.Contains("engine") || name.Contains("rusted_engine")) return "Rusted_Engine_0514205614";
        if (name.Contains("pallet")) return "Weathered_Wooden_Pallet";
        return "";
    }

    static void FixMaterialsInternal()
    {
        string rootPath = "Assets/Models/SewerProps";
        if (!Directory.Exists(rootPath)) return;

        string[] folders = Directory.GetDirectories(rootPath);
        foreach (string folder in folders)
        {
            string folderName = Path.GetFileName(folder);
            string matPath = $"{folder}/{folderName}_Mat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, matPath);
            }

            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file).ToLower();
                if (fileName.Contains(".meta")) continue;

                if (fileName.Contains("_texture.png") || fileName.EndsWith(".png") && !fileName.Contains("_"))
                    mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(file);
                else if (fileName.Contains("normal"))
                {
                    TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
                    if (importer != null && importer.textureType != TextureImporterType.NormalMap) {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.SaveAndReimport();
                    }
                    mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(file));
                    mat.EnableKeyword("_NORMALMAP");
                }
                mat.SetFloat("_Glossiness", 0.65f);
                mat.SetFloat("_Metallic", 0.1f);
            }
        }
        AssetDatabase.SaveAssets();
    }
}
