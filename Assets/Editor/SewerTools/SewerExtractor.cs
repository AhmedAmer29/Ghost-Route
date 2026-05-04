using UnityEngine;
using UnityEditor;
using System.IO;

public class SewerExtractor : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/EXTRACT 3D MODELS FROM FBX")]
    public static void Extract()
    {
        string fbxPath = "Assets/Models/SewerKit/Sewer/Models/Sewers.fbx";
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        string folderPath = "Assets/Models/SewerKit/ExtractedPipes";
        if (!Directory.Exists(folderPath)) {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        int count = 0;
        foreach (Object asset in allAssets)
        {
            if (asset is Mesh)
            {
                Mesh mesh = (Mesh)asset;
                // Ignore small junk meshes
                if (mesh.vertexCount < 50) continue;

                GameObject go = new GameObject(mesh.name);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                // Apply the brick texture
                Material mat = new Material(Shader.Find("Standard"));
                mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/SewerKit/Sewer/Textures/bricks.jpg");
                renderer.sharedMaterial = mat;

                string savePath = $"{folderPath}/{mesh.name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, savePath);
                DestroyImmediate(go);
                count++;
            }
        }
        
        Debug.Log($"Extracted {count} 3D models into {folderPath}!");
    }
}
