using UnityEngine;
using UnityEditor;

public class BankMaterialExtractor : EditorWindow
{
    [MenuItem("Tools/Setup Bank Materials")]
    static void Setup()
    {
        const string fbxPath   = "Assets/Props/Bank Full/BankNew.fbx";
        const string matFolder = "Assets/Props/Bank Full/Materials";

        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) { Debug.LogError("Cannot find BankNew.fbx"); return; }

        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.materialName     = ModelImporterMaterialName.BasedOnMaterialName;
        importer.materialSearch   = ModelImporterMaterialSearch.Everywhere;

        string[] names = {
            "Material.002", "Material.003", "Material.004", "Material.005",
            "Steel", "steel light", "dollar", "paper stack"
        };

        foreach (string n in names)
        {
            string   path = $"{matFolder}/{n}.mat";
            Material mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { Debug.LogWarning($"Missing: {path}"); continue; }

            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat);
            Debug.Log($"Mapped: {n}");
        }

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        Debug.Log("Bank material setup complete.");
    }
}
