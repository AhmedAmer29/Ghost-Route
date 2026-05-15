using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class CreateRealisticWaterPrefab : EditorWindow
{
    [MenuItem("Sewer Tools/Create Realistic Dirty Water")]
    public static void CreateWater()
    {
        // Try to load the custom shader we just made
        Shader waterShader = Shader.Find("Custom/RealisticDirtyWater");
        
        if (waterShader == null)
        {
            Debug.LogError("Could not find Custom/RealisticDirtyWater shader. Did Unity finish compiling?");
            return;
        }

        // Create the Plane
        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "RealisticSewerWater";
        
        // Remove the collider so players don't walk ON the water
        DestroyImmediate(waterPlane.GetComponent<MeshCollider>());

        // Create the Material with our Realistic Shader
        Material dirtyWaterMat = new Material(waterShader);

        // Save the material to assets
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        // Use a consistent name so it doesn't create thousands of materials
        string matPath = "Assets/Materials/RealisticDirtyWaterMat_Saved.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
        {
            AssetDatabase.CreateAsset(dirtyWaterMat, matPath);
        }
        else
        {
            dirtyWaterMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }

        // Apply material to plane
        waterPlane.GetComponent<Renderer>().sharedMaterial = dirtyWaterMat;

        // Parent it to a useful scale
        waterPlane.transform.localScale = new Vector3(10, 1, 10);
        
        // Save it as an actual Prefab so it never disappears
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        string prefabPath = "Assets/Prefabs/RealisticSewerWater.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(waterPlane, prefabPath, InteractionMode.UserAction);
        
        // Mark the scene as dirty so Unity knows you added an object and prompts you to save
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        
        Debug.Log("Realistic Dirty Water created AND safely saved as a Prefab in Assets/Prefabs!");
    }
}
