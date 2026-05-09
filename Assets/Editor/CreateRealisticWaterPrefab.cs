using UnityEngine;
using UnityEditor;

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
        
        AssetDatabase.CreateAsset(dirtyWaterMat, $"Assets/Materials/RealisticDirtyWaterMat_{System.DateTime.Now.Ticks}.mat");

        // Apply material to plane
        waterPlane.GetComponent<Renderer>().sharedMaterial = dirtyWaterMat;

        // Parent it to a useful scale
        waterPlane.transform.localScale = new Vector3(10, 1, 10);
        
        Debug.Log("Realistic Dirty Water created! Check out the Material settings to adjust Wave Speed, Scale, and Murkiness.");
    }
}
